@echo off
echo Starting Redis Server...
cd /d "C:\Users\itg\Desktop\X\redis"

echo Checking if Redis is already running...
tasklist | findstr "redis-server" > nul
if %errorlevel% equ 0 (
    echo Redis is already running.
) else (
    echo Starting Redis server...
    start /min "Redis Server" redis-server.exe redis.windows.conf
    timeout /t 3 /nobreak > nul
    echo Redis server started.
)

echo Testing Redis connection...
redis-cli.exe ping
if %errorlevel% equ 0 (
    echo Redis is responding correctly.
) else (
    echo Redis connection test failed.
)
pause