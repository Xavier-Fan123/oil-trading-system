# Production Deployment Guide

**Version**: 2.0 Enterprise Grade
**Last Updated**: November 2025
**Scope**: Infrastructure, Database, Caching, Monitoring, Disaster Recovery

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [System Requirements](#system-requirements)
3. [Pre-Deployment Checklist](#pre-deployment-checklist)
4. [Database Deployment](#database-deployment)
5. [Backend Deployment](#backend-deployment)
6. [Frontend Deployment](#frontend-deployment)
7. [Cache Layer Setup](#cache-layer-setup)
8. [Health Checks & Monitoring](#health-checks--monitoring)
9. [Backup & Disaster Recovery](#backup--disaster-recovery)
10. [Performance Tuning](#performance-tuning)
11. [Troubleshooting](#troubleshooting)
12. [Post-Deployment Verification](#post-deployment-verification)

---

## Quick Start

### Fastest Path to Production (30 minutes)

**Prerequisite**: Linux server with Docker installed

```bash
# 1. Clone repository
git clone <repo-url>
cd oil-trading-system

# 2. Configure environment
cp .env.production.example .env.production
# Edit .env.production with your settings:
#   - DATABASE_URL=postgresql://user:pass@db-host:5432/oil_trading
#   - REDIS_URL=redis://redis-host:6379
#   - JWT_SECRET=<generate-secure-secret>

# 3. Deploy with Docker Compose
docker-compose -f docker-compose.production.yml up -d

# 4. Verify deployment
./scripts/verify-deployment.sh

# 5. Access application
# Frontend: https://your-domain.com
# API: https://your-domain.com/api
# Swagger: https://your-domain.com/swagger
```

**Result**: System operational in ~15 minutes

---

## System Requirements

### Minimum Hardware (Single-Server Deployment)

```
CPU:     8 cores (Intel Xeon or equivalent)
RAM:     32 GB
Disk:    500 GB SSD (200 GB OS + Utils, 200 GB Database, 100 GB Backups)
Network: 1 Gbps connection
Uptime:  99.5% SLA minimum
```

### Recommended Hardware (High-Availability Cluster)

```
┌─────────────────────────────────────┐
│      Load Balancer (Nginx/HAProxy)  │
│      2 cores, 4 GB RAM              │
└──────┬──────────────────────────────┘
       │
   ┌───┴────┬────────────┬────────────┐
   │        │            │            │
┌──▼──┐  ┌──▼──┐  ┌──▼──┐  ┌──▼──┐
│App  │  │App  │  │App  │  │App  │
│#1   │  │#2   │  │#3   │  │#4   │
│8c   │  │8c   │  │8c   │  │8c   │
│16GB │  │16GB │  │16GB │  │16GB │
└─────┘  └─────┘  └─────┘  └─────┘
│
├─── PostgreSQL Master (16c, 64 GB RAM)
├─── PostgreSQL Replica (16c, 64 GB RAM)
├─── Redis Cluster (3 nodes, 8c each, 16 GB RAM)
├─── Backup Server (4c, 8 GB RAM)
└─── Monitoring Stack (4c, 8 GB RAM)

Total: ~30 cores, 200+ GB RAM
Supports: 5,000+ concurrent users
```

### Software Requirements

**Backend**:
- .NET 9.0 runtime
- PostgreSQL 15+ (production) or SQLite (dev)
- Redis 7.0+ (recommended for production)

**Frontend**:
- Node.js 18+ (build time only, not needed for production)
- Nginx or Apache reverse proxy

**DevOps**:
- Docker Engine 20.10+
- Docker Compose 2.0+ (optional)
- Kubernetes 1.24+ (optional, for container orchestration)

---

## Pre-Deployment Checklist

### 1. Code Quality Verification

```bash
# Run all tests
dotnet test OilTrading.sln --verbosity minimal
# Expected: All 842 tests passing
# Time: ~5 minutes

# Verify compilation
dotnet build -c Release
# Expected: Zero errors, zero warnings
# Time: ~2 minutes

# Check TypeScript compilation
cd frontend && npm run build
# Expected: No TypeScript errors
# Time: ~1 minute
```

### 2. Security Review

```bash
# Check for secrets in code
git log --all -S 'password' --source
git log --all -S 'api_key' --source
git log --all -S 'secret' --source

# Scan dependencies for vulnerabilities
dotnet list package --vulnerable
npm audit

# Verify HTTPS is configured
# Check: appsettings.Production.json for https settings
```

### 3. Database Preparation

```bash
# Verify connection string is correct
cat appsettings.Production.json | grep -A2 DefaultConnection

# Test database connectivity
dotnet ef database update --project src/OilTrading.Infrastructure

# Verify all tables created
# Expected: 40+ tables, 47 entities
SELECT COUNT(*) FROM information_schema.tables;
```

### 4. Infrastructure Readiness

```bash
# Verify PostgreSQL is operational
psql -h db-host -U admin -d oil_trading -c "SELECT 1"
# Expected: (1 row)

# Verify Redis is operational
redis-cli -h redis-host PING
# Expected: PONG

# Verify network connectivity
ping db-host
ping redis-host
ping load-balancer

# Verify disk space
df -h
# Expected: >100 GB free for backups
```

### 5. DNS and TLS

```bash
# Verify DNS resolves correctly
nslookup api.yourdomain.com
nslookup app.yourdomain.com

# Verify TLS certificate (if using HTTPS)
openssl s_client -connect api.yourdomain.com:443 -showcerts

# Certificate should be valid for 30+ days
# Issuer should be trusted CA
```

---

## Database Deployment

### Option A: SQLite (Development)

```bash
# Quick start, zero configuration
cd src/OilTrading.Api
dotnet run

# Database created automatically at: oiltrading.db
# Suitable for: Development, testing, proof-of-concept
# Limitation: Single-server only, no replication
```

### Option B: PostgreSQL (Production) - Recommended

#### Step 1: Install PostgreSQL

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install postgresql-15 postgresql-contrib-15

# RHEL/CentOS
sudo yum install postgresql15-server postgresql15-contrib

# Start service
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

#### Step 2: Create Database and User

```bash
# Connect as admin
sudo -u postgres psql

# Create database
CREATE DATABASE oil_trading;

# Create application user
CREATE USER app_user WITH PASSWORD 'secure-password-here';
GRANT CONNECT ON DATABASE oil_trading TO app_user;

# Grant schema permissions
\c oil_trading
GRANT USAGE ON SCHEMA public TO app_user;
GRANT CREATE ON SCHEMA public TO app_user;

# Grant table permissions (after migration)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app_user;

\q
```

#### Step 3: Configure Connection String

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db-host.local;Database=oil_trading;User Id=app_user;Password=secure-password-here;Port=5432;Connection Timeout=30;Command Timeout=30;Include Error Detail=false;"
  }
}
```

#### Step 4: Run Entity Framework Migrations

```bash
cd src/OilTrading.Api

# Apply migrations
dotnet ef database update --startup-project . --project ../OilTrading.Infrastructure

# Verify schema
psql -h db-host -U app_user -d oil_trading -c "\dt"
# Expected: 40+ tables listed
```

#### Step 5: Configure Replication (Optional but Recommended)

**Master Server Configuration**:
```bash
# Edit /etc/postgresql/15/main/postgresql.conf
wal_level = replica
max_wal_senders = 10
max_replication_slots = 10
hot_standby = on

# Edit /etc/postgresql/15/main/pg_hba.conf
# Add replication permission
host    replication     all             replica-host/32         md5
```

**Replica Server Configuration**:
```bash
# Create base backup from master
pg_basebackup -h master-host -D /var/lib/postgresql/15/main -U replication -v -P

# Create recovery.conf
standby_mode = 'on'
primary_conninfo = 'host=master-host port=5432 user=replication password=xxx'
```

**Replication Status Check**:
```bash
# On Master
SELECT slot_name, slot_type, active FROM pg_replication_slots;
# Expected: replication slot active

# Replication lag monitoring
SELECT now() - pg_last_wal_receive_lsn() AS replication_lag;
# Expected: < 100 milliseconds
```

---

## Backend Deployment

### Build for Production

```bash
# Build Release configuration
dotnet publish -c Release -o ./publish

# Output: ~250 MB of runtime files
# Time: ~5 minutes
```

### Docker Deployment

```dockerfile
# Dockerfile.production
FROM mcr.microsoft.com/dotnet/aspnet:9.0 as base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 as builder
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base as final
COPY --from=builder /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "OilTrading.Api.dll"]
```

```bash
# Build image
docker build -f Dockerfile.production -t oil-trading-api:latest .

# Run container
docker run -d \
  --name oil-trading-api \
  -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="Server=db-host;Database=oil_trading;..." \
  -e Redis="redis-host:6379" \
  -e JWT_Secret="your-secret-key" \
  oil-trading-api:latest
```

### Traditional Deployment (No Docker)

```bash
# 1. Create application directory
sudo mkdir -p /opt/oil-trading
sudo chown appuser:appuser /opt/oil-trading

# 2. Deploy files
cp -r publish/* /opt/oil-trading/

# 3. Create systemd service
sudo tee /etc/systemd/system/oil-trading-api.service > /dev/null <<EOF
[Unit]
Description=Oil Trading API Service
After=network.target postgresql.service redis.service

[Service]
Type=notify
User=appuser
WorkingDirectory=/opt/oil-trading
ExecStart=/usr/bin/dotnet /opt/oil-trading/OilTrading.Api.dll
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

# Environment variables
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ConnectionStrings__DefaultConnection=Server=db-host;..."
Environment="Redis=redis-host:6379"

[Install]
WantedBy=multi-user.target
EOF

# 4. Start service
sudo systemctl daemon-reload
sudo systemctl start oil-trading-api
sudo systemctl enable oil-trading-api

# 5. Monitor logs
sudo journalctl -u oil-trading-api -f
```

### Nginx Reverse Proxy Configuration

```nginx
upstream oil_trading_api {
    server localhost:5000 fail_timeout=0;
}

server {
    listen 80;
    server_name api.yourdomain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    # SSL certificates
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;

    # Proxy settings
    location /api/ {
        proxy_pass http://oil_trading_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://oil_trading_api;
        access_log off;
    }

    # Swagger UI
    location /swagger {
        proxy_pass http://oil_trading_api;
    }
}
```

---

## Frontend Deployment

### Build for Production

```bash
cd frontend

# Install dependencies
npm ci --production

# Build optimized bundle
npm run build

# Output: ./dist directory (~5 MB gzipped)
```

### Static Hosting with Nginx

```bash
# 1. Copy build output
sudo cp -r dist/* /var/www/html/

# 2. Configure Nginx
sudo tee /etc/nginx/sites-available/oil-trading-app > /dev/null <<EOF
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;

    root /var/www/html;
    index index.html;

    # SPA routing: All requests go to index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|jpg|jpeg|png|gif|ico|svg|woff|woff2)$ {
        expires 365d;
        add_header Cache-Control "public, immutable";
    }

    # Don't cache HTML
    location = /index.html {
        add_header Cache-Control "no-cache, no-store, must-revalidate";
    }
}
EOF

# 3. Enable site
sudo ln -s /etc/nginx/sites-available/oil-trading-app /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### Docker Deployment

```dockerfile
# Frontend Dockerfile
FROM node:18-alpine as builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---

## Cache Layer Setup

### Redis Installation

```bash
# Install Redis
sudo apt-get install redis-server

# Configure for production
sudo tee /etc/redis/redis.conf.production > /dev/null <<EOF
# Memory management
maxmemory 16gb
maxmemory-policy allkeys-lru

# Persistence
save 900 1
save 300 10
save 60 10000
appendonly yes
appendfsync everysec

# Replication (if using sentinel)
slaveof no one

# Security
requirepass your-secure-password-here
EOF

# Start Redis
sudo systemctl start redis-server
sudo systemctl enable redis-server

# Verify operation
redis-cli ping
# Expected: PONG
```

### Redis Sentinel (High Availability)

```bash
# Install Sentinel
sudo tee /etc/redis/sentinel.conf > /dev/null <<EOF
port 26379
sentinel monitor oil-trading-redis 127.0.0.1 6379 2
sentinel down-after-milliseconds oil-trading-redis 30000
sentinel parallel-syncs oil-trading-redis 1
sentinel failover-timeout oil-trading-redis 180000
EOF

# Start Sentinel
redis-sentinel /etc/redis/sentinel.conf
```

### Application Configuration

```json
// appsettings.Production.json
{
  "Redis": "redis-host:6379,password=secure-password,connectTimeout=5000,syncTimeout=5000",
  "CacheSettings": {
    "DashboardTtlMinutes": 5,
    "PositionTtlMinutes": 15,
    "PnLTtlMinutes": 60,
    "RiskTtlMinutes": 15
  }
}
```

---

## Health Checks & Monitoring

### Built-in Health Endpoints

```bash
# Overall system health
curl https://api.yourdomain.com/health
# Response: { "status": "Healthy" }

# Readiness probe (for Kubernetes)
curl https://api.yourdomain.com/health/ready
# Response: { "status": "Healthy" }

# Liveness probe
curl https://api.yourdomain.com/health/live
# Response: { "status": "Healthy" }

# Detailed health with metrics
curl https://api.yourdomain.com/health/detailed
# Response: Complete system status with component details
```

### Prometheus Metrics

```bash
# Prometheus metrics endpoint
curl https://api.yourdomain.com/metrics

# Metrics include:
# - HTTP request rate and latency
# - Database connection pool status
# - Cache hit rates
# - Business KPIs (active contracts, settlements)
```

### Monitoring Stack (Optional)

```yaml
# docker-compose with monitoring
version: '3.8'
services:
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana
    ports:
      - "3000:3000"

  loki:
    image: grafana/loki
    ports:
      - "3100:3100"
```

**Key Metrics to Monitor**:
- API response time (p50, p95, p99)
- Database connection pool utilization
- Cache hit rate (target: >90%)
- Error rate by endpoint
- Active contracts count
- Settlement processing time

---

## Backup & Disaster Recovery

### Database Backup Strategy

**Daily Backups** (3 methods):

1. **Logical Backup (Full Database)**
```bash
# Daily at 2 AM UTC
pg_dump -h db-host -U app_user -d oil_trading -F custom > backup-$(date +%Y%m%d).dump

# Retention: 30 days
find /backups -name "backup-*.dump" -mtime +30 -delete
```

2. **Physical Backups (WAL Archiving)**
```bash
# Continuous archiving for point-in-time recovery
archive_command = 'test ! -f /archive/%f && cp %p /archive/%f'
archive_timeout = 300

# Retention: 14 days
find /archive -type f -mtime +14 -delete
```

3. **Incremental Backups**
```bash
# Daily incremental since last full backup
pg_basebackup -h db-host -D /incremental-$(date +%Y%m%d) -r

# Retention: 7 days
```

**Backup Verification**:
```bash
# Test restore on staging weekly
pg_restore -C -d test-db backup-latest.dump

# Verify data integrity
SELECT COUNT(*) FROM Contracts WHERE CreatedAt > NOW() - INTERVAL '7 days';
# Expected: Contracts from last week present
```

### Disaster Recovery Plan

**RTO/RPO Targets**:
- RTO (Recovery Time Objective): 4 hours
- RPO (Recovery Point Objective): 1 hour

**Recovery Procedures**:

1. **Database Corruption** (RTO: 15 min, RPO: 1 hour)
```bash
# Restore from latest good backup
pg_restore -C -d oil_trading backup-$(date --date '1 hour ago' +%Y%m%d%H).dump

# Verify application connectivity
curl https://api.yourdomain.com/health
```

2. **Application Failure** (RTO: 5 min, RPO: 0)
```bash
# Application is stateless, just restart
sudo systemctl restart oil-trading-api

# Or redeploy container
docker run -d --name oil-trading-api-new ...
```

3. **Data Center Failure** (RTO: 4 hours, RPO: 1 hour)
```bash
# Failover to standby data center
# 1. Update DNS to point to standby
# 2. Promote PostgreSQL replica to master
# 3. Update Redis to failover cluster
# 4. Verify application health
```

---

## Performance Tuning

### Database Query Optimization

```bash
# Enable query logging for slow queries
log_min_duration_statement = 1000  # Log queries > 1 second

# Review slow query log
psql -h db-host -U app_user -d oil_trading -c "
  SELECT query, mean_time, calls
  FROM pg_stat_statements
  ORDER BY mean_time DESC
  LIMIT 10;"
```

### Connection Pooling

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Min Pool Size=25;Max Pool Size=100;Connection Idle Lifetime=900;"
  }
}
```

**Tuning Guidelines**:
- Min Pool: Number of CPU cores
- Max Pool: (CPU cores × 2) + 10
- Idle Lifetime: 15 minutes

### Cache Strategy

```csharp
// appsettings.Production.json Cache TTLs
{
  "CacheSettings": {
    "DashboardTtlMinutes": 5,      // Dashboard data (high churn)
    "PositionTtlMinutes": 15,       // Position calculations (medium churn)
    "PnLTtlMinutes": 60,           // P&L data (stable)
    "RiskTtlMinutes": 15,          // Risk calculations (medium churn)
    "ContractListTtlMinutes": 10   // Contract lists (frequent updates)
  }
}
```

---

## Troubleshooting

### Common Issues and Solutions

| Issue | Symptom | Solution |
|-------|---------|----------|
| **High API latency** | Responses > 1 second | Check cache hit rate, add Redis nodes |
| **Database timeout** | 502 Bad Gateway | Increase connection pool, optimize queries |
| **Out of memory** | Service OOMKilled | Increase Redis max-memory, reduce cache TTL |
| **Replication lag** | Data inconsistency | Check replica network, increase bandwidth |
| **Disk space full** | Write failures | Rotate logs, archive old data |

### Diagnostic Commands

```bash
# API health
curl -i https://api.yourdomain.com/health

# Database connectivity
psql -h db-host -U app_user -d oil_trading -c "SELECT 1"

# Redis connectivity
redis-cli -h redis-host PING

# Active database connections
psql -c "SELECT count(*) as connections FROM pg_stat_activity"

# Cache statistics
redis-cli INFO stats | grep -E "hits|misses"

# System resources
free -h
df -h
top -n 1
```

---

## Post-Deployment Verification

### Automated Verification Script

```bash
#!/bin/bash
# verify-deployment.sh

set -e

echo "Starting deployment verification..."

# 1. API Health
echo "Checking API health..."
curl -f https://api.yourdomain.com/health
echo "✓ API health check passed"

# 2. Database
echo "Checking database connectivity..."
psql -h db-host -U app_user -d oil_trading -c "SELECT COUNT(*) FROM Contracts"
echo "✓ Database connectivity verified"

# 3. Redis
echo "Checking Redis connectivity..."
redis-cli -h redis-host PING
echo "✓ Redis connectivity verified"

# 4. Frontend
echo "Checking frontend..."
curl -f https://yourdomain.com/ | grep -q "DOCTYPE html"
echo "✓ Frontend responsive"

# 5. Run smoke tests
echo "Running smoke tests..."
dotnet test OilTrading.SmokeTests.csproj --logger "console;verbosity=minimal"
echo "✓ Smoke tests passed"

echo ""
echo "✅ Deployment verification complete!"
echo "System is ready for production traffic."
```

### Manual Verification Checklist

```
[ ] API responds to health check
[ ] Database has all 40+ tables
[ ] Redis cache operational
[ ] Frontend loads without errors
[ ] Login functionality works
[ ] Can create purchase contract
[ ] Can create sales contract
[ ] Can create settlement
[ ] Can view dashboard
[ ] API documentation (Swagger) accessible
[ ] Backups are running
[ ] Monitoring/alerting configured
[ ] Team trained on operations
[ ] Runbooks documented
[ ] Incident response plan ready
```

---

## Summary

Production deployment requires careful planning across:
- **Database**: Replication, backups, optimization
- **Backend**: Load balancing, health checks, monitoring
- **Frontend**: Static hosting, caching strategy
- **Infrastructure**: Security, disaster recovery, performance

**Deployment time**: 2-4 hours for initial setup
**Ongoing maintenance**: ~4 hours/month for updates, backups, monitoring

For API documentation, see [API_REFERENCE_COMPLETE.md](./API_REFERENCE_COMPLETE.md)
For security hardening, see [SECURITY_AND_COMPLIANCE.md](./SECURITY_AND_COMPLIANCE.md)
For troubleshooting, see [TESTING_AND_QUALITY.md](./TESTING_AND_QUALITY.md)

