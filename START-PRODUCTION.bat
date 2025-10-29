@echo off
REM Production startup script for Oil Trading System v2.6.10
REM Starts all services: Redis, Backend API, Frontend

echo ========================================
echo   Oil Trading System v2.6.10
echo   Production Ready - Full Stack Startup
echo ========================================
echo.

cd /d "C:\Users\itg\Desktop\X"

echo [1/4] Starting Redis Cache Server...
echo Starting Redis on port 6379...
powershell -Command "Start-Process -FilePath 'C:\Users\itg\Desktop\X\redis\redis-server.exe' -ArgumentList 'C:\Users\itg\Desktop\X\redis\redis.windows.conf' -WindowStyle Normal" 2>nul
timeout /t 3 /nobreak >nul
echo Redis cache server started.

echo [2/4] Starting Backend API Server...
echo Starting .NET Core API on port 5000...
start "Oil Trading API" cmd /k "cd /d C:\Users\itg\Desktop\X\src\OilTrading.Api && dotnet run"
timeout /t 12 /nobreak >nul
echo Backend API server started.

echo [3/4] Starting Frontend React Application...
echo Starting React frontend on port 3002+...
start "Oil Trading Frontend" cmd /k "cd /d C:\Users\itg\Desktop\X\frontend && \"C:\Users\itg\nodejs\npm.cmd\" run dev"
timeout /t 8 /nobreak >nul
echo Frontend application started.

echo.
echo ========================================
echo   System Startup Complete!
echo ========================================
echo.
echo Application Access Points:
echo - Frontend Application: http://localhost:3002
echo - Backend API Server:   http://localhost:5000
echo - API Health Check:     http://localhost:5000/health
echo - API Documentation:    http://localhost:5000/swagger
echo - Redis Cache Server:   localhost:6379
echo.
echo System Services:
echo - Contract Management (Purchase/Sales)
echo - Shipping Operations Management
echo - Contract Matching and Natural Hedging
echo - Risk Management Dashboard
echo - Settlement Processing
echo - Real-time Position Monitoring
echo.
echo Latest Updates (v2.6.10):
echo - Fixed Shipping Operation date validation
echo - Fixed Quantity Unit dropdown (MT, BBL only)
echo - Backend compilation issues resolved
echo - Debug logging added for troubleshooting
echo.
echo Opening application in browser...
timeout /t 3 /nobreak >nul
start http://localhost:3002

echo Done! All services should now be running.
echo Check the 3 console windows for any errors.
echo.
