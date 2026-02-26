from __future__ import annotations

import logging
import re
from pathlib import Path
from typing import Any

from playwright.sync_api import Frame, Page

from .utils import (
    capture_page_artifacts,
    clean_text,
    clip_text,
    dump_frame_diagnostics,
    dump_top_level_dom,
    extract_request_like_id,
    iter_frames,
    looks_like_login_page,
    now_utc_iso,
    pick_field_by_keywords,
    resolve_url,
    safe_count,
    timestamp_for_filename,
    wait_with_jitter,
)

LOGGER = logging.getLogger("oa_scraper")


def _frame_score(frame: Frame) -> int:
    score = 0
    frame_url = (frame.url or "").lower()
    if any(token in frame_url for token in ("request", "workflow", "detail", "view")):
        score += 2

    score += min(safe_count(frame, "table tr"), 20)
    score += min(safe_count(frame, "text=流程"), 4)
    score += min(safe_count(frame, "text=申请人"), 4)
    score += min(safe_count(frame, "text=节点"), 4)
    score += min(safe_count(frame, "text=意见"), 4)
    return score


def _choose_detail_frame(page: Page) -> Frame:
    ranked = sorted(iter_frames(page), key=_frame_score, reverse=True)
    return ranked[0] if ranked else page.main_frame


def _extract_raw_detail(frame: Frame) -> dict[str, Any]:
    result = frame.evaluate(
        """
        () => {
            const normalize = (value) => (value || "").replace(/\s+/g, " ").trim();

            const getValue = (node) => {
                if (!node) return "";
                const tag = (node.tagName || "").toLowerCase();
                if (tag === "input" || tag === "textarea" || tag === "select") {
                    return normalize(node.value || "");
                }
                return normalize(node.textContent || "");
            };

            const fields = {};
            const addPair = (key, value) => {
                const normalizedKey = normalize(key).replace(/[：:]/g, "");
                const normalizedValue = normalize(value);
                if (!normalizedKey || !normalizedValue) return;
                if (normalizedKey.length > 50 || normalizedValue.length > 1000) return;
                if (!(normalizedKey in fields)) {
                    fields[normalizedKey] = normalizedValue;
                }
            };

            let title = "";
            const titleSelectors = [
                "h1",
                "h2",
                "h3",
                "[id*='title']",
                "[class*='title']",
                "[id*='subject']",
                "[class*='subject']"
            ];
            for (const selector of titleSelectors) {
                for (const node of Array.from(document.querySelectorAll(selector)).slice(0, 30)) {
                    const text = normalize(node.textContent);
                    if (text && text.length >= 2 && text.length <= 200) {
                        title = text;
                        break;
                    }
                }
                if (title) break;
            }

            for (const row of Array.from(document.querySelectorAll("table tr"))) {
                const cells = Array.from(row.querySelectorAll("th, td")).map((cell) => normalize(cell.textContent)).filter(Boolean);
                if (cells.length < 2) continue;
                if (cells.length === 2) {
                    addPair(cells[0], cells[1]);
                    continue;
                }
                for (let i = 0; i < cells.length - 1; i += 2) {
                    addPair(cells[i], cells[i + 1]);
                }
            }

            for (const label of Array.from(document.querySelectorAll("label, dt, [class*='label'], [class*='fieldname'], [class*='itemlabel']")).slice(0, 300)) {
                const key = normalize(label.textContent);
                if (!key || key.length > 50) continue;

                let value = "";
                const next = label.nextElementSibling;
                if (next) {
                    value = getValue(next);
                }
                if (!value && label.parentElement) {
                    const candidate = label.parentElement.querySelector("input, textarea, select, span, div");
                    value = getValue(candidate);
                }
                addPair(key, value);
            }

            let commentsText = "";
            const commentSelectors = [
                "[id*='comment']",
                "[class*='comment']",
                "[id*='remark']",
                "[class*='remark']",
                "[id*='opinion']",
                "[class*='opinion']",
                "textarea"
            ];
            for (const selector of commentSelectors) {
                for (const node of Array.from(document.querySelectorAll(selector)).slice(0, 60)) {
                    const candidate = getValue(node);
                    if (candidate.length > commentsText.length) {
                        commentsText = candidate;
                    }
                }
            }

            const bodyText = normalize((document.body && document.body.innerText) || "");

            return {
                title,
                fields,
                comments_text: commentsText,
                body_text: bodyText.slice(0, 9000),
            };
        }
        """
    )

    return result if isinstance(result, dict) else {}


def _extract_id_from_body(body_text: str) -> str:
    pattern = r"(?i)(?:requestid|workflowid|request id|workflow id|编号)\s*[:：=]?\s*([A-Za-z0-9_-]+)"
    match = re.search(pattern, body_text or "")
    if match:
        return clean_text(match.group(1))
    return ""


def extract_detail(
    page: Page,
    detail_url: str,
    base_url: str,
    timeout_ms: int,
    debug_dir: Path,
    screenshots_dir: Path,
) -> dict[str, Any]:
    full_url = resolve_url(base_url, detail_url)
    detail_page = page.context.new_page()
    detail_page.set_default_timeout(timeout_ms)

    try:
        detail_page.goto(full_url, wait_until="domcontentloaded", timeout=timeout_ms)
        wait_with_jitter(detail_page, min_ms=500, max_ms=1000)

        if looks_like_login_page(detail_page):
            capture_page_artifacts(detail_page, screenshots_dir, "detail_redirected_to_login")
            raise RuntimeError("Session appears expired: detail page redirected to login.")

        frame = _choose_detail_frame(detail_page)
        raw = _extract_raw_detail(frame)

        title = clean_text(str(raw.get("title") or ""))
        field_map = {
            clean_text(str(key)).replace("：", "").replace(":", ""): clean_text(str(value))
            for key, value in dict(raw.get("fields") or {}).items()
            if clean_text(str(value))
        }

        if not title:
            title = pick_field_by_keywords(field_map, ["标题", "主题", "流程名称", "流程"])

        flow_name = pick_field_by_keywords(field_map, ["流程名称", "流程", "流程类型"]) or title
        creator = pick_field_by_keywords(field_map, ["申请人", "发起人", "创建人", "提交人", "填报人"])
        created_time = pick_field_by_keywords(field_map, ["创建时间", "申请时间", "提交时间", "发起时间", "到达时间"])
        current_node = pick_field_by_keywords(field_map, ["当前节点", "节点", "当前环节", "处理节点", "当前处理"])

        request_id = pick_field_by_keywords(
            field_map,
            ["requestid", "workflowid", "request id", "workflow id", "请求id", "流程id", "编号", "id"],
        )

        body_text = clean_text(str(raw.get("body_text") or ""))
        comments_text = clean_text(str(raw.get("comments_text") or ""))

        if not request_id:
            request_id = extract_request_like_id(full_url) or _extract_id_from_body(body_text)

        preview_source = comments_text or body_text
        detail_preview = clip_text(preview_source, limit=1800)

        if not title and not field_map and len(body_text) < 30:
            stamp = timestamp_for_filename()
            dump_top_level_dom(detail_page, debug_dir / f"detail_dom_outline_{stamp}.json")
            dump_frame_diagnostics(detail_page, debug_dir / f"detail_frame_diagnostics_{stamp}.json")
            capture_page_artifacts(detail_page, screenshots_dir, f"detail_extract_failed_{stamp}")
            raise RuntimeError("Detail extraction returned empty content; see debug artifacts.")

        return {
            "detail_title": title,
            "detail_flow_name": flow_name,
            "detail_creator": creator,
            "detail_created_time": created_time,
            "detail_current_node": current_node,
            "detail_request_id": request_id,
            "detail_preview": detail_preview,
            "detail_page_url": detail_page.url,
            "detail_frame_url": frame.url,
            "detail_scraped_at_utc": now_utc_iso(),
            "detail_field_map": field_map,
        }

    except Exception:
        capture_page_artifacts(detail_page, screenshots_dir, "detail_error")
        raise
    finally:
        detail_page.close()
