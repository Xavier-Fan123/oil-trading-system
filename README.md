# Oil Trading & Risk Management System v2.7.0

An enterprise-grade oil trading and risk management platform built with .NET 9 and React 18, implementing Clean Architecture principles with advanced contract matching, external contract number resolution, and full settlement/shipping operation management.

**Latest Update (Oct 30, 2025)**: Complete external contract number resolution system implemented - users can now create settlements and shipping operations using external contract numbers instead of GUIDs.

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
- **Contract Settlements**: http://localhost:3000/settlements (NEW v2.2.0)
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
- **TradeGroups** - Multi-strategy trading group management (NEW ✨)
- **Tags** - Trading strategy classification and risk management tags (UPDATED ✨)

### Key Features

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

- [x] **API Routing Fix (v2.6.1)** - Fixed mixed API versioning, all endpoints working ✨
- [x] **100% Test Pass Rate (v2.6.0)** - 100/100 unit tests passing, 85.1% coverage
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