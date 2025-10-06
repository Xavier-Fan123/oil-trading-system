# Oil Trading System - Monitoring and Operations Guide

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Quick Start](#quick-start)
4. [Monitoring Components](#monitoring-components)
5. [Health Checks](#health-checks)
6. [Performance Monitoring](#performance-monitoring)
7. [Alert Management](#alert-management)
8. [Troubleshooting](#troubleshooting)
9. [Maintenance Tasks](#maintenance-tasks)
10. [Emergency Procedures](#emergency-procedures)

## 📊 Overview

The Oil Trading System monitoring infrastructure provides comprehensive observability across the entire application stack, from business metrics to system resources. This guide covers all operational aspects of the monitoring system.

### Key Features
- **Real-time Monitoring**: Continuous monitoring of all system components
- **Business Metrics**: Trading-specific KPIs and risk indicators
- **Distributed Tracing**: End-to-end request tracing with OpenTelemetry
- **Log Aggregation**: Centralized logging with ELK Stack
- **Alerting**: Multi-channel alert notifications
- **Performance Analysis**: Automated performance reports and bottleneck detection

## 🏗️ Architecture

### Monitoring Stack Components

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Grafana       │    │   Prometheus    │    │   AlertManager  │
│   (Dashboards)  │    │   (Metrics)     │    │   (Alerts)      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   ELK Stack     │    │   Oil Trading   │    │   OpenTelemetry │
│   (Logs)        │    │   API           │    │   (Tracing)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Exporters     │    │   Infrastructure│    │   Business      │
│   (System)      │    │   (DB, Cache)   │    │   Logic         │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Data Flow
1. **Metrics Collection**: Prometheus scrapes metrics from various exporters
2. **Log Aggregation**: Logs flow through Logstash to Elasticsearch
3. **Trace Collection**: OpenTelemetry sends traces to Jaeger/Tempo
4. **Visualization**: Grafana displays unified dashboards
5. **Alerting**: AlertManager processes and routes alerts

## 🚀 Quick Start

### Starting the Monitoring Stack

**Windows:**
```batch
start-monitoring-stack.bat
```

**Linux/macOS:**
```bash
./scripts/monitoring/start-monitoring-stack.sh
```

### Stopping the Monitoring Stack

**Windows:**
```batch
stop-monitoring-stack.bat
```

**Linux/macOS:**
```bash
./scripts/monitoring/stop-monitoring-stack.sh
```

### Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| Grafana | http://localhost:3000 | admin/admin123 |
| Prometheus | http://localhost:9090 | None |
| AlertManager | http://localhost:9093 | None |
| Kibana | http://localhost:5601 | None |
| Jaeger | http://localhost:16686 | None |
| API Health | http://localhost:8080/health | None |
| API Metrics | http://localhost:8080/metrics | None |

## 📡 Monitoring Components

### 1. Prometheus (Metrics)
- **Purpose**: Time-series metrics collection and storage
- **Port**: 9090
- **Configuration**: `monitoring/prometheus.yml`
- **Data Retention**: 30 days (configurable)

**Key Metrics Collected:**
- System metrics (CPU, memory, disk)
- Application metrics (response time, throughput)
- Business metrics (contracts, trading volume)
- Infrastructure metrics (database, cache)

### 2. Grafana (Visualization)
- **Purpose**: Metrics visualization and dashboards
- **Port**: 3000
- **Configuration**: `monitoring/grafana/`
- **Default Login**: admin/admin123

**Pre-configured Dashboards:**
- Oil Trading Comprehensive Overview
- System Performance
- Business KPIs
- Infrastructure Health

### 3. ELK Stack (Logging)

#### Elasticsearch
- **Purpose**: Log storage and search
- **Port**: 9200
- **Indices**: `oil-trading-*`
- **Retention**: Configurable via ILM policies

#### Logstash
- **Purpose**: Log processing and enrichment
- **Port**: 5044 (Beats), 5000 (TCP/UDP)
- **Configuration**: `monitoring/logstash/pipeline/`

#### Kibana
- **Purpose**: Log visualization and analysis
- **Port**: 5601
- **Index Patterns**: `oil-trading-*`

### 4. Jaeger (Distributed Tracing)
- **Purpose**: Request tracing and performance analysis
- **Port**: 16686
- **Storage**: In-memory (configurable)
- **Integration**: OpenTelemetry

### 5. AlertManager (Alerting)
- **Purpose**: Alert routing and notification
- **Port**: 9093
- **Configuration**: `monitoring/alertmanager/alertmanager.yml`
- **Channels**: Email, Slack, PagerDuty

## 🏥 Health Checks

### API Health Endpoints

| Endpoint | Purpose | Expected Response |
|----------|---------|-------------------|
| `/health` | Basic health check | 200 OK with JSON status |
| `/health/ready` | Kubernetes readiness | 200 OK when ready to serve traffic |
| `/health/live` | Kubernetes liveness | 200 OK when application is alive |
| `/health/detailed` | Comprehensive health | Detailed JSON with all subsystems |

### Manual Health Check Script

```bash
# Run comprehensive health check
./scripts/monitoring/health-check-system.sh --detailed

# Check specific components
./scripts/monitoring/health-check-system.sh --format json

# Generate Prometheus metrics
./scripts/monitoring/health-check-system.sh --format prometheus
```

### Health Check Automation

The system includes automated health monitoring via:
- Prometheus blackbox exporter
- Container health checks
- Application-level health endpoints
- Performance monitoring script

## 📈 Performance Monitoring

### Performance API Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/api/performance/report` | Generate performance report |
| `/api/performance/metrics/summary` | Current metrics summary |
| `/api/performance/trends` | Performance trends analysis |
| `/api/performance/bottlenecks` | Bottleneck identification |
| `/api/performance/benchmark` | Run performance benchmarks |
| `/api/performance/alerts` | Performance-based alerts |

### Automated Performance Monitoring

```bash
# Start continuous performance monitoring
./scripts/monitoring/performance-monitor.sh

# Custom configuration
./scripts/monitoring/performance-monitor.sh \
  --interval 30 \
  --cpu-threshold 70 \
  --memory-threshold 75 \
  --slack-webhook "https://hooks.slack.com/..."
```

### Performance Reports

Reports are automatically generated:
- **Hourly**: Basic performance summary
- **Daily**: Comprehensive analysis with trends
- **Weekly**: Full system analysis with recommendations

Access reports via:
- API: `GET /api/performance/report?format=json|csv|pdf`
- File system: `./reports/performance-*.json`

## 🚨 Alert Management

### Alert Categories

| Category | Severity | Response Time | Escalation |
|----------|----------|---------------|------------|
| Critical | Critical | Immediate | PagerDuty + Phone |
| Business | High | 15 minutes | Email + Slack |
| Performance | Medium | 30 minutes | Slack |
| System | Low | 1 hour | Email |

### Alert Rules

**System Alerts:**
- API Down (1 minute)
- High CPU (>80% for 10 minutes)
- High Memory (>90% for 5 minutes)
- Disk Space Low (<10%)
- Database Connection Failures

**Business Alerts:**
- High Risk Exposure (>$100M)
- Contracts Expiring (>20 in 24h)
- Price Feed Stale (>5 minutes)
- Settlement Failures
- Position Limit Breach (>95%)

**Performance Alerts:**
- High Response Time (P95 >2s for 5 minutes)
- High Error Rate (>5% for 5 minutes)
- Database Slow Queries (>1s for 5 minutes)
- Cache Miss Rate (>20%)

### Alert Configuration

Edit alert rules in:
- Prometheus: `monitoring/rules/oil-trading-alerts.yml`
- Grafana: `monitoring/grafana/alerting/alerting.yml`
- AlertManager: `monitoring/alertmanager/alertmanager.yml`

### Notification Channels

**Email Configuration:**
```yaml
# monitoring/alertmanager/alertmanager.yml
global:
  smtp_smarthost: 'localhost:587'
  smtp_from: 'alerts@oiltrading.com'
```

**Slack Configuration:**
```yaml
receivers:
  - name: 'slack-critical'
    slack_configs:
      - api_url: 'YOUR_SLACK_WEBHOOK_URL'
        channel: '#oil-trading-critical'
```

## 🔧 Troubleshooting

### Common Issues and Solutions

#### 1. Services Won't Start

**Symptoms:**
- Docker containers failing to start
- Port binding errors
- Service health checks failing

**Diagnosis:**
```bash
# Check container status
docker-compose -p oiltrading ps

# View container logs
docker-compose -p oiltrading logs <service_name>

# Check port availability
netstat -tlnp | grep :9090
```

**Solutions:**
- Ensure required ports are available
- Check Docker daemon is running
- Verify sufficient system resources
- Review service logs for specific errors

#### 2. High Memory Usage

**Symptoms:**
- Memory usage alerts
- Application slowdown
- Container restarts

**Diagnosis:**
```bash
# Check system memory
free -h

# Check container memory usage
docker stats

# Check application metrics
curl http://localhost:8080/api/performance/metrics/summary
```

**Solutions:**
- Restart services with high memory usage
- Review application logs for memory leaks
- Adjust container memory limits
- Scale horizontally if needed

#### 3. Database Connection Issues

**Symptoms:**
- Database health check failures
- Application errors in logs
- Slow query performance

**Diagnosis:**
```bash
# Check database container
docker-compose -p oiltrading logs postgres

# Test database connectivity
docker-compose -p oiltrading exec postgres pg_isready -U postgres

# Check connection count
curl http://localhost:9187/metrics | grep pg_stat_database_numbackends
```

**Solutions:**
- Restart database container
- Check connection string configuration
- Verify database credentials
- Monitor connection pool usage

#### 4. Missing Metrics

**Symptoms:**
- Empty dashboards
- No data in Prometheus
- Exporter targets down

**Diagnosis:**
```bash
# Check Prometheus targets
curl http://localhost:9090/api/v1/targets

# Check exporter status
curl http://localhost:9100/metrics
curl http://localhost:9187/metrics

# Verify Prometheus configuration
docker-compose -p oiltrading exec prometheus cat /etc/prometheus/prometheus.yml
```

**Solutions:**
- Restart affected exporters
- Verify network connectivity
- Check Prometheus configuration
- Ensure exporters are properly configured

#### 5. Alert Notifications Not Working

**Symptoms:**
- Alerts firing but no notifications
- AlertManager shows failed sends
- Missing notification channels

**Diagnosis:**
```bash
# Check AlertManager status
curl http://localhost:9093/api/v1/status

# View AlertManager configuration
curl http://localhost:9093/api/v1/config

# Check alert rules
curl http://localhost:9090/api/v1/rules
```

**Solutions:**
- Verify notification channel configurations
- Test webhook URLs manually
- Check email server connectivity
- Review AlertManager logs

### Log Analysis

#### Important Log Locations

| Component | Log Location | Key Patterns |
|-----------|--------------|--------------|
| Application | `./logs/oil-trading-*.txt` | ERROR, FATAL, Exception |
| Containers | `docker logs <container>` | Error patterns |
| System | `/var/log/syslog` | System errors |
| Performance | `./logs/performance-monitor.log` | ALERT patterns |

#### Log Analysis Commands

```bash
# Find application errors
grep -i "error\|exception\|fatal" ./logs/oil-trading-*.txt

# Monitor real-time logs
tail -f ./logs/oil-trading-$(date +%Y%m%d).txt

# Search for specific patterns
grep "Database" ./logs/oil-trading-*.txt | grep -i "timeout\|connection"

# Performance alerts
grep "ALERT" ./logs/performance-monitor-alerts.log
```

## 🔄 Maintenance Tasks

### Daily Tasks

1. **System Health Check**
```bash
./scripts/monitoring/health-check-system.sh --detailed
```

2. **Review Alerts**
```bash
curl http://localhost:9093/api/v1/alerts | jq '.data[] | select(.state=="firing")'
```

3. **Check Disk Space**
```bash
df -h
du -sh ./logs/* | sort -h
```

### Weekly Tasks

1. **Performance Report Review**
```bash
curl http://localhost:8080/api/performance/report > weekly-report.json
```

2. **Log Cleanup**
```bash
./scripts/monitoring/cleanup-monitoring-data.sh --retention 7
```

3. **Update Dashboards**
- Review Grafana dashboards for optimization
- Add new business metrics as needed
- Update alert thresholds based on trends

### Monthly Tasks

1. **Comprehensive System Review**
```bash
./scripts/monitoring/cleanup-monitoring-data.sh --retention 30
```

2. **Capacity Planning**
- Review resource usage trends
- Plan for scaling requirements
- Update alert thresholds

3. **Configuration Backup**
```bash
# Backup monitoring configurations
tar -czf monitoring-backup-$(date +%Y%m%d).tar.gz monitoring/
```

### Automated Maintenance

Set up cron jobs for automated maintenance:

```bash
# Add to crontab
0 2 * * * /path/to/cleanup-monitoring-data.sh --retention 30
0 6 * * 1 /path/to/health-check-system.sh --detailed --format json > /var/log/weekly-health-check.json
```

## 🚨 Emergency Procedures

### System Down Procedure

1. **Immediate Assessment**
```bash
# Quick health check
curl -f http://localhost:8080/health || echo "API DOWN"

# Check all containers
docker-compose -p oiltrading ps

# Check system resources
top
df -h
```

2. **Service Recovery**
```bash
# Restart all services
./stop-monitoring-stack.bat
./start-monitoring-stack.bat

# Or restart specific service
docker-compose -p oiltrading restart <service_name>
```

3. **Validation**
```bash
# Wait for services to stabilize
sleep 60

# Run health check
./scripts/monitoring/health-check-system.sh
```

### High Risk Exposure Procedure

1. **Immediate Notification**
- Alert trading team immediately
- Notify risk management
- Document exposure details

2. **System Check**
```bash
# Check current risk metrics
curl http://localhost:8080/api/risk/calculate

# Review active contracts
curl http://localhost:8080/api/metrics/business
```

3. **Monitoring Enhancement**
- Increase monitoring frequency
- Set up additional alerts
- Prepare for position adjustments

### Data Loss Prevention

1. **Backup Current State**
```bash
# Backup volumes
docker run --rm -v oiltrading_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup-$(date +%Y%m%d_%H%M%S).tar.gz /data

# Backup configurations
cp -r monitoring/ backup/monitoring-$(date +%Y%m%d_%H%M%S)/
```

2. **Document Incident**
- Record all actions taken
- Note system state before/after
- Prepare incident report

### Contact Information

**Emergency Contacts:**
- System Administrator: admin@oiltrading.com
- Trading Team: trading-team@oiltrading.com
- DevOps Team: devops@oiltrading.com
- On-Call Engineer: +1-XXX-XXX-XXXX

**Escalation Matrix:**
1. Level 1: DevOps Team (5 minutes)
2. Level 2: System Administrator (15 minutes)
3. Level 3: Management (30 minutes)

## 📚 Additional Resources

### Documentation Links
- [API Documentation](http://localhost:8080/swagger)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Elasticsearch Documentation](https://www.elastic.co/guide/)

### Training Materials
- Monitoring Best Practices
- Alert Management Guidelines
- Performance Optimization Guide
- Troubleshooting Playbooks

### Support Channels
- Internal Wiki: https://wiki.oiltrading.com
- Support Tickets: support@oiltrading.com
- Community Forum: https://forum.oiltrading.com

---

## 📝 Revision History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-01-XX | Initial version | Claude Code |
| 1.1 | TBD | Performance updates | TBD |

---

*This guide is maintained as part of the Oil Trading System documentation. For updates or corrections, please contact the DevOps team.*