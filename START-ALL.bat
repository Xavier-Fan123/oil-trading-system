@echo off
REM Oil Trading System - Complete Startup Script
REM Starts: Redis Server + Backend API + Frontend React App

setlocal enabledelayedexpansion

echo.
echo ============================================
echo Oil Trading System - Full Stack Startup
echo ============================================
echo.

REM Kill any existing processes and clean Vite cache
echo Cleaning up existing processes and Vite cache...
taskkill /f /im dotnet.exe >nul 2>&1
taskkill /f /im node.exe >nul 2>&1
if exist "C:\Users\itg\Desktop\X\frontend\node_modules\.vite" (
    rmdir /s /q "C:\Users\itg\Desktop\X\frontend\node_modules\.vite"
    echo [OK] Vite cache cleaned
)
timeout /t 2 >nul

REM Start Redis in a new window
echo [1/3] Starting Redis Cache Server on port 6379...
start "Redis Server" /d "C:\Users\itg\Desktop\X\redis" "C:\Users\itg\Desktop\X\redis\redis-server.exe" "C:\Users\itg\Desktop\X\redis\redis.windows.conf"
timeout /t 3 >nul

REM Start Backend API in a new window
echo [2/3] Starting Backend API on port 5000...
start "Backend API" cmd /k "cd /d C:\Users\itg\Desktop\X\src\OilTrading.Api && dotnet run --no-build"
timeout /t 10 >nul

REM Start Frontend React App in a new window (with EMFILE fix)
echo [3/3] Starting Frontend React App on port 3002 (with optimized file watching)...
start "Frontend App" cmd /k "cd /d C:\Users\itg\Desktop\X\frontend && npm run dev"
timeout /t 8 >nul

echo.
echo ============================================
echo System Startup Complete!
echo ============================================
echo.
echo Application Access Points:
echo - Frontend:        http://localhost:3002
echo - Backend API:     http://localhost:5000
echo - API Health:      http://localhost:5000/health
echo - API Swagger:     http://localhost:5000/swagger
echo - Redis:           localhost:6379
echo.
echo Opening browser in 5 seconds...
timeout /t 5 >nul
start http://localhost:3002
echo.
echo System is running. All windows will remain open.
echo Press CTRL+C in each window to stop services.
echo.
pause

endlocal
