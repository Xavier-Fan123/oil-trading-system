# Oil Trading System

Oil Trading System is a full-stack platform for physical oil trading, settlement, position management, and operational workflows. This repository combines the .NET 9 backend, the React/Vite frontend, automated tests, and deployment assets in a single monorepo.

## What Is In This Repository

- ASP.NET Core API with Clean Architecture layers under `src/`
- React 18 + TypeScript + Vite client under `frontend/`
- xUnit-based unit, integration, and benchmark suites under `tests/`
- Docker Compose, Helm, Kubernetes, Nginx, and monitoring assets for deployment and operations

## Core Capabilities

- Purchase and sales contract lifecycle management
- Contract matching and settlement workflows
- Position tracking and risk analytics
- Market data ingestion and reporting
- Shipping operations and trade-group support

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- Docker Desktop or a local Redis/PostgreSQL setup for reproducible local runs

### Recommended Local Start on Windows

```powershell
.\START-ALL.bat
```

The startup script launches the bundled Redis server, the ASP.NET Core API on `http://localhost:5000`, and the Vite frontend on `http://localhost:3002`.

### Manual Development Start

```powershell
# Redis
cd redis
.\redis-server.exe .\redis.windows.conf

# API
cd ..\src\OilTrading.Api
dotnet run

# Frontend
cd ..\..\frontend
npm install
npm run dev
```

Swagger is available at `http://localhost:5000/swagger`, and the health endpoint is `http://localhost:5000/health`.

### Docker Compose

```bash
docker compose up -d
```

The default compose stack provisions PostgreSQL, Redis, the API, Nginx, and the monitoring services declared in [`docker-compose.yml`](docker-compose.yml). The bundled compose settings are suitable for local or lab environments and should be hardened before public deployment.

## Repository Layout

```text
frontend/     React 18 client application
src/          .NET domain, application, infrastructure, and API projects
tests/        Unit, integration, and benchmark suites
deployment/   Environment-specific compose assets
helm/         Helm chart for cluster deployment
k8s/          Kubernetes manifests and supporting config
monitoring/   Prometheus, Grafana, ELK, and telemetry configuration
scripts/      Operational, data, and deployment scripts
```

## Engineering Practices

- Shared .NET build settings live in [`Directory.Build.props`](Directory.Build.props)
- Example environment files are provided in [`.env.example`](.env.example) and [`frontend/.env.example`](frontend/.env.example)
- GitHub Actions workflows cover build, test, and security automation
- Long-form architecture, API, and operational documentation lives in [`docs/`](docs/README.md)

## Documentation

- [Documentation index](docs/README.md)
- [Architecture blueprint](docs/architecture-blueprint.md)
- [API reference](docs/api-reference.md)
- [Advanced features guide](docs/advanced-features-guide.md)
- [Production deployment guide](docs/production-deployment-guide.md)
- [Testing and quality notes](docs/testing-and-quality.md)

## Security and Configuration

Keep local environment files and deployment secrets out of version control. Use the example env files as templates, and inject real credentials through local configuration, CI secrets, or your deployment platform.

## License

This project is released under the [MIT License](LICENSE).
