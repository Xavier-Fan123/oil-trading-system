from __future__ import annotations

import json
import logging
import random
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable
from urllib.parse import urljoin

from playwright.sync_api import Frame, Page

LOGGER_NAME = "oa_scraper"


def configure_logging(log_file: Path | None = None) -> logging.Logger:
    logger = logging.getLogger(LOGGER_NAME)
    if logger.handlers:
        return logger

    logger.setLevel(logging.INFO)
    formatter = logging.Formatter("%(asctime)s %(levelname)s %(message)s")

    stream_handler = logging.StreamHandler()
    stream_handler.setFormatter(formatter)
    logger.addHandler(stream_handler)

    if log_file:
        ensure_dir(log_file.parent)
        file_handler = logging.FileHandler(log_file, encoding="utf-8")
        file_handler.setFormatter(formatter)
        logger.addHandler(file_handler)

    return logger


def get_logger() -> logging.Logger:
    return logging.getLogger(LOGGER_NAME)


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def timestamp_for_filename() -> str:
    return datetime.now().strftime("%Y%m%d_%H%M%S")


def now_utc_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def clean_text(value: str | None) -> str:
    return re.sub(r"\s+", " ", value or "").strip()


def clip_text(value: str | None, limit: int = 1800) -> str:
    return clean_text(value)[:limit]


def sanitize_filename(value: str) -> str:
    cleaned = re.sub(r"[^\w.\-]+", "_", value.strip())
    return cleaned or "artifact"


def resolve_url(base_url: str, maybe_relative: str) -> str:
    if not maybe_relative:
        return base_url
    return urljoin(base_url.rstrip("/") + "/", maybe_relative)


def wait_with_jitter(page: Page, min_ms: int = 300, max_ms: int = 900) -> None:
    page.wait_for_timeout(random.randint(min_ms, max_ms))


def safe_count(frame: Frame, selector: str) -> int:
    try:
        return frame.locator(selector).count()
    except Exception:  # noqa: BLE001
        return 0


def iter_frames(page: Page) -> Iterable[Frame]:
    return page.frames


def looks_like_login_page(page: Page) -> bool:
    url = (page.url or "").lower()
    if "login" in url or "sso" in url:
        return True

    for frame in iter_frames(page):
        has_password = safe_count(frame, "input[type='password']") > 0
        has_username = safe_count(
            frame,
            "input[name*='user'], input[id*='user'], input[name*='login'], input[id*='login']",
        ) > 0
        has_login_button = (
            safe_count(frame, "button:has-text('登录')") > 0
            or safe_count(frame, "input[type='submit'][value*='登录']") > 0
            or safe_count(frame, "input[type='button'][value*='登录']") > 0
        )
        if has_password and (has_username or has_login_button):
            return True
    return False


def capture_page_artifacts(page: Page, outdir: Path, prefix: str) -> dict[str, str]:
    ensure_dir(outdir)
    stamp = timestamp_for_filename()
    safe_prefix = sanitize_filename(prefix)
    screenshot_path = outdir / f"{safe_prefix}_{stamp}.png"
    html_path = outdir / f"{safe_prefix}_{stamp}.html"
    logger = get_logger()

    try:
        page.screenshot(path=str(screenshot_path), full_page=True)
    except Exception as exc:  # noqa: BLE001
        logger.warning("Failed to save screenshot: %s", exc)

    try:
        html_path.write_text(page.content(), encoding="utf-8")
    except Exception as exc:  # noqa: BLE001
        logger.warning("Failed to save HTML dump: %s", exc)

    return {"screenshot": str(screenshot_path), "html": str(html_path)}


def dump_top_level_dom(page: Page, out_file: Path) -> None:
    logger = get_logger()
    payload: list[dict[str, Any]] = []

    for frame in page.frames:
        try:
            nodes = frame.evaluate(
                """
                () => {
                    const normalize = (value) => (value || "").replace(/\\s+/g, " ").trim();
                    const root = document.body || document.documentElement;
                    if (!root) return [];
                    return Array.from(root.children).slice(0, 120).map((node) => ({
                        tag: (node.tagName || "").toLowerCase(),
                        id: node.id || "",
                        className: typeof node.className === "string" ? node.className : "",
                        textPreview: normalize(node.textContent || "").slice(0, 120),
                    }));
                }
                """
            )
            payload.append({"frame_url": frame.url, "nodes": nodes})
        except Exception as exc:  # noqa: BLE001
            payload.append({"frame_url": frame.url, "error": str(exc)})

    ensure_dir(out_file.parent)
    out_file.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    logger.info("Wrote DOM outline to %s", out_file)


def dump_frame_diagnostics(page: Page, out_file: Path) -> None:
    payload = []
    for frame in page.frames:
        payload.append(
            {
                "url": frame.url,
                "title_like": safe_count(frame, "h1, h2, h3, [class*='title'], [id*='title']"),
                "table_rows": safe_count(frame, "table tr"),
                "todo_links": safe_count(
                    frame,
                    "a[href*='request'], a[href*='workflow'], a[onclick*='request'], a[onclick*='workflow']",
                ),
                "todo_keywords": safe_count(frame, "text=待办"),
            }
        )

    ensure_dir(out_file.parent)
    out_file.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def pick_field_by_keywords(field_map: dict[str, str], keywords: Iterable[str]) -> str:
    normalized = {
        clean_text(key).lower(): clean_text(value)
        for key, value in field_map.items()
        if clean_text(value)
    }

    for keyword in keywords:
        target = keyword.lower()
        for key, value in normalized.items():
            if target in key:
                return value
    return ""


def extract_request_like_id(text: str) -> str:
    match = re.search(r"(?i)(?:requestid|workflowid|request_id|workflow_id|id)=([^&\\s]+)", text or "")
    if match:
        return clean_text(match.group(1))
    return ""
