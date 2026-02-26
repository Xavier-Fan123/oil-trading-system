from __future__ import annotations

import logging
import sys
import time
from pathlib import Path

from playwright.sync_api import Browser, BrowserContext, Frame, Page

from .utils import capture_page_artifacts, iter_frames, looks_like_login_page, resolve_url, safe_count, wait_with_jitter

LOGGER = logging.getLogger("oa_scraper")

USER_SELECTORS = [
    "input[name='loginid']",
    "input[id='loginid']",
    "input[name='username']",
    "input[id='username']",
    "input[name*='user']",
    "input[id*='user']",
    "input[name*='login']",
    "input[id*='login']",
    "input[type='text']",
]

PASS_SELECTORS = [
    "input[name='userpassword']",
    "input[id='userpassword']",
    "input[name='password']",
    "input[id='password']",
    "input[type='password']",
]

LOGIN_BUTTON_SELECTORS = [
    "button#submit",
    "button[type='submit']",
    "input[type='submit']",
    "input[type='button'][value*='登录']",
    "button:has-text('登录')",
    "a:has-text('登录')",
    "[id*='loginbtn']",
    "[id*='submit']",
]

CAPTCHA_SELECTORS = [
    "input[name*='captcha']",
    "input[id*='captcha']",
    "img[src*='captcha']",
    "input[name*='verify']",
    "input[id*='verify']",
    "text=验证码",
]


def load_or_create_context(browser: Browser, state_path: Path) -> BrowserContext:
    if state_path.exists():
        LOGGER.info("Loading storage state from %s", state_path)
        return browser.new_context(storage_state=str(state_path), locale="zh-CN")

    LOGGER.info("No storage state found at %s; creating a fresh browser context", state_path)
    return browser.new_context(locale="zh-CN")


def save_state(context: BrowserContext, state_path: Path) -> None:
    context.storage_state(path=str(state_path))
    LOGGER.info("Saved storage state to %s", state_path)


def _first_selector(frame: Frame, selectors: list[str]) -> str | None:
    for selector in selectors:
        if safe_count(frame, selector) > 0:
            return selector
    return None


def _find_login_frame(page: Page) -> tuple[Frame | None, str | None, str | None]:
    for frame in iter_frames(page):
        pass_selector = _first_selector(frame, PASS_SELECTORS)
        if not pass_selector:
            continue
        user_selector = _first_selector(frame, USER_SELECTORS)
        if user_selector:
            return frame, user_selector, pass_selector
    return None, None, None


def _wait_for_login_form(page: Page, timeout_ms: int) -> tuple[Frame | None, str | None, str | None]:
    deadline = time.monotonic() + max(timeout_ms, 1) / 1000
    while time.monotonic() < deadline:
        frame, user_selector, pass_selector = _find_login_frame(page)
        if frame and user_selector and pass_selector:
            return frame, user_selector, pass_selector

        if not looks_like_login_page(page):
            # If OA loaded a logged-in homepage, stop waiting.
            return None, None, None

        page.wait_for_timeout(500)

    return None, None, None


def _captcha_present(frame: Frame) -> bool:
    return any(safe_count(frame, selector) > 0 for selector in CAPTCHA_SELECTORS)


def _pause_for_manual_step(message: str) -> None:
    if not sys.stdin or not sys.stdin.isatty():
        raise RuntimeError(
            "Captcha/manual login interaction is required, but stdin is non-interactive. "
            "Run with an interactive terminal and --headless false."
        )
    print(message)
    input()


def _submit_login(frame: Frame, pass_selector: str) -> None:
    for selector in LOGIN_BUTTON_SELECTORS:
        if safe_count(frame, selector) <= 0:
            continue
        try:
            frame.locator(selector).first.click(timeout=4000)
            return
        except Exception:  # noqa: BLE001
            continue

    frame.locator(pass_selector).first.press("Enter")


def _session_is_authenticated(page: Page, base_url: str, timeout_ms: int) -> bool:
    probe_url = resolve_url(base_url, "/spa/workflow/static/index.html#/main/workflow/listDoing")
    try:
        page.goto(probe_url, wait_until="domcontentloaded", timeout=timeout_ms)
        page.wait_for_timeout(3000)
    except Exception:  # noqa: BLE001
        return False

    # If probe route is still a login page, session is not authenticated.
    if looks_like_login_page(page):
        return False

    # Any workflow/todo route or populated table indicates authenticated session.
    url_lower = (page.url or "").lower()
    if "workflow" in url_lower or "listdoing" in url_lower:
        return True

    for frame in iter_frames(page):
        if safe_count(frame, "table tr") > 0:
            return True

    return False


def login_if_needed(
    page: Page,
    base_url: str,
    username: str,
    password: str,
    timeout_ms: int,
    artifacts_dir: Path,
) -> None:
    page.goto(base_url, wait_until="domcontentloaded", timeout=timeout_ms)
    wait_with_jitter(page, min_ms=500, max_ms=1000)

    if not looks_like_login_page(page):
        LOGGER.info("Session appears authenticated; no login needed.")
        return

    frame, user_selector, pass_selector = _wait_for_login_form(page, timeout_ms=timeout_ms)
    if not frame or not user_selector or not pass_selector:
        if _session_is_authenticated(page=page, base_url=base_url, timeout_ms=timeout_ms):
            LOGGER.info("Login form not found, but session probe indicates authenticated state.")
            return

        capture_page_artifacts(page, artifacts_dir, "login_selectors_missing")
        raise RuntimeError("Unable to locate OA login form selectors.")

    frame.locator(user_selector).first.fill(username, timeout=timeout_ms)
    frame.locator(pass_selector).first.fill(password, timeout=timeout_ms)

    if _captcha_present(frame):
        _pause_for_manual_step("Captcha detected. Please solve it in the browser, then press Enter to continue...")

    _submit_login(frame, pass_selector)
    try:
        page.wait_for_load_state("domcontentloaded", timeout=timeout_ms)
    except Exception:  # noqa: BLE001
        pass
    wait_with_jitter(page, min_ms=700, max_ms=1300)

    if looks_like_login_page(page):
        # One manual follow-up for token/captcha flows without brute force attempts.
        retry_frame, _, retry_pass_selector = _wait_for_login_form(page, timeout_ms=min(15000, timeout_ms))
        if retry_frame and retry_pass_selector and _captcha_present(retry_frame):
            _pause_for_manual_step(
                "Still on login page. Complete captcha or extra fields, then press Enter to retry submit..."
            )
            _submit_login(retry_frame, retry_pass_selector)
            try:
                page.wait_for_load_state("domcontentloaded", timeout=timeout_ms)
            except Exception:  # noqa: BLE001
                pass
            wait_with_jitter(page, min_ms=700, max_ms=1300)

    if looks_like_login_page(page):
        capture_page_artifacts(page, artifacts_dir, "login_failed")
        raise RuntimeError("Login failed or redirected back to login page.")

    LOGGER.info("Login completed successfully.")
