@echo off
REM Quick start script for Oil Trading System
REM Automatically starts all required services

cd /d "C:\Users\itg\Desktop\X"

echo ========================================
echo   Oil Trading System - Quick Start
echo ========================================
echo.

REM Start Redis
echo [1/3] Starting Redis Cache...
start "Redis" cmd /k "cd redis && redis-server.exe redis.windows.conf"
timeout /t 2 /nobreak >nul

REM Start Backend
echo [2/3] Starting Backend API (port 5000)...
start "Backend API" cmd /k "cd src\OilTrading.Api && dotnet run"
timeout /t 10 /nobreak >nul

REM Start Frontend
echo [3/3] Starting Frontend (port 3002+)...
start "Frontend" cmd /k "cd frontend && \"C:\Users\itg\nodejs\npm.cmd\" run dev"

echo.
echo ========================================
echo   System Started!
echo ========================================
echo.
echo Opening application in browser...
timeout /t 5 /nobreak

REM Open browser
start http://localhost:3002

echo Done! Check the console windows for any errors.
