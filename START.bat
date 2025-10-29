@echo off
REM Quick start script for Oil Trading System v2.6.10
REM Automatically starts all required services

cd /d "C:\Users\itg\Desktop\X"

echo ========================================
echo   Oil Trading System - Quick Start v2.6.10
echo ========================================
echo.

REM Start Redis
echo [1/3] Starting Redis Cache Server (port 6379)...
start "Redis" cmd /k "cd /d C:\Users\itg\Desktop\X\redis && redis-server.exe redis.windows.conf"
timeout /t 3 /nobreak >nul
echo Redis started.

REM Start Backend
echo [2/3] Starting Backend API Server (port 5000)...
start "Backend API" cmd /k "cd /d C:\Users\itg\Desktop\X\src\OilTrading.Api && dotnet run"
timeout /t 12 /nobreak >nul
echo Backend API started.

REM Start Frontend
echo [3/3] Starting Frontend React App (port 3002+)...
start "Frontend" cmd /k "cd /d C:\Users\itg\Desktop\X\frontend && \"C:\Users\itg\nodejs\npm.cmd\" run dev"

echo.
echo ========================================
echo   System Started Successfully!
echo ========================================
echo.
echo Access Points:
echo - Frontend:  http://localhost:3002
echo - Backend:   http://localhost:5000
echo - Health:    http://localhost:5000/health
echo - Swagger:   http://localhost:5000/swagger
echo - Redis:     localhost:6379
echo.
echo Opening application in browser...
timeout /t 5 /nobreak

REM Open browser
start http://localhost:3002

echo.
echo Done! Check the 3 console windows for any errors.
echo If you see errors, check the QUICK_START.md or DEBUG_GUIDE.md.
