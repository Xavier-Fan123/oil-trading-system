@echo off
setlocal enabledelayedexpansion

echo ================================================
echo 🧪 Oil Trading System - Complete Test Suite
echo ================================================
echo Starting comprehensive test execution with coverage analysis...
echo.

:: Set variables
set "SOLUTION_DIR=%~dp0"
set "TEST_RESULTS_DIR=%SOLUTION_DIR%TestResults"
set "COVERAGE_DIR=%TEST_RESULTS_DIR%\Coverage"
set "TIMESTAMP=%date:~-4,4%-%date:~-10,2%-%date:~-7,2%_%time:~0,2%-%time:~3,2%-%time:~6,2%"
set "TIMESTAMP=%TIMESTAMP: =0%"

:: Create directories
if not exist "%TEST_RESULTS_DIR%" mkdir "%TEST_RESULTS_DIR%"
if not exist "%COVERAGE_DIR%" mkdir "%COVERAGE_DIR%"

echo 📁 Test results will be saved to: %TEST_RESULTS_DIR%
echo 📊 Coverage reports will be saved to: %COVERAGE_DIR%
echo ⏰ Test run timestamp: %TIMESTAMP%
echo.

:: Check for required tools
echo 🔍 Checking for required tools...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK not found. Please install .NET 9 SDK.
    pause
    exit /b 1
)

:: Install ReportGenerator tool if not present
dotnet tool list -g | findstr reportgenerator >nul
if %errorlevel% neq 0 (
    echo 📦 Installing ReportGenerator tool...
    dotnet tool install -g dotnet-reportgenerator-globaltool
)

echo ✅ Required tools are available
echo.

:: Clean previous test results
echo 🧹 Cleaning previous test results...
if exist "%TEST_RESULTS_DIR%" (
    rd /s /q "%TEST_RESULTS_DIR%" >nul 2>&1
    mkdir "%TEST_RESULTS_DIR%"
    mkdir "%COVERAGE_DIR%"
)

:: Build the solution
echo 🔨 Building solution...
dotnet build "%SOLUTION_DIR%OilTrading.sln" --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ❌ Build failed. Please fix compilation errors.
    pause
    exit /b %errorlevel%
)
echo ✅ Build completed successfully
echo.

:: Run tests with coverage
echo 🧪 Running test suite with coverage analysis...
echo.

:: Set coverage collection parameters
set "COVERAGE_COLLECT=--collect:"XPlat Code Coverage""
set "COVERAGE_SETTINGS=--settings:"%SOLUTION_DIR%tests\CodeCoverage.runsettings""
set "TEST_LOGGER=--logger:trx --logger:console;verbosity=detailed"
set "RESULTS_DIR=--results-directory:"%TEST_RESULTS_DIR%""

:: Run Unit Tests
echo 📋 Phase 1: Running Unit Tests...
dotnet test "%SOLUTION_DIR%tests\OilTrading.Tests\OilTrading.Tests.csproj" ^
    %COVERAGE_COLLECT% ^
    %TEST_LOGGER% ^
    %RESULTS_DIR% ^
    --configuration Release ^
    --no-build ^
    --verbosity normal

if %errorlevel% neq 0 (
    echo ⚠️ Some unit tests failed. Continuing with other test phases...
    set "UNIT_TEST_FAILED=1"
) else (
    echo ✅ Unit tests completed successfully
)
echo.

:: Run Integration Tests
echo 📋 Phase 2: Running Integration Tests...
dotnet test "%SOLUTION_DIR%tests\OilTrading.IntegrationTests\OilTrading.IntegrationTests.csproj" ^
    %COVERAGE_COLLECT% ^
    %TEST_LOGGER% ^
    %RESULTS_DIR% ^
    --configuration Release ^
    --no-build ^
    --verbosity normal

if %errorlevel% neq 0 (
    echo ⚠️ Some integration tests failed. Continuing with other test phases...
    set "INTEGRATION_TEST_FAILED=1"
) else (
    echo ✅ Integration tests completed successfully
)
echo.

:: Run Performance Benchmarks
echo 📋 Phase 3: Running Performance Benchmarks...
if exist "%SOLUTION_DIR%tests\OilTrading.Benchmarks\OilTrading.Benchmarks.csproj" (
    dotnet run --project "%SOLUTION_DIR%tests\OilTrading.Benchmarks\OilTrading.Benchmarks.csproj" ^
        --configuration Release ^
        --no-build ^
        -- --artifacts "%TEST_RESULTS_DIR%\Benchmarks"
    
    if %errorlevel% neq 0 (
        echo ⚠️ Some benchmarks failed. Continuing...
        set "BENCHMARK_FAILED=1"
    ) else (
        echo ✅ Performance benchmarks completed successfully
    )
) else (
    echo ℹ️ No benchmark project found, skipping performance tests
)
echo.

:: Generate Coverage Report
echo 📊 Generating coverage report...
set "COVERAGE_FILES=%TEST_RESULTS_DIR%\**\coverage.cobertura.xml"
reportgenerator ^
    "-reports:%COVERAGE_FILES%" ^
    "-targetdir:%COVERAGE_DIR%" ^
    "-reporttypes:Html;HtmlSummary;Badges;TextSummary;Cobertura" ^
    "-historydir:%COVERAGE_DIR%\History" ^
    "-title:Oil Trading System - Test Coverage Report" ^
    "-tag:%TIMESTAMP%" ^
    -verbosity:Info

if %errorlevel% neq 0 (
    echo ⚠️ Coverage report generation failed
    set "COVERAGE_FAILED=1"
) else (
    echo ✅ Coverage report generated successfully
)
echo.

:: Run K6 Performance Tests (if available)
echo 📋 Phase 4: Running K6 Performance Tests...
if exist "%SOLUTION_DIR%tests\performance\load-test.js" (
    where k6 >nul 2>&1
    if !errorlevel! equ 0 (
        echo 🚀 Starting API for performance testing...
        start "API Server" dotnet run --project "%SOLUTION_DIR%src\OilTrading.Api\OilTrading.Api.csproj" --urls "http://localhost:5000"
        
        :: Wait for API to start
        timeout /t 10 /nobreak >nul
        
        echo 🧪 Running K6 performance tests...
        k6 run "%SOLUTION_DIR%tests\performance\load-test.js" ^
            --out json="%TEST_RESULTS_DIR%\performance-results.json" ^
            --summary-export="%TEST_RESULTS_DIR%\performance-summary.json"
        
        if !errorlevel! neq 0 (
            echo ⚠️ K6 performance tests had issues
            set "K6_FAILED=1"
        ) else (
            echo ✅ K6 performance tests completed
        )
        
        :: Stop the API server
        taskkill /f /im dotnet.exe /fi "WINDOWTITLE eq API Server*" >nul 2>&1
    ) else (
        echo ℹ️ K6 not installed, skipping performance tests
        echo    Install K6 from: https://k6.io/docs/get-started/installation/
    )
) else (
    echo ℹ️ No K6 performance tests found
)
echo.

:: Generate Test Summary
echo 📋 Generating test summary...
set "SUMMARY_FILE=%TEST_RESULTS_DIR%\TestSummary_%TIMESTAMP%.txt"

echo ================================================ > "%SUMMARY_FILE%"
echo 🧪 Oil Trading System - Test Results Summary >> "%SUMMARY_FILE%"
echo ================================================ >> "%SUMMARY_FILE%"
echo Test Run Date: %date% %time% >> "%SUMMARY_FILE%"
echo Timestamp: %TIMESTAMP% >> "%SUMMARY_FILE%"
echo. >> "%SUMMARY_FILE%"

echo 📊 TEST RESULTS: >> "%SUMMARY_FILE%"
if not defined UNIT_TEST_FAILED (
    echo ✅ Unit Tests: PASSED >> "%SUMMARY_FILE%"
) else (
    echo ❌ Unit Tests: FAILED >> "%SUMMARY_FILE%"
)

if not defined INTEGRATION_TEST_FAILED (
    echo ✅ Integration Tests: PASSED >> "%SUMMARY_FILE%"
) else (
    echo ❌ Integration Tests: FAILED >> "%SUMMARY_FILE%"
)

if not defined BENCHMARK_FAILED (
    if exist "%TEST_RESULTS_DIR%\Benchmarks" (
        echo ✅ Performance Benchmarks: COMPLETED >> "%SUMMARY_FILE%"
    ) else (
        echo ℹ️ Performance Benchmarks: SKIPPED >> "%SUMMARY_FILE%"
    )
) else (
    echo ❌ Performance Benchmarks: FAILED >> "%SUMMARY_FILE%"
)

if not defined K6_FAILED (
    if exist "%TEST_RESULTS_DIR%\performance-results.json" (
        echo ✅ K6 Performance Tests: COMPLETED >> "%SUMMARY_FILE%"
    ) else (
        echo ℹ️ K6 Performance Tests: SKIPPED >> "%SUMMARY_FILE%"
    )
) else (
    echo ❌ K6 Performance Tests: FAILED >> "%SUMMARY_FILE%"
)

if not defined COVERAGE_FAILED (
    echo ✅ Coverage Report: GENERATED >> "%SUMMARY_FILE%"
) else (
    echo ❌ Coverage Report: FAILED >> "%SUMMARY_FILE%"
)

echo. >> "%SUMMARY_FILE%"
echo 📁 ARTIFACTS LOCATION: >> "%SUMMARY_FILE%"
echo Test Results: %TEST_RESULTS_DIR% >> "%SUMMARY_FILE%"
echo Coverage Report: %COVERAGE_DIR%\index.html >> "%SUMMARY_FILE%"
if exist "%TEST_RESULTS_DIR%\Benchmarks" (
    echo Benchmark Results: %TEST_RESULTS_DIR%\Benchmarks >> "%SUMMARY_FILE%"
)
if exist "%TEST_RESULTS_DIR%\performance-summary.json" (
    echo Performance Results: %TEST_RESULTS_DIR%\performance-summary.json >> "%SUMMARY_FILE%"
)

:: Display Coverage Summary
if exist "%COVERAGE_DIR%\Summary.txt" (
    echo. >> "%SUMMARY_FILE%"
    echo 📊 COVERAGE SUMMARY: >> "%SUMMARY_FILE%"
    type "%COVERAGE_DIR%\Summary.txt" >> "%SUMMARY_FILE%"
)

:: Display final results
echo.
echo ================================================
echo 🎯 TEST EXECUTION COMPLETED
echo ================================================

type "%SUMMARY_FILE%"

echo.
echo 📁 Detailed results available at:
echo    Test Results: %TEST_RESULTS_DIR%
if not defined COVERAGE_FAILED (
    echo    Coverage Report: %COVERAGE_DIR%\index.html
)

:: Check overall status
set "OVERALL_FAILED=0"
if defined UNIT_TEST_FAILED set "OVERALL_FAILED=1"
if defined INTEGRATION_TEST_FAILED set "OVERALL_FAILED=1"

if %OVERALL_FAILED% equ 0 (
    echo.
    echo ✅ All critical tests passed successfully!
    echo 🎉 Ready for deployment
) else (
    echo.
    echo ⚠️ Some tests failed. Please review the results.
    echo 🔍 Check individual test reports for details
)

echo.
echo 💡 To view the coverage report, open: %COVERAGE_DIR%\index.html
echo 💡 To view detailed test results, check: %TEST_RESULTS_DIR%
echo.

:: Open coverage report if successful
if not defined COVERAGE_FAILED (
    echo 🌐 Opening coverage report in default browser...
    start "" "%COVERAGE_DIR%\index.html"
)

echo Press any key to exit...
pause >nul

exit /b %OVERALL_FAILED%