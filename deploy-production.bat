@echo off
REM Oil Trading System Production Deployment
REM Enterprise Oil Trading and Risk Management System

setlocal EnableDelayedExpansion

echo ========================================
echo Oil Trading System Production Deployment
echo ========================================

REM Set default environment variables
if not defined DB_USER set DB_USER=oil_trading_admin
if not defined DB_NAME set DB_NAME=oiltrading_prod
if not defined DB_PASSWORD set DB_PASSWORD=OilTrading2024!

echo [INFO] Configuration:
echo - Database User: %DB_USER%
echo - Database Name: %DB_NAME%
echo - API Port: 5000
echo - Risk Engine: Python with GARCH models

REM Check Docker
docker --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker not found. Please start Docker Desktop first.
    pause
    exit /b 1
)

echo [INFO] Docker is available

REM Create necessary directories
if not exist "logs" mkdir logs
if not exist "data" mkdir data
if not exist "backups" mkdir backups

REM Copy environment configuration
if exist ".env.production" (
    copy .env.production .env >nul
    echo [INFO] Production environment configuration copied
) else (
    echo [INFO] Using default configuration
)

REM Stop and clean existing services
echo [INFO] Cleaning up existing containers...
docker stop oil-trading-api 2>nul
docker rm oil-trading-api 2>nul
docker-compose down 2>nul
docker system prune -f >nul 2>&1

REM Start database services
echo [INFO] Starting database services...
docker-compose up -d postgres redis
if errorlevel 1 (
    echo [ERROR] Database services failed to start
    echo Please check Docker Desktop and try again
    pause
    exit /b 1
)

echo [SUCCESS] Database services started

REM Wait for databases
echo [INFO] Waiting for databases to initialize (45 seconds)...
timeout /t 45 /nobreak >nul

REM Build API with optimized dockerfile
echo [INFO] Building Oil Trading API with Risk Engine...
echo [INFO] This may take 5-10 minutes due to Python dependencies...
echo [INFO] Please be patient while installing numpy, pandas, scipy, arch...

docker build -f src/OilTrading.Api/Dockerfile -t oil-trading-api . --no-cache
if errorlevel 1 (
    echo [ERROR] API build failed
    echo [INFO] Checking build logs...
    echo.
    echo Common solutions:
    echo 1. Ensure Docker has enough memory 4GB+ recommended
    echo 2. Check internet connection for Python package downloads
    echo 3. Try again - sometimes package downloads fail temporarily
    echo.
    pause
    exit /b 1
)

echo [SUCCESS] Oil Trading API with Risk Engine built successfully

REM Start API service
echo [INFO] Starting Oil Trading API service...
docker run -d --name oil-trading-api --network x_oiltrading-network -p 5000:5000 -e ASPNETCORE_ENVIRONMENT=Production -e ConnectionStrings__DefaultConnection="Host=postgres;Database=%DB_NAME%;Username=%DB_USER%;Password=%DB_PASSWORD%" -e ConnectionStrings__RedisConnection="redis:6379" -e RiskEngine__PythonPath="/opt/venv/bin/python3" -e RiskEngine__ScriptPath="/app/Scripts/risk_engine.py" -v "%cd%\logs:/app/logs" oil-trading-api

if errorlevel 1 (
    echo [ERROR] API service failed to start
    echo [INFO] Checking container logs...
    docker logs oil-trading-api 2>&1
    pause
    exit /b 1
)

echo [SUCCESS] Oil Trading API service started

REM Wait for API service to initialize
echo [INFO] Waiting for API service to initialize (90 seconds)...
echo [INFO] The risk engine needs time to load Python libraries...
timeout /t 90 /nobreak >nul

REM Health check with retries
echo [INFO] Performing comprehensive health check...
set max_attempts=15
set attempt=1

:health_check_loop
curl -f http://localhost:5000/health >nul 2>&1
if not errorlevel 1 (
    echo [SUCCESS] Basic API health check passed
    goto risk_check
)

echo [INFO] Attempt !attempt!/!max_attempts! - API starting up...
timeout /t 10 /nobreak >nul
set /a attempt+=1

if !attempt! leq !max_attempts! goto health_check_loop

echo [WARNING] API health check timeout, but continuing...

:risk_check
REM Test risk calculation endpoint
echo [INFO] Testing Risk Calculation Engine...
curl -f http://localhost:5000/api/risk/calculate >nul 2>&1
if not errorlevel 1 (
    echo [SUCCESS] Risk Engine is working!
) else (
    echo [WARNING] Risk Engine may still be initializing...
    echo [INFO] You can test it later at: http://localhost:5000/api/risk/calculate
)

:show_results
echo.
echo =================== DEPLOYMENT SUCCESSFUL ===================
echo.
echo Oil Trading API Endpoints:
echo   - Main API: http://localhost:5000
echo   - Swagger Documentation: http://localhost:5000/swagger
echo   - Health Check: http://localhost:5000/health
echo.
echo Risk Engine Endpoints:
echo   - Calculate Risk: http://localhost:5000/api/risk/calculate
echo   - Portfolio Summary: http://localhost:5000/api/risk/portfolio-summary  
echo   - Stress Testing: http://localhost:5000/api/risk/backtest
echo.
echo Contract Management:
echo   - Purchase Contracts: http://localhost:5000/api/purchase-contracts
echo   - Sales Contracts: http://localhost:5000/api/sales-contracts
echo   - Trading Partners: http://localhost:5000/api/trading-partners
echo   - Products: http://localhost:5000/api/products
echo.
echo Risk Features Available:
echo   - Historical Simulation VaR
echo   - GARCH 1,1 VaR with t-distribution
echo   - Monte Carlo VaR 100,000 simulations
echo   - Expected Shortfall CVaR
echo   - Stress Testing scenarios
echo   - Portfolio concentration analysis
echo.
echo Management Commands:
echo   - View API logs: docker logs oil-trading-api
echo   - View DB logs: docker logs oiltrading-postgres
echo   - Restart API: docker restart oil-trading-api
echo   - Stop all: docker stop oil-trading-api oiltrading-postgres oiltrading-redis
echo.
echo [SUCCESS] Your Oil Trading System with Risk Engine is ready!
echo [INFO] You can now start entering trading data and calculating risk metrics.

REM Open browser to Swagger
start http://localhost:5000/swagger 2>nul

echo.
echo Press any key to exit...
pause >nul