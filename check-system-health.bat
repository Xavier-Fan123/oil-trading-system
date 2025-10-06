@echo off
echo ========================================
echo Oil Trading System - Health Check
echo ========================================
echo.

echo [1/4] Checking Docker...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker not available
    goto :error
) else (
    echo ✅ Docker available
)

echo.
echo [2/4] Checking Redis container...
docker ps | findstr redis >nul
if errorlevel 1 (
    echo ❌ Redis container not running
    echo Starting Redis...
    docker-compose up -d redis
    timeout /t 5 /nobreak
) else (
    echo ✅ Redis container running
)

echo.
echo [3/4] Checking Backend API...
curl -s http://localhost:5000/api/dashboard/overview >nul 2>&1
if errorlevel 1 (
    echo ❌ Backend API not responding
    echo Check if dotnet run is started in src/OilTrading.Api
    goto :error
) else (
    echo ✅ Backend API responding
)

echo.
echo [4/4] Checking Frontend...
curl -s http://localhost:3000 >nul 2>&1
if errorlevel 1 (
    echo ❌ Frontend not responding
    echo Check if npm run dev is started in frontend
    goto :error
) else (
    echo ✅ Frontend responding
)

echo.
echo ========================================
echo ✅ All systems healthy!
echo ========================================
echo - Frontend: http://localhost:3000
echo - Backend: http://localhost:5000
echo - Redis: Port 6379
echo.
goto :end

:error
echo.
echo ========================================
echo ❌ System health check failed!
echo ========================================
echo Please run start-complete-system.bat
echo.

:end
pause