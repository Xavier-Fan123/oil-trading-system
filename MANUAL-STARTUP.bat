@echo off
echo ========================================
echo   Oil Trading System v2.4.1
echo   Manual Startup Instructions
echo ========================================
echo.

echo STEP 1: Start Redis Cache (REQUIRED)
echo Command: powershell -Command "Start-Process -FilePath 'C:\Users\itg\Desktop\X\redis\redis-server.exe' -ArgumentList 'C:\Users\itg\Desktop\X\redis\redis.windows.conf' -WindowStyle Hidden"
echo.

echo STEP 2: Start Backend API
echo Open Command Prompt, then run:
echo cd "C:\Users\itg\Desktop\X\src\OilTrading.Api"
echo dotnet run
echo.

echo STEP 3: Start Frontend (AS ADMINISTRATOR)
echo Open Command Prompt as Administrator, then run:
echo cd "C:\Users\itg\Desktop\X\frontend"  
echo npm run dev
echo.

echo TROUBLESHOOTING:
echo - If npm fails: Use "D:\npm.cmd" run dev
echo - If Node.js not found: Use "D:\node.exe" --version to test
echo - Frontend will auto-select port if 3000 is busy
echo.

echo Access Points:
echo - Frontend: http://localhost:3000 (or auto-selected)
echo - Backend API: http://localhost:5000
echo - API Docs: http://localhost:5000/swagger
echo.

pause