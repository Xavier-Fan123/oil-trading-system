
from __future__ import annotations

import argparse
import hashlib
import json
import logging
import os
import re
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any
from xml.etree import ElementTree as ET

from playwright.sync_api import Page, sync_playwright

from oa_scraper.auth import load_or_create_context, login_if_needed, save_state
from oa_scraper.scrape_detail import extract_detail
from oa_scraper.scrape_list import extract_todo_rows, goto_todo_list
from oa_scraper.utils import (
    clean_text,
    clip_text,
    configure_logging,
    ensure_dir,
    iter_frames,
    looks_like_login_page,
    now_utc_iso,
    resolve_url,
    safe_count,
    timestamp_for_filename,
    wait_with_jitter,
)

LOGGER = logging.getLogger("oa_scraper")


def parse_bool(value: str) -> bool:
    value = value.strip().lower()
    if value in {"1", "true", "t", "yes", "y"}:
        return True
    if value in {"0", "false", "f", "no", "n"}:
        return False
    raise argparse.ArgumentTypeError("Expected true/false")


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="OA D240 -> X incremental contract sync")
    p.add_argument("--base-url", default="http://oa.itg.cn/")
    p.add_argument("--x-api-base", default="http://localhost:5000/api")
    p.add_argument("--outdir", default="output/oa_x_sync")
    p.add_argument("--state-file", default="output/oa_x_sync_state.json")
    p.add_argument("--headless", type=parse_bool, default=True)
    p.add_argument("--timeout", type=int, default=45000)
    p.add_argument("--limit", type=int, default=0)
    p.add_argument("--max-pages", type=int, default=200)
    p.add_argument("--max-attachments", type=int, default=6)
    p.add_argument("--flow-keyword", default="D240 \u56fd\u8d38\u80a1\u4efd\u901a\u7528\u5408\u540c\u6d41\u7a0b")
    p.add_argument("--creators", default="\u6797\u79b9\u777f,\u4faf\u4f1f\u5cf0")
    p.add_argument("--contract-keywords", default="\u81ea\u8425\u8f6c\u53e3\u91c7\u8d2d,\u81ea\u8425\u8f6c\u53e3\u9500\u552e,\u81ea\u8425\u5185\u8d38\u91c7\u8d2d,\u81ea\u8425\u5185\u8d38\u9500\u552e,\u5408\u540c\u7f16\u53f7,ITGR-,\u8d2d\u9500\u5408\u540c")
    p.add_argument("--default-side", choices=["sales", "purchase"], default="sales")
    p.add_argument("--update-existing", type=parse_bool, default=True)
    p.add_argument("--dry-run", type=parse_bool, default=False)
    p.add_argument("--interval-minutes", type=int, default=0)
    return p.parse_args()


def jread(path: Path, default: Any) -> Any:
    if not path.exists():
        return default
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return default


def jwrite(path: Path, payload: Any) -> None:
    ensure_dir(path.parent)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def norm(value: str | None) -> str:
    return clean_text(value or "")


def compact(value: str | None) -> str:
    return re.sub(r"[\s:\uff1a,\uff0c;\uff1b/\\(){}<>]+", "", norm(value)).lower()


def split_creators(value: str) -> list[str]:
    return [clean_text(x) for x in re.split(r"[,\uff0c;\uff1b\s]+", value or "") if clean_text(x)]


def unwrap_list(payload: Any) -> list[dict[str, Any]]:
    if isinstance(payload, list):
        return [x for x in payload if isinstance(x, dict)]
    if isinstance(payload, dict):
        for key in ("items", "data", "value"):
            if isinstance(payload.get(key), list):
                return [x for x in payload[key] if isinstance(x, dict)]
    return []


def api_request(base_url: str, method: str, path: str, payload: dict[str, Any] | None = None, timeout: int = 20) -> tuple[int, Any]:
    url = f"{base_url.rstrip('/')}/{path.lstrip('/')}"
    headers = {"Accept": "application/json"}
    data = None
    if payload is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(url=url, data=data, headers=headers, method=method.upper())
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            try:
                return resp.status, json.loads(body)
            except Exception:
                return resp.status, body
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")
        try:
            return exc.code, json.loads(body)
        except Exception:
            return exc.code, body
    except Exception as exc:  # noqa: BLE001
        return 0, {"error": str(exc)}


def x_health(base_url: str) -> bool:
    # Primary readiness check.
    status, _ = api_request(base_url, "GET", "health")
    if status == 200:
        return True

    # Fallback: some deployments return non-200 health due optional dependencies,
    # while contract endpoints are still available for sync.
    status, _ = api_request(base_url, "GET", "purchase-contracts?page=1&pageSize=1")
    return status == 200


def flow_match(flow_keyword: str, text_blob: str) -> bool:
    blob = compact(text_blob)
    key = compact(flow_keyword)
    if key and key in blob:
        return True
    return "d240" in blob


def creator_match(creators: list[str], text_blob: str) -> bool:
    if not creators:
        return True
    s = norm(text_blob)
    return any(name in s for name in creators)


def contract_keyword_match(contract_keywords: str, text_blob: str) -> bool:
    keywords = split_creators(contract_keywords)
    if not keywords:
        return True
    s = norm(text_blob)
    lower = s.lower()
    for kw in keywords:
        key = clean_text(kw)
        if key and key.lower() in lower:
            return True
    return False


def target_contract_match(flow_keyword: str, contract_keywords: str, text_blob: str) -> bool:
    return flow_match(flow_keyword, text_blob) or contract_keyword_match(contract_keywords, text_blob)

def select_trader_id(users: list[dict[str, Any]]) -> str:
    for u in users:
        role = norm(str(u.get("role") or u.get("roleName") or "")).lower()
        if role == "trader":
            return str(u.get("id") or "")
    return str(users[0].get("id") or "") if users else ""


def apply_ascii_replacements(value: str) -> str:
    out = clean_text(value)
    replacements = [
        ("\u80a1\u4efd\u6709\u9650\u516c\u53f8", " Co Ltd "),
        ("\u6709\u9650\u516c\u53f8", " Co Ltd "),
        ("\u516c\u53f8", " Co "),
        ("\u96c6\u56e2", " Group "),
        ("\u56fd\u9645", " International "),
        ("\u8d38\u6613", " Trading "),
        ("\u77f3\u6cb9", " Petroleum "),
        ("\u5316\u5de5", " Chemical "),
        ("\u80fd\u6e90", " Energy "),
        ("\u4f9b\u5e94\u5546", " Supplier "),
        ("\u5ba2\u6237", " Customer "),
        ("\u4ea4\u6613\u5bf9\u624b", " Counterparty "),
        ("\u88c5\u8d27\u6e2f", " LoadPort "),
        ("\u5378\u8d27\u6e2f", " DischargePort "),
        ("\u4e2d\u56fd", " China "),
    ]
    for src, dst in replacements:
        out = out.replace(src, dst)
    return out


def force_ascii_text(value: str) -> str:
    s = apply_ascii_replacements(value)
    if not s:
        return ""
    s = re.sub(r"[^A-Za-z0-9 .,/()_&:=;\-]+", " ", s)
    s = re.sub(r"\s+", " ", s).strip()
    return s


def force_ascii_name(value: str, fallback_prefix: str) -> str:
    s = force_ascii_text(value)
    generic = {
        "CO", "CO LTD", "COMPANY", "GROUP", "TRADING", "PETROLEUM", "CHEMICAL",
        "ENERGY", "SUPPLIER", "CUSTOMER", "COUNTERPARTY", "PORT", "LOADPORT", "DISCHARGEPORT",
    }
    token = re.sub(r"[^A-Za-z0-9]+", " ", s).strip().upper()
    compact_token = re.sub(r"[^A-Za-z0-9]+", "", s).upper()
    if s and token and token not in generic and len(compact_token) >= 2:
        return s
    raw = clean_text(value)
    if not raw:
        return ""
    suffix = hashlib.md5(raw.encode("utf-8", errors="ignore")).hexdigest()[:8].upper()
    return f"{fallback_prefix}-{suffix}"


def detect_product_name(text: str) -> str:
    s = clean_text(text)
    if not s:
        return ""
    upper = s.upper()
    rules: list[tuple[tuple[str, ...], str]] = [
        (("VLSFO", "LSFO", "\u4f4e\u786b\u71c3\u6599\u6cb9", "\u71c3\u6599\u6cb9"), "Low Sulfur Fuel Oil"),
        (("GASOIL", "DIESEL", "\u67f4\u6cb9"), "Gas Oil (Diesel)"),
        (("GASOLINE", "MOGAS", "\u6c7d\u6cb9"), "Gasoline"),
        (("PTA",), "PTA"),
        (("MEG", "\u4e59\u4e8c\u9187"), "MEG"),
        (("STYRENE", "\u82ef\u4e59\u70ef"), "STYRENE"),
        (("BRENT", "\u5e03\u4f26\u7279"), "Brent Crude Oil"),
        (("WTI", "WEST TEXAS INTERMEDIATE"), "West Texas Intermediate"),
    ]
    for keys, canonical in rules:
        for key in keys:
            if key.isascii() and key.upper() in upper:
                return canonical
            if (not key.isascii()) and key in s:
                return canonical
    return ""


def normalize_product_name(value: str) -> str:
    detected = detect_product_name(value)
    if detected:
        return detected

    ascii_name = force_ascii_name(value, "Product")
    if not ascii_name:
        return "West Texas Intermediate"

    alias = {
        "GASOIL": "Gas Oil (Diesel)",
        "DIESEL": "Gas Oil (Diesel)",
        "GASOLINE": "Gasoline",
        "MOGAS": "Gasoline",
        "LSFO": "Low Sulfur Fuel Oil",
        "VLSFO": "Low Sulfur Fuel Oil",
        "WTI": "West Texas Intermediate",
        "BRENT": "Brent Crude Oil",
    }
    return alias.get(ascii_name.upper(), ascii_name)


def normalize_counterparty_name(value: str) -> str:
    return clean_text(value)


def normalize_port_name(value: str) -> str:
    s = clean_text(value)
    if not s:
        return "Unknown"

    upper = s.upper()
    rules: list[tuple[tuple[str, ...], str]] = [
        (("QINGDAO", "\u9752\u5c9b"), "Qingdao"),
        (("NINGBO", "\u5b81\u6ce2"), "Ningbo"),
        (("SINGAPORE", "\u65b0\u52a0\u5761"), "Singapore"),
        (("DALIAN", "\u5927\u8fde"), "Dalian"),
        (("SHANGHAI", "\u4e0a\u6d77"), "Shanghai"),
        (("ZHANJIANG", "\u6e5b\u6c5f"), "Zhanjiang"),
        (("ZHOU SHAN", "ZHOUSHAN", "\u821f\u5c71"), "Zhoushan"),
        (("TIANJIN", "\u5929\u6d25"), "Tianjin"),
        (("QINZHOU", "\u94a6\u5dde"), "Qinzhou"),
        (("YINGKOU", "\u8425\u53e3"), "Yingkou"),
    ]
    for keys, canonical in rules:
        for key in keys:
            if key.isascii() and key in upper:
                return canonical
            if (not key.isascii()) and key in s:
                return canonical

    return force_ascii_name(s, "Port") or "Unknown"


def find_partner(partners: list[dict[str, Any]], company_name: str) -> dict[str, Any] | None:
    target_raw = compact(company_name)
    target_ascii = compact(normalize_counterparty_name(company_name))
    for p in partners:
        name_raw = clean_text(str(p.get("companyName") or ""))
        cand_raw = compact(name_raw)
        cand_ascii = compact(normalize_counterparty_name(name_raw))
        if target_raw and target_raw in {cand_raw, cand_ascii}:
            return p
        if target_ascii and target_ascii in {cand_raw, cand_ascii}:
            return p
    return None


def find_product(products: list[dict[str, Any]], product_name: str) -> dict[str, Any] | None:
    canonical = normalize_product_name(product_name)
    target = compact(canonical)
    target_code = compact(infer_product_code(canonical))
    for p in products:
        code = compact(str(p.get("code") or p.get("productCode") or ""))
        name = compact(str(p.get("name") or p.get("productName") or ""))
        if target and (target in name or target == code or target_code == code):
            return p
    return None


def infer_product_name(text: str) -> str:
    detected = detect_product_name(text)
    return detected or "West Texas Intermediate"


def infer_product_code(name: str) -> str:
    canonical = normalize_product_name(name)
    key = clean_text(canonical).upper()
    m = {
        "GASOLINE": "GASOLINE",
        "GAS OIL (DIESEL)": "GASOIL",
        "LOW SULFUR FUEL OIL": "LSFO",
        "WEST TEXAS INTERMEDIATE": "WTI",
        "BRENT CRUDE OIL": "BRENT",
        "PTA": "PTA",
        "MEG": "MEG",
        "STYRENE": "STYRENE",
    }
    if key in m:
        return m[key]
    code = re.sub(r"[^A-Za-z0-9]+", "_", key).strip("_")
    if code:
        return code[:24]
    suffix = hashlib.md5(canonical.encode("utf-8", errors="ignore")).hexdigest()[:8].upper()
    return f"AUTO_{suffix}"


def ensure_partner(base_url: str, partners_cache: list[dict[str, Any]], company_name: str) -> str:
    company_name = normalize_counterparty_name(company_name)
    if not company_name:
        return ""
    p = find_partner(partners_cache, company_name)
    if p:
        return str(p.get("id") or "")
    payload = {
        "companyName": company_name,
        "partnerType": "Both",
        "creditLimit": 10000000,
        "creditLimitValidUntil": (datetime.now() + timedelta(days=365)).strftime("%Y-%m-%dT%H:%M:%S"),
        "paymentTermDays": 30,
    }
    status, data = api_request(base_url, "POST", "trading-partners", payload)
    if status not in (200, 201) or not isinstance(data, dict):
        LOGGER.error("Create partner failed: %s %s", status, data)
        return ""
    partners_cache.append(data)
    return str(data.get("id") or "")


def ensure_product(base_url: str, products_cache: list[dict[str, Any]], product_name: str, unit: str) -> str:
    product_name = normalize_product_name(product_name)
    unit = normalize_quantity_unit(unit)
    p = find_product(products_cache, product_name)
    if p:
        return str(p.get("id") or "")
    payload = {
        "code": infer_product_code(product_name),
        "name": product_name,
        "type": "RefinedProducts",
        "grade": "Standard",
        "specification": "Auto-created from OA D240 sync",
        "unitOfMeasure": unit,
        "density": 0.85,
        "origin": "Unknown",
    }
    status, data = api_request(base_url, "POST", "products", payload)
    if status not in (200, 201) or not isinstance(data, dict):
        LOGGER.error("Create product failed: %s %s", status, data)
        return ""
    products_cache.append(data)
    return str(data.get("id") or "")



def parse_url_from_onclick(onclick: str) -> str:
    m = re.search(r"(https?://[^'\"\s)]+|/[A-Za-z0-9_./?=&%-]+(?:\.[A-Za-z0-9]{2,5})?)", onclick or "", flags=re.I)
    return clean_text(m.group(1)) if m else ""


def click_attachment_tab(page: Page) -> None:
    selectors = [
        "a:has-text('\u9644\u4ef6')",
        "span:has-text('\u9644\u4ef6')",
        "div:has-text('\u9644\u4ef6')",
        "a:has-text('\u76f8\u5173\u9644\u4ef6')",
        "a:has-text('\u9644\u4ef6\u4fe1\u606f')",
        "a:has-text('Attachment')",
        "span:has-text('Attachment')",
        "div:has-text('Attachment')",
    ]
    for frame in iter_frames(page):
        for sel in selectors:
            if safe_count(frame, sel) <= 0:
                continue
            try:
                frame.locator(sel).first.click(timeout=1200)
                page.wait_for_timeout(800)
                return
            except Exception:
                continue



def collect_attachment_candidates(page: Page, base_url: str) -> list[dict[str, str]]:
    click_attachment_tab(page)
    candidates: list[dict[str, str]] = []
    seen: set[str] = set()
    for frame in iter_frames(page):
        try:
            rows = frame.evaluate(
                """
                () => {
                    const normalize = (v) => (v || '').replace(/\s+/g,' ').trim();
                    const nodes = Array.from(document.querySelectorAll('a,button,[onclick],[data-href],[data-link],span,div')).slice(0,1500);
                    const fileRe = /\.(pdf|docx?|xlsx?|xlsm|xls|csv|txt|zip|rar)(\?|$)/i;
                    const kws = ['\\u9644\\u4ef6','\\u76f8\\u5173\\u9644\\u4ef6','\\u9644\\u4ef6\\u4fe1\\u606f','download','attach','file'];
                    const out = [];
                    for (const n of nodes) {
                        const text = normalize(n.textContent || n.innerText || '');
                        const title = normalize(n.getAttribute('title') || '');
                        const href = normalize(n.getAttribute('href') || n.getAttribute('data-href') || n.getAttribute('data-link') || '');
                        const onclick = normalize(n.getAttribute('onclick') || '');
                        const merged = (text + ' ' + title + ' ' + href + ' ' + onclick).toLowerCase();
                        const hit = kws.some(k => merged.includes(k.toLowerCase())) || fileRe.test(href) || fileRe.test(onclick) || fileRe.test(text) || fileRe.test(title);
                        if (!hit) continue;
                        out.push({text, title, href, onclick});
                    }
                    return out;
                }
                """
            )
        except Exception:
            rows = []
        for r in rows if isinstance(rows, list) else []:
            name = clean_text(str(r.get("text") or r.get("title") or ""))
            href = clean_text(str(r.get("href") or ""))
            onclick = clean_text(str(r.get("onclick") or ""))
            if not href:
                href = parse_url_from_onclick(onclick)
            if href:
                href = resolve_url(base_url, href)
            key = f"{name}|{href}|{onclick}"
            if key in seen:
                continue
            seen.add(key)
            candidates.append({"name": name or "attachment", "href": href, "onclick": onclick, "frame_url": frame.url})
    return candidates

def parse_filename_from_headers(headers: dict[str, str]) -> str:
    cd = headers.get("content-disposition", "")
    if not cd:
        return ""
    m1 = re.search(r"filename\*=UTF-8''([^;]+)", cd, flags=re.I)
    if m1:
        return urllib.parse.unquote(m1.group(1))
    m2 = re.search(r'filename="?([^";]+)"?', cd, flags=re.I)
    return m2.group(1).strip() if m2 else ""


def guess_filename(item: dict[str, str], headers: dict[str, str], idx: int) -> str:
    fn = parse_filename_from_headers(headers)
    if fn:
        return fn
    name = clean_text(item.get("name") or "")
    if name and re.search(r"\.[A-Za-z0-9]{2,5}$", name):
        return name
    path_name = Path(urllib.parse.urlparse(item.get("href") or "").path).name
    if path_name:
        return path_name
    return f"attachment_{idx}.bin"


def download_attachments(page: Page, candidates: list[dict[str, str]], outdir: Path, max_attachments: int, timeout_ms: int) -> list[dict[str, str]]:
    ensure_dir(outdir)
    out: list[dict[str, str]] = []
    for idx, item in enumerate(candidates[: max(max_attachments, 0)], start=1):
        record = dict(item)
        href = clean_text(item.get("href") or "")
        if not href:
            record["download_error"] = "No direct href"
            out.append(record)
            continue
        try:
            resp = page.context.request.get(href, timeout=timeout_ms)
            if not resp.ok:
                record["download_error"] = f"HTTP {resp.status}"
                out.append(record)
                continue
            body = resp.body()
            headers = {k.lower(): v for k, v in resp.headers.items()}
            ctype = headers.get("content-type", "").lower()
            if not body:
                record["download_error"] = "Empty body"
                out.append(record)
                continue
            if "text/html" in ctype and b"<html" in body[:200].lower():
                record["download_error"] = "Response is HTML"
                out.append(record)
                continue
            safe = re.sub(r"[^\w.\-]+", "_", guess_filename(item, headers, idx)).strip("_") or f"attachment_{idx}.bin"
            path = outdir / safe
            path.write_bytes(body)
            record["downloaded_path"] = str(path)
        except Exception as exc:
            record["download_error"] = str(exc)
        out.append(record)
    return out


def decode_bytes(data: bytes) -> str:
    for enc in ("utf-8", "utf-8-sig", "gb18030", "gbk", "utf-16", "latin-1"):
        try:
            return data.decode(enc)
        except Exception:
            continue
    return data.decode("utf-8", errors="replace")


def read_docx_text(path: Path) -> str:
    try:
        with zipfile.ZipFile(path) as zf:
            if "word/document.xml" not in zf.namelist():
                return ""
            xml = decode_bytes(zf.read("word/document.xml"))
            root = ET.fromstring(xml)
            return clean_text(" ".join((n.text or "") for n in root.findall(".//{*}t")))
    except Exception:
        return ""


def read_xlsx_text(path: Path) -> str:
    try:
        with zipfile.ZipFile(path) as zf:
            names = set(zf.namelist())
            ss: list[str] = []
            if "xl/sharedStrings.xml" in names:
                ss_root = ET.fromstring(decode_bytes(zf.read("xl/sharedStrings.xml")))
                ss = [(n.text or "") for n in ss_root.findall(".//{*}t")]
            cells: list[str] = []
            for name in sorted(names):
                if not name.startswith("xl/worksheets/") or not name.endswith(".xml"):
                    continue
                root = ET.fromstring(decode_bytes(zf.read(name)))
                for c in root.findall(".//{*}c"):
                    t = c.attrib.get("t", "")
                    v = c.find("{*}v")
                    if v is None or v.text is None:
                        continue
                    raw = v.text.strip()
                    if t == "s" and raw.isdigit():
                        i = int(raw)
                        if 0 <= i < len(ss):
                            cells.append(ss[i])
                    else:
                        cells.append(raw)
            return clean_text(" ".join(cells))
    except Exception:
        return ""


def read_pdf_text(path: Path) -> str:
    try:
        from pypdf import PdfReader  # type: ignore

        reader = PdfReader(str(path))
        return clean_text(" ".join((p.extract_text() or "") for p in reader.pages[:20]))
    except Exception:
        return ""


def read_attachment_text(path: Path) -> str:
    ext = path.suffix.lower()
    if ext in {".txt", ".csv", ".json", ".xml"}:
        try:
            return clean_text(path.read_text(encoding="utf-8"))
        except Exception:
            try:
                return clean_text(path.read_text(encoding="gb18030"))
            except Exception:
                return ""
    if ext == ".docx":
        return read_docx_text(path)
    if ext in {".xlsx", ".xlsm"}:
        return read_xlsx_text(path)
    if ext == ".pdf":
        return read_pdf_text(path)
    return ""


def extract_external_number(text: str, request_id: str) -> str:
    patterns = [
        r"(?:\b(?:external(?:\s*contract)?\s*no\.?|contract\s*no\.?|ext(?:ernal)?\s*no\.?)\b|\u5916\u90e8\u5408\u540c\u53f7|\u5408\u540c\u53f7)\s*[:\uff1a]?\s*([A-Za-z0-9][A-Za-z0-9_\-/.]{4,})",
        r"\b(ITGR-[A-Z0-9]+-[A-Z]-\d{4})\b",
        r"\b([A-Z]{2,8}-\d{4}-[A-Za-z0-9_\-/.]{3,})\b",
        r"\b([A-Z]{1,6}\d{4}[A-Za-z0-9_\-/.]{2,})\b",
    ]
    for p in patterns:
        m = re.search(p, text, flags=re.I)
        if m:
            return clean_text(m.group(1))
    return f"OA-D240-{request_id}" if request_id else ""


def extract_counterparty(text: str, title: str) -> str:
    patterns = (
        r"(?:counterparty|supplier|customer|buyer|seller)\s*[:\uff1a]?\s*([^\n\r,;|]{2,120}?)(?=\s*(?:\b(?:quantity|qty|price|product|item|load(?:ing)?\s*port|discharge\s*port|port\s*of\s*loading|port\s*of\s*discharge|contract|external)\b|$))",
        r"(?:\u4ea4\u6613\u5bf9\u624b|\u4f9b\u5e94\u5546|\u5ba2\u6237|\u4e70\u65b9|\u5356\u65b9)\s*[:\uff1a]?\s*([^\n\r,;|]{2,120}?)(?=\s*(?:\u4ea7\u54c1|\u54c1\u540d|\u89c4\u683c|\u6570\u91cf|\u4ef7\u683c|\u88c5\u8d27\u6e2f|\u5378\u8d27\u6e2f|\u5408\u540c\u53f7|\u5916\u90e8\u5408\u540c\u53f7|$))",
    )
    for pattern in patterns:
        m = re.search(pattern, text, flags=re.I)
        if m:
            return clean_text(m.group(1))

    for part in re.split(r"[|,;/\-\uff0c\uff1b]", title or ""):
        s = clean_text(part)
        if not s:
            continue
        upper = s.upper()
        if any(token in upper for token in ("COMPANY", "LTD", "LLC", "INC", "CO.", "CORP")):
            return s
        if any(token in s for token in ("\u516c\u53f8", "\u96c6\u56e2", "\u8d38\u6613", "\u80a1\u4efd")):
            return s

    return ""

def extract_valid_dates(text: str) -> list[datetime]:
    candidates: list[tuple[int, datetime]] = []
    patterns: list[tuple[str, tuple[str, ...]]] = [
        (
            r"(?<![A-Za-z0-9])((?:20\d{2})[-/.](?:0?[1-9]|1[0-2])[-/.](?:0?[1-9]|[12]\d|3[01]))(?![A-Za-z0-9])",
            ("%Y-%m-%d", "%Y/%m/%d", "%Y.%m.%d"),
        ),
        (
            r"(?<![A-Za-z0-9])((?:20\d{2})\u5e74(?:0?[1-9]|1[0-2])\u6708(?:0?[1-9]|[12]\d|3[01])\u65e5)(?![A-Za-z0-9])",
            ("%Y\u5e74%m\u6708%d\u65e5",),
        ),
    ]
    for pattern, fmts in patterns:
        for m in re.finditer(pattern, text):
            raw = clean_text(m.group(1))
            parsed: datetime | None = None
            for fmt in fmts:
                try:
                    parsed = datetime.strptime(raw, fmt)
                    break
                except Exception:
                    continue
            if not parsed:
                continue
            if parsed.year < 2000 or parsed.year > 2100:
                continue
            candidates.append((m.start(1), parsed))
    candidates.sort(key=lambda x: x[0])
    result: list[datetime] = []
    seen: set[str] = set()
    for _, dt in candidates:
        k = dt.strftime("%Y-%m-%d")
        if k in seen:
            continue
        seen.add(k)
        result.append(dt)
    return result


def normalize_laycan_window(dates: list[datetime]) -> tuple[datetime, datetime]:
    floor = datetime.now().replace(hour=0, minute=0, second=0, microsecond=0) + timedelta(days=1)
    d1 = dates[0] if dates else floor
    d2 = dates[1] if len(dates) > 1 else (d1 + timedelta(days=1))
    if d1.year < 2000 or d1.year > 2100:
        d1 = floor
    if d2.year < 2000 or d2.year > 2100:
        d2 = d1 + timedelta(days=1)
    if d1 < floor:
        d1 = floor
    if d2 <= d1:
        d2 = d1 + timedelta(days=1)
    return d1, d2

def parse_first_decimal(text: str, default: float = 0.0) -> float:
    # Support formatted numbers like 154,500.000 and plain decimals.
    m = re.search(r"-?\d[\d,]*(?:\.\d+)?", clean_text(text))
    if not m:
        return default

    token = m.group(0).replace(",", "")
    try:
        return float(token)
    except Exception:
        return default


def normalize_quantity_unit(raw: str) -> str:
    token = clean_text(raw).upper()
    if not token:
        return "BBL"

    if token in {"MT", "TON", "TONNE", "METRIC TON", "METRIC TONS"}:
        return "MT"
    if token in {"BBL", "BARREL", "BARRELS"}:
        return "BBL"
    if token in {"GAL", "GALLON", "GALLONS", "L", "LITER", "LITRE", "LITERS", "LITRES"}:
        return "GAL"

    if any(t in token for t in ("\u5428", "\u516c\u5428")):
        return "MT"
    if "\u6876" in token:
        return "BBL"
    if any(t in token for t in ("\u52a0\u4ed1", "\u5347")):
        return "GAL"

    return "BBL"


def parse_quantity_and_unit(text: str) -> tuple[float, str]:
    s = clean_text(text)
    upper = s.upper()
    qty = parse_first_decimal(s, 0.0)

    has_mt = (
        "\u5428" in s
        or "\u516c\u5428" in s
        or " MT" in f" {upper} "
        or "METRIC TON" in upper
    )
    has_bbl = (
        "\u6876" in s
        or "BBL" in upper
        or "BARREL" in upper
    )
    has_gal = (
        "\u52a0\u4ed1" in s
        or "GAL" in upper
        or "LITER" in upper
        or "LITRE" in upper
    )

    if has_mt:
        unit = "MT"
    elif has_bbl:
        unit = "BBL"
    elif has_gal:
        unit = "GAL"
    else:
        unit = "BBL"

    return qty, normalize_quantity_unit(unit)




def normalize_ext_contract_number(value: str) -> str:
    ext = clean_text(value).replace(" ", "")
    ext = re.sub(r"[/?|]+", "/", ext)
    return ext


def infer_side_from_row(contract_type: str, external_number: str, default_side: str) -> str:
    ct = clean_text(contract_type)
    ct_upper = ct.upper()
    ext = clean_text(external_number).upper()

    buy_tokens = ("\u91c7\u8d2d", "\u8fdb\u53e3", "\u4e70\u5165")
    sell_tokens = ("\u9500\u552e", "\u51fa\u53e3", "\u5356\u51fa")

    if any(token in ct for token in buy_tokens) or "BUY" in ct_upper:
        return "purchase"
    if any(token in ct for token in sell_tokens) or "SELL" in ct_upper:
        return "sales"

    if re.search(r"-B\d{4}$", ext):
        return "purchase"
    if re.search(r"-S\d{4}$", ext):
        return "sales"

    return default_side



def infer_contract_type_from_ext(external_number: str) -> str:
    ext = clean_text(external_number).upper()
    if "-EXW-" in ext:
        return "EXW"
    return "CARGO"


def row_value(row: dict[str, Any], key: str) -> str:
    cell = row.get(key)
    if isinstance(cell, dict):
        return clean_text(str(cell.get("value") or ""))
    return clean_text(str(cell or ""))


def row_attachments(row: dict[str, Any], key: str) -> list[dict[str, str]]:
    cell = row.get(key)
    if not isinstance(cell, dict):
        return []
    special = dict(cell.get("specialobj") or {})
    files = special.get("filedatas")
    if not isinstance(files, list):
        return []
    out: list[dict[str, str]] = []
    for f in files:
        if not isinstance(f, dict):
            continue
        out.append({
            "name": clean_text(str(f.get("filename") or f.get("filerealname") or "attachment")),
            "href": clean_text(str(f.get("filelink") or "")),
            "ext": clean_text(str(f.get("fileExtendName") or "")),
            "imagefileid": clean_text(str(f.get("imagefileid") or "")),
        })
    return out


def extract_api_contract_rows(load_payload: dict[str, Any], detail_payload: dict[str, Any], default_side: str) -> list[dict[str, Any]]:
    detail1_rows = dict(dict(detail_payload.get("detail_1") or {}).get("rowDatas") or {})
    detail2_rows = dict(dict(detail_payload.get("detail_2") or {}).get("rowDatas") or {})

    meta_by_ext: dict[str, dict[str, str]] = {}
    for row in detail2_rows.values():
        if not isinstance(row, dict):
            continue
        ext = normalize_ext_contract_number(row_value(row, "field56231"))
        if not ext:
            continue
        meta_by_ext[ext] = {
            "sign_date": row_value(row, "field56234"),
            "counterparty": row_value(row, "field56237"),
            "contract_type": row_value(row, "field56235"),
        }

    out: list[dict[str, Any]] = []
    for row in detail1_rows.values():
        if not isinstance(row, dict):
            continue
        ext = normalize_ext_contract_number(row_value(row, "field50979"))
        if not ext or "ITGR-" not in ext.upper():
            continue

        contract_type = row_value(row, "field50978")
        product_name = normalize_product_name(row_value(row, "field50981"))
        qty_text = row_value(row, "field50984")
        price_text = row_value(row, "field50985")
        amt_text = row_value(row, "field50986")
        cp = normalize_counterparty_name(row_value(row, "field50989"))
        pay_terms = force_ascii_text(row_value(row, "field50982"))
        date_window = clean_text(row_value(row, "field50983"))

        qty, unit = parse_quantity_and_unit(qty_text)
        fixed_price = parse_first_decimal(price_text, 0.0)
        amount_mw = parse_first_decimal(amt_text, 0.0)

        meta = meta_by_ext.get(ext, {})
        if not cp:
            cp = normalize_counterparty_name(clean_text(meta.get("counterparty") or ""))
        if not contract_type:
            contract_type = clean_text(meta.get("contract_type") or "")

        date_blob = " ".join([date_window, clean_text(meta.get("sign_date") or "")])
        dates = extract_valid_dates(date_blob)
        d1, d2 = normalize_laycan_window(dates)

        side = infer_side_from_row(contract_type, ext, default_side)
        ctype = infer_contract_type_from_ext(ext)
        delivery_terms = "EXW" if ctype == "EXW" else "FOB"

        out.append({
            "external_contract_number": ext,
            "contract_side": side,
            "contract_type": ctype,
            "counterparty": cp,
            "product_name": product_name,
            "quantity": max(qty, 0.0001),
            "quantity_unit": normalize_quantity_unit(unit),
            "fixed_price": max(fixed_price, 0.0001),
            "amount_million": amount_mw,
            "delivery_terms": delivery_terms,
            "payment_terms": pay_terms or "NET 30",
            "laycan_start": d1,
            "laycan_end": d2,
            "load_port": "Unknown",
            "discharge_port": "Unknown",
        })

    out.sort(key=lambda x: clean_text(str(x.get("external_contract_number") or "")))
    return out


def collect_api_attachment_candidates(detail_payload: dict[str, Any], base_url: str) -> list[dict[str, str]]:
    rows = dict(dict(detail_payload.get("detail_3") or {}).get("rowDatas") or {})
    out: list[dict[str, str]] = []
    seen: set[str] = set()
    for row in rows.values():
        if not isinstance(row, dict):
            continue
        for key in list(row.keys()):
            for f in row_attachments(row, key):
                href = resolve_url(base_url, f.get("href") or "")
                if not href or href in seen:
                    continue
                seen.add(href)
                out.append({
                    "name": clean_text(f.get("name") or "attachment"),
                    "href": href,
                    "onclick": "",
                    "frame_url": "api_detail_3",
                })
    return out


def parse_json_bytes(raw: bytes) -> dict[str, Any]:
    if not raw:
        return {}
    try:
        return json.loads(decode_bytes(raw))
    except Exception:
        return {}


def pick_best_draft_from_api(item: dict[str, Any], api_rows: list[dict[str, Any]], default_side: str) -> dict[str, Any] | None:
    if not api_rows:
        return None
    ranked = sorted(
        api_rows,
        key=lambda r: (
            0 if clean_text(str(r.get("contract_side") or "")).lower() == "sales" else 1,
            0 if "-S" in clean_text(str(r.get("external_contract_number") or "")).upper() else 1,
            clean_text(str(r.get("external_contract_number") or "")),
        ),
    )
    best = dict(ranked[0])
    req = clean_text(str(item.get("list_request_id") or item.get("detail_request_id") or ""))
    title = force_ascii_text(str(item.get("list_title") or item.get("detail_title") or ""))
    src = force_ascii_text(str(item.get("detail_page_url") or item.get("detail_url") or ""))
    note_amt = best.get("amount_million")
    note_parts = [f"OA D240 sync(api_detail); request_id={req}; title={title}; source={src}"]
    if isinstance(note_amt, (int, float)) and note_amt > 0:
        note_parts.append(f"amount_million={note_amt}")
    best["request_id"] = req
    best["notes"] = clip_text("; ".join(note_parts), 1600)
    return best


def extract_contract_draft(item: dict[str, Any], attachment_texts: list[str], default_side: str) -> dict[str, Any]:
    req = clean_text(str(item.get("list_request_id") or item.get("detail_request_id") or ""))
    title = force_ascii_text(str(item.get("list_title") or item.get("detail_title") or ""))
    src = force_ascii_text(str(item.get("detail_page_url") or item.get("detail_url") or ""))
    text = clean_text(" ".join([
        title,
        clean_text(str(item.get("list_row_text") or "")),
        clean_text(str(item.get("detail_preview") or "")),
        " ".join(
            f"{clean_text(str(k))}:{clean_text(str(v))}"
            for k, v in dict(item.get("detail_field_map") or {}).items()
            if clean_text(str(v))
        ),
        " ".join(attachment_texts),
    ]))

    ext = extract_external_number(text, req)
    cp = normalize_counterparty_name(extract_counterparty(text, title))

    qty = 1.0
    unit = "BBL"
    qty_m = re.search(
        r"(?:\b(?:qty|quantity)\b|\u6570\u91cf)\s*[:\uff1a]?\s*([0-9][0-9,]*(?:\.\d+)?)\s*([A-Za-z\u4e00-\u9fff]{1,20})?",
        text,
        flags=re.I,
    )
    if qty_m:
        qty = parse_first_decimal(qty_m.group(1), 1.0)
        unit = normalize_quantity_unit(clean_text(qty_m.group(2) or ""))
    else:
        qty_m2 = re.search(
            r"([0-9][0-9,]*(?:\.\d+)?)\s*(MT|BBL|GAL|\u516c?\u5428|\u6876|\u52a0\u4ed1)",
            text,
            flags=re.I,
        )
        if qty_m2:
            qty = parse_first_decimal(qty_m2.group(1), 1.0)
            unit = normalize_quantity_unit(clean_text(qty_m2.group(2) or ""))
        else:
            fallback_qty, fallback_unit = parse_quantity_and_unit(text)
            qty = fallback_qty if fallback_qty > 0 else 1.0
            unit = normalize_quantity_unit(fallback_unit)

    price = 1.0
    price_m = re.search(
        r"(?:\b(?:price|unit\s*price|fixed\s*price)\b|\u4ef7\u683c|\u5355\u4ef7)\s*[:\uff1a]?\s*(?:[A-Z]{3}\s*)?([0-9][0-9,]*(?:\.\d+)?)",
        text,
        flags=re.I,
    )
    if price_m:
        price = parse_first_decimal(price_m.group(1), 1.0)
    else:
        price_m2 = re.search(r"(?:USD|CNY|RMB|EUR)\s*([0-9][0-9,]*(?:\.\d+)?)", text, flags=re.I)
        if price_m2:
            price = parse_first_decimal(price_m2.group(1), 1.0)

    dates = extract_valid_dates(text)
    d1, d2 = normalize_laycan_window(dates)

    load_m = re.search(
        r"(?:\b(?:load(?:ing)?\s*port|port\s*of\s*loading)\b|\u88c5\u8d27\u6e2f)\s*[:\uff1a]?\s*([^\n\r,;|]{2,80}?)(?=\s*(?:\b(?:discharge\s*port|port\s*of\s*discharge|unload(?:ing)?\s*port|quantity|qty|price|supplier|customer)\b|\u5378\u8d27\u6e2f|\u6570\u91cf|\u4ef7\u683c|$))",
        text,
        flags=re.I,
    )
    dis_m = re.search(
        r"(?:\b(?:discharge\s*port|port\s*of\s*discharge|unload(?:ing)?\s*port)\b|\u5378\u8d27\u6e2f)\s*[:\uff1a]?\s*([^\n\r,;|]{2,80}?)(?=\s*(?:\b(?:load(?:ing)?\s*port|port\s*of\s*loading|quantity|qty|price|supplier|customer)\b|\u88c5\u8d27\u6e2f|\u6570\u91cf|\u4ef7\u683c|$))",
        text,
        flags=re.I,
    )

    contract_type_hint = clean_text(str(item.get("contract_type") or item.get("list_contract_type") or item.get("detail_contract_type") or ""))
    side = infer_side_from_row(contract_type_hint, ext, default_side)
    cc = compact(text)
    if any(t in cc for t in ("supplier", "\u4f9b\u5e94\u5546", "\u5356\u65b9")):
        side = "purchase"
    elif any(t in cc for t in ("customer", "\u5ba2\u6237", "\u4e70\u65b9")):
        side = "sales"

    ctype = infer_contract_type_from_ext(ext)
    delivery_terms = "EXW" if ctype == "EXW" else "FOB"

    return {
        "request_id": req,
        "external_contract_number": ext,
        "contract_side": side,
        "contract_type": ctype,
        "delivery_terms": delivery_terms,
        "counterparty": cp,
        "product_name": normalize_product_name(infer_product_name(text)),
        "quantity": max(qty, 0.0001),
        "quantity_unit": normalize_quantity_unit(unit),
        "fixed_price": max(price, 0.0001),
        "payment_terms": "NET 30",
        "laycan_start": d1,
        "laycan_end": d2,
        "load_port": normalize_port_name(clean_text(load_m.group(1)) if load_m else "Unknown"),
        "discharge_port": normalize_port_name(clean_text(dis_m.group(1)) if dis_m else "Unknown"),
        "notes": clip_text(
            force_ascii_text(f"OA D240 sync; request_id={req}; title={title}; source={src}"),
            1600,
        ),
    }
def build_sales_payload(draft: dict[str, Any], partner_id: str, product_id: str, trader_id: str) -> dict[str, Any]:
    ctype = clean_text(str(draft.get("contract_type") or "CARGO")).upper()
    delivery = clean_text(str(draft.get("delivery_terms") or "FOB")).upper()
    return {
        "externalContractNumber": draft["external_contract_number"],
        "contractType": ctype if ctype in {"CARGO", "EXW", "DEL"} else "CARGO",
        "customerId": partner_id,
        "productId": product_id,
        "traderId": trader_id,
        "quantity": draft["quantity"],
        "quantityUnit": draft["quantity_unit"],
        "tonBarrelRatio": 7.6,
        "pricingType": "Fixed",
        "fixedPrice": draft["fixed_price"],
        "deliveryTerms": delivery if delivery in {"FOB", "CFR", "CIF", "EXW", "DAP", "DDP"} else "FOB",
        "laycanStart": draft["laycan_start"].strftime("%Y-%m-%dT%H:%M:%S"),
        "laycanEnd": draft["laycan_end"].strftime("%Y-%m-%dT%H:%M:%S"),
        "loadPort": normalize_port_name(str(draft.get("load_port") or "Unknown")),
        "dischargePort": normalize_port_name(str(draft.get("discharge_port") or "Unknown")),
        "settlementType": "TT",
        "creditPeriodDays": 30,
        "paymentTerms": force_ascii_text(str(draft.get("payment_terms") or "NET 30")) or "NET 30",
        "notes": force_ascii_text(str(draft.get("notes") or "")),
    }


def build_purchase_payload(draft: dict[str, Any], partner_id: str, product_id: str, trader_id: str) -> dict[str, Any]:
    qmap = {"MT": 1, "BBL": 2, "GAL": 3}
    ctypemap = {"CARGO": 1, "EXW": 2, "DEL": 3}
    dtmap = {"FOB": 1, "CIF": 2, "CFR": 3, "DAP": 4, "DDP": 5, "DES": 6, "DDU": 7, "STS": 8, "ITT": 9, "EXW": 10}
    ctype = clean_text(str(draft.get("contract_type") or "CARGO")).upper()
    delivery = clean_text(str(draft.get("delivery_terms") or "FOB")).upper()
    return {
        "externalContractNumber": draft["external_contract_number"],
        "contractType": ctypemap.get(ctype, 1),
        "supplierId": partner_id,
        "productId": product_id,
        "traderId": trader_id,
        "quantity": draft["quantity"],
        "quantityUnit": qmap.get(draft["quantity_unit"], 2),
        "tonBarrelRatio": 7.6,
        "pricingType": 1,
        "fixedPrice": draft["fixed_price"],
        "deliveryTerms": dtmap.get(delivery, 1),
        "laycanStart": draft["laycan_start"].strftime("%Y-%m-%dT%H:%M:%S"),
        "laycanEnd": draft["laycan_end"].strftime("%Y-%m-%dT%H:%M:%S"),
        "loadPort": normalize_port_name(str(draft.get("load_port") or "Unknown")),
        "dischargePort": normalize_port_name(str(draft.get("discharge_port") or "Unknown")),
        "settlementType": 1,
        "creditPeriodDays": 30,
        "paymentTerms": force_ascii_text(str(draft.get("payment_terms") or "NET 30")) or "NET 30",
        "notes": force_ascii_text(str(draft.get("notes") or "")),
    }


def build_sales_update_payload(draft: dict[str, Any], partner_id: str, product_id: str, trader_id: str) -> dict[str, Any]:
    delivery = clean_text(str(draft.get("delivery_terms") or "FOB")).upper()
    return {
        "externalContractNumber": draft["external_contract_number"],
        "customerId": partner_id,
        "productId": product_id,
        "traderId": trader_id,
        "quantity": draft["quantity"],
        "quantityUnit": draft["quantity_unit"],
        "tonBarrelRatio": 7.6,
        "pricingType": "Fixed",
        "fixedPrice": draft["fixed_price"],
        "deliveryTerms": delivery if delivery in {"FOB", "CFR", "CIF", "EXW", "DAP", "DDP"} else "FOB",
        "laycanStart": draft["laycan_start"].strftime("%Y-%m-%dT%H:%M:%S"),
        "laycanEnd": draft["laycan_end"].strftime("%Y-%m-%dT%H:%M:%S"),
        "loadPort": normalize_port_name(str(draft.get("load_port") or "Unknown")),
        "dischargePort": normalize_port_name(str(draft.get("discharge_port") or "Unknown")),
        "settlementType": "TT",
        "creditPeriodDays": 30,
        "paymentTerms": force_ascii_text(str(draft.get("payment_terms") or "NET 30")) or "NET 30",
        "notes": force_ascii_text(str(draft.get("notes") or "")),
    }


def build_purchase_update_payload(draft: dict[str, Any], partner_id: str, product_id: str, trader_id: str) -> dict[str, Any]:
    delivery = clean_text(str(draft.get("delivery_terms") or "FOB")).upper()
    return {
        "externalContractNumber": draft["external_contract_number"],
        "supplierId": partner_id,
        "productId": product_id,
        "traderId": trader_id,
        "quantity": draft["quantity"],
        "quantityUnit": draft["quantity_unit"],
        "tonBarrelRatio": 7.6,
        "pricingType": "Fixed",
        "fixedPrice": draft["fixed_price"],
        "deliveryTerms": delivery if delivery in {"FOB", "CFR", "CIF", "EXW", "DAP", "DDP"} else "FOB",
        "laycanStart": draft["laycan_start"].strftime("%Y-%m-%dT%H:%M:%S"),
        "laycanEnd": draft["laycan_end"].strftime("%Y-%m-%dT%H:%M:%S"),
        "loadPort": normalize_port_name(str(draft.get("load_port") or "Unknown")),
        "dischargePort": normalize_port_name(str(draft.get("discharge_port") or "Unknown")),
        "settlementType": "TT",
        "creditPeriodDays": 30,
        "paymentTerms": force_ascii_text(str(draft.get("payment_terms") or "NET 30")) or "NET 30",
        "notes": force_ascii_text(str(draft.get("notes") or "")),
    }


def get_contract_by_external(base_url: str, side: str, external_number: str) -> dict[str, Any] | None:
    encoded = urllib.parse.quote(external_number, safe="")
    status, payload = api_request(base_url, "GET", f"{side}-contracts/by-external/{encoded}")
    if status != 200:
        return None
    items = unwrap_list(payload)
    return items[0] if items else None


def find_existing_contract(base_url: str, external_number: str) -> tuple[str, dict[str, Any]] | None:
    p = get_contract_by_external(base_url, "purchase", external_number)
    if p:
        return "purchase", p
    s = get_contract_by_external(base_url, "sales", external_number)
    if s:
        return "sales", s
    return None


def find_existing_by_request_id(base_url: str, request_id: str) -> tuple[str, dict[str, Any]] | None:
    if not request_id:
        return None
    target = f"request_id={request_id}"
    for side in ("purchase", "sales"):
        status, payload = api_request(base_url, "GET", f"{side}-contracts?page=1&pageSize=200")
        if status != 200:
            continue
        for item in unwrap_list(payload):
            notes = clean_text(str(item.get("notes") or ""))
            if target in notes:
                return side, item
    return None


def scan_candidates(args: argparse.Namespace, outdir: Path, creators: list[str]) -> list[dict[str, Any]]:
    screens = outdir / "screenshots"
    atts = outdir / "attachments"
    debug = outdir / "debug"
    ensure_dir(screens)
    ensure_dir(atts)
    ensure_dir(debug)

    limit = args.limit if args.limit > 0 else 10**9
    user = os.getenv("OA_USER", "")
    pwd = os.getenv("OA_PASS", "")
    result: list[dict[str, Any]] = []

    with sync_playwright() as pw:
        browser = pw.chromium.launch(headless=args.headless)
        context = load_or_create_context(browser, state_path=Path("storage_state.json"))
        page = context.new_page()
        page.set_default_timeout(args.timeout)
        try:
            try:
                login_if_needed(
                    page=page,
                    base_url=args.base_url,
                    username=user,
                    password=pwd,
                    timeout_ms=args.timeout,
                    artifacts_dir=screens,
                )
            except Exception:
                if looks_like_login_page(page) and (not user or not pwd):
                    raise RuntimeError("OA session expired and OA_USER/OA_PASS are missing.")
                raise

            save_state(context, Path("storage_state.json"))
            page = goto_todo_list(page=page, base_url=args.base_url, timeout_ms=args.timeout, debug_dir=debug)
            rows = extract_todo_rows(
                page=page,
                base_url=args.base_url,
                limit=limit,
                debug_dir=debug,
                creator_filter="",
                max_pages=max(args.max_pages, 1),
            )
            LOGGER.info("OA rows: %s", len(rows))

            for i, row in enumerate(rows, start=1):
                detail_url = clean_text(str(row.get("detail_url") or ""))
                if not detail_url:
                    continue
                try:
                    detail = extract_detail(
                        page=page,
                        detail_url=detail_url,
                        base_url=args.base_url,
                        timeout_ms=args.timeout,
                        debug_dir=debug,
                        screenshots_dir=screens,
                    )
                except Exception as exc:
                    LOGGER.warning("Skip row %s detail extraction failed: %s", i, exc)
                    continue
                merged = {**row, **detail}
                blob = clean_text(" ".join([
                    clean_text(str(merged.get("list_title") or "")),
                    clean_text(str(merged.get("list_row_text") or "")),
                    clean_text(str(merged.get("detail_title") or "")),
                    clean_text(str(merged.get("detail_flow_name") or "")),
                    clean_text(str(merged.get("detail_preview") or "")),
                ]))
                if not creator_match(creators, blob):
                    continue
                if not target_contract_match(args.flow_keyword, args.contract_keywords, blob):
                    continue

                dp = context.new_page()
                dp.set_default_timeout(args.timeout)
                api_payload: dict[str, Any] = {"load_form": {}, "detail_data": {}}

                def _on_resp(resp: Any) -> None:
                    u = (resp.url or "").lower()
                    try:
                        raw = resp.body()
                    except Exception:
                        return
                    if "/api/workflow/reqform/loadform" in u:
                        data = parse_json_bytes(raw)
                        if isinstance(data, dict) and data:
                            api_payload["load_form"] = data
                    elif "/api/workflow/reqform/detaildata" in u:
                        data = parse_json_bytes(raw)
                        if isinstance(data, dict) and data:
                            api_payload["detail_data"] = data

                dp.on("response", _on_resp)
                try:
                    dp.goto(resolve_url(args.base_url, detail_url), wait_until="domcontentloaded", timeout=args.timeout)
                    try:
                        dp.wait_for_response(lambda r: "/api/workflow/reqform/loadForm" in (r.url or ""), timeout=min(args.timeout, 15000))
                    except Exception:
                        pass
                    try:
                        dp.wait_for_response(lambda r: "/api/workflow/reqform/detailData" in (r.url or ""), timeout=min(args.timeout, 15000))
                    except Exception:
                        pass
                    wait_with_jitter(dp, 900, 1800)

                    cands = collect_attachment_candidates(dp, args.base_url)
                    api_cands = collect_api_attachment_candidates(dict(api_payload.get("detail_data") or {}), args.base_url)
                    seen_href: set[str] = set()
                    merged_cands: list[dict[str, str]] = []
                    for c in [*api_cands, *cands]:
                        href = clean_text(c.get("href") or "")
                        if not href or href in seen_href:
                            continue
                        seen_href.add(href)
                        merged_cands.append(c)

                    req_id = clean_text(str(merged.get("list_request_id") or merged.get("detail_request_id") or f"row_{i}"))
                    down = download_attachments(dp, merged_cands, atts / req_id, args.max_attachments, args.timeout)
                finally:
                    try:
                        dp.remove_listener("response", _on_resp)
                    except Exception:
                        pass
                    dp.close()

                texts = [read_attachment_text(Path(x["downloaded_path"])) for x in down if x.get("downloaded_path")]
                texts = [t for t in texts if t]
                merged["detail_api_load_form"] = dict(api_payload.get("load_form") or {})
                merged["detail_api_detail_data"] = dict(api_payload.get("detail_data") or {})

                api_rows = extract_api_contract_rows(
                    dict(merged.get("detail_api_load_form") or {}),
                    dict(merged.get("detail_api_detail_data") or {}),
                    args.default_side,
                )
                draft = pick_best_draft_from_api(merged, api_rows, args.default_side)
                if not draft:
                    draft = extract_contract_draft(merged, texts, args.default_side)

                merged["sync_draft"] = draft
                merged["attachments"] = down
                merged["api_rows"] = api_rows
                result.append(merged)
        finally:
            context.close()
            browser.close()

    return result

def update_state(state: dict[str, Any], request_id: str, external_number: str, status: str) -> None:
    if request_id and request_id not in state["processed_request_ids"]:
        state["processed_request_ids"].append(request_id)
    if external_number and external_number not in state["processed_external_numbers"]:
        state["processed_external_numbers"].append(external_number)
    state["history"].append(
        {
            "ts_utc": now_utc_iso(),
            "request_id": request_id,
            "external_contract_number": external_number,
            "status": status,
        }
    )
    state["history"] = state["history"][-2000:]


def run_once(args: argparse.Namespace) -> dict[str, Any]:
    outdir = Path(args.outdir)
    reports = outdir / "reports"
    state_file = Path(args.state_file)
    ensure_dir(outdir)
    ensure_dir(reports)

    state = jread(
        state_file,
        {
            "processed_request_ids": [],
            "processed_external_numbers": [],
            "last_scan_utc": "",
            "history": [],
        },
    )
    done_req = set(state.get("processed_request_ids") or [])
    done_ext = set(state.get("processed_external_numbers") or [])

    if not x_health(args.x_api_base):
        raise RuntimeError(f"X API unavailable: {args.x_api_base}")

    creators = split_creators(args.creators)
    LOGGER.info("Sync start creators=%s flow=%s dry_run=%s", creators, args.flow_keyword, args.dry_run)

    candidates = scan_candidates(args, outdir, creators)
    LOGGER.info("Candidates after D240+creator filters: %s", len(candidates))

    _, partners_payload = api_request(args.x_api_base, "GET", "trading-partners")
    _, products_payload = api_request(args.x_api_base, "GET", "products")
    _, users_payload = api_request(args.x_api_base, "GET", "users")
    partners_cache = unwrap_list(partners_payload)
    products_cache = unwrap_list(products_payload)
    users = unwrap_list(users_payload)
    trader_id = select_trader_id(users)
    if not trader_id:
        raise RuntimeError("No trader user found in X")

    report: dict[str, Any] = {
        "started_utc": now_utc_iso(),
        "flow_keyword": args.flow_keyword,
        "creators": creators,
        "dry_run": args.dry_run,
        "candidates_total": len(candidates),
        "created": [],
        "updated": [],
        "skipped": [],
        "errors": [],
    }

    for item in candidates:
        draft = dict(item.get("sync_draft") or {})
        req = clean_text(str(draft.get("request_id") or item.get("list_request_id") or ""))
        ext = clean_text(str(draft.get("external_contract_number") or ""))
        side = clean_text(str(draft.get("contract_side") or args.default_side)).lower()
        side = "purchase" if side == "purchase" else "sales"

        if ext and ext in done_ext and not args.update_existing:
            report["skipped"].append({"request_id": req, "external_contract_number": ext, "reason": "already_processed_external"})
            continue
        if not ext:
            report["errors"].append({"request_id": req, "reason": "missing_external_contract_number"})
            continue

        cp = clean_text(str(draft.get("counterparty") or ""))
        if not cp:
            report["errors"].append({"request_id": req, "external_contract_number": ext, "reason": "missing_counterparty"})
            continue

        partner_id = ensure_partner(args.x_api_base, partners_cache, cp)
        if not partner_id:
            report["errors"].append({"request_id": req, "external_contract_number": ext, "reason": "partner_create_failed"})
            continue

        unit = clean_text(str(draft.get("quantity_unit") or "BBL")).upper()
        product_name = clean_text(str(draft.get("product_name") or "West Texas Intermediate"))
        product_id = ensure_product(args.x_api_base, products_cache, product_name, unit)
        if not product_id:
            report["errors"].append({"request_id": req, "external_contract_number": ext, "reason": "product_create_failed"})
            continue

        create_payload = (
            build_purchase_payload(draft, partner_id, product_id, trader_id)
            if side == "purchase"
            else build_sales_payload(draft, partner_id, product_id, trader_id)
        )

        existing = find_existing_contract(args.x_api_base, ext)
        if not existing and req:
            existing = find_existing_by_request_id(args.x_api_base, req)

        if existing and args.update_existing:
            existing_side, existing_item = existing
            cid = clean_text(str(existing_item.get("id") or ""))
            if not cid:
                report["errors"].append({
                    "request_id": req,
                    "external_contract_number": ext,
                    "reason": "existing_contract_missing_id",
                    "side": existing_side,
                })
                continue

            current_ext = clean_text(str(existing_item.get("externalContractNumber") or ""))
            current_qty = float(existing_item.get("quantity") or 0)
            current_unit = clean_text(str(existing_item.get("quantityUnit") or "")).upper()
            current_value = float(existing_item.get("contractValue") or 0)
            target_value = float(draft.get("quantity") or 0) * float(draft.get("fixed_price") or 0)
            already_up_to_date = (
                current_ext == ext
                and abs(current_qty - float(draft.get("quantity") or 0)) < 1e-6
                and current_unit == clean_text(str(draft.get("quantity_unit") or "")).upper()
                and abs(current_value - target_value) < 0.5
            )
            if already_up_to_date:
                report["skipped"].append({
                    "request_id": req,
                    "external_contract_number": ext,
                    "reason": "already_up_to_date",
                    "side": existing_side,
                })
                if not args.dry_run:
                    update_state(state, req, ext, "already_up_to_date")
                    done_req.add(req)
                    done_ext.add(ext)
                continue

            update_payload = (
                build_purchase_update_payload(draft, partner_id, product_id, trader_id)
                if existing_side == "purchase"
                else build_sales_update_payload(draft, partner_id, product_id, trader_id)
            )

            if args.dry_run:
                report["updated"].append({
                    "request_id": req,
                    "external_contract_number": ext,
                    "existing_side": existing_side,
                    "contract_id": cid,
                    "partner_id": partner_id,
                    "product_id": product_id,
                    "dry_run_payload": update_payload,
                })
                continue

            status, detail = api_request(args.x_api_base, "PUT", f"{existing_side}-contracts/{cid}", update_payload)
            if status not in (200, 204):
                report["errors"].append({
                    "request_id": req,
                    "external_contract_number": ext,
                    "side": existing_side,
                    "reason": "update_contract_failed",
                    "detail": detail,
                })
                continue

            report["updated"].append({
                "request_id": req,
                "external_contract_number": ext,
                "side": existing_side,
                "contract_id": cid,
            })
            update_state(state, req, ext, "updated")
            done_req.add(req)
            done_ext.add(ext)
            continue

        if existing:
            report["skipped"].append({"request_id": req, "external_contract_number": ext, "reason": "already_exists_in_x"})
            if not args.dry_run:
                update_state(state, req, ext, "exists_in_x")
                done_req.add(req)
                done_ext.add(ext)
            continue

        if args.dry_run:
            report["created"].append({
                "request_id": req,
                "external_contract_number": ext,
                "side": side,
                "partner_id": partner_id,
                "product_id": product_id,
                "dry_run_payload": create_payload,
            })
            continue

        status, created = api_request(args.x_api_base, "POST", f"{side}-contracts", create_payload)
        if status not in (200, 201):
            report["errors"].append({
                "request_id": req,
                "external_contract_number": ext,
                "side": side,
                "reason": "create_contract_failed",
                "detail": created,
            })
            continue

        report["created"].append({
            "request_id": req,
            "external_contract_number": ext,
            "side": side,
            "contract_id": created,
        })
        update_state(state, req, ext, "created")
        done_req.add(req)
        done_ext.add(ext)

    state["last_scan_utc"] = now_utc_iso()
    if not args.dry_run:
        jwrite(state_file, state)

    report["finished_utc"] = now_utc_iso()
    report_path = reports / f"oa_x_sync_report_{timestamp_for_filename()}.json"
    jwrite(report_path, report)
    LOGGER.info("Sync done candidates=%s created=%s updated=%s skipped=%s errors=%s report=%s", report["candidates_total"], len(report["created"]), len(report.get("updated") or []), len(report["skipped"]), len(report["errors"]), report_path)
    return {"report_path": str(report_path), **report}


def main() -> None:
    args = parse_args()
    ensure_dir(Path(args.outdir))
    configure_logging(Path(args.outdir) / "oa_x_sync.log")

    if args.interval_minutes <= 0:
        run_once(args)
        return

    LOGGER.info("Start polling every %s minutes", args.interval_minutes)
    while True:
        start = datetime.now(timezone.utc)
        try:
            run_once(args)
        except Exception as exc:  # noqa: BLE001
            LOGGER.exception("Sync cycle failed: %s", exc)
        elapsed = int((datetime.now(timezone.utc) - start).total_seconds())
        sleep_sec = max(args.interval_minutes * 60 - elapsed, 5)
        LOGGER.info("Sleep %s seconds", sleep_sec)
        time.sleep(sleep_sec)


if __name__ == "__main__":
    main()















