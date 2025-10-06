@echo off
echo ========================================
echo   WebSocket Issues Fix Script
echo   Oil Trading System v2.5.0
echo ========================================
echo.

echo [INFO] Checking WebSocket configuration issues...

echo [1/4] Killing existing Node.js processes...
taskkill /F /IM node.exe 2>nul
echo Node.js processes terminated.

echo [2/4] Checking if vite.config.ts has correct WebSocket settings...
cd /d "C:\Users\itg\Desktop\X\frontend"

findstr /C:"port: 3001" vite.config.ts >nul
if %errorlevel%==0 (
    echo [OK] WebSocket HMR port configuration found.
) else (
    echo [WARNING] WebSocket HMR port not configured correctly.
    echo Please ensure vite.config.ts has: hmr: { port: 3001 }
)

findstr /C:"usePolling: true" vite.config.ts >nul
if %errorlevel%==0 (
    echo [OK] File polling configuration found.
) else (
    echo [WARNING] File polling not configured correctly.
    echo Please ensure vite.config.ts has: watch: { usePolling: true }
)

echo [3/4] Checking port availability...
netstat -an | findstr :3000 >nul
if %errorlevel%==0 (
    echo [WARNING] Port 3000 is in use. Frontend may have port conflicts.
) else (
    echo [OK] Port 3000 is available.
)

netstat -an | findstr :3001 >nul
if %errorlevel%==0 (
    echo [WARNING] Port 3001 is in use. WebSocket HMR may have conflicts.
) else (
    echo [OK] Port 3001 is available for WebSocket HMR.
)

echo [4/4] Restarting frontend with WebSocket fixes...
echo Starting frontend server...
echo Note: This will open a new command window for the frontend.

start "Oil Trading Frontend - WebSocket Fixed" cmd /k "cd /d \"C:\Users\itg\Desktop\X\frontend\" && echo Starting with WebSocket fixes applied... && \"D:\npm.cmd\" run dev"

echo.
echo ========================================
echo   WebSocket Fix Complete!
echo ========================================
echo.
echo If WebSocket errors persist:
echo 1. Check Windows Firewall settings
echo 2. Disable antivirus real-time protection temporarily
echo 3. Try running as Administrator
echo 4. Check CLAUDE.md for detailed troubleshooting
echo.
echo Press any key to exit...
pause >nul