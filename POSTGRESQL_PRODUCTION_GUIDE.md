# Oil Trading System - PostgreSQL Production Deployment Guide

## 🎯 Overview

This guide provides comprehensive instructions for deploying the Oil Trading System with PostgreSQL in a production environment. The deployment includes master-replica database replication, load balancing, monitoring, and automated backup capabilities.

## 📋 Prerequisites

### System Requirements
- **OS**: Windows Server 2019+ or Linux (Ubuntu 20.04+)
- **Memory**: Minimum 8GB RAM (16GB recommended)
- **Storage**: Minimum 100GB SSD (500GB recommended)
- **CPU**: Minimum 4 cores (8 cores recommended)
- **Network**: Stable internet connection for Docker images

### Software Dependencies
- Docker Desktop 4.0+ or Docker Engine 20.10+
- Docker Compose 2.0+
- Git (for source code management)
- curl (for health checks)

### Security Requirements
- Administrator/root privileges
- Firewall configuration for ports: 80, 443, 5432, 6379, 3001, 9090
- SSL certificates (for HTTPS in production)

## 🚀 Quick Start Deployment

### 1. Automated Deployment (Recommended)

For Windows environments:
```batch
# Run as Administrator
deploy-postgresql-production.bat
```

For Linux environments:
```bash
# Run with sudo if needed
chmod +x deploy-postgresql-production.sh
./deploy-postgresql-production.sh
```

### 2. Manual Deployment Steps

If you prefer manual control over the deployment process:

#### Step 1: Environment Setup
```bash
# Clone the repository
git clone <repository-url>
cd oil-trading-system

# Create environment file
cp .env.example .env
# Edit .env with your production values
```

#### Step 2: Configuration Files
Create the necessary configuration directories:
```bash
mkdir -p configs/{postgres,nginx,redis}
mkdir -p scripts/postgres/{master,replica}
mkdir -p logs/nginx
mkdir -p ssl
mkdir -p backups
```

#### Step 3: Database Deployment
```bash
# Start PostgreSQL master
docker-compose -f docker-compose.production.yml up -d postgres-master

# Wait for master to be ready
docker-compose -f docker-compose.production.yml exec postgres-master pg_isready -U postgres

# Start PostgreSQL replica
docker-compose -f docker-compose.production.yml up -d postgres-replica
```

#### Step 4: Application Services
```bash
# Start Redis cache
docker-compose -f docker-compose.production.yml up -d redis-master

# Start API instances
docker-compose -f docker-compose.production.yml up -d oil-trading-api-1 oil-trading-api-2

# Start load balancer
docker-compose -f docker-compose.production.yml up -d nginx
```

#### Step 5: Monitoring Stack
```bash
# Start monitoring services
docker-compose -f docker-compose.production.yml up -d prometheus grafana
```

## 🔧 Configuration Details

### Environment Variables (.env)

Critical environment variables that must be configured:

```bash
# Database Configuration
POSTGRES_DB=OilTradingDb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=CHANGE_IN_PRODUCTION
POSTGRES_REPLICATION_USER=replica_user
POSTGRES_REPLICATION_PASSWORD=CHANGE_IN_PRODUCTION

# Security (CRITICAL: Change these values!)
JWT_SECRET=your-super-secret-jwt-key-min-256-bits
ENCRYPTION_KEY=your-32-character-encryption-key

# External Services
MARKET_DATA_API_KEY=your-market-data-provider-key
APPINSIGHTS_INSTRUMENTATIONKEY=your-application-insights-key

# Backup Configuration
S3_BACKUP_BUCKET=your-backup-bucket-name
AWS_ACCESS_KEY_ID=your-aws-access-key
AWS_SECRET_ACCESS_KEY=your-aws-secret-key
```

### PostgreSQL Configuration

#### Master Database (postgresql.master.conf)
Key performance settings:
```ini
# Connection Settings
max_connections = 200
shared_buffers = 1024MB
effective_cache_size = 3GB

# Replication Settings
wal_level = replica
max_wal_senders = 3
wal_keep_size = 64MB

# Performance Settings
work_mem = 8MB
maintenance_work_mem = 256MB
checkpoint_completion_target = 0.9
```

#### Replica Database (postgresql.replica.conf)
```ini
# Standby Settings
hot_standby = on
max_standby_streaming_delay = 30s
wal_receiver_status_interval = 10s
hot_standby_feedback = on
```

### Read-Write Separation

The application is configured to automatically route:
- **Write operations** → Master database (postgres-master:5432)
- **Read operations** → Replica database (postgres-replica:5433)

Connection strings are configured in `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres-master;Port=5432;Database=OilTradingDb;...",
    "ReadConnection": "Host=postgres-replica;Port=5432;Database=OilTradingDb;..."
  }
}
```

## 📊 Monitoring and Alerting

### Prometheus Metrics
Access Prometheus at: `http://localhost:9090`

Key metrics monitored:
- API response times and error rates
- Database connection pools and query performance
- Redis cache hit rates and memory usage
- System resources (CPU, memory, disk)
- Business metrics (contract processing, risk limits)

### Grafana Dashboards
Access Grafana at: `http://localhost:3001`
- **Default credentials**: admin/admin123
- **Pre-configured dashboards**:
  - Oil Trading System Overview
  - Database Performance
  - API Performance
  - System Resources
  - Business Metrics

### Alert Rules
Critical alerts configured:
- **API Instance Down**: Immediate notification
- **Database Connection Issues**: 1-minute threshold
- **High Error Rate**: 5% threshold over 3 minutes
- **Memory Usage**: 85% threshold
- **Replication Lag**: >60 seconds
- **Risk Limit Breach**: Immediate notification

## 🔒 Security Configuration

### Database Security
1. **Authentication**: md5 authentication required
2. **Replication User**: Dedicated user with minimal privileges
3. **Network**: Restricted access via Docker networks
4. **Encryption**: SSL/TLS for connections (configure certificates)

### Application Security
1. **JWT Tokens**: Configurable expiration (default: 8 hours)
2. **Data Encryption**: AES-256 for sensitive data
3. **CORS**: Restricted to known domains
4. **Rate Limiting**: 1000 requests/minute per IP

### Network Security
```bash
# Firewall configuration (adjust for your environment)
# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow database (restrict to application servers)
sudo ufw allow from <app-server-ip> to any port 5432

# Allow monitoring (restrict to admin network)
sudo ufw allow from <admin-network> to any port 3001
sudo ufw allow from <admin-network> to any port 9090
```

## 💾 Backup and Recovery

### Automated Backup
Backup script runs daily at 2 AM (configurable):
```bash
# Manual backup execution
./scripts/backup/backup-database.sh --type full

# Incremental backup
./scripts/backup/backup-database.sh --type incremental

# Restore from backup
./scripts/backup/restore-database.sh --file /backups/backup_file.sql.gz
```

### Backup Types
1. **Logical Backup** (pg_dump): Best for small-medium databases
2. **Physical Backup** (pg_basebackup): Best for large databases
3. **Incremental Backup**: Daily changes only

### S3 Integration
Backups are automatically uploaded to S3 if configured:
```bash
# S3 bucket structure
s3://your-backup-bucket/
├── oil-trading-backups/
│   ├── 2024/01/15/
│   │   ├── OilTradingDb_full_20240115_020000.sql.gz.gpg
│   │   └── OilTradingDb_full_20240115_020000.sql.gz.gpg.sha256
```

## 🔍 Health Checks and Maintenance

### Health Check Endpoints
- **Application Health**: `GET /api/health`
- **Detailed Health**: `GET /api/health/detailed`
- **Liveness Probe**: `GET /api/health/liveness`
- **Readiness Probe**: `GET /api/health/readiness`

### Regular Maintenance Tasks

#### Daily
- Monitor alert notifications
- Review backup success
- Check system resource usage

#### Weekly
- Review database performance metrics
- Analyze slow query reports
- Update security patches

#### Monthly
- Full system backup verification
- Performance optimization review
- Capacity planning assessment

### Database Maintenance
```bash
# Connect to master database
docker-compose -f docker-compose.production.yml exec postgres-master psql -U postgres -d OilTradingDb

# Check replication status
SELECT * FROM pg_stat_replication;

# Check database size
SELECT pg_size_pretty(pg_database_size('OilTradingDb'));

# Analyze table statistics
ANALYZE;

# Reindex if needed
REINDEX DATABASE OilTradingDb;
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Replica Lag Issues
```bash
# Check replication lag
docker-compose exec postgres-master psql -U postgres -c "SELECT * FROM pg_stat_replication;"

# If lag is high, check network and disk I/O
```

#### 2. API Connection Timeouts
```bash
# Check connection pool status
curl http://localhost/api/health/detailed

# Restart API instances if needed
docker-compose -f docker-compose.production.yml restart oil-trading-api-1 oil-trading-api-2
```

#### 3. Redis Memory Issues
```bash
# Check Redis memory usage
docker-compose exec redis-master redis-cli info memory

# Clear cache if needed (use with caution)
docker-compose exec redis-master redis-cli flushdb
```

#### 4. Database Connection Pool Exhaustion
```bash
# Check active connections
docker-compose exec postgres-master psql -U postgres -c "SELECT count(*) FROM pg_stat_activity;"

# Adjust max_connections if needed
```

### Log Locations
- **Application Logs**: `./logs/oil-trading-*.log`
- **Nginx Logs**: `./logs/nginx/access.log`, `./logs/nginx/error.log`
- **Database Logs**: Inside PostgreSQL containers
- **Container Logs**: `docker-compose logs <service-name>`

## 📈 Performance Optimization

### Database Optimization
1. **Query Performance**: Use `EXPLAIN ANALYZE` for slow queries
2. **Index Optimization**: Monitor unused indexes
3. **Connection Pooling**: Tune pool sizes based on load
4. **Vacuum Strategy**: Configure auto-vacuum settings

### Application Optimization
1. **Caching Strategy**: Redis for frequently accessed data
2. **Connection Management**: Use read replicas for reports
3. **Async Processing**: Background jobs for heavy operations

### System Optimization
1. **Resource Allocation**: Monitor and adjust container limits
2. **Network**: Use fast storage (SSD) for database
3. **Load Balancing**: Add more API instances as needed

## 🔄 Scaling Strategies

### Horizontal Scaling
1. **API Instances**: Add more containers behind load balancer
2. **Read Replicas**: Add additional read-only database replicas
3. **Redis Cluster**: Implement Redis clustering for cache scaling

### Vertical Scaling
1. **Database Resources**: Increase memory and CPU allocation
2. **Storage**: Use faster storage (NVMe SSD)
3. **Network**: Upgrade to higher bandwidth

## 📝 Deployment Checklist

### Pre-Deployment
- [ ] System requirements verified
- [ ] Docker and Docker Compose installed
- [ ] Environment variables configured
- [ ] SSL certificates prepared (for HTTPS)
- [ ] Firewall rules configured
- [ ] Backup strategy planned

### Deployment
- [ ] Run deployment script or manual steps
- [ ] Verify all containers are running
- [ ] Test health check endpoints
- [ ] Verify database replication
- [ ] Test API functionality
- [ ] Configure monitoring alerts

### Post-Deployment
- [ ] Update DNS records (if applicable)
- [ ] Configure external monitoring
- [ ] Set up log aggregation
- [ ] Test backup and recovery procedures
- [ ] Document any customizations
- [ ] Train operational team

## 📞 Support and Contacts

### Documentation
- **API Documentation**: Available at `/swagger` endpoint
- **Architecture Docs**: See `CLAUDE.md` for system architecture
- **Troubleshooting**: See troubleshooting section above

### Monitoring
- **Grafana**: Real-time dashboards and metrics
- **Prometheus**: Raw metrics and alerting
- **Application Logs**: Structured logging with Serilog

### Emergency Procedures
1. **API Outage**: Restart API containers, check database connectivity
2. **Database Issues**: Check replication status, failover to replica if needed
3. **Performance Issues**: Scale API instances, check resource usage
4. **Data Loss**: Restore from latest backup, assess data integrity

---

**Last Updated**: January 2025
**Version**: 1.0.0
**Environment**: Production PostgreSQL Deployment