# Oil Trading & Risk Management System v2.8.0

An enterprise-grade oil trading and risk management platform built with .NET 9 and React 18, implementing Clean Architecture principles with advanced contract matching, external contract number resolution, comprehensive settlement management, and full shipping operation capabilities.

**Latest Update (Oct 31, 2025)**: Settlement Module Redesign Complete! New v2.8.0 implements complete purchase and sales settlement workflows with separate CQRS commands/queries, dedicated REST controllers, comprehensive validation, and one-to-many settlement support. Full lifecycle: Create → Calculate → Approve → Finalize with audit trail.

## 🚀 Quick Start

**For immediate startup, see**: [STARTUP-GUIDE.md](STARTUP-GUIDE.md)

**For detailed system information, see**: [CLAUDE.md](CLAUDE.md)

## 🏗️ Architecture Overview

This solution follows Clean Architecture principles with clear separation of concerns:

- **OilTrading.Core** - Domain entities and business rules
- **OilTrading.Application** - Application services, CQRS commands/queries, and business logic
- **OilTrading.Infrastructure** - Data access, external services, and infrastructure concerns
- **OilTrading.Api** - Web API controllers and presentation layer
- **OilTrading.Web** - Next.js frontend application
- **OilTrading.Tests** - Unit and integration tests

## 🚀 Tech Stack

### Backend (.NET 9)
- **Entity Framework Core** with SQLite/PostgreSQL support
- **MediatR** for CQRS pattern implementation
- **FluentValidation** for request validation
- **AutoMapper** for object mapping
- **Serilog** for structured logging
- **Swagger/OpenAPI** for API documentation

### Frontend (React 18)
- **TypeScript** for type safety
- **Material-UI (MUI)** for comprehensive UI components
- **TanStack Query** for state management and API caching
- **Axios** for HTTP client
- **Vite** for fast development and building
- **React Router** for client-side routing

### Infrastructure
- **PostgreSQL** with TimescaleDB extension for time-series data (Production)
- **SQLite** for development and testing (Default)
- **Redis** for high-performance caching (REQUIRED - included in START.bat)
- **RabbitMQ** for message queuing (Optional)
- **Docker** for production containerization

## 🏁 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker](https://www.docker.com/get-started) (optional for production deployment)

### ⚡ Complete System Startup (Windows) - ONE CLICK

```batch
# Double-click to start Redis + Backend + Frontend + Open Browser
START.bat
```

This unified script automatically:
1. ✅ Starts Redis cache server (required for performance)
2. ✅ Launches .NET API backend (localhost:5000) 
3. ✅ Starts React frontend (localhost:3000)
4. ✅ Opens browser to the application
5. ✅ Displays complete system information and access points

### 1. Clone and Setup

```bash
git clone <repository-url>
cd oil-trading-system
```

### Alternative: Manual Setup (if START.bat doesn't work)

#### 2. Start Redis Cache Server

```bash
# Windows: Start Redis (included in project)
start-redis.bat

# Or manually:
C:\Users\itg\Desktop\X\redis\redis-server.exe C:\Users\itg\Desktop\X\redis\redis.windows.conf
```

#### 3. Database Setup (Automatic)

The system uses SQLite by default with automatic database creation and seeding.
No manual database setup required for development.

### 4. Run Backend API

```bash
# From project root
cd src/OilTrading.Api
dotnet run
```

The API will be available at `http://localhost:5000` with Swagger docs at `http://localhost:5000/swagger`

### 5. Run Frontend

```bash
# Navigate to frontend project
cd frontend

# Install dependencies (run as Administrator on Windows)
npm install

# Start development server
npm run dev
```

The web application will be available at `http://localhost:3000`

### 📍 Application URLs

- **Main Dashboard**: http://localhost:3000
- **Purchase Contracts**: http://localhost:3000/purchase-contracts
- **Sales Contracts**: http://localhost:3000/sales-contracts
- **Contract Settlements**: http://localhost:3000/settlements (NEW v2.8.0 - Complete redesign)
- **Trade Groups**: http://localhost:3000/trade-groups
- **Tags Management**: http://localhost:3000/tags
- **API Documentation**: http://localhost:5000/swagger
- **API Health Check**: http://localhost:5000/health

## 🐳 Docker Development

### Full Stack with Docker

```bash
# Build and start all services
docker-compose up --build

# Run in background
docker-compose up -d --build
```

### Services Ports

- **Web App**: http://localhost:3000
- **API**: http://localhost:8080
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## 📊 Database Schema

### Core Entities

- **Users** - System users with role-based access
- **TradingPartners** - Suppliers and customers
- **Products** - Oil products and specifications
- **PurchaseContracts** - Purchase agreements with suppliers
- **SalesContracts** - Sales agreements with customers
- **PurchaseSettlements** - Settlement management for purchase contracts (NEW v2.8.0 ✨)
- **SalesSettlements** - Settlement management for sales contracts (NEW v2.8.0 ✨)
- **TradeGroups** - Multi-strategy trading group management
- **Tags** - Trading strategy classification and risk management tags

### Key Features

- **Settlement Management (v2.8.0)** - Complete purchase and sales settlement workflows with CQRS pattern
- **Settlement Lifecycle** - Create → Calculate → Approve → Finalize with full audit trail
- **One-to-Many Settlements** - Multiple settlements per contract support
- **TradeGroup Management** - Advanced multi-leg trading strategies with 9 strategy types
- **Futures-Spot Integration** - Complete integration between paper and physical positions
- **Risk Management** - Portfolio VaR, stress testing, and risk limit monitoring
- **Contract Linking** - Link sales contracts to purchase contracts for P&L tracking
- **Multi-currency Support** - Support for different currencies
- **Flexible Pricing** - Fixed, floating, formula-based, and index pricing
- **Tag System** - Advanced trading strategy classification system
- **Audit Trail** - Complete audit trail for all transactions

## 🔧 Development Commands

### Backend

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Add migration
dotnet ef migrations add <MigrationName> --project src/OilTrading.Infrastructure --startup-project src/OilTrading.Api

# Update database
dotnet ef database update --project src/OilTrading.Infrastructure --startup-project src/OilTrading.Api

# Generate SQL script
dotnet ef migrations script --project src/OilTrading.Infrastructure --startup-project src/OilTrading.Api
```

### Frontend

```bash
# Install dependencies (run as Administrator on Windows)
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint code
npm run lint

# Type check
npx tsc --noEmit
```

## 📋 Settlement Management API (v2.8.0)

### Purchase Settlement Endpoints

Purchase settlements manage settlement operations for purchase contracts.

```
GET    /api/purchase-settlements/{settlementId}           - Get settlement details
GET    /api/purchase-settlements/contract/{contractId}    - Get all settlements for contract
POST   /api/purchase-settlements                          - Create new settlement
POST   /api/purchase-settlements/{settlementId}/calculate - Calculate settlement amounts
POST   /api/purchase-settlements/{settlementId}/approve   - Approve settlement
POST   /api/purchase-settlements/{settlementId}/finalize  - Finalize settlement (lock for editing)
```

#### Create Purchase Settlement Request

```json
{
  "purchaseContractId": "550e8400-e29b-41d4-a716-446655440000",
  "externalContractNumber": "EXT-2025-001",
  "documentNumber": "DOC-2025-001",
  "documentType": "Invoice",
  "documentDate": "2025-11-01T00:00:00Z"
}
```

#### Calculate Settlement Request

```json
{
  "calculationQuantityMT": 1000.0,
  "calculationQuantityBBL": 5000.0,
  "benchmarkAmount": 85.50,
  "adjustmentAmount": 2.25,
  "calculationNote": "Settlement calculation for November 2025"
}
```

### Sales Settlement Endpoints

Sales settlements manage settlement operations for sales contracts.

```
GET    /api/sales-settlements/{settlementId}             - Get settlement details
GET    /api/sales-settlements/contract/{contractId}      - Get all settlements for contract
POST   /api/sales-settlements                            - Create new settlement
POST   /api/sales-settlements/{settlementId}/calculate   - Calculate settlement amounts
POST   /api/sales-settlements/{settlementId}/approve     - Approve settlement
POST   /api/sales-settlements/{settlementId}/finalize    - Finalize settlement (lock for editing)
```

#### Create Sales Settlement Request

```json
{
  "salesContractId": "660e8400-e29b-41d4-a716-446655440001",
  "externalContractNumber": "EXT-2025-002",
  "documentNumber": "DOC-2025-002",
  "documentType": "Invoice",
  "documentDate": "2025-11-01T00:00:00Z"
}
```

### Settlement Response

```json
{
  "id": "770e8400-e29b-41d4-a716-446655440002",
  "contractId": "550e8400-e29b-41d4-a716-446655440000",
  "settlementNumber": "SETTLE-2025-001",
  "status": "Approved",
  "calculationQuantityMT": 1000.0,
  "calculationQuantityBBL": 5000.0,
  "benchmarkAmount": 85.50,
  "adjustmentAmount": 2.25,
  "totalAmount": 432750.00,
  "currency": "USD",
  "createdBy": "trader@example.com",
  "createdDate": "2025-11-01T10:00:00Z",
  "approvedBy": "manager@example.com",
  "approvedDate": "2025-11-01T11:00:00Z"
}
```

### Settlement Workflow

1. **Create** - Initialize settlement with contract reference and document details
2. **Calculate** - Compute settlement amounts based on quantities and prices
3. **Approve** - Validate settlement and approve for finalization
4. **Finalize** - Lock settlement preventing further edits, create audit trail

### Validation Rules

- **Quantities**: Both MT and BBL must be non-negative; at least one must be greater than zero
- **Benchmark Amount**: Must be positive (> 0)
- **Document Date**: Cannot be in the future
- **Document Type**: Must be valid enum value (Invoice, BillOfLading, etc.)
- **Contract Reference**: Must reference existing purchase or sales contract

### Error Responses

```json
{
  "status": 400,
  "error": "Validation Error",
  "details": [
    "Calculation quantity MT must be non-negative",
    "Benchmark amount must be greater than zero"
  ]
}
```

```json
{
  "status": 404,
  "error": "Not Found",
  "message": "Settlement not found with ID: 770e8400-e29b-41d4-a716-446655440002"
}
```

## 🧪 Testing

### Backend Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend Tests

```bash
# Run tests (when implemented)
npm test

# Run tests in watch mode
npm run test:watch
```

## 📁 Project Structure

```
oil-trading-system/
├── src/
│   ├── OilTrading.Api/          # Web API project
│   ├── OilTrading.Application/  # Application layer
│   ├── OilTrading.Core/         # Domain layer
│   ├── OilTrading.Infrastructure/ # Infrastructure layer
│   └── OilTrading.Web/          # Next.js frontend
├── tests/
│   └── OilTrading.Tests/        # Test projects
├── scripts/
│   └── init-db.sql              # Database initialization
├── docker-compose.yml           # Docker services
├── OilTrading.sln              # Visual Studio solution
└── README.md
```

## 🔒 Security

- JWT-based authentication (ready to implement)
- Role-based authorization
- CORS configuration for frontend integration
- Input validation with FluentValidation
- SQL injection protection with EF Core parameterized queries

## 📈 Performance

- **Caching**: Redis integration for performance optimization
- **Database**: PostgreSQL with TimescaleDB for time-series data
- **Query Optimization**: EF Core with proper indexing
- **Frontend**: Next.js with automatic code splitting and optimization

## 🚀 Deployment

### Production Environment Variables

#### Backend (.NET)
```bash
ConnectionStrings__DefaultConnection="Host=prod-db;Database=oil_trading;Username=user;Password=pass"
ConnectionStrings__Redis="prod-redis:6379"
ASPNETCORE_ENVIRONMENT=Production
```

#### Frontend (Next.js)
```bash
NEXT_PUBLIC_API_BASE_URL=https://api.yourdomain.com/api
NODE_ENV=production
```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:

1. Check the [Issues](https://github.com/yourorg/oil-trading-system/issues) page
2. Create a new issue if your problem isn't already reported
3. Contact the development team

## 🔄 Recent Updates & Roadmap

### ✅ Completed (October 2025)

- [x] **Settlement Module Redesign (v2.8.0)** - Complete CQRS implementation with dual controllers, separate services, and comprehensive validation
- [x] **Position Module Fix (v2.7.1)** - Payment terms validation and position display rendering fully functional
- [x] **Risk Override Auto-Retry (v2.7.2)** - Automatic header-based retry for concentration limit overrides
- [x] **External Contract Resolution (v2.7.0)** - Complete system for resolving external contract numbers with GUID mapping
- [x] **API Routing Fix (v2.6.1)** - Fixed mixed API versioning, all endpoints working ✨
- [x] **100% Test Pass Rate (v2.6.0)** - 842/842 unit tests passing, 85.1% coverage
- [x] **Contract Matching System (v2.3)** - Manual matching for natural hedging
- [x] **Frontend-Backend Alignment (v2.4)** - Perfect DTO and API alignment
- [x] **TradeGroup Management System** - Complete multi-strategy trading group management
- [x] **Futures-Spot Integration** - Advanced futures-physical position integration
- [x] **Tag System Redesign** - Trading strategy classification and risk management tags
- [x] **Frontend Architecture** - Migrated from Next.js to React 18 + Vite + MUI
- [x] **API Error Handling** - Robust error handling with standardized responses
- [x] **TypeScript Enhancement** - Improved type safety and enum handling

### 🚧 In Progress

- [ ] Database Migration System - Complete SQLite to PostgreSQL migration
- [ ] Real-time Data Updates - WebSocket integration for live market data

### 🎯 Future Roadmap

- [ ] User authentication and authorization with JWT
- [ ] Real-time market data integration APIs
- [ ] Advanced risk analytics dashboard with charts
- [ ] Automated contract matching algorithms
- [ ] Mobile application development
- [ ] Integration with external trading platforms
- [ ] Advanced reporting and analytics
- [ ] Audit logging and compliance features
- [ ] Performance optimization and caching
- [ ] Multi-language support