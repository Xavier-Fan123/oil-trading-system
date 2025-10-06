# 🏭 石油交易系统生产部署指南

## 📋 部署清单

### 1. 环境要求
- **操作系统**: Linux (Ubuntu 20.04+) 或 Windows Server 2019+
- **容器平台**: Docker 20.10+ 和 Docker Compose 2.0+
- **内存**: 最小8GB RAM，推荐16GB+
- **存储**: 最小100GB SSD，推荐500GB+
- **网络**: 稳定的互联网连接，开放端口5000(API), 5432(PostgreSQL), 6379(Redis)

### 2. 核心组件
- **API服务**: ASP.NET Core 9.0 应用
- **数据库**: PostgreSQL 15 (生产级配置)
- **缓存**: Redis 7.0
- **前端**: React 18 单页应用
- **监控**: Prometheus + Grafana
- **反向代理**: Nginx

## 🗄️ 数据库配置

### PostgreSQL 生产配置
```yaml
# docker-compose.production.yml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: oil-trading-postgres
    environment:
      POSTGRES_DB: oiltrading_prod
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
    ports:
      - "5432:5432"
    restart: unless-stopped
    command: >
      postgres
      -c shared_preload_libraries=pg_stat_statements
      -c max_connections=200
      -c shared_buffers=512MB
      -c effective_cache_size=1536MB
      -c maintenance_work_mem=128MB
      -c checkpoint_completion_target=0.9
      -c wal_buffers=16MB
      -c default_statistics_target=100
      -c random_page_cost=1.1
      -c effective_io_concurrency=200
      -c work_mem=4MB
      -c min_wal_size=1GB
      -c max_wal_size=4GB

  redis:
    image: redis:7-alpine
    container_name: oil-trading-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    restart: unless-stopped
    command: redis-server --appendonly yes --maxmemory 512mb --maxmemory-policy allkeys-lru

volumes:
  postgres_data:
  redis_data:
```

## ⚙️ 应用配置

### 生产环境配置文件
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=${DB_HOST};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Port=5432;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=50;Connection Lifetime=300;"
  },
  "Cache": {
    "Redis": {
      "ConnectionString": "${REDIS_CONNECTION}",
      "InstanceName": "OilTrading_Prod"
    }
  },
  "RiskEngine": {
    "PythonPath": "/usr/bin/python3",
    "ScriptPath": "/app/Scripts/risk_engine.py"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  },
  "AllowedHosts": "*",
  "ASPNETCORE_ENVIRONMENT": "Production"
}
```

## 🐳 Docker 部署配置

### API 服务 Dockerfile 优化
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# 安装Python和依赖
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# 安装Python包
COPY requirements.txt .
RUN pip3 install -r requirements.txt

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["src/OilTrading.Api/OilTrading.Api.csproj", "src/OilTrading.Api/"]
COPY ["src/OilTrading.Application/OilTrading.Application.csproj", "src/OilTrading.Application/"]
COPY ["src/OilTrading.Core/OilTrading.Core.csproj", "src/OilTrading.Core/"]
COPY ["src/OilTrading.Infrastructure/OilTrading.Infrastructure.csproj", "src/OilTrading.Infrastructure/"]

# 还原依赖
RUN dotnet restore "src/OilTrading.Api/OilTrading.Api.csproj"

# 复制源代码
COPY . .
WORKDIR "/src/src/OilTrading.Api"

# 构建应用
RUN dotnet build "OilTrading.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OilTrading.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/requirements.txt .
COPY --from=build /src/src/OilTrading.Api/Scripts ./Scripts

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "OilTrading.Api.dll"]
```

### 完整生产 Docker Compose
```yaml
version: '3.8'

services:
  # API 服务
  oil-trading-api:
    build: 
      context: .
      dockerfile: src/OilTrading.Api/Dockerfile
    container_name: oil-trading-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_HOST=postgres
      - DB_NAME=oiltrading_prod
      - DB_USER=${DB_USER}
      - DB_PASSWORD=${DB_PASSWORD}
      - REDIS_CONNECTION=redis:6379
    ports:
      - "5000:5000"
    depends_on:
      - postgres
      - redis
    restart: unless-stopped
    volumes:
      - ./logs:/app/logs
    networks:
      - oil-trading-network

  # PostgreSQL 数据库
  postgres:
    image: postgres:15-alpine
    container_name: oil-trading-postgres
    environment:
      POSTGRES_DB: oiltrading_prod
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
      - ./backups:/backups
    ports:
      - "5432:5432"
    restart: unless-stopped
    networks:
      - oil-trading-network

  # Redis 缓存
  redis:
    image: redis:7-alpine
    container_name: oil-trading-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    restart: unless-stopped
    networks:
      - oil-trading-network

  # Nginx 反向代理
  nginx:
    image: nginx:alpine
    container_name: oil-trading-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - oil-trading-api
    restart: unless-stopped
    networks:
      - oil-trading-network

  # Prometheus 监控
  prometheus:
    image: prom/prometheus:latest
    container_name: oil-trading-prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    restart: unless-stopped
    networks:
      - oil-trading-network

  # Grafana 可视化
  grafana:
    image: grafana/grafana:latest
    container_name: oil-trading-grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./monitoring/grafana/datasources:/etc/grafana/provisioning/datasources
    restart: unless-stopped
    networks:
      - oil-trading-network

volumes:
  postgres_data:
  redis_data:
  prometheus_data:
  grafana_data:

networks:
  oil-trading-network:
    driver: bridge
```

## 🔐 安全配置

### 环境变量配置 (.env)
```env
# 数据库配置
DB_USER=oil_trading_admin
DB_PASSWORD=your_secure_password_here
DB_HOST=postgres
DB_NAME=oiltrading_prod

# Redis配置
REDIS_CONNECTION=redis:6379

# 监控配置
GRAFANA_PASSWORD=your_grafana_password

# SSL证书路径
SSL_CERT_PATH=./ssl/cert.pem
SSL_KEY_PATH=./ssl/key.pem

# API密钥 (如果需要)
API_KEY=your_api_key_here

# JWT配置 (如果实现认证)
JWT_SECRET=your_jwt_secret_key
JWT_ISSUER=OilTradingSystem
JWT_AUDIENCE=OilTradingClients
```

## 🚀 部署步骤

### 1. 服务器准备
```bash
# 更新系统
sudo apt update && sudo apt upgrade -y

# 安装Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# 安装Docker Compose
sudo apt install docker-compose-plugin

# 创建部署目录
mkdir -p /opt/oil-trading
cd /opt/oil-trading

# 克隆代码 (或上传文件)
# git clone your-repo-url .
```

### 2. 配置文件设置
```bash
# 复制环境变量文件
cp .env.example .env

# 编辑配置
nano .env

# 设置权限
chmod 600 .env
```

### 3. SSL证书配置
```bash
# 创建SSL目录
mkdir -p ssl

# 生成自签名证书 (测试用)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout ssl/key.pem -out ssl/cert.pem

# 或者使用Let's Encrypt (推荐)
# sudo snap install certbot
# sudo certbot certonly --standalone -d your-domain.com
```

### 4. 数据库初始化
```bash
# 启动数据库服务
docker-compose up -d postgres redis

# 等待数据库启动
sleep 30

# 运行数据库迁移
docker-compose exec oil-trading-api dotnet ef database update
```

### 5. 启动所有服务
```bash
# 构建并启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f oil-trading-api
```

## 📊 监控和维护

### 健康检查端点
- **API健康**: http://your-server:5000/health
- **数据库连接**: http://your-server:5000/health/db
- **Redis连接**: http://your-server:5000/health/redis

### 监控仪表板
- **Grafana**: http://your-server:3000
- **Prometheus**: http://your-server:9090

### 日志管理
```bash
# 查看API日志
docker-compose logs oil-trading-api

# 查看数据库日志
docker-compose logs postgres

# 清理旧日志
docker system prune -f
```

### 备份策略
```bash
# 数据库备份脚本
#!/bin/bash
BACKUP_DIR="/opt/oil-trading/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# 创建备份
docker-compose exec postgres pg_dump -U ${DB_USER} ${DB_NAME} > \
  "${BACKUP_DIR}/oiltrading_backup_${DATE}.sql"

# 保留最近30天的备份
find ${BACKUP_DIR} -name "*.sql" -mtime +30 -delete
```

## 🎯 性能优化

### 数据库优化
- 定期执行 `VACUUM ANALYZE`
- 监控慢查询日志
- 配置适当的连接池大小

### 应用优化
- 启用响应压缩
- 配置缓存策略
- 设置合理的超时时间

### 监控指标
- CPU和内存使用率
- 数据库连接数
- API响应时间
- 缓存命中率

## 🚨 故障排除

### 常见问题
1. **数据库连接失败**: 检查连接字符串和网络连接
2. **缓存连接失败**: 验证Redis服务状态
3. **API启动失败**: 查看应用日志和配置文件
4. **性能问题**: 监控资源使用和数据库查询

### 紧急恢复
```bash
# 快速重启服务
docker-compose restart oil-trading-api

# 从备份恢复数据库
docker-compose exec postgres psql -U ${DB_USER} -d ${DB_NAME} < backup.sql

# 清理缓存
docker-compose exec redis redis-cli FLUSHALL
```