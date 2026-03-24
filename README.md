# Oil Trading & Risk Management System

Enterprise-grade oil trading platform built with .NET 9 and React 18, implementing Clean Architecture with DDD, CQRS, and advanced contract lifecycle management.

## Architecture

```
src/
├── OilTrading.Core             # Domain entities, value objects, interfaces
├── OilTrading.Application      # CQRS commands/queries (MediatR), validators, DTOs
├── OilTrading.Infrastructure   # EF Core, PostgreSQL, Redis, repositories
└── OilTrading.Api              # ASP.NET Core Web API, auth, middleware

frontend/                       # React 18 + TypeScript + MUI + Vite

tests/
├── OilTrading.Tests            # Original test suite
├── OilTrading.UnitTests        # Domain & application unit tests
├── OilTrading.IntegrationTests # API integration tests (Testcontainers)
└── OilTrading.Benchmarks       # Performance benchmarks
```

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core, EF Core 9 |
| Frontend | React 18, TypeScript, MUI, Vite, Recharts |
| Database | PostgreSQL 15 (prod), SQLite (dev) |
| Cache | Redis 7 |
| Patterns | CQRS, Repository, Unit of Work, Domain Events |
| Auth | JWT Bearer tokens, role-based access |
| Observability | OpenTelemetry, Prometheus, Serilog |
| Testing | xUnit, Moq, FluentAssertions, Bogus, Coverlet |

## Core Features

- **Purchase/Sales Contracts** -- Full lifecycle with approval workflow (Draft -> Active -> Completed)
- **Contract Matching** -- Manual purchase-to-sales matching for natural hedging
- **Settlement** -- Mixed-unit calculations (MT/BBL) with B/L data and 4-step workflow
- **Position Tracking** -- Real-time net position with VaR risk metrics
- **Market Data** -- Price feeds, basis analysis, and forward curves
- **Shipping Operations** -- Logistics and freight management
- **Trade Groups** -- Multi-leg strategy support with VaR aggregation
- **Reporting** -- Contract execution reports with Excel export

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- Redis (bundled in `redis/` for Windows)

### One-Click Start (Windows)

```
Double-click: START-ALL.bat
```

Starts Redis + Backend API (localhost:5000) + Frontend (localhost:3002) and opens browser.

### Manual Setup

```bash
# 1. Redis
cd redis
redis-server.exe redis.windows.conf

# 2. Backend API
cd src/OilTrading.Api
dotnet run                          # localhost:5000, Swagger at /swagger

# 3. Frontend
cd frontend
npm install
npm run dev                         # localhost:3002
```

### Docker

```bash
docker compose up -d                # API + PostgreSQL + Redis
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage (Cobertura XML)
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML coverage report
dotnet tool restore
dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
# Open coveragereport/index.html in browser
```

## API Documentation

Swagger UI: `http://localhost:5000/swagger`

Health check: `http://localhost:5000/health`

## License

MIT
