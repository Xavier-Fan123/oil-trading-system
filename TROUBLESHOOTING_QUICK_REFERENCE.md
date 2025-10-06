# Oil Trading System - Troubleshooting Quick Reference

## 🚨 Emergency Quick Actions

### System is Down
```bash
# 1. Quick status check
docker-compose -p oiltrading ps
curl -f http://localhost:8080/health

# 2. Restart everything
./stop-monitoring-stack.bat && ./start-monitoring-stack.bat

# 3. Check logs
docker-compose -p oiltrading logs --tail=50 oiltrading-api
```

### High CPU/Memory Usage
```bash
# 1. Check resource usage
docker stats
top -p $(pgrep -f "oil")

# 2. Restart high-usage services
docker-compose -p oiltrading restart <service_name>

# 3. Check for memory leaks
curl http://localhost:8080/api/performance/metrics/summary
```

### Database Issues
```bash
# 1. Check database status
docker-compose -p oiltrading exec postgres pg_isready -U postgres

# 2. Restart database
docker-compose -p oiltrading restart postgres

# 3. Check connections
curl http://localhost:9187/metrics | grep pg_stat_database_numbackends
```

## 🔍 Diagnostic Commands

### Service Status
| Command | Purpose |
|---------|---------|
| `docker-compose -p oiltrading ps` | Check all containers |
| `curl http://localhost:8080/health` | API health |
| `curl http://localhost:9090/-/healthy` | Prometheus health |
| `curl http://localhost:9093/-/healthy` | AlertManager health |

### Performance Check
| Command | Purpose |
|---------|---------|
| `curl http://localhost:8080/api/performance/metrics/summary` | Performance summary |
| `curl http://localhost:8080/api/metrics/business` | Business metrics |
| `docker stats --no-stream` | Container resource usage |
| `free -h && df -h` | System resources |

### Log Analysis
| Command | Purpose |
|---------|---------|
| `tail -f ./logs/oil-trading-$(date +%Y%m%d).txt` | Live application logs |
| `docker-compose -p oiltrading logs --tail=100 <service>` | Container logs |
| `grep -i "error\|exception" ./logs/oil-trading-*.txt` | Find errors |
| `journalctl -u docker --since "1 hour ago"` | Docker daemon logs |

## 🚨 Common Error Patterns

### Application Errors
| Error Pattern | Likely Cause | Quick Fix |
|---------------|--------------|-----------|
| `Connection timeout` | Database/Redis down | Restart infrastructure services |
| `Port already in use` | Service conflict | Check port usage: `netstat -tlnp` |
| `OutOfMemoryException` | Memory leak | Restart API service |
| `Database connection failed` | DB not ready | Wait 30s, check DB logs |
| `ERR_CONNECTION_REFUSED` | Backend API not running | Start backend: `dotnet run` |

### Frontend/WebSocket Errors **[NEW - August 2025]**
| Error Pattern | Likely Cause | Quick Fix |
|---------------|--------------|-----------|
| `[vite] failed to connect to websocket` | Windows WebSocket port conflict | Separate HMR port in vite.config.ts |
| `WebSocket connection failed` | Same-port HTTP/WS conflict | Use `hmr: { port: 3001 }` |
| `Could not determine Node.js install directory` | npm path issues | Use explicit paths: `"D:\npm.cmd"` |
| Frontend can't load | Backend API down | Check `curl http://localhost:5000/health` |

### Container Errors
| Error Pattern | Likely Cause | Quick Fix |
|---------------|--------------|-----------|
| `Container exits immediately` | Configuration error | Check logs: `docker logs <container>` |
| `Cannot connect to Docker daemon` | Docker service down | `sudo systemctl start docker` |
| `No space left on device` | Disk full | Clean up: `docker system prune -f` |
| `Port is already allocated` | Port conflict | Change port or stop conflicting service |

### Network Errors
| Error Pattern | Likely Cause | Quick Fix |
|---------------|--------------|-----------|
| `Connection refused` | Service not running | Start service |
| `DNS resolution failed` | Network config | Check Docker network |
| `Timeout waiting for connection` | Firewall/routing | Check port accessibility |

## 📊 Service-Specific Troubleshooting

### Oil Trading API
**Symptoms**: HTTP 500 errors, slow responses
```bash
# Check API logs
docker-compose -p oiltrading logs oiltrading-api

# Check health endpoint
curl http://localhost:8080/health/detailed

# Performance metrics
curl http://localhost:8080/api/performance/alerts
```

**Common Fixes**:
- Restart API: `docker-compose -p oiltrading restart oiltrading-api`
- Check database connectivity
- Review memory usage

### PostgreSQL Database
**Symptoms**: Connection errors, slow queries
```bash
# Check database status
docker-compose -p oiltrading exec postgres pg_isready -U postgres

# View connections
docker-compose -p oiltrading exec postgres psql -U postgres -c "SELECT * FROM pg_stat_activity;"

# Check disk space
docker-compose -p oiltrading exec postgres df -h
```

**Common Fixes**:
- Restart database: `docker-compose -p oiltrading restart postgres`
- Check disk space
- Review slow queries

### Redis Cache
**Symptoms**: Cache misses, memory alerts
```bash
# Check Redis status
docker-compose -p oiltrading exec redis redis-cli ping

# Check memory usage
docker-compose -p oiltrading exec redis redis-cli info memory

# Monitor commands
docker-compose -p oiltrading exec redis redis-cli monitor
```

**Common Fixes**:
- Clear cache: `docker-compose -p oiltrading exec redis redis-cli flushall`
- Restart Redis: `docker-compose -p oiltrading restart redis`
- Check memory limits

### Prometheus
**Symptoms**: No metrics, targets down
```bash
# Check targets
curl http://localhost:9090/api/v1/targets

# Check configuration
curl http://localhost:9090/api/v1/status/config

# Reload configuration
curl -X POST http://localhost:9090/-/reload
```

**Common Fixes**:
- Restart Prometheus: `docker-compose -p oiltrading restart prometheus`
- Check target connectivity
- Verify configuration syntax

### Grafana
**Symptoms**: Dashboards not loading, no data
```bash
# Check Grafana logs
docker-compose -p oiltrading logs grafana

# Test datasource connection
curl -u admin:admin123 http://localhost:3000/api/datasources/proxy/1/api/v1/query?query=up
```

**Common Fixes**:
- Restart Grafana: `docker-compose -p oiltrading restart grafana`
- Check datasource configuration
- Verify dashboard queries

## 🔧 Recovery Procedures

### Complete System Recovery
```bash
# 1. Stop all services
./stop-monitoring-stack.bat

# 2. Clean up containers and networks
docker-compose -p oiltrading down --remove-orphans
docker network prune -f

# 3. Start fresh
./start-monitoring-stack.bat

# 4. Wait for stabilization (2-3 minutes)
sleep 180

# 5. Run health check
./scripts/monitoring/health-check-system.sh
```

### Data Recovery
```bash
# 1. Stop services
docker-compose -p oiltrading stop

# 2. Backup current volumes
docker run --rm -v oiltrading_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup.tar.gz /data

# 3. Restore from backup (if needed)
docker run --rm -v oiltrading_postgres_data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres-backup.tar.gz -C /

# 4. Start services
docker-compose -p oiltrading start
```

### Configuration Reset
```bash
# 1. Backup current config
cp -r monitoring/ backup/monitoring-$(date +%Y%m%d)/

# 2. Reset to defaults
git checkout -- monitoring/

# 3. Restart affected services
docker-compose -p oiltrading restart prometheus grafana alertmanager
```

## 📞 Escalation Path

### Level 1 (0-5 minutes)
- **Self-service**: Use this quick reference
- **Actions**: Restart services, check logs
- **Tools**: Docker commands, curl checks

### Level 2 (5-15 minutes)
- **Contact**: DevOps team (devops@oiltrading.com)
- **Provide**: Error messages, service status, steps taken
- **Tools**: Full diagnostic scripts

### Level 3 (15-30 minutes)
- **Contact**: System Administrator (admin@oiltrading.com)
- **Escalate if**: Multiple services down, data corruption suspected
- **Provide**: Complete system state, timeline of events

### Level 4 (30+ minutes)
- **Contact**: Management and vendors
- **Escalate if**: Extended outage, business impact
- **Actions**: Disaster recovery procedures

## 📋 Pre-Call Checklist

Before contacting support, gather:

### System Information
```bash
# System status
docker-compose -p oiltrading ps > system-status.txt
./scripts/monitoring/health-check-system.sh > health-check.txt

# Resource usage
docker stats --no-stream > resource-usage.txt
free -h && df -h > system-resources.txt

# Recent logs
tail -100 ./logs/oil-trading-$(date +%Y%m%d).txt > recent-logs.txt
```

### Error Information
- Exact error messages
- Time when issue started
- Steps that led to the issue
- Impact on business operations
- Steps already attempted

### Environment Details
- Which environment (dev/staging/prod)
- Recent changes or deployments
- Current system load
- Any recent alerts or warnings

## 🔍 Monitoring Queries

### Prometheus Queries for Troubleshooting
```promql
# High error rate
rate(http_requests_total{status=~"5.."}[5m]) > 0.01

# High response time
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 2

# High CPU usage
100 - (avg by (instance) (rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100) > 80

# High memory usage
(node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes) / node_memory_MemTotal_bytes * 100 > 90

# Database connections
pg_stat_database_numbackends > 50
```

### Useful Grafana Queries
```sql
-- Application logs with errors
{job="oil-trading-api"} |= "ERROR" or "FATAL" or "Exception"

-- Performance issues
{job="oil-trading-api"} |= "timeout" or "slow" or "performance"

-- Database issues
{job="oil-trading-api"} |= "database" or "connection" or "query"
```

## 🛠️ Useful Tools

### Command Line Tools
| Tool | Purpose | Installation |
|------|---------|--------------|
| `curl` | HTTP requests | Usually pre-installed |
| `jq` | JSON parsing | `apt install jq` or `brew install jq` |
| `docker` | Container management | Docker Desktop |
| `netstat` | Network connections | Usually pre-installed |
| `htop` | Process monitoring | `apt install htop` |

### Online Resources
- [Docker Documentation](https://docs.docker.com/)
- [Prometheus Querying](https://prometheus.io/docs/prometheus/latest/querying/)
- [Grafana Alerting](https://grafana.com/docs/grafana/latest/alerting/)
- [Elasticsearch Query DSL](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl.html)

---

*Keep this guide handy for quick problem resolution. For complex issues, refer to the complete [Monitoring Operations Guide](./MONITORING_OPERATIONS_GUIDE.md).*