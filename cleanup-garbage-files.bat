@echo off
echo Starting cleanup of garbage and outdated files...
echo.

REM Remove Redis dump files
echo [1/8] Removing Redis dump files...
if exist "dump.rdb" del /q "dump.rdb" 2>nul

REM Remove log files
echo [2/8] Removing old log files...
if exist "logs\" (
    del /q "logs\*.txt" 2>nul
    echo Removed files from logs directory
)

if exist "src\OilTrading.Api\logs\" (
    del /q "src\OilTrading.Api\logs\*.txt" 2>nul
    echo Removed API log files
)

REM Remove Chinese-named files that cause encoding issues
echo [3/8] Removing files with encoding issues...
if exist "创建测试数据.sql" del /q "创建测试数据.sql" 2>nul
if exist "本地部署验证.bat" del /q "本地部署验证.bat" 2>nul
if exist "第一周部署验证报告.md" del /q "第一周部署验证报告.md" 2>nul
if exist "第一周验证清单.md" del /q "第一周验证清单.md" 2>nul
if exist "部署验证指南.md" del /q "部署验证指南.md" 2>nul
if exist "鍚姩绯荤粺.bat" del /q "鍚姩绯荤粺.bat" 2>nul
if exist "閮ㄧ讲楠岃瘉鎶ュ憡.txt" del /q "閮ㄧ讲楠岃瘉鎶ュ憡.txt" 2>nul

REM Remove build artifacts (bin and obj folders)
echo [4/8] Removing build artifacts...
for /d /r . %%d in (bin obj) do @if exist "%%d" rd /s /q "%%d" 2>nul

REM Remove unnecessary zip files
echo [5/8] Removing zip archives...
if exist "redis-windows.zip" del /q "redis-windows.zip" 2>nul

REM Remove duplicate documentation files
echo [6/8] Removing duplicate documentation...
if exist "DEPLOYMENT_GUIDE_EN.md" del /q "DEPLOYMENT_GUIDE_EN.md" 2>nul

REM Clean up backup directories if empty
echo [7/8] Cleaning backup directories...
if exist "backups\" (
    rd /q "backups" 2>nul || echo backups directory contains files, keeping it
)

REM Remove old Windows batch test files
echo [8/8] Removing old test files...
if exist "test-postgresql-production.bat" del /q "test-postgresql-production.bat" 2>nul

echo.
echo Cleanup completed successfully!
echo The following types of files were removed:
echo - Redis dump files
echo - Old log files
echo - Files with Chinese characters causing encoding issues
echo - Build artifacts in bin/obj folders
echo - Duplicate documentation
echo - Temporary zip archives
echo.
echo System is now cleaner and ready for use.
pause