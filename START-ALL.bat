@echo off
REM Oil Trading System - Complete Startup Script
REM Start Redis, Backend API, and Frontend in separate windows
REM No VS Code termination - VS Code stays open

setlocal enabledelayedexpansion
cd /d "C:\Users\itg\Desktop\X"

echo.
echo ============================================
echo  Oil Trading System - Complete Startup
echo ============================================
echo.

REM Only kill dotnet, node, redis - NOT VS Code
echo Cleaning up old processes (NOT closing VS Code)...
taskkill /F /IM dotnet.exe >nul 2>&1
taskkill /F /IM node.exe >nul 2>&1
taskkill /F /IM redis-server.exe >nul 2>&1
timeout /t 2 /nobreak >nul

REM Start Redis
echo.
echo [1/3] Starting Redis Cache Server (port 6379)...
start "Redis-Cache" cmd /k "cd /d C:\Users\itg\Desktop\X\redis && redis-server.exe redis.windows.conf"
timeout /t 3 /nobreak >nul

REM Start Backend API
echo [2/3] Starting Backend API (port 5000)...
start "Backend-API" cmd /k "cd /d C:\Users\itg\Desktop\X\src\OilTrading.Api && dotnet run"
timeout /t 10 /nobreak >nul

REM Start Frontend
echo [3/3] Starting Frontend React App (port 3002)...
start "Frontend-App" cmd /k "cd /d C:\Users\itg\Desktop\X\frontend && npm run dev"
timeout /t 8 /nobreak >nul

echo.
echo ============================================
echo  All Services Started!
echo ============================================
echo.
echo Access Points:
echo   Frontend:  http://localhost:3002
echo   Backend:   http://localhost:5000
echo   Health:    http://localhost:5000/health
echo   Swagger:   http://localhost:5000/swagger
echo.
echo Opening frontend in browser...
timeout /t 2 /nobreak >nul
start http://localhost:3002

echo.
echo Done! Check the 3 new terminal windows for status.
echo VS Code remains open and unaffected.
echo.
pause
