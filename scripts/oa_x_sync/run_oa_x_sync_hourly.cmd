@echo off
setlocal
cd /d "%~dp0"
set "PY_EXE=%~dp0.venv\Scripts\python.exe"
if not exist "%PY_EXE%" (
  echo ERROR: Missing Python venv at %PY_EXE%
  exit /b 9009
)
"%PY_EXE%" "%~dp0run_oa_x_sync.py" --interval-minutes 0 --headless true --dry-run false --limit 200 --max-pages 30 --max-attachments 6 --timeout 45000 --state-file "%~dp0output\oa_x_sync_state.json" --outdir "%~dp0output\oa_x_sync"
