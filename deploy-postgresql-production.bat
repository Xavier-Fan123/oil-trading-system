@echo off
setlocal enabledelayedexpansion

:: ================================================================
:: Oil Trading System - PostgreSQL Production Deployment Script
:: ================================================================
:: This script deploys the Oil Trading System with PostgreSQL in production mode
:: Features: Master-Replica replication, Load balancing, Monitoring, Backup

echo.
echo ================================================================================
echo  Oil Trading System - PostgreSQL Production Deployment
echo ================================================================================
echo.

:: Check if running as Administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator for proper Docker operations.
    echo Please right-click Command Prompt and select "Run as administrator"
    pause
    exit /b 1
)

:: Check Docker availability
echo [%time%] Checking Docker availability...
docker --version >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Docker is not installed or not accessible.
    echo Please install Docker Desktop and ensure it's running.
    pause
    exit /b 1
)

:: Check Docker Compose availability
docker-compose --version >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Docker Compose is not available.
    echo Please ensure Docker Compose is installed.
    pause
    exit /b 1
)

:: Set deployment variables
set DEPLOYMENT_DATE=%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set DEPLOYMENT_DATE=!DEPLOYMENT_DATE: =0!
set BACKUP_DIR=.\backups\pre-deployment-%DEPLOYMENT_DATE%
set CONFIG_DIR=.\configs
set SCRIPT_DIR=.\scripts

echo [%time%] Deployment started at: %DEPLOYMENT_DATE%
echo [%time%] Backup directory: %BACKUP_DIR%

:: Create necessary directories
echo [%time%] Creating directory structure...
if not exist "!BACKUP_DIR!" mkdir "!BACKUP_DIR!"
if not exist "!CONFIG_DIR!" mkdir "!CONFIG_DIR!"
if not exist "!CONFIG_DIR!\postgres" mkdir "!CONFIG_DIR!\postgres"
if not exist "!CONFIG_DIR!\nginx" mkdir "!CONFIG_DIR!\nginx"
if not exist "!CONFIG_DIR!\redis" mkdir "!CONFIG_DIR!\redis"
if not exist "!SCRIPT_DIR!\postgres\master" mkdir "!SCRIPT_DIR!\postgres\master"
if not exist "!SCRIPT_DIR!\postgres\replica" mkdir "!SCRIPT_DIR!\postgres\replica"
if not exist ".\logs" mkdir ".\logs"
if not exist ".\logs\nginx" mkdir ".\logs\nginx"
if not exist ".\ssl" mkdir ".\ssl"

:: Backup existing environment file if it exists
if exist ".env" (
    echo [%time%] Backing up existing .env file...
    copy ".env" "!BACKUP_DIR!\.env.backup" >nul
)

:: Create production environment file
echo [%time%] Creating production environment configuration...
call :create_env_file

:: Create PostgreSQL configuration files
echo [%time%] Creating PostgreSQL configuration files...
call :create_postgres_configs

:: Create Nginx configuration
echo [%time%] Creating Nginx load balancer configuration...
call :create_nginx_config

:: Create Redis configuration
echo [%time%] Creating Redis configuration...
call :create_redis_config

:: Stop any existing containers
echo [%time%] Stopping existing containers...
docker-compose -f docker-compose.yml down --remove-orphans >nul 2>&1
docker-compose -f docker-compose.production.yml down --remove-orphans >nul 2>&1

:: Pull latest images
echo [%time%] Pulling latest Docker images...
docker-compose -f docker-compose.production.yml pull

:: Build application images
echo [%time%] Building application images...
docker-compose -f docker-compose.production.yml build --no-cache

:: Deploy the production environment
echo [%time%] Starting PostgreSQL production deployment...
docker-compose -f docker-compose.production.yml up -d postgres-master

:: Wait for master database to be ready
echo [%time%] Waiting for PostgreSQL master to be ready...
:wait_master
timeout /t 5 /nobreak >nul
docker-compose -f docker-compose.production.yml exec postgres-master pg_isready -U postgres -d OilTradingDb >nul 2>&1
if %errorLevel% neq 0 (
    echo [%time%] Master database not ready yet, waiting...
    goto wait_master
)
echo [%time%] PostgreSQL master is ready!

:: Start replica database
echo [%time%] Starting PostgreSQL replica...
docker-compose -f docker-compose.production.yml up -d postgres-replica

:: Wait for replica to be ready
echo [%time%] Waiting for PostgreSQL replica to be ready...
:wait_replica
timeout /t 10 /nobreak >nul
docker-compose -f docker-compose.production.yml exec postgres-replica pg_isready -U postgres -d OilTradingDb >nul 2>&1
if %errorLevel% neq 0 (
    echo [%time%] Replica database not ready yet, waiting...
    goto wait_replica
)
echo [%time%] PostgreSQL replica is ready!

:: Start Redis
echo [%time%] Starting Redis cache...
docker-compose -f docker-compose.production.yml up -d redis-master

:: Wait for Redis to be ready
echo [%time%] Waiting for Redis to be ready...
:wait_redis
timeout /t 3 /nobreak >nul
docker-compose -f docker-compose.production.yml exec redis-master redis-cli ping >nul 2>&1
if %errorLevel% neq 0 (
    echo [%time%] Redis not ready yet, waiting...
    goto wait_redis
)
echo [%time%] Redis is ready!

:: Start API instances
echo [%time%] Starting Oil Trading API instances...
docker-compose -f docker-compose.production.yml up -d oil-trading-api-1 oil-trading-api-2

:: Wait for API instances to be ready
echo [%time%] Waiting for API instances to be ready...
timeout /t 30 /nobreak >nul

:: Start load balancer
echo [%time%] Starting Nginx load balancer...
docker-compose -f docker-compose.production.yml up -d nginx

:: Start monitoring stack
echo [%time%] Starting monitoring stack...
docker-compose -f docker-compose.production.yml up -d prometheus grafana

:: Verify deployment
echo [%time%] Verifying deployment...
call :verify_deployment

:: Show deployment summary
call :show_deployment_summary

echo.
echo ================================================================================
echo  PostgreSQL Production Deployment Completed Successfully!
echo ================================================================================
echo.
echo Access Points:
echo   - Oil Trading System: http://localhost
echo   - API Documentation: http://localhost/swagger
echo   - Grafana Monitoring: http://localhost:3001 (admin/admin123)
echo   - Prometheus Metrics: http://localhost:9090
echo.
echo Next Steps:
echo   1. Configure SSL certificates for HTTPS
echo   2. Set up external monitoring alerts
echo   3. Configure backup schedule
echo   4. Update DNS records for production domain
echo.
pause
goto :eof

:: Function to create environment file
:create_env_file
(
echo # Oil Trading System - Production Environment Configuration
echo # Generated on %date% %time%
echo.
echo # Database Configuration
echo POSTGRES_DB=OilTradingDb
echo POSTGRES_USER=postgres
echo POSTGRES_PASSWORD=postgres123
echo POSTGRES_REPLICATION_USER=replica_user
echo POSTGRES_REPLICATION_PASSWORD=replica_pass
echo POSTGRES_MASTER_PORT=5432
echo POSTGRES_REPLICA_PORT=5433
echo.
echo # Redis Configuration
echo REDIS_MASTER_PORT=6379
echo.
echo # Security Configuration (CHANGE THESE IN PRODUCTION!)
echo JWT_SECRET=your-super-secret-jwt-key-min-256-bits-change-in-production
echo ENCRYPTION_KEY=your-encryption-key-change-in-production
echo.
echo # External Services
echo MARKET_DATA_API_KEY=your-market-data-api-key
echo APPINSIGHTS_INSTRUMENTATIONKEY=your-app-insights-key
echo.
echo # Monitoring Configuration
echo GRAFANA_ADMIN_PASSWORD=admin123
echo GRAFANA_DB_NAME=grafana
echo GRAFANA_DB_USER=grafana
echo GRAFANA_DB_PASSWORD=grafana123
echo.
echo # Backup Configuration
echo BACKUP_RETENTION_DAYS=30
echo BACKUP_ENCRYPTION_ENABLED=true
echo S3_BACKUP_BUCKET=your-backup-bucket
echo AWS_ACCESS_KEY_ID=your-aws-access-key
echo AWS_SECRET_ACCESS_KEY=your-aws-secret-key
) > .env
goto :eof

:: Function to create PostgreSQL configurations
:create_postgres_configs
:: Master configuration
(
echo # PostgreSQL Master Configuration
echo listen_addresses = '*'
echo port = 5432
echo max_connections = 200
echo shared_buffers = 1024MB
echo effective_cache_size = 3GB
echo maintenance_work_mem = 256MB
echo checkpoint_completion_target = 0.9
echo wal_buffers = 16MB
echo default_statistics_target = 100
echo random_page_cost = 1.1
echo effective_io_concurrency = 200
echo work_mem = 8MB
echo min_wal_size = 2GB
echo max_wal_size = 8GB
echo wal_level = replica
echo max_wal_senders = 3
echo wal_keep_size = 64MB
echo archive_mode = on
echo archive_command = 'cp %%p /var/lib/postgresql/archive/%%f'
echo log_statement = 'ddl'
echo log_min_duration_statement = 1000
echo log_checkpoints = on
echo log_lock_waits = on
echo log_connections = on
echo log_disconnections = on
echo logging_collector = on
echo log_filename = 'postgresql-%%Y-%%m-%%d_%%H%%M%%S.log'
echo log_rotation_age = 1d
echo log_rotation_size = 100MB
) > "!CONFIG_DIR!\postgres\postgresql.master.conf"

:: Replica configuration
(
echo # PostgreSQL Replica Configuration
echo listen_addresses = '*'
echo port = 5432
echo max_connections = 100
echo shared_buffers = 512MB
echo effective_cache_size = 1GB
echo hot_standby = on
echo max_standby_streaming_delay = 30s
echo wal_receiver_status_interval = 10s
echo hot_standby_feedback = on
) > "!CONFIG_DIR!\postgres\postgresql.replica.conf"

:: pg_hba.conf
(
echo # PostgreSQL Host-Based Authentication Configuration
echo local   all             postgres                                peer
echo host    all             postgres        127.0.0.1/32            md5
echo host    all             postgres        ::1/128                 md5
echo host    all             all             0.0.0.0/0               md5
echo host    replication     replica_user    0.0.0.0/0               md5
) > "!CONFIG_DIR!\postgres\pg_hba.conf"
goto :eof

:: Function to create Nginx configuration
:create_nginx_config
(
echo events {
echo     worker_connections 1024;
echo }
echo.
echo http {
echo     upstream oil_trading_api {
echo         server oil-trading-api-1:8080;
echo         server oil-trading-api-2:8080;
echo     }
echo.
echo     server {
echo         listen 80;
echo         server_name localhost;
echo.
echo         location / {
echo             proxy_pass http://oil_trading_api;
echo             proxy_set_header Host $host;
echo             proxy_set_header X-Real-IP $remote_addr;
echo             proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
echo             proxy_set_header X-Forwarded-Proto $scheme;
echo         }
echo.
echo         location /health {
echo             proxy_pass http://oil_trading_api/api/health;
echo         }
echo     }
echo }
) > "!CONFIG_DIR!\nginx\nginx.production.conf"
goto :eof

:: Function to create Redis configuration
:create_redis_config
(
echo # Redis Master Configuration
echo bind 0.0.0.0
echo port 6379
echo protected-mode no
echo maxmemory 1gb
echo maxmemory-policy allkeys-lru
echo appendonly yes
echo save 900 1
echo save 300 10
echo save 60 10000
echo tcp-keepalive 60
echo timeout 300
) > "!CONFIG_DIR!\redis\redis-master.conf"
goto :eof

:: Function to verify deployment
:verify_deployment
echo [%time%] Running deployment verification checks...

:: Check container status
echo [%time%] Checking container status...
for %%c in (postgres-master postgres-replica redis-master oil-trading-api-1 oil-trading-api-2 nginx) do (
    docker-compose -f docker-compose.production.yml ps %%c | findstr "Up" >nul
    if !errorLevel! equ 0 (
        echo   ✓ %%c is running
    ) else (
        echo   ✗ %%c is not running
    )
)

:: Check API health
echo [%time%] Checking API health...
timeout /t 5 /nobreak >nul
curl -s http://localhost/api/health >nul 2>&1
if %errorLevel% equ 0 (
    echo   ✓ API health check passed
) else (
    echo   ✗ API health check failed
)

:: Check database connectivity
echo [%time%] Checking database connectivity...
docker-compose -f docker-compose.production.yml exec postgres-master pg_isready -U postgres -d OilTradingDb >nul 2>&1
if %errorLevel% equ 0 (
    echo   ✓ Master database is accessible
) else (
    echo   ✗ Master database is not accessible
)

docker-compose -f docker-compose.production.yml exec postgres-replica pg_isready -U postgres -d OilTradingDb >nul 2>&1
if %errorLevel% equ 0 (
    echo   ✓ Replica database is accessible
) else (
    echo   ✗ Replica database is not accessible
)

:: Check Redis connectivity
echo [%time%] Checking Redis connectivity...
docker-compose -f docker-compose.production.yml exec redis-master redis-cli ping >nul 2>&1
if %errorLevel% equ 0 (
    echo   ✓ Redis is accessible
) else (
    echo   ✗ Redis is not accessible
)
goto :eof

:: Function to show deployment summary
:show_deployment_summary
echo.
echo ================================================================================
echo  DEPLOYMENT SUMMARY
echo ================================================================================
echo  Deployment Date: %DEPLOYMENT_DATE%
echo  Environment: Production
echo  Database: PostgreSQL 15 with Master-Replica
echo  Cache: Redis 7
echo  Load Balancer: Nginx
echo  API Instances: 2 (Load Balanced)
echo  Monitoring: Prometheus + Grafana
echo.
echo  Configuration Files Created:
echo    - .env (Environment variables)
echo    - configs\postgres\postgresql.master.conf
echo    - configs\postgres\postgresql.replica.conf
echo    - configs\postgres\pg_hba.conf
echo    - configs\nginx\nginx.production.conf
echo    - configs\redis\redis-master.conf
echo.
echo  Docker Containers Running:
docker-compose -f docker-compose.production.yml ps --format "table {{.Name}}\t{{.State}}\t{{.Ports}}"
echo.
goto :eof