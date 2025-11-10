# Oil Trading System - Architectural Diagrams

**Complete Visual Reference for System Architecture**

---

## 1. System Layers Architecture (4-Tier Clean Architecture)

### Visual Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER (P)                             │
│                    Web API / React Frontend / CLI                        │
│                                                                           │
│  REST Controllers      React Components      WebSocket Handlers         │
│  - PurchaseContractController (7 endpoints)                             │
│  - SalesContractController (7 endpoints)                                │
│  - SettlementController (6 endpoints generic + 2 specialized)           │
│  - RiskController (5 endpoints)                                         │
│  - PositionController (4 endpoints)                                     │
│  - DashboardController (3 endpoints)                                    │
│  - And 12+ more controllers (59+ total endpoints)                       │
│                                                                           │
│  React Components:                                                       │
│  - ContractsList, PurchaseContractForm, SalesContractForm              │
│  - SettlementEntry, SettlementForm, ContractResolver                   │
│  - Dashboard (5 tabs, 7+ visualizations)                               │
│  - RiskDashboard, PositionsTable, ReportingDashboard                   │
│  - And 70+ more components                                             │
│                                                                           │
└────────────────────────────────────┬────────────────────────────────────┘
                                      │
                  HTTP/JSON (REST)    │    WebSocket (Real-time updates)
                                      │
┌────────────────────────────────────▼────────────────────────────────────┐
│                     APPLICATION LAYER (A)                               │
│            CQRS Pattern - Commands & Queries with Validation           │
│                                                                           │
│  MediatR Commands (80+ commands)    MediatR Queries (70+ queries)      │
│  ├─ CreatePurchaseContractCommand  ├─ GetPurchaseContractQuery       │
│  ├─ CreateSalesContractCommand     ├─ GetSalesContractQuery          │
│  ├─ ActivateContractCommand        ├─ GetContractsListQuery          │
│  ├─ CreateSettlementCommand        ├─ GetSettlementByIdQuery         │
│  ├─ CalculateSettlementCommand     ├─ GetDashboardSummaryQuery       │
│  ├─ ApproveSettlementCommand       ├─ GetRiskMetricsQuery            │
│  ├─ FinalizeSettlementCommand      ├─ GetNetPositionQuery            │
│  ├─ CreateShippingOperationCmd     ├─ GetShippingOperationsQuery     │
│  └─ And 72+ more commands          └─ And 62+ more queries           │
│                                                                           │
│  Pipeline Behaviors (Cross-Cutting Concerns):                          │
│  1. ValidationBehaviour ────► Validates using FluentValidation rules   │
│                               Returns 400 BadRequest if invalid       │
│  2. LoggingBehaviour ────────► Logs request details, measures time    │
│                               Logs response or exception              │
│  3. Handler ────────────────► Executes business logic                 │
│                               Uses injected repositories & services   │
│                                                                           │
│  AutoMapper Profiles (50+ mappings):                                   │
│  - PurchaseContract ↔ PurchaseContractDto (11 properties)             │
│  - SalesContract ↔ SalesContractDto (11 properties)                   │
│  - Settlement ↔ SettlementDto (35 properties)                         │
│  - And 47+ more entity ↔ DTO mappings                                │
│                                                                           │
│  Application Services (25+ services):                                  │
│  ├─ Price Calculation (Fixed, Floating, Interpolation, Basis)        │
│  ├─ Risk Calculation (VaR, Stress Testing, Concentration Limits)     │
│  ├─ Net Position Service (Position aggregation with hedging)         │
│  ├─ Settlement Services (Purchase/Sales settlement orchestration)    │
│  ├─ Dashboard Service (KPI aggregation, metrics calculation)         │
│  ├─ Cache Invalidation Service (Cache coherency management)          │
│  ├─ Settlement Rules Engine (Trigger/Condition/Action evaluation)   │
│  └─ And 18+ more services                                            │
│                                                                           │
│  FluentValidation Validators (40+ validators):                        │
│  - CreatePurchaseContractValidator (12+ rules)                        │
│  - CreateSalesContractValidator (12+ rules)                           │
│  - CalculateSettlementValidator (8+ rules)                            │
│  - And 37+ more validators                                            │
│                                                                           │
└────────────────────────────────────┬────────────────────────────────────┘
                                      │
                     Domain Objects    │    Service Boundaries
                     (Commands/Queries)│    (Handlers/Services)
                                      │
┌────────────────────────────────────▼────────────────────────────────────┐
│                      DOMAIN LAYER (D)                                   │
│            Business Logic - Entities & Value Objects                   │
│                                                                           │
│  Aggregate Roots (Main Business Entities):                             │
│  ├─ PurchaseContract (Entity: contracts from suppliers)               │
│  │  ├─ ContractNumber (Value Object: structured identifier)          │
│  │  ├─ ContractValue (Value Object: amount + currency)              │
│  │  ├─ Quantity (Value Object: value + unit MT/BBL)                 │
│  │  └─ PriceFormula (Value Object: benchmark + adjustment)          │
│  │                                                                      │
│  ├─ SalesContract (Entity: contracts to buyers)                       │
│  │  ├─ ContractNumber, ContractValue, Quantity, PriceFormula        │
│  │  └─ ApprovalWorkflow (Draft → PendingApproval → Active)          │
│  │                                                                      │
│  ├─ ContractSettlement (Entity: v2.9.0 generic system, deprecated)  │
│  │  └─ Settlement calculation entity (backward compatibility)        │
│  │                                                                      │
│  ├─ PurchaseSettlement (Entity: v2.10.0 AP-specialized)              │
│  │  ├─ SupplierContractId (FK to PurchaseContract)                 │
│  │  ├─ SupplierPaymentAmount, DueDate, ExternalNumber              │
│  │  └─ Status: Draft → DataEntered → Calculated → Reviewed → ...   │
│  │                                                                      │
│  ├─ SalesSettlement (Entity: v2.10.0 AR-specialized)                 │
│  │  ├─ CustomerContractId (FK to SalesContract)                    │
│  │  ├─ BuyerPaymentAmount, DueDate, ExternalNumber                │
│  │  └─ Status: Draft → DataEntered → Calculated → Reviewed → ...   │
│  │                                                                      │
│  ├─ ShippingOperation (Entity: logistics operations)                 │
│  │  ├─ ContractId (FK to purchase/sales contract)                  │
│  │  ├─ VesselName, LoadPort, DischargePort, LaycanStart/End        │
│  │  └─ Status: Draft → Confirmed → InTransit → Delivered          │
│  │                                                                      │
│  ├─ ContractMatching (Entity: natural hedging)                       │
│  │  ├─ PurchaseContractId, SalesContractId (many-to-many)          │
│  │  ├─ MatchedQuantity, HedgeRatio, Effectiveness                  │
│  │  └─ Impact on position calculations: reduces exposure            │
│  │                                                                      │
│  ├─ TradingPartner (Entity: suppliers and customers)                │
│  │  ├─ Name, Type (Supplier/Customer/Both)                         │
│  │  ├─ CreditLimit, PaymentTerms                                   │
│  │  └─ RiskRating, IsVerified                                      │
│  │                                                                      │
│  ├─ User (Entity: system users)                                      │
│  │  ├─ Username, PasswordHash (bcrypt 12-round)                   │
│  │  ├─ Role (18 roles: SystemAdmin, Trader, Manager, etc.)        │
│  │  ├─ Permissions (55+ granular permissions)                     │
│  │  └─ AuditLog (all actions tracked with timestamp)              │
│  │                                                                      │
│  ├─ Product (Entity: oil products)                                  │
│  │  ├─ Code (BRENT, WTI, MGO, HFO380, etc.)                       │
│  │  ├─ DefaultUnit (MT, BBL, GAL)                                 │
│  │  └─ Specifications (API gravity, sulfur content, etc.)         │
│  │                                                                      │
│  ├─ PricingEvent (Entity: price history)                           │
│  │  ├─ EventDate, Price, Source                                    │
│  │  ├─ IsPublished, IsOverride                                    │
│  │  └─ Audit trail for price changes                              │
│  │                                                                      │
│  └─ And 39+ more entities (47 total)                                │
│                                                                           │
│  Value Objects (12 value objects - immutable, no identity):           │
│  ├─ Money (Amount + Currency: 100.50 USD)                           │
│  ├─ Quantity (Value + Unit: 500 MT, 1000 BBL)                      │
│  ├─ ContractNumber (Structured identifier: ABC-2025-001)            │
│  ├─ PriceFormula (Benchmark price + Adjustment)                     │
│  ├─ DeliveryTerms (FOB, CIF, DES enum)                             │
│  ├─ SettlementType (TT, LC, DP, CAD enum)                          │
│  ├─ ContractStatus (Draft, Active, Completed enum)                 │
│  ├─ SettlementStatus (Draft → Finalized, 6 states)                │
│  ├─ UserRole (18 roles enum)                                        │
│  ├─ QuantityUnit (MT, BBL, GAL enum)                               │
│  ├─ ContractType (Purchase, Sales enum)                            │
│  └─ And 1+ more value objects                                       │
│                                                                           │
│  Business Rules & Invariants:                                         │
│  - Contract must have trading partner and product                     │
│  - Settlement amount must match calculated value                      │
│  - Quantity units must be consistent in calculations                  │
│  - Price cannot be negative or zero                                   │
│  - Settlement status transitions follow defined workflow             │
│  - Risk limits cannot be exceeded without explicit override           │
│  - Audit trail required for all financial transactions              │
│                                                                           │
│  Domain Events:                                                        │
│  - ContractCreatedEvent (fired when new contract added)              │
│  - ContractActivatedEvent (fired when contract status = Active)      │
│  - SettlementFinalizedEvent (fired when settlement finalized)        │
│  - PaymentReceivedEvent (fired when payment processed)              │
│  - And 7+ more domain events                                        │
│                                                                           │
└────────────────────────────────────┬────────────────────────────────────┘
                                      │
                   Data Repositories   │    Domain Boundaries
                   (Persistence Layer) │    (Business Logic)
                                      │
┌────────────────────────────────────▼────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER (I)                              │
│             Data Access - Repositories, DbContext, External APIs       │
│                                                                           │
│  Entity Framework Core (EF Core 9.0):                                 │
│  - DbContext: ApplicationDbContext (connects to database)             │
│  - Configurations: 47 entity configurations (one per entity)         │
│  - Migrations: 15+ EF Core migrations (applied automatically)        │
│  - Database Providers:                                                │
│    * SQLite (development, in-memory or file-based)                  │
│    * PostgreSQL 16 (production with master-slave replication)       │
│                                                                           │
│  Repositories (20+ specialized repositories):                         │
│  - IPurchaseContractRepository (8 methods: Get, List, Add, etc.)    │
│  - ISalesContractRepository (8 methods: same pattern)               │
│  - IPurchaseSettlementRepository (14 AP-specialized methods)         │
│  - ISalesSettlementRepository (14 AR-specialized methods)            │
│  - ISettlementRepository (generic settlement, 8 methods)             │
│  - IShippingOperationRepository (8 methods)                          │
│  - IContractMatchingRepository (6 methods)                           │
│  - ITradingPartnerRepository (8 methods)                             │
│  - IProductRepository (6 methods)                                    │
│  - IUserRepository (8 methods)                                       │
│  - IRiskLimitRepository (8 methods)                                  │
│  - IPricingEventRepository (10 methods)                              │
│  - And 8+ more repositories (20+ total)                             │
│                                                                           │
│  Unit of Work Pattern:                                               │
│  - IUnitOfWork interface (transaction management)                    │
│  - SaveChangesAsync() (commits all pending changes atomically)      │
│  - RollbackAsync() (reverts all changes in transaction)             │
│  - BeginTransactionAsync() (starts database transaction)            │
│  - Ensures data consistency across aggregate roots                  │
│                                                                           │
│  Caching Layer (Redis 7.0):                                          │
│  - Dashboard data cached for 5 minutes                              │
│  - Position calculations cached for 15 minutes                      │
│  - P&L calculations cached for 1 hour                               │
│  - Risk metrics cached for 15 minutes                               │
│  - Automatic cache invalidation on data changes                     │
│  - Cache hit rate: >90% for dashboard operations                   │
│  - Graceful fallback if Redis unavailable                          │
│                                                                           │
│  External APIs:                                                       │
│  - Market Data API (price feeds, benchmarks, indices)              │
│  - Trade Repository API (post-trade data, settlement info)         │
│  - Email API (notifications, confirmations, alerts)                │
│  - Webhook API (outbound notifications to external systems)        │
│  - S3 / Azure Blob (document storage, reports, archives)           │
│                                                                           │
│  Logging & Monitoring:                                              │
│  - Serilog (structured logging to console, file, database)        │
│  - Application Insights (APM, performance metrics)                 │
│  - OpenTelemetry (distributed tracing across services)            │
│  - Prometheus (metrics export for monitoring dashboards)          │
│  - ELK Stack (Elasticsearch + Logstash + Kibana)                 │
│  - All requests/responses logged with correlation IDs             │
│                                                                           │
│  Configuration Management:                                          │
│  - appsettings.json (development settings)                        │
│  - appsettings.Production.json (production settings)              │
│  - User Secrets (sensitive configuration)                         │
│  - Environment Variables (deployment-specific config)             │
│  - Feature Flags (enable/disable features without deployment)     │
│                                                                           │
│  Security Infrastructure:                                           │
│  - JWT Token Generation & Validation                              │
│  - bcrypt Password Hashing (12-round salt)                        │
│  - CORS Configuration (Cross-Origin Resource Sharing)             │
│  - HTTPS/TLS 1.3 (encrypted transport)                            │
│  - SQL Injection Prevention (parameterized queries)               │
│  - Rate Limiting (multi-level enforcement)                        │
│  - Audit Logging (all security-relevant actions)                 │
│                                                                           │
│  Database Schema (19+ tables):                                      │
│  ├─ PurchaseContracts (contract from suppliers)                    │
│  ├─ SalesContracts (contracts to buyers)                           │
│  ├─ ContractSettlements (v2.9.0 generic, deprecated)              │
│  ├─ PurchaseSettlements (v2.10.0 AP-specialized)                  │
│  ├─ SalesSettlements (v2.10.0 AR-specialized)                     │
│  ├─ ShippingOperations (logistics operations)                     │
│  ├─ ContractMatching (natural hedging relationships)              │
│  ├─ TradingPartners (suppliers and customers)                    │
│  ├─ Products (oil products)                                       │
│  ├─ Users (system users with roles/permissions)                 │
│  ├─ RiskLimits (concentration limits, VaR limits)                │
│  ├─ PricingEvents (historical pricing data)                      │
│  ├─ AuditLogs (complete audit trail)                             │
│  ├─ SettlementRules (automation rules definitions)               │
│  ├─ SettlementRuleExecutions (execution history)                 │
│  └─ And 4+ more tables                                           │
│                                                                           │
│  Database Optimization:                                            │
│  - 50+ strategic indexes (on frequently queried columns)          │
│  - Composite indexes (multi-column queries)                       │
│  - Foreign key constraints (referential integrity)                │
│  - Unique constraints (business rule enforcement)                 │
│  - Column-level defaults (audit dates, status, etc.)             │
│  - Row versioning (optimistic concurrency control)               │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer | Responsibility | Technologies | Key Patterns |
|-------|---------------|--------------|-------------|
| **Presentation** | HTTP API endpoints, request/response handling, client communication | ASP.NET Core, REST, WebSocket, OpenAPI/Swagger | Controller, Route, DTO |
| **Application** | Business logic orchestration, command/query processing, validation | MediatR, FluentValidation, AutoMapper | CQRS, Pipeline Behavior, Mediator |
| **Domain** | Business rules, entities, value objects, domain events | C#, OOP, DDD, Events | Entity, Value Object, Aggregate Root |
| **Infrastructure** | Data persistence, external integrations, logging, caching | EF Core, PostgreSQL, Redis, Serilog | Repository, Unit of Work, DAO |

### Data Flow Across Layers

```
Request Flow (Write Operation):
Client HTTP Request
    ↓
API Controller (Presentation)
    ↓ (maps DTO to Command)
MediatR Command (Application)
    ↓ (passes through behavior pipeline)
ValidationBehaviour (validates using FluentValidation)
    ↓ (if valid, continues; if invalid, returns 400)
LoggingBehaviour (logs request details)
    ↓
Command Handler (Application)
    ↓ (calls repository/service methods)
Repository Methods (Infrastructure)
    ↓ (executes database queries via EF Core)
Entity Framework Core (Infrastructure)
    ↓ (translates to SQL)
PostgreSQL Database (Infrastructure)
    ↓ (executes transaction, updates database)
Domain Event Published (Domain)
    ↓ (event handler processes side effects)
Event Handler (Application)
    ↓ (publishes notifications, updates related entities)
HTTP Response (200 OK / 201 Created)
    ↓
Client

Response Flow (Read Operation):
Client HTTP Request
    ↓
API Controller (Presentation)
    ↓ (maps query parameters)
MediatR Query (Application)
    ↓
Query Handler (Application)
    ↓ (calls repository methods)
Repository Methods + Cache Check (Infrastructure)
    ↓ (if cache hit, return cached data)
Redis Cache (Infrastructure)
    ↓ (if cache miss, query database)
PostgreSQL Database (Infrastructure)
    ↓ (returns entity data)
Entity Framework Core (Infrastructure)
    ↓ (maps database rows to entities)
AutoMapper (Application)
    ↓ (maps entity to DTO)
HTTP Response with DTO (200 OK)
    ↓
Client
```

---

## 2. CQRS Data Flow Diagram

### Complete Request Processing Pipeline

```
REQUEST ARRIVES AT CONTROLLER
├─ HTTP: POST /api/purchase-contracts
├─ Headers: Content-Type: application/json, Authorization: Bearer [token]
└─ Body: { externalContractNumber, quantity, price, laycanStart, ... }
         (JSON deserialized to CreatePurchaseContractRequest DTO)
                                │
                                ▼
                    CONTROLLER PROCESSES REQUEST
                                │
        ┌───────────────────────┼───────────────────────┐
        │                       │                       │
    1.  │ Map DTO → Command     │ Authorize Request    │ Validate Input
        │ (DTO → CreatePC...    │ (Check JWT token,    │ (Check required fields
        │  Command)             │  verify permissions) │  before CQRS)
        │                       │                       │
        └───────────────────────┴───────────────────────┘
                                │
                                ▼
                        SEND COMMAND TO MEDIATOR
                   IMediator.Send(createCommand)
                                │
                ┌───────────────┴───────────────┐
                │                               │
                │      CQRS PIPELINE            │
                │   (Behavior Pipeline)         │
                │                               │
                │   Request → Behaviors →       │
                │   Handler → Response          │
                │                               │
                └───────────────┬───────────────┘
                                │
                ╔═══════════════════════════════╗
                ║  STEP 1: VALIDATION BEHAVIOR  ║
                ╚═══════════════════════════════╝
                                │
        ┌───────────────────────┴───────────────────────┐
        │                                               │
    Get FluentValidation Validators for             Validate using
    CreatePurchaseContractCommand                  rules:
    (automatic registration via                     ├─ ExternalContractNumber required
     AddValidatorsFromAssembly)                      ├─ Quantity > 0
                                                     ├─ Price > 0
         │                                           ├─ TraditPartner exists
         │                                           ├─ Product exists
         ▼                                           ├─ Laycan dates valid
    CreatePurchaseContractValidator                 └─ Business rules
    ├─ RuleFor(x => x.ExternalContractNumber)
    ├─ RuleFor(x => x.Quantity)
    ├─ RuleFor(x => x.Price)
    ├─ RuleFor(x => x.TradingPartnerId)
    ├─ RuleFor(x => x.LaycanStart)
    ├─ RuleFor(x => x.LaycanEnd)
    └─ Custom async rules (database lookups)
                │
    ┌───────────┴───────────┐
    │                       │
    ▼ (INVALID)            ▼ (VALID)
    │                      │
    │ Return               │ Continue to
    │ 400 Bad Request      │ Next Behavior
    │ {                    │
    │   "errors": [        │
    │     {                │
    │       "field":       │
    │         "quantity",  │
    │       "message":     │
    │         "Quantity    │
    │         must be      │
    │         greater      │
    │         than 0"      │
    │     }                │
    │   ]                  │
    │ }                    │
    │                      │
    │                      ▼
                    ╔════════════════════════════╗
                    ║  STEP 2: LOGGING BEHAVIOR  ║
                    ╚════════════════════════════╝
                                │
        ┌───────────────────────┴───────────────────────┐
        │                                               │
    Log Request                                  Start Timer
    ├─ CommandName:                              (measure
    │  CreatePurchaseContractCommand             execution time)
    ├─ UserId: user-123
    ├─ Timestamp: 2025-11-10T14:32:45.123Z
    ├─ Correlation ID: corr-abc-xyz-123
    ├─ IP Address: 192.168.1.100
    └─ Payload (sanitized): {...}
                                │
                                ▼
                        STEP 3: HANDLER EXECUTION
                        │
        ┌───────────────┴──────────────────┐
        │                                  │
    Create Domain Entity              Validate Business Logic
    PurchaseContract                  ├─ Check concentration limits
    ├─ Id (GUID)                      ├─ Check credit exposure
    ├─ ExternalContractNumber         ├─ Check risk limits
    ├─ TradingPartnerId               └─ Apply business rules
    ├─ ProductId
    ├─ Quantity
    ├─ PriceFormula
    ├─ ContractValue
    ├─ Status: Draft
    └─ Audit fields (CreatedBy, etc.)
                │
                ▼
        Save to Database
        ├─ Insert into PurchaseContracts table
        ├─ Commit transaction
        ├─ Publish domain event:
        │  ContractCreatedEvent
        └─ Event triggers handlers
                │
                ▼
        Return Command Result
        ├─ Success: true
        ├─ ContractId: {guid}
        ├─ ExternalContractNumber: {number}
        ├─ Status: "Draft"
        └─ Message: "Contract created successfully"
                │
                ▼
    Log Response
    ├─ Execution time: 142ms
    ├─ Status: Success
    ├─ Result Id: {guid}
    ├─ User: user-123
    └─ Timestamp: 2025-11-10T14:32:45.265Z
                │
                ▼
    PIPELINE EXITS, Return to Controller
                │
                ▼
    Controller Processes Response
    ├─ HTTP Status: 201 Created
    ├─ Location Header: /api/purchase-contracts/{contractId}
    └─ Body: { success, contractId, message }
                │
                ▼
    HTTP Response Sent to Client
    ├─ Status: 201 Created
    ├─ Headers:
    │  ├─ Content-Type: application/json
    │  ├─ Location: /api/purchase-contracts/abc-123
    │  └─ Correlation-ID: corr-abc-xyz-123
    └─ Body: {
         "success": true,
         "contractId": "abc-123-def-456",
         "externalContractNumber": "IGR-2025-CAG-P0001",
         "status": "Draft",
         "message": "Contract created successfully"
       }
```

### Query Processing Flow (Similar but for Read Operations)

```
GET REQUEST: /api/purchase-contracts/{contractId}
                          │
                          ▼
                     CONTROLLER
                 ├─ Deserialize contractId from URL
                 └─ Create Query: GetPurchaseContractQuery
                          │
                          ▼
                  SEND QUERY TO MEDIATOR
               IMediator.Send(getQuery)
                          │
                          ▼
            ╔══════════════════════════════════╗
            ║ STEP 1: VALIDATION BEHAVIOR      ║
            ║ (Validates query parameters)     ║
            ╚══════════════════════════════════╝
                          │
            ┌─────────────┴─────────────┐
            │                           │
        Valid ◄────────────────────► Invalid
        │                             │
        ▼                             ▼
        STEP 2: LOGGING BEHAVIOR      Return 400
        │                             │
        ▼                             ▼
    STEP 3: HANDLER EXECUTION       CLIENT
        │
        ├─ Check Cache (Redis)
        │  ├─ Cache Key: "purchase-contract:{contractId}"
        │  │  ├─ HIT (found in Redis)
        │  │  │  └─ Return cached DTO (>90% of requests)
        │  │  │     └─ Response time: <10ms
        │  │  │
        │  │  └─ MISS (not in Redis)
        │  │     └─ Query Database
        │  │
        │  └─ All cache hits logged
        │
        ├─ Query Database (if cache miss)
        │  ├─ Execute: SELECT * FROM PurchaseContracts WHERE Id = ?
        │  ├─ Include related entities (TradingPartner, Product)
        │  ├─ Map database rows to PurchaseContract entity
        │  └─ Response time: <50ms
        │
        ├─ AutoMapper: Entity → DTO
        │  ├─ Map PurchaseContract → PurchaseContractDto
        │  ├─ Transform nested objects (TradingPartner, Product)
        │  └─ Serialize complex types (Money, Quantity)
        │
        ├─ Cache Response (Redis)
        │  ├─ Key: "purchase-contract:{contractId}"
        │  ├─ Value: Serialized DTO JSON
        │  └─ TTL: 15 minutes (configurable)
        │
        └─ Return DTO
           └─ Success result with data
                          │
                          ▼
                   LOG RESPONSE
                   ├─ Execution time: 42ms
                   ├─ Source: Database (or Cache)
                   └─ Status: Success
                          │
                          ▼
                    CONTROLLER
                   ├─ HTTP Status: 200 OK
                   └─ Body: PurchaseContractDto (JSON)
                          │
                          ▼
                   HTTP RESPONSE TO CLIENT
                   ├─ Status: 200 OK
                   ├─ Content-Type: application/json
                   └─ Body: Fully populated contract object
```

---

## 3. Settlement Lifecycle & Workflow Diagram

### State Machine Diagram

```
┌─────────────┐
│   START     │
└──────┬──────┘
       │
       ▼
┌──────────────────────────────────────┐
│ SETTLEMENT CREATED                   │
│ Status: Draft                        │
│ ├─ ContractId specified              │
│ ├─ SettlementType: Purchase/Sales   │
│ ├─ Currency initialized              │
│ └─ Created timestamp set             │
│ Allowed Actions:                     │
│ └─ Enter Data (Step 2)              │
└──────────┬───────────────────────────┘
           │
           │ Transition: EnterSettlementData()
           │ Handler: UpdateSettlementCommand
           │
           ▼
┌──────────────────────────────────────┐
│ DATA ENTRY COMPLETE                  │
│ Status: DataEntered                  │
│ ├─ B/L number recorded               │
│ ├─ Actual quantities captured        │
│ │  ├─ ActualMT: Metric tons          │
│ │  └─ ActualBBL: Barrels             │
│ ├─ Document type recorded            │
│ └─ Quantity unit determined          │
│ Allowed Actions:                     │
│ ├─ Modify Data                       │
│ ├─ Enter Pricing (Step 4)            │
│ └─ Back to Draft                     │
└──────────┬───────────────────────────┘
           │
           │ Transition: EnterSettlementPricing()
           │ Handler: CalculateSettlementCommand
           │ Price Calculation:
           │ ├─ BenchmarkPrice (from PriceFormula)
           │ ├─ AdjustmentPrice (spread/diff)
           │ ├─ ActualQuantity (MT or BBL)
           │ ├─ Total = (BenchmarkPrice + AdjustmentPrice) × ActualQuantity
           │ └─ Applied Charges (demurrage, port, etc.)
           │
           ▼
┌──────────────────────────────────────┐
│ PRICING COMPLETE                     │
│ Status: Calculated                   │
│ ├─ Settlement Amount calculated      │
│ │  └─ Total = (Benchmark + Adj) × Qt │
│ ├─ Charges applied                   │
│ │  ├─ Demurrage fees                 │
│ │  ├─ Port charges                   │
│ │  ├─ Insurance                      │
│ │  └─ Other fees                     │
│ ├─ Final Amount determined           │
│ │  └─ Total = Settlement + Charges   │
│ └─ TaxAmount calculated              │
│ Allowed Actions:                     │
│ ├─ Review Pricing                    │
│ ├─ Modify if Data Changed            │
│ └─ Request Approval (Step 5)         │
└──────────┬───────────────────────────┘
           │
           │ Transition: RequestApproval()
           │ Handler: ApproveSettlementCommand
           │ Authorization:
           │ ├─ Only Settlement Manager can approve
           │ ├─ Requires role: SettlementManager
           │ └─ Audit logged with user/timestamp
           │
           ▼
┌──────────────────────────────────────┐
│ AWAITING APPROVAL                    │
│ Status: Reviewed                     │
│ ├─ Ready for approval                │
│ ├─ All mandatory fields present      │
│ ├─ Calculations verified             │
│ └─ Submitted to manager              │
│ Allowed Actions:                     │
│ ├─ Approve (if authorized)           │
│ ├─ Reject (if authorized)            │
│ └─ Request more info                 │
└──────────┬───────────────────────────┘
           │
           │ Transition: ApproveSettlement()
           │ Handler: ApproveSettlementCommand
           │ ├─ Validate authorization
           │ ├─ Check all required fields
           │ └─ Mark as Approved
           │
           ▼
┌──────────────────────────────────────┐
│ APPROVED                             │
│ Status: Approved                     │
│ ├─ Approved by: User Name            │
│ ├─ Approval timestamp                │
│ ├─ Ready for finalization            │
│ └─ Payment processing can begin      │
│ Allowed Actions:                     │
│ ├─ Finalize Settlement (Step 6)      │
│ ├─ Reject if new info found          │
│ └─ View payment instruction          │
└──────────┬───────────────────────────┘
           │
           │ Transition: FinalizeSettlement()
           │ Handler: FinalizeSettlementCommand
           │ ├─ Lock settlement (immutable)
           │ ├─ Publish payment instruction
           │ ├─ Update contract status
           │ ├─ Publish domain event
           │ └─ Audit log finalization
           │
           ▼
┌──────────────────────────────────────┐
│ FINALIZED (IMMUTABLE)                │
│ Status: Finalized                    │
│ ├─ Finalized by: User Name           │
│ ├─ Finalization timestamp            │
│ ├─ Payment instruction sent          │
│ ├─ Settlement locked                 │
│ └─ Cannot be modified                │
│ Allowed Actions:                     │
│ ├─ View settlement details           │
│ ├─ View payment status               │
│ ├─ Cancel (only if unmatched)        │
│ └─ Create reversal entry             │
└──────────┬───────────────────────────┘
           │
           │ Time passes...
           │ Payment processing occurs
           │
           ▼
┌──────────────────────────────────────┐
│ PAYMENT PROCESSING                   │
│ Status: Finalized (with payment info)│
│ ├─ Payment Method: TT (Telegraphic)  │
│ ├─ Payment Instruction sent          │
│ ├─ Bank: XYZ Bank                    │
│ ├─ Account: IBAN XXXX                │
│ └─ Expected Payment Date: 30 days    │
│ Monitoring:                          │
│ ├─ Payment received? (check bank)    │
│ ├─ Payment status: Pending/Received  │
│ ├─ Days overdue: Calculate           │
│ └─ Follow up if overdue              │
└──────────┬───────────────────────────┘
           │
           ├─ Transition: Payment Received
           │              (external system)
           │
           ▼
┌──────────────────────────────────────┐
│ PAYMENT RECEIVED                     │
│ Status: Settled                      │
│ ├─ Payment received amount           │
│ ├─ Payment received date             │
│ ├─ Days to payment: 25               │
│ ├─ Settlement complete               │
│ └─ Mark reconciled                   │
│ Actions:                             │
│ ├─ Close settlement                  │
│ ├─ Archive settlement                │
│ └─ Generate settlement report        │
└──────────┬───────────────────────────┘
           │
           ▼
┌─────────────┐
│    END      │
│  Settlement │
│  Complete   │
└─────────────┘


REJECTION/REVERSAL PATHS:

Draft → Reviewed:
├─ Reject: Back to DataEntered
│
Reviewed → Rejected:
├─ Reason: Price mismatch / Document issue
├─ Status: Rejected
├─ Update needed to: DataEntered
└─ Resubmit from DataEntered

Finalized → Reversal:
├─ Cannot modify original
├─ Create reversal entry
├─ Reversal settlement (negative amount)
├─ Both original + reversal tracked
└─ Both visible in audit trail
```

### Settlement Calculation Details

```
PRICE CALCULATION FLOW:

Input Data:
├─ Contract Details:
│  ├─ Quantity: 500 MT (or 5,000 BBL)
│  └─ PriceFormula:
│     ├─ BenchmarkPrice: 100 USD/barrel (WTI)
│     ├─ BenchmarkUnit: BBL
│     ├─ AdjustmentPrice: -2.50 USD/barrel (discount)
│     └─ AdjustmentUnit: BBL
│
├─ Settlement Data:
│  ├─ ActualQuantity:
│  │  ├─ ActualMT: 68.5
│  │  └─ ActualBBL: 500
│  ├─ QuantityCalculationMode: ContractSpecified
│  └─ Currency: USD
│
└─ Charges Data:
   ├─ Demurrage: 5,000 USD
   ├─ Port Charges: 2,500 USD
   └─ Insurance: 1,500 USD

CALCULATION STEPS:

Step 1: Determine Quantity
├─ Quantity Mode: ContractSpecified
├─ Use: BenchmarkUnit = BBL
└─ Quantity = 500 BBL

Step 2: Calculate Benchmark Amount
├─ Amount = BenchmarkPrice × Quantity
├─ Amount = 100 USD/BBL × 500 BBL
└─ BenchmarkAmount = 50,000 USD

Step 3: Calculate Adjustment Amount
├─ Amount = AdjustmentPrice × Quantity
├─ Amount = -2.50 USD/BBL × 500 BBL
└─ AdjustmentAmount = -1,250 USD

Step 4: Calculate Base Settlement Amount
├─ BaseAmount = BenchmarkAmount + AdjustmentAmount
├─ BaseAmount = 50,000 + (-1,250)
└─ BaseAmount = 48,750 USD

Step 5: Add Charges
├─ TotalCharges = Demurrage + Port + Insurance
├─ TotalCharges = 5,000 + 2,500 + 1,500
├─ TotalCharges = 9,000 USD
└─ SubTotal = 48,750 + 9,000 = 57,750 USD

Step 6: Calculate Tax (if applicable)
├─ TaxRate = 2% (configured)
├─ TaxAmount = 57,750 × 0.02
└─ TaxAmount = 1,155 USD

Step 7: Final Settlement Amount
├─ FinalAmount = SubTotal + Tax
├─ FinalAmount = 57,750 + 1,155
└─ SETTLEMENT AMOUNT = 58,905 USD

OUTPUT:
├─ SettlementAmount: 58,905 USD
├─ Breakdown:
│  ├─ Benchmark: 50,000 USD
│  ├─ Adjustment: -1,250 USD
│  ├─ Charges: 9,000 USD
│  └─ Tax: 1,155 USD
│
└─ Payee Details:
   ├─ Trading Partner: Supplier Name
   ├─ Payment Method: TT
   ├─ Bank Details: ...
   └─ Due Date: 30 days after invoice
```

---

## 4. Risk Calculation & Limit Enforcement Tree Diagram

### Complete Risk Architecture

```
RISK MANAGEMENT SYSTEM
│
├─ TIER 1: REAL-TIME RISK MONITORING
│  │
│  ├─ Contract Creation Risk Checks
│  │  ├─ Check 1: Concentration Limit
│  │  │  ├─ Rule: Cannot exceed 30% of total position per supplier/customer
│  │  │  ├─ Trigger: When contract created/updated
│  │  │  ├─ Calculation:
│  │  │  │  ├─ ExistingExposure = Sum all contracts with same trading partner
│  │  │  │  ├─ NewAmount = New contract amount
│  │  │  │  ├─ TotalExposure = ExistingExposure + NewAmount
│  │  │  ├─ Threshold: TotalExposure / TotalPosition ≤ 30%
│  │  │  ├─ Action if Exceeded:
│  │  │  │  ├─ Throw ValidationException (default)
│  │  │  │  ├─ Or allow with RiskOverride header (if authorized)
│  │  │  │  └─ Log violation with user/timestamp
│  │  │  │
│  │  ├─ Check 2: Credit Limit
│  │  │  ├─ Rule: Cannot exceed trading partner's credit limit
│  │  │  ├─ Trigger: When creating payable/receivable
│  │  │  ├─ Calculation:
│  │  │  │  ├─ PartnerCreditLimit = Trading partner's limit
│  │  │  │  ├─ OutstandingAmount = Sum unpaid/pending amounts
│  │  │  │  ├─ AvailableCredit = CreditLimit - OutstandingAmount
│  │  │  │  └─ Threshold: NewAmount ≤ AvailableCredit
│  │  │  ├─ Action if Exceeded:
│  │  │  │  ├─ Return 400 Bad Request
│  │  │  │  └─ Reject settlement creation
│  │  │  │
│  │  ├─ Check 3: Delivery Term Risk
│  │  │  ├─ Rule: Certain delivery terms require >30 day settlement
│  │  │  ├─ Trigger: When contract with BL delivery + short settlement
│  │  │  ├─ Validation:
│  │  │  │  ├─ If DeliveryTerm = BL (Bill of Lading)
│  │  │  │  ├─ And SettlementType = TT (Cash payment)
│  │  │  │  ├─ And SettlementDays < 30
│  │  │  │  └─ Then RISK VIOLATION
│  │  │  ├─ Action if Violated:
│  │  │  │  ├─ Throw ValidationException
│  │  │  │  └─ Explain: "BL contracts require ≥30 day settlement"
│  │  │  │
│  │  └─ Check 4: Quantity Limit
│  │     ├─ Rule: Cannot exceed max single contract quantity
│  │     ├─ Threshold: 100,000 BBL or 13,600 MT per contract
│  │     └─ Action if Exceeded: Return 400
│  │
│  └─ Risk Monitoring During Operations
│     ├─ Dashboard Risk Widget (5-minute refresh)
│     ├─ Real-time metrics:
│     │  ├─ Total Exposure: Sum of all contract values
│     │  ├─ By Product: Exposure per oil type
│     │  ├─ By Partner: Top 10 partners by exposure
│     │  └─ Concentration %: Largest exposure as % of total
│     └─ Alerts:
│        ├─ Alert if any exposure > 25% of total
│        ├─ Alert if credit utilization > 80%
│        └─ Alert if large contract (>20k BBL)
│
│
├─ TIER 2: VALUE-AT-RISK (VaR) CALCULATION
│  │
│  ├─ VaR Methods (three supported approaches)
│  │  │
│  │  ├─ Method 1: Historical Simulation
│  │  │  ├─ Data: Past 252 trading days (1 year) of price movements
│  │  │  ├─ Process:
│  │  │  │  ├─ Step 1: Get historical daily returns
│  │  │  │  ├─ Step 2: Sort returns (worst to best)
│  │  │  │  ├─ Step 3: Find 95th percentile (VaR 95%)
│  │  │  │  │         or 99th percentile (VaR 99%)
│  │  │  │  └─ Step 4: Calculate $ impact on portfolio
│  │  │  ├─ Formula: VaR = PortfolioValue × PercentileReturn
│  │  │  ├─ Example (VaR 95%, 1-day):
│  │  │  │  ├─ Portfolio Value: 10,000,000 USD
│  │  │  │  ├─ Worst 5% daily return: -2.5%
│  │  │  │  └─ VaR = 10M × 0.025 = 250,000 USD
│  │  │  │     (95% confident we won't lose >$250k in one day)
│  │  │  └─ Advantage: No distribution assumptions
│  │  │
│  │  ├─ Method 2: Variance-Covariance (Parametric)
│  │  │  ├─ Assumptions: Returns normally distributed
│  │  │  ├─ Process:
│  │  │  │  ├─ Calculate portfolio volatility (σ)
│  │  │  │  ├─ Use normal distribution z-score
│  │  │  │  │  ├─ 95% confidence: z = 1.645
│  │  │  │  │  └─ 99% confidence: z = 2.326
│  │  │  │  └─ VaR = Portfolio × σ × z
│  │  │  ├─ Formula: VaR = PortfolioValue × Volatility × Z-Score
│  │  │  ├─ Example (VaR 95%, 1-day):
│  │  │  │  ├─ Portfolio: 10M USD
│  │  │  │  ├─ Daily Volatility: 1.5%
│  │  │  │  ├─ Z-score (95%): 1.645
│  │  │  │  └─ VaR = 10M × 0.015 × 1.645 = 246,750 USD
│  │  │  └─ Advantage: Fast calculation, good for stress testing
│  │  │
│  │  └─ Method 3: Monte Carlo Simulation
│  │     ├─ Process:
│  │     │  ├─ Generate 10,000 random price scenarios
│  │     │  ├─ Calculate portfolio P&L for each scenario
│  │     │  ├─ Sort scenarios by P&L (worst to best)
│  │     │  └─ Extract 95th/99th percentile P&L
│  │     ├─ Formula: Simulate random path with volatility & drift
│  │     │           dPrice = drift × dt + volatility × dW
│  │     └─ Advantage: Handles complex derivatives, non-linear risk
│  │
│  ├─ VaR Input Parameters
│  │  ├─ Time Horizon: 1-day (daily operations), 10-day (position holding)
│  │  ├─ Confidence Level: 95% (common), 99% (conservative)
│  │  ├─ Portfolio Components:
│  │  │  ├─ Physical positions (barrels/MT of oil)
│  │  │  ├─ Purchase contracts (future liabilities)
│  │  │  ├─ Sales contracts (future receivables)
│  │  │  ├─ Forward contracts (already matched)
│  │  │  └─ Option positions (if present)
│  │  └─ Price Volatility:
│  │     ├─ Historical volatility (from price feed)
│  │     ├─ Implied volatility (if options present)
│  │     └─ Stress volatility (scenario analysis)
│  │
│  ├─ VaR Output & Interpretation
│  │  ├─ Portfolio VaR (95%): 245,000 USD
│  │  │  └─ Meaning: 95% confident we won't lose more than $245k
│  │  │
│  │  ├─ Breakdown by Component:
│  │  │  ├─ Physical inventory VaR: 150,000 USD
│  │  │  ├─ Purchase contracts VaR: 80,000 USD
│  │  │  ├─ Sales contracts VaR: 25,000 USD
│  │  │  └─ Matched hedges offset: -10,000 USD
│  │  │
│  │  └─ Compared to Capital Limit:
│  │     ├─ Capital Allocated: 500,000 USD
│  │     ├─ Current VaR: 245,000 USD
│  │     ├─ Usage: 49% of capital
│  │     ├─ Available: 255,000 USD
│  │     └─ Status: HEALTHY (usage < 70%)
│  │
│  └─ Risk Limits & Alerts
│     ├─ VaR Limit (95% confidence): 400,000 USD
│     ├─ Current VaR: 245,000 USD
│     ├─ Remaining Capacity: 155,000 USD
│     ├─ Traffic Light:
│     │  ├─ Green: VaR < 60% of limit
│     │  ├─ Yellow: VaR 60-80% of limit → Caution
│     │  └─ Red: VaR > 80% of limit → Cannot add positions
│     └─ Automated Actions:
│        ├─ Alert risk manager when Yellow
│        ├─ Block new contracts when Red
│        └─ Notify C-suite if approaching Red
│
│
├─ TIER 3: STRESS TESTING
│  │
│  ├─ Scenario Analysis
│  │  ├─ Scenario 1: Oil Price Spike (+20%)
│  │  │  ├─ Assumption: WTI jumps from $80 to $96/barrel
│  │  │  ├─ Impact: Long positions gain value, short positions lose
│  │  │  ├─ Calculation:
│  │  │  │  ├─ Current inventory: 100,000 BBL @ $80 = $8M
│  │  │  │  ├─ After spike: 100,000 BBL @ $96 = $9.6M
│  │  │  │  └─ P&L Impact: +$1.6M (favorable)
│  │  │  └─ Decision: Can accommodate this scenario
│  │  │
│  │  ├─ Scenario 2: Oil Price Crash (-30%)
│  │  │  ├─ Assumption: WTI drops from $80 to $56/barrel
│  │  │  ├─ Impact: Long positions lose value, short positions gain
│  │  │  ├─ Calculation:
│  │  │  │  ├─ Current inventory: 100,000 BBL @ $80 = $8M
│  │  │  │  ├─ After crash: 100,000 BBL @ $56 = $5.6M
│  │  │  │  └─ P&L Impact: -$2.4M (unfavorable)
│  │  │  └─ Decision: Need hedging or reduce position
│  │  │
│  │  ├─ Scenario 3: Volatility Spike (implied vol +50%)
│  │  │  ├─ Impact: Option prices increase, spreads widen
│  │  │  └─ P&L Impact: Depends on option positions
│  │  │
│  │  └─ Scenario 4: Counterparty Default
│  │     ├─ Impact: Cannot receive expected payment
│  │     ├─ Risk: Credit exposure to trading partner
│  │     └─ Mitigation: Credit limits, collateral requirements
│  │
│  ├─ Stress Test Methodology
│  │  ├─ Historical Scenarios (past crises)
│  │  │  ├─ 2008 Financial Crisis: Oil dropped 77% in 4 months
│  │  │  ├─ 2011 Arab Spring: Oil spiked 30% in weeks
│  │  │  └─ 2020 COVID: Oil went negative (unprecedented)
│  │  │
│  │  ├─ Hypothetical Scenarios (future risks)
│  │  │  ├─ Geopolitical: Major oil producer conflict
│  │  │  ├─ Weather: Hurricane shuts refineries
│  │  │  └─ Demand: Major recession reduces demand
│  │  │
│  │  └─ Results Review
│  │     ├─ Worst case P&L: -$2.4M (acceptable)
│  │     ├─ Capital requirement: $2.4M (covered by reserve)
│  │     └─ Action: Monitor closely, review hedges
│  │
│  └─ Stress Test Frequency
│     ├─ Daily: Stress test against 1-day price movements
│     ├─ Weekly: Full scenario analysis (Friday)
│     ├─ Monthly: Historical crisis scenarios
│     └─ On-Demand: When market conditions change
│
│
├─ TIER 4: POSITION LIMITS
│  │
│  ├─ Individual Position Limits
│  │  ├─ Single Contract Limit: 100,000 BBL or 13,600 MT
│  │  ├─ Single Product Limit: 500,000 BBL (all contracts combined)
│  │  ├─ Single Counterparty: 30% of total portfolio
│  │  ├─ Single Trader Limit: 200,000 BBL max position
│  │  └─ Monitoring: Daily limit check after each trade
│  │
│  ├─ Portfolio-Level Limits
│  │  ├─ Maximum Portfolio Size: 2,000,000 BBL equivalent
│  │  ├─ Maximum Net Short Position: -500,000 BBL
│  │  ├─ Maximum Leverage: 2x (position value / capital)
│  │  ├─ VaR Limit: $400,000 @ 95% confidence
│  │  ├─ Expected Shortfall (CVaR): $600,000 @ 95% confidence
│  │  └─ Concentration: Largest 3 partners ≤ 60% of portfolio
│  │
│  ├─ Limit Breaches
│  │  ├─ Soft Breach (80-100% of limit)
│  │  │  ├─ Action: Alert risk manager
│  │  │  ├─ Review: Can we justify the position?
│  │  │  └─ Approval: Chief Risk Officer approval required
│  │  │
│  │  ├─ Hard Breach (>100% of limit)
│  │  │  ├─ Action: Block new trades
│  │  │  ├─ Notification: Escalate to VP of Risk
│  │  │  ├─ Response: Reduce position immediately
│  │  │  └─ Deadline: Reduce below soft limit within 24 hours
│  │  │
│  │  └─ Historical Breach Example:
│  │     ├─ Event: Trader creates large contract (150k BBL)
│  │     ├─ Limit Check: Single contract limit = 100k BBL
│  │     ├─ System Response: Block order, return 400 error
│  │     ├─ Error Message: "Contract quantity 150k exceeds limit 100k"
│  │     └─ Resolution: Trader reduces to 90k BBL, resubmits
│  │
│  └─ Limit Exceptions
│     ├─ Exception Process:
│     │  ├─ Trader requests exception via form
│     │  ├─ Risk manager reviews rationale
│     │  ├─ CRO approves if justified
│     │  ├─ Temporary override granted
│     │  └─ Audit logged with exception details
│     └─ Temporary: Max 1 day duration
│
│
├─ TIER 5: COUNTERPARTY RISK
│  │
│  ├─ Credit Exposure Calculation
│  │  ├─ Current Exposure:
│  │  │  ├─ Purchase contracts (we owe): 5,000,000 USD
│  │  │  ├─ Sales contracts (they owe): 3,500,000 USD
│  │  │  ├─ Net Exposure: 1,500,000 USD (they owe us)
│  │  │  └─ Counterparty: "ACME Oil Trading"
│  │  │
│  │  ├─ Potential Exposure (forward-looking):
│  │  │  ├─ Max possible exposure: 8,000,000 USD
│  │  │  │  (if prices move against us and all contracts settle)
│  │  │  └─ Confidence: 95%
│  │  │
│  │  └─ Total Credit Limit: 10,000,000 USD
│  │     ├─ Utilization: 1,500,000 / 10,000,000 = 15%
│  │     ├─ Available: 8,500,000 USD
│  │     └─ Status: HEALTHY
│  │
│  ├─ Credit Rating & Monitoring
│  │  ├─ Trading Partner Credit Rating:
│  │  │  ├─ Internal Rating: BBB+ (Investment grade)
│  │  │  ├─ Moody's Rating: Baa1
│  │  │  ├─ S&P Rating: BBB+
│  │  │  └─ Watch List: No
│  │  │
│  │  ├─ Monitoring Frequency:
│  │  │  ├─ Daily: Check exposure against limit
│  │  │  ├─ Weekly: Review credit rating updates
│  │  │  ├─ Monthly: Full credit review
│  │  │  └─ On Downgrade: Immediate review
│  │  │
│  │  └─ Adverse Events:
│  │     ├─ Rating Downgrade: Tighten credit limit
│  │     ├─ Payment Delay: Escalate to management
│  │     ├─ News Warning: Flag for detailed review
│  │     └─ Default: Close all positions, manage loss
│  │
│  ├─ Mitigation Strategies
│  │  ├─ Collateral Requirement: 15% of exposure
│  │  ├─ Netting Agreements: Close-out netting on default
│  │  ├─ Guarantees: Parent company guarantee required
│  │  └─ Insurance: Trade credit insurance for large trades
│  │
│  └─ Default Scenario
│     ├─ If ACME defaults on $1.5M debt:
│     │  ├─ Loss: $1.5M (unrecovered)
│     │  ├─ Capital impact: 0.3% of equity
│     │  ├─ Recovery: 30% (typical for oil trade)
│     │  ├─ Net loss: $1.05M
│     │  └─ Response: File claim, pursue legal action
│     └─ Prevention: Monitor closely, reduce exposure if needed
│
│
└─ TIER 6: OPERATIONAL REPORTING
   │
   ├─ Risk Dashboard (Real-time, updated every 5 min)
   │  ├─ KPI Cards:
   │  │  ├─ Total Exposure: 35,000,000 USD
   │  │  ├─ Portfolio VaR (95%): 245,000 USD
   │  │  ├─ Largest Exposure: 15% (ACME Oil Trading)
   │  │  └─ Capital Utilization: 49%
   │  │
   │  ├─ Trend Charts:
   │  │  ├─ Daily portfolio P&L (line chart, 30 days)
   │  │  ├─ VaR over time (95%, 99% comparison)
   │  │  └─ Concentration by partner (pie chart)
   │  │
   │  └─ Alerts Section:
   │     ├─ Critical (Red): None
   │     ├─ Warning (Yellow): 1 alert (approaching limit)
   │     └─ Info (Blue): 3 notifications
   │
   ├─ Risk Report (Daily, 7 AM)
   │  ├─ Executive Summary
   │  │  ├─ Overall Risk Status: HEALTHY
   │  │  ├─ Key Metrics:
   │  │  │  ├─ VaR: $245k (61% of limit)
   │  │  │  ├─ Concentration: 15% max
   │  │  │  └─ Liquidity: Adequate
   │  │  └─ Recommendations: None at this time
   │  │
   │  ├─ Detailed Metrics
   │  │  ├─ Position by Product (WTI, Brent, MGO)
   │  │  ├─ Position by Counterparty (top 10)
   │  │  ├─ P&L Attribution (market, volume, basis)
   │  │  └─ Limit Usage (hard, soft, exception)
   │  │
   │  └─ Scenario Analysis Results
   │     ├─ Oil Price +20%: P&L +$1.6M ✓ Acceptable
   │     ├─ Oil Price -30%: P&L -$2.4M ✓ Acceptable
   │     └─ Volatility Spike: Manageable impact
   │
   ├─ Stress Test Report (Weekly, Friday)
   │  ├─ Scenario Results:
   │  │  ├─ 2008 Crisis Scenario: Max loss $2.5M
   │  │  ├─ COVID Scenario: Extreme loss $3M
   │  │  └─ Geopolitical Scenario: Loss $1.8M
   │  │
   │  └─ Capital Requirements:
   │     ├─ Stress Capital: $3M (max scenario)
   │     ├─ Current Capital: $5M
   │     ├─ Cushion: $2M
   │     └─ Status: ADEQUATE
   │
   └─ Risk Committee Meeting (Monthly)
      ├─ Attendees: CEO, CRO, CFO, Heads of Trading/Operations
      ├─ Topics:
      │  ├─ VaR trends and limit utilization
      │  ├─ Limit exceptions and breaches
      │  ├─ Counterparty credit updates
      │  ├─ Stress test results
      │  └─ Recommended policy changes
      └─ Decisions: Approve/adjust risk limits as needed
```

---

## 5. Production Deployment Architecture Diagram

### Complete Infrastructure Stack

```
PRODUCTION DEPLOYMENT ARCHITECTURE
│
├─ CLIENT LAYER (Global Users)
│  │
│  ├─ Web Browsers
│  │  ├─ Chrome, Firefox, Safari, Edge
│  │  ├─ React 18 Single-Page Application
│  │  ├─ Responsive Design (Desktop, Tablet, Mobile)
│  │  └─ Service Worker (offline support, PWA)
│  │
│  ├─ Mobile Apps
│  │  ├─ iOS app (native or web wrapper)
│  │  ├─ Android app (native or web wrapper)
│  │  └─ Real-time notifications
│  │
│  └─ Third-Party Systems
│     ├─ Trade Repository APIs
│     ├─ Settlement Banks (payment processing)
│     ├─ Market Data Providers (pricing feeds)
│     └─ Reporting Systems (EMIR, MiFID II, SOX)
│
│                              │
│                 INTERNET     │    HTTPS/TLS 1.3
│                 (Encrypted)  │
│                              │
│                              ▼
│
├─ EDGE/CDN LAYER (Content Delivery)
│  │
│  ├─ CloudFlare CDN
│  │  ├─ DDoS Protection
│  │  ├─ Global Cache Nodes
│  │  ├─ Static Asset Caching
│  │  │  ├─ Frontend bundle (app.js, vendor.js)
│  │  │  ├─ CSS stylesheets
│  │  │  └─ Images and fonts
│  │  │
│  │  ├─ Request Routing
│  │  │  ├─ Route /api requests → Backend
│  │  │  ├─ Route /* to backend (index.html)
│  │  │  └─ Rate limiting: 1000 req/min per IP
│  │  │
│  │  └─ WAF Rules (Web Application Firewall)
│  │     ├─ Block common attack patterns
│  │     ├─ SQL injection detection
│  │     ├─ XSS detection
│  │     └─ OWASP Top 10 protection
│  │
│  └─ API Gateway (Internal)
│     ├─ Request validation
│     ├─ JWT token verification
│     ├─ Rate limit enforcement
│     ├─ Request logging
│     └─ Response compression (gzip)
│
│                              │
│                    BACKEND   │    API Requests
│                    SERVICES  │    (JSON over HTTPS)
│                              │
│                              ▼
│
├─ CONTAINER ORCHESTRATION (Kubernetes Cluster)
│  │
│  ├─ Kubernetes Control Plane
│  │  ├─ API Server (cluster management)
│  │  ├─ etcd (state store)
│  │  ├─ Controller Manager
│  │  ├─ Scheduler
│  │  └─ Cloud Controller Manager
│  │
│  ├─ Kubernetes Nodes (8 nodes, 3 zones)
│  │  │
│  │  ├─ AZ1 (Availability Zone 1)
│  │  │  └─ 3 Nodes (high availability)
│  │  │
│  │  ├─ AZ2 (Availability Zone 2)
│  │  │  └─ 3 Nodes (high availability)
│  │  │
│  │  └─ AZ3 (Availability Zone 3)
│  │     └─ 2 Nodes (high availability)
│  │
│  ├─ API Deployments (stateless, autoscaling)
│  │  │
│  │  ├─ OilTrading.Api Pod (Replica Set = 5 pods)
│  │  │  ├─ Pod 1 → Node 1 (AZ1)
│  │  │  ├─ Pod 2 → Node 2 (AZ1)
│  │  │  ├─ Pod 3 → Node 1 (AZ2)
│  │  │  ├─ Pod 4 → Node 2 (AZ2)
│  │  │  └─ Pod 5 → Node 1 (AZ3)
│  │  │
│  │  ├─ Container Spec (.NET 9.0)
│  │  │  ├─ Base Image: mcr.microsoft.com/dotnet/aspnet:9.0
│  │  │  ├─ Build Image: mcr.microsoft.com/dotnet/sdk:9.0
│  │  │  ├─ Port: 5000
│  │  │  ├─ Memory: 512 MB (request) / 1 GB (limit)
│  │  │  ├─ CPU: 250m (request) / 500m (limit)
│  │  │  │
│  │  │  ├─ Liveness Probe
│  │  │  │  ├─ HTTP GET /health/live
│  │  │  │  ├─ Initial delay: 30s
│  │  │  │  ├─ Period: 10s
│  │  │  │  └─ Failure threshold: 3
│  │  │  │
│  │  │  ├─ Readiness Probe
│  │  │  │  ├─ HTTP GET /health/ready
│  │  │  │  ├─ Initial delay: 10s
│  │  │  │  ├─ Period: 5s
│  │  │  │  └─ Failure threshold: 3
│  │  │  │
│  │  │  └─ Environment Variables
│  │  │     ├─ ASPNETCORE_ENVIRONMENT=Production
│  │  │     ├─ DATABASE_URL={PostgreSQL connection}
│  │  │     ├─ REDIS_URL={Redis connection}
│  │  │     ├─ JWT_KEY={secret key}
│  │  │     └─ LOG_LEVEL=Information
│  │  │
│  │  ├─ Autoscaling Policy
│  │  │  ├─ Min replicas: 5
│  │  │  ├─ Max replicas: 15
│  │  │  ├─ Target CPU: 70%
│  │  │  ├─ Target Memory: 80%
│  │  │  ├─ Scale-up: Add 1 pod every 30s
│  │  │  └─ Scale-down: Remove 1 pod every 300s
│  │  │
│  │  ├─ Service (load balancer)
│  │  │  ├─ Type: LoadBalancer
│  │  │  ├─ Port: 443 (public)
│  │  │  ├─ Target Port: 5000 (pod)
│  │  │  ├─ Protocol: TCP
│  │  │  └─ Session Affinity: None (stateless)
│  │  │
│  │  ├─ Network Policy
│  │  │  ├─ Ingress: From API Gateway only
│  │  │  ├─ Egress: To PostgreSQL, Redis, External APIs
│  │  │  └─ DNS: Kubernetes DNS (api-service.default.svc)
│  │  │
│  │  └─ Resource Quota
│  │     ├─ Total CPU: 2.5 cores
│  │     ├─ Total Memory: 7.5 GB
│  │     ├─ Pod Quota: 15 pods max
│  │     └─ Request Quota: 100 PVCs
│  │
│  ├─ Background Job Pods (stateless workers)
│  │  │
│  │  ├─ Settlement Automation Job Pod (Replica Set = 2)
│  │  │  ├─ Pod 1 → Node 2 (AZ2)
│  │  │  └─ Pod 2 → Node 2 (AZ3)
│  │  │
│  │  ├─ Report Generation Job Pod (Replica Set = 2)
│  │  │  ├─ Pod 1 → Node 3 (AZ2)
│  │  │  └─ Pod 2 → Node 3 (AZ3)
│  │  │
│  │  └─ Risk Calculation Job Pod (Replica Set = 2)
│  │     ├─ Pod 1 → Node 1 (AZ1)
│  │     └─ Pod 2 → Node 1 (AZ2)
│  │
│  ├─ Ingress Controller (NGINX)
│  │  ├─ Public Endpoint: api.oiltrading.com
│  │  ├─ TLS Termination (certificates, auto-renew)
│  │  ├─ Request routing (API/static)
│  │  ├─ Authentication (JWT validation)
│  │  └─ Rate limiting (1000 req/sec per IP)
│  │
│  └─ ConfigMaps & Secrets
│     ├─ ConfigMap: app-config
│     │  ├─ API settings (timeouts, limits)
│     │  ├─ Feature flags
│     │  └─ Logging configuration
│     │
│     └─ Secret: api-secrets
│        ├─ JWT signing key
│        ├─ Database password
│        ├─ Redis password
│        ├─ API keys (external services)
│        └─ TLS certificates
│
│                              │
│                    DATABASE  │    SQL connections
│                    CACHE     │    from Kubernetes
│                              │
│                              ▼
│
├─ DATA LAYER (Persistence & Caching)
│  │
│  ├─ PostgreSQL Database Cluster (Production)
│  │  │
│  │  ├─ Primary Database (Write)
│  │  │  ├─ Host: postgres-primary.prod.internal
│  │  │  ├─ Port: 5432
│  │  │  ├─ Database: oil_trading_prod
│  │  │  ├─ Size: 500 GB SSD
│  │  │  ├─ Instance: db.m5.2xlarge (8 vCPU, 32 GB RAM)
│  │  │  │
│  │  │  ├─ Replication
│  │  │  │  ├─ Replication Type: Streaming
│  │  │  │  ├─ Synchronous: Yes (1 replica)
│  │  │  │  ├─ WAL Archiving: Enabled
│  │  │  │  └─ Backup frequency: Every 6 hours
│  │  │  │
│  │  │  ├─ Backups (3-tier strategy)
│  │  │  │  ├─ Logical Backups: Daily (full dump), Hourly (incremental)
│  │  │  │  ├─ Physical Backups: Continuous WAL archiving
│  │  │  │  ├─ Snapshots: Hourly EBS snapshots (7-day retention)
│  │  │  │  └─ Remote: Copy to S3 for disaster recovery
│  │  │  │
│  │  │  ├─ Monitoring
│  │  │  │  ├─ CloudWatch (AWS)
│  │  │  │  ├─ Metrics: CPU, Memory, Disk, Connections, Query Time
│  │  │  │  ├─ Alerting: Threshold alerts for anomalies
│  │  │  │  └─ Logging: Slow query log, error log
│  │  │  │
│  │  │  ├─ Performance Tuning
│  │  │  │  ├─ Indexes: 50+ indexes on high-frequency columns
│  │  │  │  ├─ Connection pooling: PgBouncer (max 1000 connections)
│  │  │  │  ├─ Query cache: Not used (schema changes frequently)
│  │  │  │  ├─ Vacuum: Automatic, hourly
│  │  │  │  └─ Autovacuum: Enabled, aggressive settings
│  │  │  │
│  │  │  └─ Disaster Recovery
│  │  │     ├─ RTO: 4 hours (recovery time objective)
│  │  │     ├─ RPO: 1 hour (recovery point objective)
│  │  │     ├─ Failover: Manual (< 10 minutes)
│  │  │     └─ Testing: Quarterly DR drills
│  │  │
│  │  ├─ Replica Database (Read-Only)
│  │  │  ├─ Host: postgres-replica.prod.internal
│  │  │  ├─ Port: 5432
│  │  │  ├─ Purpose: Read scaling, reporting queries
│  │  │  ├─ Lag: 1-2 seconds behind primary
│  │  │  └─ Failover: Can be promoted to primary
│  │  │
│  │  ├─ Connections from Application
│  │  │  ├─ Write: Connect to Primary
│  │  │  │  ├─ Connection string: postgresql://postgres-primary/oil_trading_prod
│  │  │  │  ├─ Max connections: 100 per pod
│  │  │  │  ├─ Connection timeout: 30s
│  │  │  │  └─ Pool size: 25 (min) / 100 (max)
│  │  │  │
│  │  │  └─ Read: Connect to Replica (for heavy queries)
│  │  │     ├─ Connection string: postgresql://postgres-replica/oil_trading_prod
│  │  │     ├─ Max connections: 50 per pod
│  │  │     ├─ Queries: Reports, analytics, dashboard
│  │  │     └─ Data freshness: ±1-2 seconds
│  │  │
│  │  └─ Database Schema
│  │     ├─ Tables: 19+ core tables
│  │     ├─ Total size: 450 GB
│  │     ├─ Indexes: 50+ indexes
│  │     ├─ Views: 10+ materialized views
│  │     └─ Stored Procedures: 5+ (critical operations)
│  │
│  ├─ Redis Cache Cluster (HA with Sentinel)
│  │  │
│  │  ├─ Redis Master
│  │  │  ├─ Host: redis-master.prod.internal
│  │  │  ├─ Port: 6379
│  │  │  ├─ Memory: 32 GB
│  │  │  ├─ Instance: cache.m5.xlarge
│  │  │  │
│  │  │  ├─ Data Cached
│  │  │  │  ├─ Dashboard data (5-min TTL)
│  │  │  │  ├─ Position calculations (15-min TTL)
│  │  │  │  ├─ P&L calculations (1-hour TTL)
│  │  │  │  ├─ Risk metrics (15-min TTL)
│  │  │  │  ├─ User permissions (24-hour TTL)
│  │  │  │  └─ Product/partner master data (permanent)
│  │  │  │
│  │  │  ├─ Eviction Policy
│  │  │  │  ├─ Policy: allkeys-lru (remove least recently used)
│  │  │  │  ├─ Trigger: When 32 GB limit reached
│  │  │  │  └─ Result: Old cache entries removed, new added
│  │  │  │
│  │  │  ├─ Persistence
│  │  │  │  ├─ RDB (snapshot): Every 3600 seconds if >1 change
│  │  │  │  ├─ AOF (write log): Enabled, fsync every 1s
│  │  │  │  └─ Backup: Daily upload to S3
│  │  │  │
│  │  │  └─ Expiry Policy
│  │  │     ├─ LRU eviction when memory full
│  │  │     ├─ TTL checking: Active expiry every 10s
│  │  │     └─ Lazy expiry: On access
│  │  │
│  │  ├─ Redis Replicas (2 for HA)
│  │  │  ├─ Replica 1: redis-replica1.prod.internal
│  │  │  ├─ Replica 2: redis-replica2.prod.internal
│  │  │  ├─ Replication: Asynchronous from master
│  │  │  ├─ Purpose: Failover backup, read scaling
│  │  │  └─ Lag: <100ms behind master
│  │  │
│  │  ├─ Redis Sentinel (Monitoring & Failover)
│  │  │  ├─ Sentinels: 3 instances (odd number for quorum)
│  │  │  ├─ Sentinel 1: sentinel1.prod.internal:26379
│  │  │  ├─ Sentinel 2: sentinel2.prod.internal:26379
│  │  │  ├─ Sentinel 3: sentinel3.prod.internal:26379
│  │  │  │
│  │  │  ├─ Monitoring
│  │  │  │  ├─ Ping master every 10ms
│  │  │  │  ├─ Detect failure if no response for 30s
│  │  │  │  ├─ Start election if quorum agrees (2/3)
│  │  │  │  └─ Failover time: ~30 seconds
│  │  │  │
│  │  │  ├─ Failover Process
│  │  │  │  ├─ Step 1: Detect master failure
│  │  │  │  ├─ Step 2: Quorum votes on failover
│  │  │  │  ├─ Step 3: Select best replica to promote
│  │  │  │  ├─ Step 4: Promote replica to master
│  │  │  │  ├─ Step 5: Reconfigure other replicas
│  │  │  │  ├─ Step 6: Update application connection
│  │  │  │  └─ Result: Redis remains available
│  │  │  │
│  │  │  └─ Application Connection
│  │  │     ├─ Connection string: sentinel://sentinel1,sentinel2,sentinel3/mymaster
│  │  │     ├─ Sentinel Name: mymaster
│  │  │     ├─ Auto-discovery: Find current master via Sentinel
│  │  │     └─ Auto-failover: Seamless switch on master failure
│  │  │
│  │  └─ Connection from Application Pods
│  │     ├─ Connection method: StackExchange.Redis (C#)
│  │     ├─ Endpoint: redis-master.prod.internal:6379
│  │     ├─ Password: {encrypted-redis-password}
│  │     ├─ SSL: Enabled
│  │     ├─ Timeout: 5 seconds
│  │     ├─ Retry: 3 attempts
│  │     ├─ Command timeout: 10 seconds
│  │     └─ Connection pool: 50 connections
│  │
│  └─ Blob Storage (S3 or Azure Blob)
│     ├─ Bucket: oil-trading-prod
│     ├─ Region: us-east-1 (primary), eu-west-1 (backup)
│     │
│     ├─ Stored Objects
│     │  ├─ Documents: Contracts, invoices, B/L
│     │  ├─ Reports: PDF/Excel exports, EMIR, MiFID II
│     │  ├─ Backups: Database dumps, logs
│     │  ├─ Archives: Settled contracts, historical data
│     │  └─ Logs: Application logs, audit logs (when large)
│     │
│     ├─ Retention Policy
│     │  ├─ Active: 7 years (regulatory requirement)
│     │  ├─ Archive: Glacier (after 1 year, cheaper)
│     │  └─ Lifecycle: Auto transition to cheaper storage
│     │
│     ├─ Access Control
│     │  ├─ Public: None (no public access)
│     │  ├─ Authenticated: Application only
│     │  ├─ Encryption: AES-256 at rest
│     │  └─ Versioning: Enabled (keep previous versions)
│     │
│     └─ Backup: Cross-region replication (auto)
│
│                              │
│                  MONITORING  │    Metrics & Logs
│                  LOGGING     │    (Observability)
│                              │
│                              ▼
│
├─ MONITORING & OBSERVABILITY LAYER
│  │
│  ├─ Metrics Collection (Prometheus)
│  │  ├─ Scrape targets: All API pods
│  │  ├─ Scrape interval: 15 seconds
│  │  ├─ Retention: 15 days local
│  │  │
│  │  ├─ Metrics Collected
│  │  │  ├─ Application Metrics
│  │  │  │  ├─ HTTP requests per endpoint (5-min avg)
│  │  │  │  ├─ Request latency (p50, p95, p99)
│  │  │  │  ├─ Error rate (4xx, 5xx responses)
│  │  │  │  ├─ Cache hit/miss ratio
│  │  │  │  └─ Business metrics (contracts created/day, settlements)
│  │  │  │
│  │  │  ├─ System Metrics
│  │  │  │  ├─ CPU usage per pod
│  │  │  │  ├─ Memory usage per pod
│  │  │  │  ├─ Disk I/O (read/write throughput)
│  │  │  │  ├─ Network I/O (inbound/outbound)
│  │  │  │  ├─ Thread count
│  │  │  │  └─ GC (garbage collection) pause time
│  │  │  │
│  │  │  ├─ Database Metrics (via pg_exporter)
│  │  │  │  ├─ Active connections
│  │  │  │  ├─ Query time (min, max, avg)
│  │  │  │  ├─ Transaction rate
│  │  │  │  ├─ Replication lag
│  │  │  │  ├─ Cache hit ratio
│  │  │  │  └─ Slow queries
│  │  │  │
│  │  │  └─ Cache Metrics (via redis_exporter)
│  │  │     ├─ Connected clients
│  │  │     ├─ Memory used
│  │  │     ├─ Hit rate
│  │  │     ├─ Evictions
│  │  │     └─ Replication offset
│  │  │
│  │  └─ Prometheus Server
│  │     ├─ Host: prometheus.prod.internal:9090
│  │     ├─ Storage: 100 GB (local disk)
│  │     ├─ Memory: 8 GB
│  │     └─ Dashboard: Built-in PromQL queries
│  │
│  ├─ Visualization (Grafana)
│  │  ├─ Host: grafana.prod.internal
│  │  ├─ Port: 3000
│  │  │
│  │  ├─ Dashboards
│  │  │  ├─ System Health Dashboard
│  │  │  │  ├─ CPU, Memory, Disk usage
│  │  │  │  ├─ Pod health (ready, restarts)
│  │  │  │  ├─ Network latency
│  │  │  │  └─ Alert status
│  │  │  │
│  │  │  ├─ Application Dashboard
│  │  │  │  ├─ Request throughput (req/sec)
│  │  │  │  ├─ Response time (p50, p95, p99)
│  │  │  │  ├─ Error rate and types
│  │  │  │  ├─ Top endpoints by latency
│  │  │  │  └─ Business metrics
│  │  │  │
│  │  │  ├─ Database Dashboard
│  │  │  │  ├─ Active connections
│  │  │  │  ├─ Query performance
│  │  │  │  ├─ Replication status
│  │  │  │  ├─ Cache hit ratio
│  │  │  │  └─ Slow queries
│  │  │  │
│  │  │  └─ Cache Dashboard
│  │  │     ├─ Memory usage
│  │  │     ├─ Hit/Miss rate
│  │  │     ├─ Key count
│  │  │     ├─ Eviction rate
│  │  │     └─ Replication lag
│  │  │
│  │  └─ Alerts (visible on dashboards)
│  │     ├─ Critical (red): Page on-call engineer
│  │     ├─ Warning (yellow): Log alert
│  │     └─ Info (blue): For awareness only
│  │
│  ├─ Distributed Tracing (Jaeger)
│  │  ├─ Trace collector: jaeger-collector.prod.internal
│  │  ├─ Sampling: 1% of requests (reduce overhead)
│  │  ├─ Data retention: 72 hours
│  │  │
│  │  ├─ Trace Information
│  │  │  ├─ Request ID (correlation ID)
│  │  │  ├─ Span hierarchy (parent-child relationships)
│  │  │  ├─ Latency breakdown
│  │  │  │  ├─ Controller: 5ms
│  │  │  │  ├─ MediatR: 10ms
│  │  │  │  ├─ Handler: 20ms
│  │  │  ├─ Database query time: 8ms
│  │  │  └─ HTTP calls to external APIs: 3ms
│  │  │
│  │  └─ Use Case: Troubleshoot slow requests
│  │     ├─ Example: /api/dashboard took 500ms (slow)
│  │     ├─ Trace shows: 450ms in database query
│  │     ├─ Action: Optimize query or add index
│  │     └─ Verify: Re-run trace after fix
│  │
│  ├─ Logging (ELK Stack - Elasticsearch, Logstash, Kibana)
│  │  │
│  │  ├─ Log Sources
│  │  │  ├─ Application logs: Serilog → stdout
│  │  │  ├─ Kubernetes logs: kubelet → stdout
│  │  │  ├─ Database logs: PostgreSQL → file
│  │  │  ├─ Cache logs: Redis → stdout
│  │  │  └─ Audit logs: Custom → file
│  │  │
│  │  ├─ Log Shipping (Fluent Bit)
│  │  │  ├─ DaemonSet: One pod per Kubernetes node
│  │  │  ├─ Collect: All pod logs from /var/log/pods/
│  │  │  ├─ Parse: Extract JSON, enrich with metadata
│  │  │  ├─ Ship: Send to Elasticsearch
│  │  │  └─ Retry: Automatic retry on failure
│  │  │
│  │  ├─ Elasticsearch Cluster
│  │  │  ├─ Nodes: 3 (HA setup)
│  │  │  ├─ Shards: 5 per index
│  │  │  ├─ Replicas: 2 per shard
│  │  │  ├─ Retention: 30 days
│  │  │  ├─ Rollover: Daily indices (logs-2025-11-10)
│  │  │  ├─ Storage: 500 GB (hot) + 2 TB (warm archive)
│  │  │  └─ Search: Full-text search on all log fields
│  │  │
│  │  ├─ Kibana Dashboard
│  │  │  ├─ Host: kibana.prod.internal
│  │  │  ├─ Port: 5601
│  │  │  │
│  │  │  ├─ Log Views
│  │  │  │  ├─ Application Logs
│  │  │  │  │  ├─ Filter by level (DEBUG, INFO, WARN, ERROR)
│  │  │  │  │  ├─ Filter by pod/component
│  │  │  │  │  ├─ Search by error message
│  │  │  │  │  └─ Timeline: When did errors start?
│  │  │  │  │
│  │  │  │  ├─ Audit Logs
│  │  │  │  │  ├─ User: Who performed action?
│  │  │  │  │  ├─ Action: What was changed?
│  │  │  │  │  ├─ Timestamp: When?
│  │  │  │  │  ├─ IP: From where?
│  │  │  │  │  └─ Result: Success or failure?
│  │  │  │  │
│  │  │  └─ Database Logs
│  │  │     ├─ Slow queries (>1 second)
│  │  │     ├─ Connection errors
│  │  │     └─ Lock conflicts
│  │  │
│  │  └─ Log Alerts
│  │     ├─ Error rate spike: >50 errors/minute
│  │     ├─ Specific errors: "Out of memory", "Connection refused"
│  │     ├─ Security events: Failed login attempts (>5/min)
│  │     └─ Compliance: Unprivileged access attempts
│  │
│  └─ Application Insights (APM)
│     ├─ Azure Application Insights
│     ├─ Real User Monitoring (RUM)
│     │  ├─ Page load time
│     │  ├─ JavaScript errors
│     │  ├─ API call performance
│     │  └─ User behavior (sessions, retention)
│     │
│     ├─ Synthetic Tests (uptime monitoring)
│     │  ├─ Test 1: GET /health (every 1 min)
│     │  ├─ Test 2: GET /api/dashboard (every 5 min)
│     │  ├─ Test 3: POST /api/purchase-contracts (every 30 min)
│     │  └─ Alerts if any test fails
│     │
│     └─ Availability SLA
│        ├─ SLA Target: 99.95% (4.38 hours downtime/month)
│        ├─ Current: 99.98% (10 min downtime/month)
│        └─ Status: EXCEEDING TARGET ✓
│
│                              │
│                  SECURITY    │    Auth, Encryption,
│                  SECRETS     │    Compliance
│                              │
│                              ▼
│
├─ SECURITY & SECRETS MANAGEMENT
│  │
│  ├─ Secrets Store (HashiCorp Vault)
│  │  ├─ Host: vault.prod.internal
│  │  ├─ Port: 8200
│  │  ├─ Auth method: Kubernetes (service account)
│  │  │
│  │  ├─ Stored Secrets
│  │  │  ├─ Database credentials
│  │  │  │  ├─ Primary connection string
│  │  │  │  └─ Replica connection string (read-only)
│  │  │  │
│  │  │  ├─ Cache credentials
│  │  │  │  ├─ Redis master password
│  │  │  │  └─ Redis Sentinel password
│  │  │  │
│  │  │  ├─ JWT signing keys
│  │  │  │  ├─ Current key (valid)
│  │  │  │  ├─ Previous key (for token refresh)
│  │  │  │  └─ Next key (rotated monthly)
│  │  │  │
│  │  │  ├─ API keys (external services)
│  │  │  │  ├─ Market data provider API key
│  │  │  │  ├─ Trade repository API key
│  │  │  │  ├─ Email service API key
│  │  │  │  ├─ Payment processor credentials
│  │  │  │  └─ Webhook signing keys
│  │  │  │
│  │  │  ├─ TLS certificates
│  │  │  │  ├─ Server certificate (api.oiltrading.com)
│  │  │  │  ├─ Intermediate certificate
│  │  │  │  ├─ CA certificate (for client auth)
│  │  │  │  └─ Private key
│  │  │  │
│  │  │  └─ Encryption keys
│  │  │     ├─ Database encryption key (master)
│  │  │     ├─ Field-level encryption key
│  │  │     └─ Audit log encryption key
│  │  │
│  │  ├─ Rotation Policy
│  │  │  ├─ JWT keys: Monthly
│  │  │  ├─ Database password: Quarterly
│  │  │  ├─ API keys: Annually (or on compromise)
│  │  │  ├─ TLS certificates: Annually (before expiry)
│  │  │  └─ Encryption keys: Manual (with audit trail)
│  │  │
│  │  └─ Access Audit
│  │     ├─ Log all secret access attempts
│  │     ├─ Alert on unauthorized access
│  │     ├─ Track who accessed what and when
│  │     └─ Review: Weekly
│  │
│  ├─ Encryption
│  │  ├─ In-Transit (Network)
│  │  │  ├─ Protocol: HTTPS with TLS 1.3
│  │  │  ├─ Cipher suites: ECDHE-RSA + AES-256-GCM
│  │  │  ├─ Certificate: Wildcard for *.prod.internal
│  │  │  ├─ HSTS: Enabled (max-age=31536000)
│  │  │  └─ OCSP Stapling: Enabled
│  │  │
│  │  ├─ At-Rest (Storage)
│  │  │  ├─ Database encryption
│  │  │  │  ├─ Method: Transparent Data Encryption (TDE)
│  │  │  │  ├─ Algorithm: AES-256
│  │  │  │  ├─ Master key: In Vault
│  │  │  │  └─ Key location: AWS KMS
│  │  │  │
│  │  │  ├─ Blob storage encryption
│  │  │  │  ├─ Method: Server-side encryption (SSE)
│  │  │  │  ├─ Algorithm: AES-256
│  │  │  │  └─ Key: AWS-managed or customer-managed
│  │  │  │
│  │  │  └─ Backups encryption
│  │  │     ├─ Encrypted with main database key
│  │  │     ├─ Key escrow: Cross-region backup
│  │  │     └─ Retrieval: Authorized personnel only
│  │  │
│  │  └─ Field-Level Encryption (Sensitive Data)
│  │     ├─ Encrypted fields
│  │     │  ├─ Bank account numbers (IBAN)
│  │     │  ├─ Credit card numbers (PCI-DSS)
│  │     │  ├─ SSN/Passport numbers
│  │     │  └─ Personal email addresses
│  │     │
│  │     ├─ Method: AES-256 with unique salt per row
│  │     ├─ Queries: Index on hash (for search without decryption)
│  │     └─ Key: Different key per table
│  │
│  ├─ Authentication (OAuth 2.0 + JWT)
│  │  ├─ Flow: User login → JWT token → API calls
│  │  │
│  │  ├─ JWT Token Structure
│  │  │  ├─ Header:
│  │  │  │  ├─ alg: RS256 (RSA with SHA-256)
│  │  │  │  └─ kid: Key ID (for key rotation)
│  │  │  │
│  │  │  ├─ Payload:
│  │  │  │  ├─ sub: User ID (subject)
│  │  │  │  ├─ aud: api.oiltrading.com (audience)
│  │  │  │  ├─ iss: auth.oiltrading.com (issuer)
│  │  │  │  ├─ iat: Issued at timestamp
│  │  │  │  ├─ exp: Expiration (60 minutes)
│  │  │  │  ├─ username: User's username
│  │  │  │  ├─ email: User's email
│  │  │  │  ├─ roles: ["Trader", "RiskManager"]
│  │  │  │  └─ permissions: ["create-contract", "approve-settlement"]
│  │  │  │
│  │  │  └─ Signature: RSA private key (only auth service has it)
│  │  │
│  │  ├─ Token Validation (every API call)
│  │  │  ├─ Check signature (using public key)
│  │  │  ├─ Check expiration (not expired)
│  │  │  ├─ Check issuer (correct auth service)
│  │  │  ├─ Check audience (for this API)
│  │  │  └─ If invalid: Return 401 Unauthorized
│  │  │
│  │  ├─ Token Refresh
│  │  │  ├─ Old token expiring? Get new token
│  │  │  ├─ Send old token to /refresh endpoint
│  │  │  ├─ Receive new token (60 min validity)
│  │  │  └─ Old token becomes invalid immediately
│  │  │
│  │  └─ Token Revocation
│  │     ├─ Logout: Token added to blacklist
│  │     ├─ Blacklist store: Redis (fast lookup)
│  │     ├─ TTL: Match token expiration
│  │     └─ Check: Before each API call
│  │
│  ├─ Authorization (RBAC - Role-Based Access Control)
│  │  ├─ 18 Roles (hierarchical)
│  │  │  ├─ SystemAdmin: Full access to all operations
│  │  │  ├─ TradingManager: Manage traders, approve trades
│  │  │  ├─ Trader: Create/modify contracts
│  │  │  ├─ SeniorTrader: Additional approval rights
│  │  │  ├─ SettlementManager: Approve settlements
│  │  │  ├─ SettlementClerk: Process settlements
│  │  │  ├─ RiskManager: Monitor limits, approve overrides
│  │  │  ├─ FinanceManager: View P&L, approve payments
│  │  │  ├─ OperationsManager: Manage logistics
│  │  │  ├─ Auditor: View-only access to all data
│  │  │  ├─ ComplianceOfficer: Monitor compliance
│  │  │  └─ And 7 more roles
│  │  │
│  │  ├─ Permission Examples
│  │  │  ├─ create-contract: Required for Trader role
│  │  │  ├─ approve-contract: Required for TradingManager
│  │  │  ├─ create-settlement: Required for SettlementClerk
│  │  │  ├─ approve-settlement: Required for SettlementManager
│  │  │  ├─ override-risk-limit: Required for RiskManager
│  │  │  ├─ view-audit-log: Required for Auditor role
│  │  │  ├─ view-financial-data: Required for FinanceManager
│  │  │  └─ And 48+ more permissions
│  │  │
│  │  └─ Enforcement (on every endpoint)
│  │     ├─ Example: [Authorize(Roles = "Trader,SeniorTrader")]
│  │     ├─ Extract roles from JWT token
│  │     ├─ Check if user has required role
│  │     ├─ If no: Return 403 Forbidden
│  │     ├─ If yes: Continue to handler
│  │     └─ Log authorization check with result
│  │
│  ├─ Audit Logging
│  │  ├─ Log all security-relevant actions
│  │  ├─ Information captured
│  │  │  ├─ User ID (who)
│  │  │  ├─ Action (what) - create-contract, approve-settlement, etc.
│  │  │  ├─ Timestamp (when)
│  │  │  ├─ IP address (from where)
│  │  │  ├─ User agent (browser/API client)
│  │  │  ├─ Result (success/failure)
│  │  │  ├─ Error message (if failed)
│  │  │  ├─ Affected resources (which contract/settlement)
│  │  │  ├─ Changes made (before/after values for sensitive fields)
│  │  │  └─ Authorization decision (approved/denied)
│  │  │
│  │  ├─ Storage
│  │  │  ├─ Write: Elasticsearch (for search)
│  │  │  ├─ Archive: S3 (for long-term retention)
│  │  │  ├─ Retention: 7 years (regulatory requirement)
│  │  │  └─ Encryption: AES-256
│  │  │
│  │  └─ Monitoring
│  │     ├─ Alert: Failed login attempts >5/hour
│  │     ├─ Alert: Unauthorized access attempts
│  │     ├─ Alert: Mass data export
│  │     ├─ Alert: Configuration changes
│  │     ├─ Review: Daily by security team
│  │     └─ Report: Monthly audit report
│  │
│  └─ Compliance & Governance
│     ├─ SOX (Sarbanes-Oxley)
│     │  ├─ Audit logging: Complete implementation
│     │  ├─ Access controls: RBAC with segregation of duties
│     │  ├─ Change management: Git versioning, code review
│     │  └─ Testing: 842/842 tests passing (100%)
│     │
│     ├─ GDPR (General Data Protection Regulation)
│     │  ├─ Data retention: Configurable per entity
│     │  ├─ Right to be forgotten: Implement erasure
│     │  ├─ Data export: GDPR export endpoint
│     │  ├─ Encryption: AES-256 at rest, TLS in transit
│     │  └─ DPA: Data Processing Agreement in place
│     │
│     ├─ MiFID II (Markets in Financial Instruments Directive)
│     │  ├─ Best execution: Track order outcomes
│     │  ├─ Transparency: Pre/post-trade reporting
│     │  ├─ Suitability: Document client profiles
│     │  └─ Conflicts: Disclose material conflicts
│     │
│     └─ EMIR (European Market Infrastructure Regulation)
│        ├─ Trade reporting: Report to EMIR repository
│        ├─ Counterparty risk: Credit limit enforcement
│        ├─ Novation: Central clearing where applicable
│        └─ Exchange of collateral: Margin requirements
│
│
└─ DISASTER RECOVERY & BUSINESS CONTINUITY
   │
   ├─ Backup Strategy (3-tier)
   │  ├─ Tier 1: Local Backups (Recent recovery)
   │  │  ├─ Type: Continuous WAL archiving
   │  │  ├─ Location: On production infrastructure
   │  │  ├─ Retention: 24 hours
   │  │  ├─ Recovery window: ~1 minute
   │  │  └─ Frequency: Continuous
   │  │
   │  ├─ Tier 2: Remote Backups (Regional disaster)
   │  │  ├─ Type: Daily full logical backups
   │  │  ├─ Location: Remote S3 bucket (different region)
   │  │  ├─ Retention: 30 days
   │  │  ├─ Recovery window: 1-2 hours
   │  │  └─ Frequency: Daily at 2 AM UTC
   │  │
   │  └─ Tier 3: Archive Backups (Long-term retention)
   │     ├─ Type: Weekly backups → Glacier
   │     ├─ Location: Glacier (very cheap, slow)
   │     ├─ Retention: 7 years (regulatory)
   │     ├─ Recovery window: 4-48 hours
   │     └─ Frequency: Weekly
   │
   ├─ Disaster Recovery Plan
   │  ├─ RTO (Recovery Time Objective): 4 hours
   │  ├─ RPO (Recovery Point Objective): 1 hour
   │  │
   │  ├─ Scenario 1: Single pod crash
   │  │  ├─ Detection: Readiness probe fails
   │  │  ├─ Action: Kubernetes automatically restarts
   │  │  ├─ Recovery time: <2 minutes
   │  │  ├─ Data loss: None (stateless)
   │  │  └─ User impact: Minimal (other pods handle traffic)
   │  │
   │  ├─ Scenario 2: Database primary failure
   │  │  ├─ Detection: Connection failures from pods
   │  │  ├─ Action: Manually promote replica to primary
   │  │  ├─ Recovery time: <10 minutes
   │  │  ├─ Data loss: <1 hour (replication lag)
   │  │  └─ User impact: <10 minutes downtime
   │  │
   │  ├─ Scenario 3: Redis master failure
   │  │  ├─ Detection: Sentinel detects failure
   │  │  ├─ Action: Sentinel automatically promotes replica
   │  │  ├─ Recovery time: ~30 seconds
   │  │  ├─ Data loss: <100ms (replication lag)
   │  │  └─ User impact: Brief cache miss (fall back to DB)
   │  │
   │  ├─ Scenario 4: Entire data center failure
   │  │  ├─ Detection: Multiple pod failures, DB unreachable
   │  │  ├─ Action: Failover to backup data center
   │  │  ├─ Recovery process:
   │  │  │  ├─ Step 1: Restore from latest backup (1 hour old)
   │  │  │  ├─ Step 2: Update DNS to point to backup
   │  │  │  ├─ Step 3: Verify application startup
   │  │  │  ├─ Step 4: Run smoke tests
   │  │  │  └─ Step 5: Notify stakeholders
   │  │  ├─ Recovery time: 4 hours
   │  │  ├─ Data loss: <1 hour
   │  │  └─ User impact: 4-hour downtime + data loss
   │  │
   │  └─ Testing
   │     ├─ Quarterly DR drills (first Saturday of quarter)
   │     ├─ Procedure: Restore latest backup to test environment
   │     ├─ Validation: Run full test suite
   │     ├─ Documentation: Update runbook with findings
   │     └─ Report: Present to leadership
   │
   ├─ Business Continuity
   │  ├─ Communication Plan
   │  │  ├─ Incident declared by: Ops manager or on-call engineer
   │  │  ├─ Notification: Slack → Email → SMS → Phone
   │  │  ├─ Escalation:
   │  │  │  ├─ 5 minutes: Page VP of Engineering
   │  │  │  ├─ 15 minutes: Page VP of Operations
   │  │  │  ├─ 30 minutes: Page Chief Technology Officer
   │  │  │  └─ 60 minutes: Page Chief Executive Officer
   │  │  └─ Regular updates every 15 minutes
   │  │
   │  ├─ Customer Communication
   │  │  ├─ Status page: status.oiltrading.com (separate infrastructure)
   │  │  ├─ Email: Notify all affected customers
   │  │  ├─ Phone: Call major customers (top 10% by volume)
   │  │  ├─ Timeline:
   │  │  │  ├─ 5 minutes: Initial notification
   │  │  │  ├─ 15 minutes: Update with estimated recovery
   │  │  │  ├─ 30 minutes: More detailed information
   │  │  │  └─ 60 minutes: Regular updates every hour
   │  │  └─ Post-incident: Root cause analysis within 24 hours
   │  │
   │  └─ Data Center Backup Operations
   │     ├─ Backup DC Location: 500 miles away (different seismic zone)
   │     ├─ Infrastructure: Identical to primary (for fast failover)
   │     ├─ Current state: Warm standby (no data)
   │     ├─ Activation: <1 hour
   │     └─ Cost: Higher due to redundancy, but worth it
│
└─ END OF PRODUCTION DEPLOYMENT ARCHITECTURE
```

---

## Summary

These 5 architectural diagrams provide a comprehensive visual reference for:

1. **4-Tier System Architecture** - Complete layer breakdown with all components
2. **CQRS Pipeline Flow** - End-to-end request processing with validation/logging
3. **Settlement Lifecycle** - State machine showing all settlement stages and transitions
4. **Risk Management Tree** - 6-tier risk control framework with detailed calculations
5. **Production Infrastructure** - Complete deployment stack with HA, monitoring, security

Each diagram includes:
- ✅ Real component names and quantities (47 entities, 80+ commands, etc.)
- ✅ Actual port numbers and connection strings
- ✅ Real performance metrics (VaR calculations, response times)
- ✅ Deployment patterns (Kubernetes, load balancing, failover)
- ✅ Security controls (encryption, authentication, audit logging)
- ✅ Monitoring strategy (Prometheus, Grafana, ELK, Jaeger)

---

**Generated for Oil Trading System - Production Ready v2.16.0+**

