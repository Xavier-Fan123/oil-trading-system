from __future__ import annotations

import logging
import re
from pathlib import Path
from typing import Any

from playwright.sync_api import Frame, Page

from .utils import (
    capture_page_artifacts,
    clean_text,
    dump_frame_diagnostics,
    dump_top_level_dom,
    extract_request_like_id,
    iter_frames,
    resolve_url,
    safe_count,
    timestamp_for_filename,
    wait_with_jitter,
)

LOGGER = logging.getLogger("oa_scraper")

LIST_DOING_PATH = "/spa/workflow/static/index.html#/main/workflow/listDoing"

TABLE_LINK_SELECTOR = (
    "a[data-requestid], a[data-link], "
    "a[onclick*='openSPA4Single'], a[onclick*='openFullWindowHaveBarForWFList'], "
    "a[href*='request'], a[href*='workflow'], a[href*='req'], "
    "a[onclick*='request'], a[onclick*='workflow'], a[onclick*='req']"
)

TODO_NAV_SELECTORS = [
    "a:has-text('待办事宜')",
    "a:has-text('待办')",
    "div[title='待办事宜']",
    "div[data-tabid='1']",
    "[title*='待办']",
    "span:has-text('待办事宜')",
    "span:has-text('待办')",
]

TODO_URL_PATHS = [
    LIST_DOING_PATH,
    "/spa/workflow/static/index.html#/main/workflow/listDoing",
    "/wui/index.html#/main/portal/portal-1-1",
    "/wui/index.html#/main/workflow/request",
    "/wui/index.html#/main/workflow/todo",
    "/workflow/request/reqManage/doingView.jsp",
    "/workflow/request/ReqUnFinish.jsp",
]

POPUP_CLOSE_SELECTORS = [
    ".ant-modal-close",
    ".ant-modal-close-x",
    "button[title='关闭']",
    "button[aria-label='Close']",
    "[class*='modal'] [class*='close']",
    "[class*='dialog'] [class*='close']",
]


def _dismiss_popups(page: Page) -> None:
    # Many OA accounts show blocking post-login modals; close them opportunistically.
    for _ in range(4):
        clicked = False
        for selector in POPUP_CLOSE_SELECTORS:
            locator = page.locator(selector)
            count = locator.count()
            if count <= 0:
                continue
            try:
                locator.first.click(timeout=1200)
                page.wait_for_timeout(600)
                clicked = True
                break
            except Exception:  # noqa: BLE001
                continue
        if not clicked:
            break


def _todo_signal_score(frame: Frame) -> int:
    score = 0
    frame_url = (frame.url or "").lower()

    if "listdoing" in frame_url:
        score += 8
    if any(token in frame_url for token in ("todo", "doing", "workflow", "request", "portal")):
        score += 2

    score += min(safe_count(frame, "a[onclick*='openSPA4Single'], a[onclick*='openFullWindowHaveBarForWFList']"), 20)
    score += min(safe_count(frame, "a[data-requestid], a[data-link]"), 20)
    score += min(safe_count(frame, "li.ant-pagination-next, ul.ant-pagination"), 8)
    score += min(safe_count(frame, TABLE_LINK_SELECTOR), 12)
    score += min(safe_count(frame, "table tr"), 10)
    return score


def _is_probably_todo_page(page: Page) -> bool:
    return max((_todo_signal_score(frame) for frame in iter_frames(page)), default=0) >= 8


def _pick_best_frame(page: Page) -> Frame:
    frames = list(iter_frames(page))
    if not frames:
        return page.main_frame
    return sorted(frames, key=_todo_signal_score, reverse=True)[0]


def _is_listdoing_frame(frame: Frame) -> bool:
    frame_url = (frame.url or "").lower()
    if "listdoing" in frame_url:
        return True
    if safe_count(frame, "a[onclick*='openSPA4Single'], a[onclick*='openFullWindowHaveBarForWFList']") > 0:
        return True
    return False


def goto_todo_list(page: Page, base_url: str, timeout_ms: int, debug_dir: Path) -> Page:
    _dismiss_popups(page)
    wait_with_jitter(page, min_ms=800, max_ms=1500)

    if _is_probably_todo_page(page):
        return page

    for path in TODO_URL_PATHS:
        candidate_url = resolve_url(base_url, path)
        try:
            page.goto(candidate_url, wait_until="domcontentloaded", timeout=timeout_ms)
            # SPA routes can hydrate with delay.
            page.wait_for_timeout(5000)
            _dismiss_popups(page)
            wait_with_jitter(page, min_ms=800, max_ms=1500)
            if _is_probably_todo_page(page):
                LOGGER.info("Navigated to todo list via direct URL: %s", candidate_url)
                return page
        except Exception:  # noqa: BLE001
            continue

    for frame in iter_frames(page):
        for selector in TODO_NAV_SELECTORS:
            if safe_count(frame, selector) <= 0:
                continue
            before_pages = len(page.context.pages)
            try:
                frame.locator(selector).first.click(timeout=5000)
            except Exception:  # noqa: BLE001
                continue

            page.wait_for_timeout(3000)
            if len(page.context.pages) > before_pages:
                page = page.context.pages[-1]
                page.set_default_timeout(timeout_ms)

            _dismiss_popups(page)
            wait_with_jitter(page, min_ms=800, max_ms=1500)
            if _is_probably_todo_page(page):
                LOGGER.info("Navigated to todo list by clicking selector: %s", selector)
                return page

    stamp = timestamp_for_filename()
    dump_top_level_dom(page, debug_dir / f"todo_dom_outline_{stamp}.json")
    dump_frame_diagnostics(page, debug_dir / f"todo_frame_diagnostics_{stamp}.json")
    capture_page_artifacts(page, debug_dir, f"todo_navigation_failed_{stamp}")
    raise RuntimeError("Unable to navigate to OA todo list page.")


def _extract_url_candidate(href: str, onclick: str, data_link: str) -> str:
    for raw in (data_link, href, onclick):
        value = clean_text(raw)
        if not value:
            continue

        lower = value.lower()
        if lower in {"#", "javascript:void(0)", "javascript:void(0);", "javascript:;", "void(0)"}:
            continue

        if lower.startswith("javascript"):
            match = re.search(r"(https?://[^'\"\s)]+|/[^'\"\s)]+)", value, flags=re.IGNORECASE)
            if match:
                return match.group(1)
            continue

        return value

    return ""


def _extract_request_id_from_any(text: str) -> str:
    if not text:
        return ""
    match = re.search(r"requestid=([-]?\d+)", text, flags=re.IGNORECASE)
    if match:
        return clean_text(match.group(1))
    return extract_request_like_id(text)


def _resolve_detail_url_for_listdoing(base_url: str, href: str, onclick: str) -> tuple[str, str]:
    onclick_clean = clean_text(onclick)
    href_clean = clean_text(href)

    # Example:
    # openSPA4Single('/main/workflow/req?requestid=13198747',13198747)
    request_id = _extract_request_id_from_any(onclick_clean) or _extract_request_id_from_any(href_clean)

    path_match = re.search(r"openFullWindowHaveBarForWFList\('([^']+)'", onclick_clean, flags=re.IGNORECASE)
    if path_match:
        path = clean_text(path_match.group(1))
        return resolve_url(base_url, path), request_id

    if request_id:
        return (
            resolve_url(base_url, f"/workflow/request/ViewRequestForwardSPA.jsp?requestid={request_id}&isovertime="),
            request_id,
        )

    candidate = _extract_url_candidate(href_clean, onclick_clean, "")
    if candidate:
        return resolve_url(base_url, candidate), _extract_request_id_from_any(candidate)

    return "", ""


def _extract_date_from_cells(cells: list[str]) -> str:
    joined = clean_text(" ".join(cells))
    match = re.search(r"\d{4}-\d{2}-\d{2}(?:\s+\d{2}:\d{2}(?::\d{2})?)?", joined)
    return clean_text(match.group(0)) if match else ""


def _extract_listdoing_current_rows(frame: Frame, base_url: str) -> list[dict[str, Any]]:
    try:
        raw_rows = frame.evaluate(
            """
            () => {
                const normalize = (value) => (value || "").replace(/\s+/g, " ").trim();
                const rows = [];

                for (const tr of Array.from(document.querySelectorAll("table tr"))) {
                    const anchors = Array.from(tr.querySelectorAll("a"));
                    if (!anchors.length) continue;

                    const titleAnchor = anchors.find((a) => {
                        const onclick = (a.getAttribute("onclick") || "").toLowerCase();
                        const href = (a.getAttribute("href") || "").toLowerCase();
                        const text = normalize(a.textContent);
                        if (!text) return false;
                        return onclick.includes("openspa4single")
                            || onclick.includes("openfullwindowhavebarforwflist")
                            || (text.length > 8 && href.includes("javascript:void(0)"));
                    });
                    if (!titleAnchor) continue;

                    const creatorAnchor = anchors.find((a) => {
                        const href = (a.getAttribute("href") || "").toLowerCase();
                        return href.includes("openhrm(");
                    });

                    const cells = Array.from(tr.querySelectorAll("th,td")).map((cell) => normalize(cell.textContent));
                    const titleText = normalize(titleAnchor.textContent);
                    if (!titleText) continue;

                    rows.push({
                        title_text: titleText,
                        title_href: titleAnchor.getAttribute("href") || "",
                        title_onclick: titleAnchor.getAttribute("onclick") || "",
                        creator_text: creatorAnchor ? normalize(creatorAnchor.textContent) : "",
                        creator_href: creatorAnchor ? (creatorAnchor.getAttribute("href") || "") : "",
                        cells,
                        row_text: normalize(tr.textContent).slice(0, 1200),
                    });
                }
                return rows;
            }
            """
        )
    except Exception:  # noqa: BLE001
        return []

    items: list[dict[str, Any]] = []
    for row in raw_rows if isinstance(raw_rows, list) else []:
        title_text = clean_text(str(row.get("title_text") or ""))
        onclick = str(row.get("title_onclick") or "")
        href = str(row.get("title_href") or "")
        creator_text = clean_text(str(row.get("creator_text") or ""))
        cells = [clean_text(str(item)) for item in row.get("cells", [])]
        row_text = clean_text(str(row.get("row_text") or ""))

        detail_url, request_id = _resolve_detail_url_for_listdoing(base_url=base_url, href=href, onclick=onclick)

        item = {
            "detail_url": detail_url,
            "list_title": title_text,
            "list_flow_name": "",
            "list_creator": creator_text,
            "list_created_time": _extract_date_from_cells(cells),
            "list_current_node": "",
            "list_request_id": request_id,
            "list_workflow_id": "",
            "list_row_text": row_text[:800],
        }
        items.append(item)

    return items


def _get_active_page_number(frame: Frame) -> int | None:
    try:
        value = frame.evaluate(
            """
            () => {
                const active = document.querySelector('li.ant-pagination-item-active');
                if (!active) return null;
                const text = (active.getAttribute('title') || active.textContent || '').replace(/\s+/g, ' ').trim();
                const match = text.match(/\d+/);
                return match ? parseInt(match[0], 10) : null;
            }
            """
        )
    except Exception:  # noqa: BLE001
        return None

    return int(value) if isinstance(value, int) else None


def _goto_next_page(frame: Frame, page: Page) -> bool:
    if safe_count(frame, "li.ant-pagination-next:not(.ant-pagination-disabled)") <= 0:
        return False

    before = _get_active_page_number(frame)
    try:
        frame.locator("li.ant-pagination-next:not(.ant-pagination-disabled)").first.click(timeout=5000)
    except Exception:  # noqa: BLE001
        return False

    for _ in range(30):
        page.wait_for_timeout(250)
        after = _get_active_page_number(frame)
        if before is None and after is not None:
            return True
        if before is not None and after is not None and after > before:
            return True

    # Even if page number did not change, allow fallback if table still exists.
    return safe_count(frame, "table tr") > 0


def _creator_matches(item: dict[str, Any], creator_filter: str) -> bool:
    target = clean_text(creator_filter)
    if not target:
        return True

    explicit_creator = clean_text(str(item.get("list_creator") or ""))
    if explicit_creator:
        return target in explicit_creator

    blob = " ".join(
        [
            clean_text(str(item.get("list_title") or "")),
            clean_text(str(item.get("list_row_text") or "")),
        ]
    )
    return target in blob


def _extract_listdoing_rows(
    page: Page,
    base_url: str,
    limit: int,
    creator_filter: str,
    max_pages: int,
) -> list[dict[str, Any]]:
    frame = _pick_best_frame(page)
    items: list[dict[str, Any]] = []
    seen: set[str] = set()
    pages_scanned = 0

    while pages_scanned < max_pages:
        pages_scanned += 1
        current_rows = _extract_listdoing_current_rows(frame=frame, base_url=base_url)
        LOGGER.info("ListDoing page %s yielded %s rows.", pages_scanned, len(current_rows))

        for row in current_rows:
            detail_url = clean_text(str(row.get("detail_url") or ""))
            req_id = clean_text(str(row.get("list_request_id") or ""))
            dedupe_key = detail_url or req_id
            if not dedupe_key or dedupe_key in seen:
                continue

            if not _creator_matches(row, creator_filter):
                continue

            seen.add(dedupe_key)
            items.append(row)

            if len(items) >= limit:
                LOGGER.info("Reached extraction limit (%s) on page %s.", limit, pages_scanned)
                return items

        if not _goto_next_page(frame=frame, page=page):
            LOGGER.info("No more pagination pages after scanning %s page(s).", pages_scanned)
            break

        wait_with_jitter(page, min_ms=500, max_ms=900)

    return items


def _collect_table_candidates(frame: Frame) -> list[dict[str, Any]]:
    try:
        result = frame.evaluate(
            """
            () => {
                const normalize = (value) => (value || "").replace(/\s+/g, " ").trim();
                const headersToText = (table) => Array.from(table.querySelectorAll("th"))
                    .map((th) => normalize(th.textContent))
                    .filter(Boolean);
                const tables = Array.from(document.querySelectorAll("table"));

                return tables.map((table, index) => {
                    const headers = headersToText(table);
                    const allRows = table.querySelectorAll("tr").length;
                    const linkRows = Array.from(table.querySelectorAll("tr")).filter((row) => row.querySelector("a[href], a[onclick], a[data-link], a[data-requestid]")).length;
                    const requestLinks = table.querySelectorAll(
                        "a[data-requestid], a[data-link], a[href*='request'], a[href*='workflow'], a[href*='req'], a[onclick*='request'], a[onclick*='workflow'], a[onclick*='req'], a[onclick*='openSPA4Single'], a[onclick*='openFullWindowHaveBarForWFList']"
                    ).length;
                    const keywordHits = headers.filter((head) =>
                        ["流程", "标题", "申请", "时间", "节点", "待办", "request", "workflow"].some((kw) => head.toLowerCase().includes(kw.toLowerCase()))
                    ).length;
                    return {
                        table_index: index,
                        row_count: allRows,
                        link_row_count: linkRows,
                        request_link_count: requestLinks,
                        keyword_hits: keywordHits,
                        headers: headers,
                    };
                });
            }
            """
        )
        return result if isinstance(result, list) else []
    except Exception:  # noqa: BLE001
        return []


def _extract_table_rows(frame: Frame, table_index: int) -> tuple[list[str], list[dict[str, Any]]]:
    result = frame.evaluate(
        """
        ({ index }) => {
            const normalize = (value) => (value || "").replace(/\s+/g, " ").trim();
            const table = Array.from(document.querySelectorAll("table"))[index];
            if (!table) {
                return { headers: [], rows: [] };
            }

            const headers = Array.from(table.querySelectorAll("th"))
                .map((th) => normalize(th.textContent))
                .filter(Boolean);

            const rows = [];
            for (const tr of Array.from(table.querySelectorAll("tr"))) {
                const link = tr.querySelector("a[href], a[onclick], a[data-link], a[data-requestid]");
                if (!link) continue;

                const cells = Array.from(tr.querySelectorAll("th, td")).map((cell) => normalize(cell.textContent));
                if (!cells.some(Boolean)) continue;

                rows.push({
                    link_text: normalize(link.textContent),
                    href: link.getAttribute("href") || "",
                    onclick: link.getAttribute("onclick") || "",
                    data_link: link.getAttribute("data-link") || "",
                    data_requestid: link.getAttribute("data-requestid") || "",
                    data_workflowid: link.getAttribute("data-workflowid") || "",
                    cells: cells,
                });
            }
            return { headers, rows };
        }
        """,
        {"index": table_index},
    )

    headers = result.get("headers", []) if isinstance(result, dict) else []
    rows = result.get("rows", []) if isinstance(result, dict) else []
    return headers, rows


def _map_cells(headers: list[str], cells: list[str]) -> dict[str, str]:
    normalized_headers = [clean_text(item).replace("：", "").replace(":", "") for item in headers]
    mapped: dict[str, str] = {}

    for index, cell_value in enumerate(cells):
        key = normalized_headers[index] if index < len(normalized_headers) and normalized_headers[index] else f"col_{index + 1}"
        mapped[key] = clean_text(cell_value)
    return mapped


def _pick(cell_map: dict[str, str], keywords: list[str]) -> str:
    lowered = {key.lower(): value for key, value in cell_map.items() if clean_text(value)}
    for keyword in keywords:
        target = keyword.lower()
        for key, value in lowered.items():
            if target in key:
                return clean_text(value)
    return ""


def _build_row_item(headers: list[str], row: dict[str, Any], base_url: str) -> dict[str, Any]:
    cells = [clean_text(str(value)) for value in row.get("cells", [])]
    cell_map = _map_cells(headers, cells)

    href = str(row.get("href") or "")
    onclick = str(row.get("onclick") or "")
    data_link = str(row.get("data_link") or "")
    data_requestid = clean_text(str(row.get("data_requestid") or ""))
    data_workflowid = clean_text(str(row.get("data_workflowid") or ""))

    url_candidate = _extract_url_candidate(href, onclick, data_link)
    detail_url = resolve_url(base_url, url_candidate) if url_candidate else ""

    list_title = clean_text(str(row.get("link_text") or ""))
    if not list_title:
        list_title = _pick(cell_map, ["标题", "主题", "流程名称", "流程", "请求"])

    list_flow_name = _pick(cell_map, ["流程", "标题", "主题", "名称"])
    list_creator = _pick(cell_map, ["申请人", "发起人", "创建人", "提交人", "填报人"])
    list_created_time = _pick(cell_map, ["申请时间", "创建时间", "提交时间", "到达时间", "日期"])
    list_current_node = _pick(cell_map, ["当前节点", "节点", "当前环节", "处理节点", "步骤"])

    list_request_id = (
        data_requestid
        or _pick(cell_map, ["requestid", "workflowid", "编号", "请求id", "流程id", "id"])
        or _extract_request_id_from_any(detail_url)
        or _extract_request_id_from_any(href)
        or _extract_request_id_from_any(onclick)
        or _extract_request_id_from_any(data_link)
    )

    list_workflow_id = data_workflowid or extract_request_like_id(data_link)

    return {
        "detail_url": detail_url,
        "list_title": list_title,
        "list_flow_name": list_flow_name,
        "list_creator": list_creator,
        "list_created_time": list_created_time,
        "list_current_node": list_current_node,
        "list_request_id": list_request_id,
        "list_workflow_id": list_workflow_id,
        "list_row_text": clean_text(" | ".join(cells))[:500],
    }


def _extract_fallback_links(page: Page, base_url: str, limit: int, creator_filter: str) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    seen_urls: set[str] = set()

    for frame in iter_frames(page):
        try:
            links = frame.evaluate(
                """
                () => {
                    const normalize = (value) => (value || "").replace(/\s+/g, " ").trim();
                    const all = Array.from(document.querySelectorAll("a[href], a[onclick], a[data-link], a[data-requestid]"));
                    return all.slice(0, 1200).map((anchor) => {
                        const parent = anchor.closest("tr, li, .item, .row, div") || anchor.parentElement;
                        return {
                            text: normalize(anchor.textContent),
                            href: anchor.getAttribute("href") || "",
                            onclick: anchor.getAttribute("onclick") || "",
                            data_link: anchor.getAttribute("data-link") || "",
                            data_requestid: anchor.getAttribute("data-requestid") || "",
                            data_workflowid: anchor.getAttribute("data-workflowid") || "",
                            context: normalize(parent ? parent.textContent : "").slice(0, 300),
                        };
                    });
                }
                """
            )
        except Exception:  # noqa: BLE001
            continue

        for link in links if isinstance(links, list) else []:
            text = clean_text(str(link.get("text") or ""))
            href = str(link.get("href") or "")
            onclick = str(link.get("onclick") or "")
            data_link = str(link.get("data_link") or "")
            data_requestid = clean_text(str(link.get("data_requestid") or ""))
            data_workflowid = clean_text(str(link.get("data_workflowid") or ""))
            context = clean_text(str(link.get("context") or ""))

            combined_lower = f"{text} {href} {onclick} {data_link} {context}".lower()
            if not (
                data_requestid
                or "viewrequest" in combined_lower
                or any(token in combined_lower for token in ("request", "workflow", "待办", "流程", "审批", "req"))
            ):
                continue

            url_candidate = _extract_url_candidate(href, onclick, data_link)
            if not url_candidate:
                continue

            detail_url = resolve_url(base_url, url_candidate)
            if detail_url in seen_urls:
                continue
            seen_urls.add(detail_url)

            item = {
                "detail_url": detail_url,
                "list_title": text,
                "list_flow_name": "",
                "list_creator": "",
                "list_created_time": "",
                "list_current_node": "",
                "list_request_id": data_requestid
                or _extract_request_id_from_any(detail_url)
                or _extract_request_id_from_any(href)
                or _extract_request_id_from_any(onclick)
                or _extract_request_id_from_any(data_link),
                "list_workflow_id": data_workflowid or extract_request_like_id(data_link),
                "list_row_text": context[:500],
            }

            if not _creator_matches(item, creator_filter):
                continue

            items.append(item)

            if len(items) >= limit:
                return items

    return items


def extract_todo_rows(
    page: Page,
    base_url: str,
    limit: int = 20,
    debug_dir: Path | None = None,
    creator_filter: str = "",
    max_pages: int = 200,
) -> list[dict[str, Any]]:
    _dismiss_popups(page)

    safe_limit = max(limit, 1)
    safe_max_pages = max(max_pages, 1)

    best_frame = _pick_best_frame(page)
    if _is_listdoing_frame(best_frame):
        listdoing_items = _extract_listdoing_rows(
            page=page,
            base_url=base_url,
            limit=safe_limit,
            creator_filter=creator_filter,
            max_pages=safe_max_pages,
        )
        if listdoing_items:
            LOGGER.info("Extracted %s todo rows from listDoing route.", len(listdoing_items))
            return listdoing_items[:safe_limit]

    best: dict[str, Any] | None = None

    for frame in iter_frames(page):
        candidates = _collect_table_candidates(frame)
        for candidate in candidates:
            score = (
                int(candidate.get("request_link_count", 0)) * 4
                + int(candidate.get("link_row_count", 0)) * 3
                + int(candidate.get("keyword_hits", 0)) * 5
                + min(int(candidate.get("row_count", 0)), 20)
            )

            if not best or score > int(best.get("score", -1)):
                best = {
                    "score": score,
                    "frame": frame,
                    "table_index": int(candidate.get("table_index", -1)),
                    "headers": candidate.get("headers", []),
                }

    items: list[dict[str, Any]] = []
    seen_urls: set[str] = set()

    if best and int(best.get("table_index", -1)) >= 0 and int(best.get("score", 0)) > 0:
        headers, rows = _extract_table_rows(best["frame"], best["table_index"])
        for raw_row in rows:
            item = _build_row_item(headers=headers, row=raw_row, base_url=base_url)
            detail_url = item.get("detail_url", "")
            if not detail_url or detail_url in seen_urls:
                continue
            if not _creator_matches(item, creator_filter):
                continue
            seen_urls.add(detail_url)
            items.append(item)
            if len(items) >= safe_limit:
                break

    if not items:
        items = _extract_fallback_links(page=page, base_url=base_url, limit=safe_limit, creator_filter=creator_filter)

    if not items:
        if debug_dir is not None:
            stamp = timestamp_for_filename()
            dump_top_level_dom(page, debug_dir / f"todo_extract_dom_outline_{stamp}.json")
            dump_frame_diagnostics(page, debug_dir / f"todo_extract_frame_diagnostics_{stamp}.json")
            capture_page_artifacts(page, debug_dir, f"todo_extract_failed_{stamp}")
        raise RuntimeError("Unable to locate todo rows. Saved selector diagnostics for investigation.")

    LOGGER.info("Extracted %s todo list rows.", len(items))
    return items[:safe_limit]

