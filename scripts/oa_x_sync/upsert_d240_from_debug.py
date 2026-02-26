from __future__ import annotations

import json
import urllib.parse
from pathlib import Path
from typing import Any

import run_oa_x_sync as sync

BASE = "http://localhost:5000/api"
DEBUG_DIR = Path(r"C:\Windows\System32\output\oa_x_sync\debug")
REQUEST_IDS = ["13202021", "13200056", "13199916", "13201814", "13197613", "13197496"]


def infer_delivery_terms(ext: str) -> str:
    u = sync.clean_text(ext).upper()
    if "-EXW-" in u:
        return "EXW"
    if "-CIF-" in u:
        return "CIF"
    if "-CFR-" in u:
        return "CFR"
    return "FOB"


def jread(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def row_value(row: dict[str, Any], key: str) -> str:
    cell = row.get(key)
    if isinstance(cell, dict):
        return sync.clean_text(str(cell.get("value") or ""))
    return sync.clean_text(str(cell or ""))


def parse_qty_unit(text: str) -> tuple[float, str]:
    s = sync.clean_text(text)
    qty = sync.parse_first_decimal(s, 0.0)
    unit = "BBL"
    if "吨" in s or "MT" in s.upper():
        unit = "MT"
    elif "桶" in s or "BBL" in s.upper():
        unit = "BBL"
    elif "升" in s or "GAL" in s.upper():
        unit = "GAL"
    return max(qty, 0.0001), unit


def extract_best_row(req: str) -> dict[str, Any] | None:
    detail = jread(DEBUG_DIR / f"probe_{req}_detail.json")
    rows = dict(dict(detail.get("detail_1") or {}).get("rowDatas") or {})
    picks: list[dict[str, Any]] = []

    for row in rows.values():
        if not isinstance(row, dict):
            continue

        ext = sync.normalize_ext_contract_number(row_value(row, "field50979"))
        if "ITGR-" not in ext.upper():
            continue

        product = row_value(row, "field50981")
        qty_raw = row_value(row, "field50984")
        price_raw = row_value(row, "field50985")
        cp = row_value(row, "field50989")
        pay_terms = row_value(row, "field50982")
        date_window = row_value(row, "field50983")

        qty, unit = parse_qty_unit(qty_raw)
        price = max(sync.parse_first_decimal(price_raw, 0.0), 0.0001)
        dates = sync.extract_valid_dates(date_window)
        d1, d2 = sync.normalize_laycan_window(dates)

        picks.append(
            {
                "request_id": req,
                "external_contract_number": ext,
                "contract_side": "purchase",
                "contract_type": sync.infer_contract_type_from_ext(ext),
                "delivery_terms": infer_delivery_terms(ext),
                "counterparty": cp,
                "product_name": product or sync.infer_product_name(ext),
                "quantity": qty,
                "quantity_unit": unit,
                "fixed_price": price,
                "payment_terms": pay_terms or "NET 30",
                "laycan_start": d1,
                "laycan_end": d2,
                "load_port": "Unknown",
                "discharge_port": "Unknown",
                "notes": f"OA D240 repaired; request_id={req}; source=api_detail",
            }
        )

    if not picks:
        return None

    picks.sort(
        key=lambda r: (
            0 if "-S" in r["external_contract_number"].upper() else 1,
            r["external_contract_number"],
        )
    )
    return picks[0]


def unwrap(payload: Any) -> list[dict[str, Any]]:
    return sync.unwrap_list(payload)


def find_purchase_by_external(ext: str) -> dict[str, Any] | None:
    enc = urllib.parse.quote(ext, safe="")
    status, payload = sync.api_request(BASE, "GET", f"purchase-contracts/by-external/{enc}")
    if status != 200:
        return None
    items = unwrap(payload)
    return items[0] if items else None


def find_purchase_by_request(req: str) -> dict[str, Any] | None:
    status, payload = sync.api_request(BASE, "GET", "purchase-contracts?page=1&pageSize=500")
    if status != 200:
        return None
    target = f"request_id={req}"
    for item in unwrap(payload):
        notes = sync.clean_text(str(item.get("notes") or ""))
        if target in notes:
            return item
    return None


def ensure_ref_ids(draft: dict[str, Any], partners_cache: list[dict[str, Any]], products_cache: list[dict[str, Any]], trader_id: str) -> tuple[str, str, str]:
    cp = sync.clean_text(str(draft.get("counterparty") or ""))
    unit = sync.clean_text(str(draft.get("quantity_unit") or "BBL")).upper()
    product_name = sync.clean_text(str(draft.get("product_name") or "West Texas Intermediate"))
    partner_id = sync.ensure_partner(BASE, partners_cache, cp)
    product_id = sync.ensure_product(BASE, products_cache, product_name, unit)
    return partner_id, product_id, trader_id


def create_purchase(draft: dict[str, Any], partner_id: str, product_id: str, trader_id: str) -> tuple[int, Any]:
    payload = sync.build_purchase_payload(draft, partner_id, product_id, trader_id)
    payload["notes"] = sync.clip_text(
        f"OA D240 repaired; request_id={draft['request_id']}; source=api_detail", 1600
    )
    return sync.api_request(BASE, "POST", "purchase-contracts", payload)


def update_purchase(contract_id: str, draft: dict[str, Any], partner_id: str, product_id: str, trader_id: str) -> tuple[int, Any]:
    payload = sync.build_purchase_update_payload(draft, partner_id, product_id, trader_id)
    payload["notes"] = sync.clip_text(
        f"OA D240 repaired; request_id={draft['request_id']}; source=api_detail", 1600
    )
    return sync.api_request(BASE, "PUT", f"purchase-contracts/{contract_id}", payload)


def main() -> None:
    _, partners_payload = sync.api_request(BASE, "GET", "trading-partners")
    _, products_payload = sync.api_request(BASE, "GET", "products")
    _, users_payload = sync.api_request(BASE, "GET", "users")
    partners_cache = unwrap(partners_payload)
    products_cache = unwrap(products_payload)
    users = unwrap(users_payload)
    trader_id = sync.select_trader_id(users)
    if not trader_id:
        raise RuntimeError("No trader user found in X")

    rows: list[dict[str, Any]] = []
    for req in REQUEST_IDS:
        draft = extract_best_row(req)
        if draft:
            rows.append(draft)

    if not rows:
        raise RuntimeError("No rows extracted from debug detail files")

    results: list[dict[str, Any]] = []

    for draft in rows:
        ext = draft["external_contract_number"]
        req = draft["request_id"]

        existing = find_purchase_by_external(ext)
        if not existing:
            existing = find_purchase_by_request(req)

        partner_id, product_id, trader_id = ensure_ref_ids(draft, partners_cache, products_cache, trader_id)
        if not partner_id or not product_id or not trader_id:
            results.append({"request_id": req, "external": ext, "status": "ref_create_failed"})
            continue

        if existing:
            cid = sync.clean_text(str(existing.get("id") or ""))
            if not cid:
                results.append({"request_id": req, "external": ext, "status": "existing_missing_id"})
                continue
            st, detail = update_purchase(cid, draft, partner_id, product_id, trader_id)
            if st not in (200, 204):
                results.append({"request_id": req, "external": ext, "status": "update_failed", "detail": detail})
                continue
            results.append({
                "request_id": req,
                "external": ext,
                "status": "updated",
                "contract_id": cid,
                "partner_id": partner_id,
                "product_id": product_id,
                "qty": draft["quantity"],
                "unit": draft["quantity_unit"],
                "fixed_price": draft["fixed_price"],
            })
            continue

        st, detail = create_purchase(draft, partner_id, product_id, trader_id)
        if st not in (200, 201):
            results.append({"request_id": req, "external": ext, "status": "create_failed", "detail": detail})
            continue

        created_id = sync.clean_text(str(detail.get("id") if isinstance(detail, dict) else detail or ""))
        results.append({
            "request_id": req,
            "external": ext,
            "status": "created",
            "contract_id": created_id,
            "partner_id": partner_id,
            "product_id": product_id,
            "qty": draft["quantity"],
            "unit": draft["quantity_unit"],
            "fixed_price": draft["fixed_price"],
        })

    print("UPSERT_RESULTS")
    print(json.dumps(results, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
