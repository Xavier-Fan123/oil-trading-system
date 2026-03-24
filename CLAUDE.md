# CLAUDE.md - Oil Trading System v2.22.0

## Project Overview

Enterprise Oil Trading and Risk Management System.
- .NET 9 + EF Core 9 backend, React 18 + TypeScript + MUI frontend
- Clean Architecture + DDD + CQRS (MediatR)
- PostgreSQL (production) / SQLite (development), Redis cache
- 1,204 tests passing (100%), 85.1% code coverage

## Quick Start

```batch
Double-click: START-ALL.bat
```
Starts Redis (6379) + Backend API (5000) + Frontend (3002) in ~25 seconds.

### Manual Startup
```bash
# Terminal 1: Redis
cd "C:\Users\itg\Desktop\X\redis" && redis-server.exe redis.windows.conf

# Terminal 2: Backend
cd "C:\Users\itg\Desktop\X\src\OilTrading.Api" && dotnet run

# Terminal 3: Frontend (run as Administrator)
cd "C:\Users\itg\Desktop\X\frontend" && npm run dev
```

## URLs
- **Frontend**: http://localhost:3002/
- **Backend API**: http://localhost:5000/
- **Swagger**: http://localhost:5000/swagger
- **Health**: http://localhost:5000/health

## Project Structure

```
X/
  src/
    OilTrading.Api/              # ASP.NET Core API (controllers, middleware)
    OilTrading.Application/      # CQRS commands/queries, DTOs, services
    OilTrading.Core/             # Domain entities, value objects, interfaces
    OilTrading.Infrastructure/   # EF Core, repositories, external services
  frontend/                      # React + TypeScript + Vite
  tests/
    OilTrading.Tests/            # 647 tests
    OilTrading.UnitTests/        # 161 tests
    OilTrading.IntegrationTests/ # 34 tests
  redis/                         # Redis binary & config
  OilTrading.sln
  START-ALL.bat
```

## API Routing

All endpoints use `/api/` base path (no versioning):
- `/api/purchase-contracts/*`, `/api/sales-contracts/*`
- `/api/products/*`, `/api/trading-partners/*`
- `/api/settlements/*`, `/api/purchase-settlements/*`, `/api/sales-settlements/*`
- `/api/shipping-operations/*`, `/api/contract-matching/*`
- `/api/positions/*`, `/api/risk/*`, `/api/dashboard/*`
- `/api/market-data/*`, `/api/benchmark-pricing/*`

Frontend services all use `baseURL: 'http://localhost:5000/api'`.

## Domain Model

### Core Entities
- **PurchaseContract / SalesContract** - Full lifecycle (Draft > PendingApproval > Active > Completed)
- **ContractMatching** - Manual purchase-to-sales matching for natural hedging
- **ContractSettlement** - Mixed-unit settlement with B/L data
- **ShippingOperation** - Loading, discharge, delivery logistics
- **TradingPartner** - Suppliers/customers with credit management
- **Product** - Oil products (Brent, WTI, MGO, HFO380, etc.)

### Value Objects (EF Core OwnsOne configuration)
- **Money** (Amount + Currency), **Quantity** (Value + Unit)
- **PriceFormula** (mixed-unit: BenchmarkUnit + AdjustmentUnit)
- **ContractNumber**, **DeliveryTerms**, **SettlementType**

### Settlement Architecture (v2.10.0)
Two specialized repositories for type-safe operations:
- **IPurchaseSettlementRepository** - AP (supplier payments)
- **ISalesSettlementRepository** - AR (buyer payments)

## Critical Configuration Notes

### ENCODING WARNING
- **NEVER use Chinese/Unicode characters** in batch files, scripts, or config files
- **ASCII only** for all filenames and script content

### Windows Node.js Paths
If npm commands fail, use explicit paths:
```cmd
"D:\node.exe" --version
"D:\npm.cmd" install
```

### npm Permission Issues
Always run `npm install` as **Administrator** on Windows.

### Redis Required for Performance
- Without Redis: API responses 20+ seconds
- With Redis: API responses <200ms
- Config: `localhost:6379` in `appsettings.json`
- Graceful fallback if Redis unavailable (slow but functional)

### Database
- Development: SQLite (`oiltrading.db`) or In-Memory
- Production: PostgreSQL 15 with master-slave replication
- Connection: `appsettings.json` > `ConnectionStrings:DefaultConnection`

### WebSocket HMR (Vite)
Separate ports needed on Windows - configured in `vite.config.ts`:
```typescript
server: { port: 3000, hmr: { overlay: false, port: 3001 }, watch: { usePolling: true } }
```

## EF Core Configuration

Value objects must use `OwnsOne`. Computed properties must be `.Ignore()`d.
Indexes on owned entities go inside the `OwnsOne` block:
```csharp
builder.OwnsOne(e => e.ContractNumber, cn => {
    cn.Property(c => c.Value).HasColumnName("ContractNumber").IsRequired();
    cn.HasIndex(c => c.Value).IsUnique();
});
```

## Testing

```bash
dotnet test OilTrading.sln --verbosity minimal          # All tests
dotnet test tests/OilTrading.UnitTests                   # Unit tests only
dotnet test tests/OilTrading.IntegrationTests            # Integration tests only
```

## Frontend Troubleshooting

If API calls fail after changes:
1. Stop frontend (Ctrl+C)
2. Clear Vite cache: `rmdir /s /q node_modules\.vite`
3. Clear browser cache (Ctrl+Shift+Delete)
4. Restart: `npm run dev`

## Troubleshooting "Field Missing" / Validation Errors

Root cause priority: (1) DataSeeder not populating field > (2) DTO missing property > (3) AutoMapper config > (4) validation too strict.

Quick fix: check DataSeeder.cs for missing Update* calls, delete `oiltrading.db*`, restart.

## Documentation Index

- `ARCHITECTURE_BLUEPRINT.md` - System architecture, CQRS design
- `COMPLETE_ENTITY_REFERENCE.md` - All 47 domain entities
- `SETTLEMENT_ARCHITECTURE.md` - Settlement system deep dive
- `ADVANCED_FEATURES_GUIDE.md` - Inventory, derivatives, automation
- `PRODUCTION_DEPLOYMENT_GUIDE.md` - Infrastructure & deployment
- `API_REFERENCE_COMPLETE.md` - All 59+ REST endpoints
- `SECURITY_AND_COMPLIANCE.md` - Auth, RBAC, audit, encryption
- `TESTING_AND_QUALITY.md` - Testing strategy & CI/CD
