# PostgreSQL Setup Guide

## Overview
This guide explains how to set up and run the Oil Trading System with PostgreSQL database instead of the default SQLite.

## Prerequisites
- Docker Desktop installed and running
- .NET 9.0 SDK installed
- Git (optional, for cloning)

## Quick Start

### Option 1: Automated Setup (Recommended)
```bash
# Run the automated setup script
start-postgresql-system.bat
```

This script will:
1. Start PostgreSQL and Redis containers
2. Run database migrations
3. Seed sample data
4. Start the API with PostgreSQL configuration

### Option 2: Manual Setup

#### Step 1: Start PostgreSQL Container
```bash
docker-compose up -d postgres redis
```

#### Step 2: Run Database Migrations
```bash
cd src\OilTrading.Api
set ASPNETCORE_ENVIRONMENT=PostgreSQL
dotnet ef database update --project ..\OilTrading.Infrastructure --startup-project . --connection "Host=localhost;Database=oil_trading;Username=postgres;Password=postgres;Port=5432"
```

#### Step 3: Start the API
```bash
set ASPNETCORE_ENVIRONMENT=PostgreSQL
dotnet run
```

## Database Configuration

### Connection Details
- **Host**: localhost
- **Port**: 5432
- **Database**: oil_trading
- **Username**: postgres
- **Password**: postgres

### Connection String
```
Host=localhost;Database=oil_trading;Username=postgres;Password=postgres;Port=5432
```

## What's Included

### Docker Services
- **PostgreSQL 15** with TimescaleDB extensions
- **Redis 7** for caching
- **RabbitMQ** for messaging (optional)

### Sample Data
The system automatically seeds the following data:
- **Users**: Trader, Admin, Risk Manager accounts
- **Products**: Brent, WTI, MGO, HSFO, Jet Fuel, Gas Oil
- **Trading Partners**: Shell, BP, ExxonMobil, Vitol, Trafigura, Gunvor
- **Sample Contracts**: Purchase contract examples
- **Market Data**: 30 days of historical price data

### Database Features
- **TimescaleDB**: Optimized for time-series data (market prices, risk calculations)
- **Full-text Search**: PostgreSQL native search capabilities
- **JSONB Support**: Flexible data storage for complex objects
- **Advanced Indexing**: Optimized queries for trading operations

## API Endpoints

Once running, the API will be available at:
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Risk Dashboard**: http://localhost:5000/api/risk/calculate

## Environment Configurations

### Development (SQLite)
```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

### PostgreSQL
```bash
set ASPNETCORE_ENVIRONMENT=PostgreSQL
dotnet run
```

### Production (Docker)
```bash
docker-compose up -d
```

## Troubleshooting

### PostgreSQL Container Won't Start
```bash
# Check if port 5432 is already in use
netstat -an | findstr :5432

# Stop any conflicting PostgreSQL service
net stop postgresql-x64-13  # or your local PostgreSQL service

# Restart the container
docker-compose restart postgres
```

### Migration Errors
```bash
# Reset database (WARNING: This will delete all data)
docker-compose down -v
docker-compose up -d postgres
# Wait 10 seconds then run migrations again
```

### Connection Timeout
```bash
# Check container logs
docker-compose logs postgres

# Ensure container is healthy
docker-compose ps
```

### Performance Issues
```bash
# Check PostgreSQL performance
docker exec -it oil-trading-postgres psql -U postgres -d oil_trading -c "SELECT * FROM pg_stat_activity;"

# Monitor container resources
docker stats oil-trading-postgres
```

## Advanced Configuration

### Custom Database Settings
Edit `docker-compose.yml` to customize PostgreSQL settings:
```yaml
services:
  postgres:
    environment:
      POSTGRES_DB: oil_trading
      POSTGRES_USER: your_user
      POSTGRES_PASSWORD: your_password
    command: 
      - "postgres"
      - "-c"
      - "max_connections=200"
      - "-c"
      - "shared_buffers=256MB"
```

### TimescaleDB Hypertables
For high-performance time-series data:
```sql
-- Convert market_prices to hypertable
SELECT create_hypertable('market_prices', 'timestamp');

-- Create compression policy
SELECT add_compression_policy('market_prices', INTERVAL '7 days');
```

### Backup and Restore
```bash
# Backup
docker exec oil-trading-postgres pg_dump -U postgres oil_trading > backup.sql

# Restore
docker exec -i oil-trading-postgres psql -U postgres oil_trading < backup.sql
```

## Monitoring

### Database Metrics
- Connect to pgAdmin: http://localhost:8080 (if enabled)
- Use built-in PostgreSQL monitoring queries
- Check logs: `docker-compose logs postgres`

### Application Metrics
- Health endpoint: http://localhost:5000/health
- Application logs: `logs/oil-trading-*.txt`
- Serilog structured logging enabled

## Security Considerations

### Production Setup
1. Change default passwords
2. Use environment variables for secrets
3. Enable SSL/TLS connections
4. Configure proper firewall rules
5. Regular security updates

### Access Control
```sql
-- Create read-only user for reporting
CREATE USER reporting_user WITH PASSWORD 'secure_password';
GRANT SELECT ON ALL TABLES IN SCHEMA public TO reporting_user;
```

## Next Steps

1. **Frontend Integration**: The React frontend automatically works with PostgreSQL
2. **Risk Engine**: Python risk calculations are database-agnostic
3. **API Testing**: Use Swagger UI to test all endpoints
4. **Custom Development**: Add your business logic using the existing patterns

## Support

For issues or questions:
1. Check logs: `docker-compose logs`
2. Review this documentation
3. Check the main README.md for general system information