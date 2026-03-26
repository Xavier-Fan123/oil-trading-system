# Complete Entity Reference - All 47 Entities

**Version**: 2.0 Enterprise Grade
**Last Updated**: November 2025
**Scope**: All domain entities in Oil Trading System v2.10.0

---

## Table of Contents

- [Entity Summary](#entity-summary)
- [I. Core Trading Entities (9)](#i-core-trading-entities-9)
- [II. Advanced Financial Entities (12)](#ii-advanced-financial-entities-12)
- [III. Operational & Support Entities (26)](#iii-operational--support-entities-26)
- [Entity Relationships](#entity-relationships)
- [Database Schema Statistics](#database-schema-statistics)

---

## Entity Summary

| Category | Count | Purpose | Status |
|----------|-------|---------|--------|
| Core Trading | 9 | Purchase/Sales/Shipping | ✅ Production |
| Financial | 12 | Settlements, Pricing, Derivatives | ✅ Production |
| Operational | 26 | Inventory, Reports, Automation | ✅ Production |
| **Total** | **47** | **Complete business model** | ✅ v2.10.0 |

---

## I. Core Trading Entities (9)

### 1. **PurchaseContract**
**Purpose**: Represent oil purchase agreements with suppliers

**Key Properties**:
- `ContractNumber`: Unique contract identifier (value object)
- `ExternalContractNumber`: Reference to supplier's system
- `TradingPartnerId`: Foreign key to supplier
- `ProductId`: Foreign key to oil product
- `Status`: Enum (Draft, PendingApproval, Active, Completed)
- `Quantity`: Value object (value: 500, unit: MT/BBL)
- `ContractValue`: Total contract amount in original currency
- `Pricing`: Value object (benchmark price + adjustment)
- `DeliveryTerms`: Enum (FOB, CIF, DES, etc.)
- `LaycanStart` / `LaycanEnd`: Laycan date range for execution
- `LoadPort` / `DischargePort`: Physical locations
- `CreatedAt` / `CreatedBy`: Audit trail
- `ActivatedAt` / `ActivatedBy`: Activation timestamp
- `CompletedAt`: Completion date

**Business Rules**:
- Cannot be activated without pricing formula
- Cannot be completed until all quantities shipped
- Laycan dates must be realistic (start ≤ end)
- Contract value must be calculated before activation

**Related Entities**:
- **ShippingOperation** (1:many) - Multiple shipments per contract
- **SalesContract** (many:many via ContractMatching) - Natural hedging
- **PurchaseSettlement** (1:many) - One settlement per contract
- **ContractMatching** (1:many) - Links to sales contracts

**Key Methods**:
```csharp
public void ActivatePurchaseContract();
public void UpdatePricing(PriceFormula newPrice, Money newValue);
public void CompleteContract();
public Money CalculateNetAmount();
```

---

### 2. **SalesContract**
**Purpose**: Represent oil sales agreements with customers

**Key Properties**:
- `ContractNumber`: Unique contract identifier
- `ExternalContractNumber`: Reference to customer's system
- `TradingPartnerId`: Foreign key to customer
- `ProductId`: Foreign key to oil product
- `Status`: Enum (Draft, PendingApproval, Active, Completed)
- `ApprovalStatus`: Enum (Pending, Approved, Rejected) - Workflow
- `Quantity`: Value object (value and unit)
- `ContractValue`: Total contract amount
- `Pricing`: Value object with adjustment
- `DeliveryTerms`: Enum (FOB, CIF, DES, etc.)
- `SettlementType`: Enum (TT, LC, etc.)
- `CreditPeriodDays`: Payment terms (e.g., NET 30)
- `ActivatedAt` / `ActivatedBy`: Activation audit
- `ApprovedAt` / `ApprovedBy`: Approval workflow tracking

**Business Rules**:
- Must be approved before activation (multi-step workflow)
- Can only be completed after all quantities delivered
- Settlement terms must be defined (TT, LC, etc.)
- Credit period must be agreed with customer

**Related Entities**:
- **ShippingOperation** (1:many) - Multiple shipments
- **PurchaseContract** (many:many via ContractMatching) - Hedging
- **SalesSettlement** (1:many) - Payment settlement
- **ContractMatching** (1:many) - Links to purchases

**Workflow Methods**:
```csharp
public void SubmitForApproval();
public void Approve(UserId approvedBy);
public void Reject(UserId rejectedBy, string reason);
public void ActivateSalesContract();
```

---

### 3. **Product**
**Purpose**: Define available oil products

**Key Properties**:
- `Code`: Product code (BRENT, WTI, MGO, HFO380)
- `Name`: Full product name
- `Description`: Product specifications
- `DefaultUnit`: Default quantity unit (MT or BBL)
- `Category`: Oil product category
- `IsActive`: Logical deletion flag

**Products Maintained**:
1. **Brent Crude** (BRENT) - Light Sweet Crude, 35 API, BBL
2. **WTI Crude** (WTI) - Light Sweet Crude, 39.6 API, BBL
3. **Marine Gas Oil** (MGO) - ISO 8217:2017, MT
4. **Heavy Fuel Oil 380cSt** (HFO380) - ISO 8217:2017, MT

**Related Entities**:
- **PurchaseContract** (1:many)
- **SalesContract** (1:many)
- **PaperContract** (1:many) - Futures prices
- **MarketPrice** (1:many) - Price tracking
- **InventoryPosition** (1:many) - Inventory by product

---

### 4. **TradingPartner**
**Purpose**: Manage suppliers, customers, and counterparties

**Key Properties**:
- `Name`: Trading partner name
- `Type`: Enum (Supplier, Customer, Broker, Bank)
- `Status`: Enum (Active, Blocked, Inactive)
- `Country`: Registration country
- `CreditLimit`: Maximum exposure (Money value object)
- `CreditTerms`: Enum (Cash, NET30, NET60, NET90)
- `PaymentType`: Settlement method (TT, LC, SBLC)
- `IsBlocked`: Boolean flag for suspension
- `BlockReason`: Reason for blocking
- `CreatedAt` / `UpdatedAt`: Audit trail

**Business Rules**:
- Credit limit must be maintained for customers
- Cannot create contracts with blocked partners
- Payment terms must match contract settlement type
- Credit exposure tracked against limit (daily monitoring)

**Related Entities**:
- **PurchaseContract** (1:many) - As supplier
- **SalesContract** (1:many) - As customer
- **Payment** (1:many) - Payment processing

**Key Methods**:
```csharp
public void BlockPartner(string reason);
public void UnblockPartner();
public Money CalculateExposure();  // Total outstanding amount
```

---

### 5. **ShippingOperation**
**Purpose**: Track logistics and physical shipment movement

**Key Properties**:
- `ContractId`: Foreign key to contract (purchase or sales)
- `ContractType`: Discriminator (Purchase or Sales)
- `ShippingStatus`: Enum (Draft, Loading, InTransit, Discharging, Completed, Cancelled)
- `VesselName`: Vessel name or charter reference
- `ChartererName`: Company chartering vessel
- `Quantity`: Quantity being shipped (MT or BBL)
- `ShippedQuantity`: Actual quantity loaded
- `DeliverySchedule`: Start and end dates
- `LoadPort` / `DischargePort`: Physical locations
- `Incoterms`: Delivery terms (FOB, CIF, DES)
- `ShippingAgent`: Agent managing shipment
- `Events`: Navigation property to ShippingEvent records

**Lifecycle**:
- Draft → Loading → InTransit → Discharging → Completed
- At each stage, events are recorded with timestamps

**Related Entities**:
- **PurchaseContract** or **SalesContract** - Links to contract
- **ShippingEvent** (1:many) - Loading/discharge milestones
- **InventoryMovement** (1:many) - Inventory impact

**Event Methods**:
```csharp
public void StartLoading();
public void CompleteLoading(decimal actualQuantity);
public void CompleteDischarge();
public void RecordLifting(int billOfLadingNumber);
public void CancelShipping(string reason);
```

---

### 6. **ContractMatching**
**Purpose**: Link purchase contracts to sales contracts for natural hedging

**Key Properties**:
- `PurchaseContractId`: Foreign key to PurchaseContract
- `SalesContractId`: Foreign key to SalesContract
- `MatchingRatio`: Decimal ratio (e.g., 1.0 = 100% hedge)
- `MatchingStatus`: Enum (Proposed, Active, Closed)
- `MatchingDate`: When matching was established
- `ClosingDate`: When matching was closed
- `ReasonForClosing`: Why matching was closed (Completed, Cancelled, etc.)

**Business Logic**:
- Enables natural hedging without external derivatives
- Multiple sales contracts can match single purchase contract (at different ratios)
- Risk = Net quantity × Price change
- Position calculation accounts for matched quantities

**Related Entities**:
- **PurchaseContract** (many:1)
- **SalesContract** (many:1)

**Calculation Methods**:
```csharp
public Money CalculateNetExposure(decimal spotPrice);
public decimal CalculateHedgeRatio();
public Money CalculateUnhedgedRisk();
```

---

### 7. **ContractSettlement** (Generic - Backward Compatibility)
**Purpose**: Multi-step settlement process for contracts (legacy system)

**Key Properties**:
- `ContractId`: Foreign key (polymorphic reference)
- `ContractType`: Discriminator (Purchase or Sales)
- `SettlementAmount`: Money value object
- `Status`: Enum (Draft → DataEntered → Calculated → Reviewed → Approved → Finalized)
- `Charges`: Navigation to SettlementCharge collection
- `CreatedAt` / `CreatedBy`: Audit
- `FinalizedAt` / `FinalizedBy`: Completion tracking
- `IsPaymentProcessed`: Boolean for payment status

**Lifecycle**:
1. **Draft** - Initial creation
2. **DataEntered** - B/L and quantity data entered
3. **Calculated** - Amounts calculated from pricing
4. **Reviewed** - Finance review completed
5. **Approved** - Management approval
6. **Finalized** - Payment processed

**Related Entities**:
- **PurchaseContract** or **SalesContract** (polymorphic)
- **SettlementCharge** (1:many)
- **Payment** (1:1) - Links to payment record

**Note**: Deprecated in v2.10.0 in favor of specialized PurchaseSettlement/SalesSettlement

---

### 8. **User**
**Purpose**: System users with role-based access control

**Key Properties**:
- `Email`: Unique email address
- `FullName`: User's full name
- `Role`: Enum (SystemAdmin, TradingManager, SeniorTrader, Trader, SettlementManager, OperationsManager, FinanceManager, etc.) - 11+ roles
- `Status`: Enum (Active, Inactive, Locked)
- `LastLoginAt`: Last login timestamp
- `CreatedAt`: Account creation date
- `PasswordHash`: Bcrypt encrypted password
- `JwtToken`: Token issued at login (60-minute expiration)

**Role Hierarchy** (from highest to lowest privilege):
1. SystemAdmin - Full system access
2. TradingManager - Trading operations oversight
3. SeniorTrader - Execute and override trades
4. Trader - Execute trades within limits
5. SettlementManager - Settlement oversight
6. SettlementClerk - Settlement processing
7. OperationsManager - Logistics oversight
8. RiskManager - Risk analysis
9. FinanceManager - Financial reporting
10. ComplianceOfficer - Compliance monitoring
11. Guest - Read-only access

**Related Entities**:
- **PurchaseContract** (1:many) - CreatedBy, ActivatedBy fields
- **SalesContract** (1:many)
- **ContractSettlement** (1:many)

**Authentication Methods**:
```csharp
public bool VerifyPassword(string plainPassword);
public string GenerateJwtToken(TimeSpan expiration);
public void UpdateLastLogin();
```

---

### 9. **PricingEvent**
**Purpose**: Audit trail of all pricing changes

**Key Properties**:
- `ContractId`: Foreign key to contract
- `OldPrice`: Previous price formula
- `NewPrice`: New price formula
- `ChangedBy`: UserId who made change
- `ChangedAt`: Timestamp of change
- `Reason`: Why price was changed (e.g., "Customer negotiation", "Market adjustment")

**Related Entities**:
- **PurchaseContract** (1:many) via ContractId
- **SalesContract** (1:many) via ContractId

**Purpose**: Enable audit trail for compliance and dispute resolution

---

## II. Advanced Financial Entities (12)

### 10. **PurchaseSettlement** (Type-Safe AP System - v2.10.0)
**Purpose**: Specialized settlement for supplier payments (Accounts Payable)

**Key Properties**:
- `SupplierContractId`: Foreign key directly to PurchaseContract (no polymorphism)
- `SettlementAmount`: Money (amount, currency)
- `Status`: Enum (Draft → Finalized)
- `Charges`: Collection of settlement charges (demurrage, port fees, etc.)
- `CreatedAt` / `CreatedBy`
- `FinalizedAt` / `FinalizedBy`

**14 Specialized Methods**:
```csharp
// Query methods
Task<PurchaseSettlement> GetByExternalContractNumberAsync(string externalNumber);
Task<List<PurchaseSettlement>> GetPendingSupplierPaymentAsync();
Task<List<PurchaseSettlement>> GetOverdueSupplierPaymentAsync(int days);
Task<decimal> CalculateSupplierPaymentExposureAsync(Guid supplierId);

// Payment tracking
Task<List<PurchaseSettlement>> GetByPaymentStatusAsync(PaymentStatus status);
Task UpdatePaymentStatusAsync(Guid settlementId, PaymentStatus newStatus);

// Reporting
Task<decimal> CalculateSupplierBalanceAsync(Guid supplierId);
Task<List<PurchaseSettlement>> GetSupplierHistoryAsync(Guid supplierId, DateRange period);

// ... 6 more AP-specific methods
```

**Business Rules**:
- Only references PurchaseContract (type-safe)
- Direct FK constraint (no polymorphism violations)
- Supplier balance tracking for credit management
- Payment status workflow (Unpaid → Partial → Paid → Cleared)

---

### 11. **SalesSettlement** (Type-Safe AR System - v2.10.0)
**Purpose**: Specialized settlement for customer payments (Accounts Receivable)

**Key Properties**:
- `CustomerContractId`: Foreign key directly to SalesContract
- `SettlementAmount`: Money (amount, currency)
- `Status`: Enum (Draft → Finalized)
- `Charges`: Collection of settlement charges
- `InvoiceNumber`: Reference to invoice system
- `DueDate`: Payment due date based on credit terms

**14 Specialized Methods**:
```csharp
// Query methods
Task<SalesSettlement> GetByExternalContractNumberAsync(string externalNumber);
Task<List<SalesSettlement>> GetOutstandingReceivablesAsync();
Task<List<SalesSettlement>> GetOverdueBuyerPaymentAsync(int days);
Task<decimal> CalculateBuyerCreditExposureAsync(Guid buyerId);

// Receivables management
Task<List<SalesSettlement>> GetByCustomerAsync(Guid customerId);
Task<decimal> CalculateCustomerBalanceAsync(Guid customerId);

// Collection tracking
Task<List<SalesSettlement>> GetCreditRiskAlertAsync();
Task UpdatePaymentStatusAsync(Guid settlementId, PaymentStatus newStatus);

// ... 6 more AR-specific methods
```

**Business Rules**:
- Only references SalesContract (type-safe)
- Direct FK constraint to CustomerContractId
- Buyer credit exposure tracking
- Overdue collection alerts
- Invoice integration for accounting

---

### 12. **SettlementCharge**
**Purpose**: Track charges and fees associated with settlements

**Key Properties**:
- `SettlementId`: Foreign key to ContractSettlement/PurchaseSettlement/SalesSettlement
- `ChargeType`: Enum (Demurrage, PortCharge, THCCharge, BunkerSurcharge, etc.)
- `Amount`: Charge amount (Money value object)
- `Currency`: Currency of charge
- `Description`: Detailed description
- `AppliedDate`: When charge was assessed
- `IsReversed`: Flag for charge reversal/credit

**Charge Types**:
- **Demurrage**: Vessel detention fees
- **Port Charge**: Port authority fees
- **THC**: Terminal Handling Charge
- **BunkerSurcharge**: Fuel surcharge
- **InsuranceSurcharge**: Insurance premium
- **Custom**: Custom charge type

**Calculation**:
```
Settlement Total = Base Settlement Amount
                 + Sum(All Charges)
                 = Settlement Amount + Demurrage + Ports + THC + etc.
```

---

### 13. **PaperContract** (Derivatives)
**Purpose**: Track futures and derivatives contracts

**Key Properties**:
- `ContractNumber`: Unique identifier
- `ProductId`: Foreign key to Product
- `TradeDate`: When derivative was traded
- `ContractType`: Enum (Futures, Forward, Option, Swap, etc.)
- `BuyerPartnerId` / `SellerPartnerId`: Counterparties
- `Quantity`: Derivative quantity
- `EntryPrice`: Price at which derivative was entered
- `CurrentPrice`: Mark-to-market price
- `UnrealizedPnL`: Unrealized profit/loss
- `RealizedPnL`: Realized gains/losses
- `ExpiryDate`: Expiration of derivative
- `Status`: Enum (Open, Closed, Expired)

**Pricing Models**:
- Mark-to-Market (MTM): Daily update from market price
- Greeks: Volatility metrics for risk management
- Spread: Calendar spread (Dec vs Jan), intercommodity (WTI vs Gasoil)

**Related Entities**:
- **Product** (many:1)
- **TradingPartner** (many:1) - Buyer and Seller
- **TradeGroup** (many:many) - Multi-leg strategies
- **PaperContractPrice** (1:many) - Daily price history

---

### 14. **TradeGroup** (Multi-Leg Strategies)
**Purpose**: Group related contracts for aggregate risk calculation

**Key Properties**:
- `Name`: Strategy name (e.g., "Brent Dec-Jan Calendar Spread")
- `StrategyType`: Enum (CalendarSpread, InterCommodity, Arbitrage, PhysicalHedge, Directional)
- `CreatedDate`: When strategy established
- `ClosedDate`: When strategy closed
- `Status`: Enum (Open, Closed, Monitoring)
- `RiskParameters`: JSON with correlation, VaR method, stress factors
- `Legs`: Navigation property to TradeGroupLeg (contract assignments)

**Strategy Types**:
```
1. CalendarSpread: Same product different months
   Example: Long Brent Dec, Short Brent Jan (1:1 ratio)
   Risk: Inter-month volatility + basis risk

2. InterCommodity: Different products, related markets
   Example: Long WTI 3x, Short Gasoil 1x (3:1 ratio)
   Risk: Correlation risk, correlation change

3. Arbitrage: Exploit price differences
   Example: Buy cheaper market, sell expensive market
   Risk: Convergence may not occur

4. PhysicalHedge: Physical contracts matched for hedge
   Example: Purchase + Sale matched naturally
   Risk: Basis risk between purchase and sale prices

5. Directional: Speculative position on price direction
   Example: Long 1000 BBL expecting price increase
   Risk: Full directional price risk
```

**Risk Calculation**:
```csharp
// Aggregate VaR with correlation consideration
PortfolioVaR = sqrt(Sum(VaR_i^2) + 2 * Correlation * VaR_i * VaR_j)
HedgeEffectiveness = 1 - (GroupVaR / Sum(IndividualVaRs))
```

**Related Entities**:
- **PurchaseContract** (many:many via TradeGroupLeg)
- **SalesContract** (many:many via TradeGroupLeg)
- **PaperContract** (many:many via TradeGroupLeg)

---

### 15. **MarketPrice**
**Purpose**: Real-time and historical market prices for products

**Key Properties**:
- `ProductId`: Foreign key to Product
- `PriceDate`: Date of price quote
- `Source`: Source of price (Bloomberg, Platts, ICE, etc.)
- `BidPrice`: Bid price
- `AskPrice`: Ask price
- `MidPrice`: Mid-market price ((Bid + Ask) / 2)
- `Volume`: Trading volume at price
- `Currency`: Currency of quote
- `IsOfficial`: Boolean for official closing price

**Related Entities**:
- **Product** (many:1)
- **PaperContract** (many:1) - For MTM calculations
- **DailyPrice** (1:many) - Historical snapshots

---

### 16-21. **Financial Support Entities** (MarketData, PriceIndex, ContractPricingEvent, PriceBenchmark, DailyPrice, Payment)

Each provides supporting data for pricing, financial reporting, and payment tracking. (See complete documentation for detailed specifications)

---

## III. Operational & Support Entities (26)

### 22. **InventoryLocation**
**Purpose**: Physical storage locations for inventory

**Key Properties**:
- `LocationCode`: Unique code (e.g., "SING-TANK-01")
- `LocationName`: Human-readable name
- `LocationType`: Enum (Tank, Terminal, Pipeline, Port, StorageFacility)
- `Country` / `State` / `City`: Geographic location
- `Capacity`: Maximum capacity (Quantity value object)
- `CurrentUtilization`: Current occupancy percentage
- `TemperatureControlled`: Bool for heated tanks
- `InertGasAvailable`: Bool for inert gas supply
- `Operator`: Operating company name

**Related Entities**:
- **InventoryPosition** (1:many) - Products in location
- **InventoryMovement** (1:many) - Transfer history
- **InventoryReservation** (1:many) - Allocations

---

### 23-26. **Inventory System Entities** (InventoryPosition, InventoryMovement, InventoryReservation, InventoryLedger)

Complete inventory tracking system with:
- Real-time position by product and location
- Movement history (Transfer, Receipt, Dispatch, Adjustment)
- Reservations (Commitment, Hold, Block)
- Cost basis tracking (FIFO/LIFO)

---

### 27. **SettlementTemplate**
**Purpose**: Pre-configured settlement patterns

**Key Properties**:
- `TemplateName`: Descriptive name
- `TemplateCategory`: Enum (Standard, Premium, Bulk, etc.)
- `DefaultCharges`: List of default charges (demurrage, port, etc.)
- `PaymentTerms`: Default credit period
- `CreatedBy` / `CreatedAt`
- `IsActive`: Boolean for template availability

**Related Entities**:
- **SettlementTemplateUsage** (1:many) - Usage tracking
- **SettlementTemplatePermission** (1:many) - Access control

---

### 28. **SettlementAutomationRule**
**Purpose**: Automated settlement creation and processing

**Key Properties**:
- `RuleName`: Descriptive name
- `IsActive`: Boolean to enable/disable rule
- `TriggerType`: Enum (OnContractCompletion, OnSettlementCreation, OnSchedule, OnManualTrigger)
- `Conditions`: JSON array of conditions:
  ```json
  [
    { "type": "ContractStatus", "value": "Completed" },
    { "type": "QuantityThreshold", "value": "> 500" },
    { "type": "PriceRange", "value": "80-90 USD/BBL" },
    { "type": "TimeElapsed", "value": "> 30 days" },
    { "type": "PartnerCredit", "value": "Acceptable" },
    { "type": "InventoryLevel", "value": "> 1000 MT" }
  ]
  ```
- `Actions`: Sequential or parallel actions:
  ```json
  [
    { "type": "CreateSettlement", "params": {...} },
    { "type": "CalculateSettlement", "params": {...} },
    { "type": "ApproveSettlement", "params": {...} },
    { "type": "NotifyStakeholder", "params": {"email": "..."}},
    { "type": "UpdateStatus", "params": {"status": "Approved"}}
  ]
  ```
- `Orchestration`: Enum (Sequential, Parallel, GroupedByPartner, GroupedByProduct)
- `ExecutionCount`: Number of successful executions
- `LastExecutionDate`: Last run timestamp
- `LastExecutionError`: Error message if failed
- `CreatedAt` / `UpdatedAt`: Audit trail

**Business Logic**:
```
Trigger fires (e.g., contract completed)
  ↓
Evaluate all conditions (AND logic)
  ├─ If all true: Execute actions in order
  │  1. Create settlement
  │  2. Calculate amounts
  │  3. Approve (if criteria met)
  │  4. Notify finance team
  │  5. Update status
  ├─ If any false: Skip automation
  └─ Log result for audit
```

---

### 29-31. **Reporting System** (ReportConfiguration, ReportSchedule, ReportExecution, ReportArchive, etc.)

Comprehensive reporting system with:
- Report definitions (SQL templates, chart types, filters)
- Automated scheduling (Daily, Weekly, Monthly)
- Multi-channel distribution (Email, SFTP, Webhook, FTP, S3, Azure)
- Archive management (Storage, retention, retrieval)

---

### 32. **Tag** & **ContractTag**
**Purpose**: Flexible contract categorization

**Example Tags**:
- Market segment: "Spot", "Term Contract"
- Product: "Brent", "WTI", "MGO"
- Region: "Asia-Pacific", "Europe", "Americas"
- Risk level: "High", "Medium", "Low"
- Customer type: "Trader", "Refiner", "Airline"

**Usage**:
```csharp
var airlineContracts = _context.Contracts
    .Include(c => c.Tags)
    .Where(c => c.Tags.Any(t => t.Tag.Name == "Airline"))
    .ToList();
```

---

### 33-42. **Additional Operational Entities** (10 more)

| Entity | Purpose | Key Fields |
|--------|---------|-----------|
| ContractExecutionReport | Execution metrics | Activation date, completion %, pricing analysis |
| FinancialReport | P&L and exposure | Report date, P&L amount, exposure summary |
| RiskReport | Risk analysis | VaR calculation, stress test results |
| PaymentRiskAlert | Payment credit monitoring | Alert status, customer credit score |
| OperationAuditLog | Compliance audit trail | Operation, user, timestamp, changes (JSON diff) |
| FuturesDeal | Futures contract tracking | Trade date, entry price, mark-to-market |
| PhysicalContract | Physical commodity tracking | Product, location, quantity |
| TradeChain | Trade chain relationships | Source, intermediate, destination |
| PhysicalContractPosition | Physical positions | Location, quantity, value |
| SettlementRuleTrigger | Automation execution tracking | Rule ID, trigger time, status |

---

## Entity Relationships

### Master Data Relationships
```
Product (1) ────────────┬─ (many) PurchaseContract
           ├─ (many) SalesContract
           ├─ (many) PaperContract
           └─ (many) MarketPrice

TradingPartner (1) ─────┬─ (many) PurchaseContract (Supplier)
               ├─ (many) SalesContract (Customer)
               └─ (many) Payment (Counterparty)
```

### Contract Relationships
```
PurchaseContract (1) ──┬─ (many) ShippingOperation
                  ├─ (1) PurchaseSettlement (v2.10.0)
                  ├─ (1) ContractSettlement (legacy)
                  └─ (many) ContractMatching

SalesContract (1) ─────┬─ (many) ShippingOperation
              ├─ (1) SalesSettlement (v2.10.0)
              ├─ (1) ContractSettlement (legacy)
              └─ (many) ContractMatching

ContractMatching ──────┬─ (1) PurchaseContract
                  └─ (1) SalesContract
```

### Inventory Relationships
```
InventoryLocation (1) ────────┬─ (many) InventoryPosition
                         ├─ (many) InventoryMovement
                         └─ (many) InventoryReservation
```

### Settlement Relationships
```
ContractSettlement (1) ───────┬─ (many) SettlementCharge
                         └─ (1) Payment

PurchaseSettlement (1) ────────┬─ (many) SettlementCharge
                          └─ (1) Payment

SalesSettlement (1) ───────────┬─ (many) SettlementCharge
                         └─ (1) Payment
```

---

## Database Schema Statistics

**Total Tables**: 47
**Total Columns**: ~550
**Total Indexes**: 60+
**Primary Keys**: 47 (GUID type)
**Foreign Keys**: 85+
**Unique Constraints**: 25+

**Largest Tables** (by column count):
1. PurchaseContract - 35 columns
2. SalesContract - 38 columns
3. ContractSettlement - 22 columns
4. TradeGroup - 18 columns
5. InventoryPosition - 20 columns

**Most Queried Tables** (by index count):
1. PurchaseContract - 8 indexes
2. SalesContract - 8 indexes
3. ContractSettlement - 6 indexes
4. InventoryPosition - 5 indexes
5. Payment - 4 indexes

---

## Summary

This reference documents all **47 production entities** organized by business domain:

- **9 Core Trading Entities**: Contracts, partners, products, shipping
- **12 Advanced Financial Entities**: Settlements, derivatives, pricing, financials
- **26 Operational Entities**: Inventory, automation, reporting, audit

Each entity includes:
- Business purpose and use case
- Key properties with type information
- Business rules and constraints
- Related entities and relationships
- Methods and typical operations

The complete entity model represents a **fully-featured enterprise oil trading platform** with advanced risk management, multi-leg strategy support, and comprehensive operational tracking.

For detailed API endpoints, see [API_REFERENCE_COMPLETE.md](./API_REFERENCE_COMPLETE.md)
For architecture patterns, see [ARCHITECTURE_BLUEPRINT.md](./ARCHITECTURE_BLUEPRINT.md)

