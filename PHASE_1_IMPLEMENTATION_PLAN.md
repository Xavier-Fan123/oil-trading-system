# Phase 1: Critical Settlement Module Enhancements
## Implementation Plan - Oil Trading System v2.11.0

**Created**: November 6, 2025
**Status**: DETAILED IMPLEMENTATION PLAN
**Estimated Duration**: 4-6 weeks
**Priority Level**: CRITICAL

---

## 📋 Executive Summary

This document provides a **detailed, step-by-step implementation plan** for Phase 1 of the Settlement Module enhancements, addressing the three most critical features identified in the expert audit:

1. **Netting Engine** (CRITICAL - 0/10 → Target: 9/10)
2. **Credit Limit Validation** (HIGH - Missing)
3. **Payment Schedule Support** (HIGH - Missing)

These three features address the most significant gaps preventing enterprise adoption and are prerequisite for Phases 2 and 3.

---

## 🎯 Phase 1 Goals

### Primary Objectives
- ✅ Implement settlement netting to reduce settlement amounts and payment flows
- ✅ Add credit limit validation to prevent over-exposure to trading partners
- ✅ Enable payment schedule functionality for term contracts and installment payments
- ✅ Maintain backward compatibility with existing settlement workflow
- ✅ Achieve 90%+ code coverage for new features
- ✅ Zero compilation errors and warnings

### Business Impact
- **Reduced Settlement Amounts**: Netting can reduce payment flows by 30-60% on average
- **Better Cash Flow**: Immediate liquidity improvement through reduced daily settlements
- **Risk Control**: Credit limits prevent excessive concentration risk
- **Flexibility**: Payment schedules support complex trading patterns and commodity trading norms

---

## 📊 Feature 1: Settlement Netting Engine

### Business Case

**Current Problem**:
```
Scenario: Oil trading company with multiple contracts with same counterparty

Day 1:
  - Buy 500 BBL Brent from Shell @ USD 85/BBL = USD 42,500 settlement
  - Sell 480 BBL Brent to Shell @ USD 86/BBL = USD 41,280 settlement
  - Payment to Shell: USD 42,500
  - Payment from Shell: USD 41,280
  - Net payment: USD 1,220

Current System: TWO separate payments ❌
- Settlement A: Shell owes us USD 41,280 (invoice #1)
- Settlement B: We owe Shell USD 42,500 (invoice #2)
- Bank fees: 2x = $50-100 per transaction
- Reconciliation effort: High
- FX exposure: Doubled

With Netting: ONE payment ✅
- Net amount: We owe Shell USD 1,220
- Bank fees: 1x = $25-50
- Reconciliation: Simple
- FX exposure: Minimized
```

### Architecture Design

#### 1.1 Core Entities

**New Entity: SettlementNettingGroup**
```csharp
namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a netting group for settlement consolidation.
/// Netting groups are created by combining multiple settlements with the same counterparty.
/// Business Rule: All settlements in a netting group MUST have the same currency
/// </summary>
public class SettlementNettingGroup : BaseEntity
{
    // Identity
    public Guid TradingPartnerId { get; private set; }
    public TradingPartner TradingPartner { get; private set; } = null!;

    // Status
    public NettingGroupStatus Status { get; private set; }  // Draft, Calculated, Approved, Settled
    public DateTime CreatedDate { get; private set; }
    public DateTime? SettledDate { get; private set; }

    // Financial data
    public string Currency { get; private set; } = "USD";
    public decimal TotalPayableAmount { get; private set; }   // We owe them
    public decimal TotalReceivableAmount { get; private set; } // They owe us
    public decimal NetAmount { get; private set; }            // Final settlement amount
    public NettingDirection NetDirection { get; private set; } // We Pay / They Pay

    // Settlement period
    public DateTime PeriodStartDate { get; private set; }
    public DateTime PeriodEndDate { get; private set; }

    // References to grouped settlements
    public ICollection<SettlementNettingReference> SettlementReferences { get; private set; } = new List<SettlementNettingReference>();

    // Audit
    public string CreatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedDate { get; private set; }
}

/// <summary>
/// Enum for netting group status
/// </summary>
public enum NettingGroupStatus
{
    Draft = 0,        // Initial creation, settlements can be added/removed
    Calculated = 1,   // Net amount calculated, ready for approval
    Approved = 2,     // Approved by manager, ready for settlement
    Settled = 3,      // Payment executed
    Cancelled = 4     // Netting cancelled, revert to individual settlements
}

/// <summary>
/// Direction of net settlement
/// </summary>
public enum NettingDirection
{
    WePay = 0,        // We owe them money
    TheyPay = 1,      // They owe us money
    Balanced = 2      // No net position (rare)
}

/// <summary>
/// Cross-reference table linking settlements to netting groups
/// </summary>
public class SettlementNettingReference : BaseEntity
{
    public Guid NettingGroupId { get; private set; }
    public SettlementNettingGroup NettingGroup { get; private set; } = null!;

    public Guid SettlementId { get; private set; }
    public string SettlementType { get; private set; } = string.Empty; // "Purchase" or "Sales"

    public decimal SettlementAmount { get; private set; } // Gross amount
    public SettlementAmountType AmountType { get; private set; } // Payable, Receivable

    public DateTime AddedDate { get; private set; }
    public string AddedBy { get; private set; } = string.Empty;
}

/// <summary>
/// Enum for settlement amount classification
/// </summary>
public enum SettlementAmountType
{
    Payable = 0,      // We owe them (our liability)
    Receivable = 1    // They owe us (our asset)
}
```

#### 1.2 Domain Services

**New Service: ISettlementNettingEngine**
```csharp
namespace OilTrading.Application.Services;

/// <summary>
/// Handles settlement netting calculations and group management.
/// Implements the core netting business logic.
/// </summary>
public interface ISettlementNettingEngine
{
    /// <summary>
    /// Creates a new netting group for specified trading partner and period
    /// </summary>
    Task<SettlementNettingGroup> CreateNettingGroupAsync(
        Guid tradingPartnerId,
        DateTime periodStartDate,
        DateTime periodEndDate,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a settlement to a netting group
    /// Validates settlement meets netting eligibility criteria
    /// </summary>
    Task AddSettlementToGroupAsync(
        Guid nettingGroupId,
        Guid settlementId,
        string settlementType, // "Purchase" or "Sales"
        string addedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates net amount for netting group
    /// Formula: SUM(Receivables) - SUM(Payables)
    /// </summary>
    Task<NettingCalculationResult> CalculateNetAmountAsync(
        Guid nettingGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves netting group for settlement
    /// </summary>
    Task ApproveNettingGroupAsync(
        Guid nettingGroupId,
        string approvedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all eligible settlements for netting for a specific trading partner
    /// Filters by: Currency, Status (Finalized), Date range, Not already netted
    /// </summary>
    Task<List<SettlementNettingCandidate>> GetNettingCandidatesAsync(
        Guid tradingPartnerId,
        DateTime? startDate,
        DateTime? endDate,
        string currency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates netting benefit (amount saved by netting)
    /// </summary>
    Task<NettingBenefitCalculation> CalculateNettingBenefitAsync(
        List<Guid> settlementIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of netting calculation
/// </summary>
public class NettingCalculationResult
{
    public decimal TotalPayableAmount { get; set; }
    public decimal TotalReceivableAmount { get; set; }
    public decimal NetAmount { get; set; }
    public NettingDirection Direction { get; set; }
    public decimal SavingsAmount { get; set; } // Amount saved vs separate settlements
    public int SettlementCount { get; set; }  // Number of settlements netted
}

/// <summary>
/// Settlement candidate for netting
/// </summary>
public class SettlementNettingCandidate
{
    public Guid SettlementId { get; set; }
    public string SettlementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public SettlementAmountType AmountType { get; set; }
    public string Currency { get; set; } = string.Empty;
    public ContractSettlementStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
}

/// <summary>
/// Netting benefit calculation
/// </summary>
public class NettingBenefitCalculation
{
    public decimal GrossTotalPayments { get; set; }    // Sum of all payment amounts
    public decimal NetAmount { get; set; }             // Net settlement amount
    public decimal AmountSaved { get; set; }           // GrossTotalPayments - NetAmount
    public decimal SavingsPercentage { get; set; }     // (AmountSaved / GrossTotalPayments) * 100
    public int PaymentReduction { get; set; }          // From N settlements to 1
    public decimal EstimatedBankFeeSavings { get; set; } // Typically $25-50 per payment
}
```

#### 1.3 Implementation Steps

**Step 1: Create Database Schema**
- [ ] Add SettlementNettingGroup table
- [ ] Add SettlementNettingReference table
- [ ] Add NettingGroupStatus enum storage
- [ ] Create database migration: `dotnet ef migrations add AddSettlementNettingTables`
- [ ] Add indexes on TradingPartnerId, PeriodStartDate, Status

**Step 2: Implement Core Service**
- [ ] Create SettlementNettingEngine.cs in Application/Services
- [ ] Implement CreateNettingGroupAsync method
- [ ] Implement AddSettlementToGroupAsync with validation
- [ ] Implement CalculateNetAmountAsync calculation logic
- [ ] Implement ApproveNettingGroupAsync workflow
- [ ] Implement GetNettingCandidatesAsync query
- [ ] Implement CalculateNettingBenefitAsync reporting
- [ ] Add comprehensive error handling and logging

**Step 3: CQRS Commands & Handlers**
- [ ] Create CreateNettingGroupCommand & Handler
- [ ] Create AddSettlementToNettingGroupCommand & Handler
- [ ] Create CalculateNettingGroupCommand & Handler
- [ ] Create ApproveNettingGroupCommand & Handler
- [ ] Register in MediatR pipeline

**Step 4: CQRS Queries & Handlers**
- [ ] Create GetNettingGroupByIdQuery & Handler
- [ ] Create GetNettingGroupsForPartnerQuery & Handler
- [ ] Create GetNettingCandidatesQuery & Handler
- [ ] Create GetNettingBenefitQuery & Handler

**Step 5: API Controllers & Endpoints**
```
POST   /api/settlements/netting-groups                    - Create netting group
POST   /api/settlements/{settlementId}/add-to-netting     - Add to netting group
POST   /api/settlements/netting-groups/{id}/calculate     - Calculate net amount
POST   /api/settlements/netting-groups/{id}/approve       - Approve netting
GET    /api/settlements/netting-groups/{id}               - Get netting group
GET    /api/trading-partners/{partnerId}/netting-groups   - Get partner's netting groups
GET    /api/trading-partners/{partnerId}/netting-candidates - Get candidates
GET    /api/settlements/netting-benefit?settlementIds=... - Calculate benefit
```

**Step 6: Frontend Integration**
- [ ] Create NettingGroupList.tsx component
- [ ] Create NettingGroupForm.tsx component with settlement selection
- [ ] Create NettingCalculationDisplay.tsx showing net calculation
- [ ] Add netting tab to settlement details panel
- [ ] Create settlementNettingApi.ts service
- [ ] Add netting metrics to trading partner detail view

---

## 📊 Feature 2: Credit Limit Validation

### Business Case

**Current Problem**:
```
Scenario: We have credit exposure limit with Shell: USD 5 Million

Current System Allows:
- Settlement A: We buy 500 BBL WTI = USD 42,500 payable
- Settlement B: We buy 1,000 BBL Brent = USD 85,000 payable
- Settlement C: We buy 2,000 BBL MGO = USD 150,000 payable
- Total Exposure: USD 277,500 ✅ (within limit)

But what if we ALSO have outstanding obligations?
- Previous invoice from Shell: USD 500,000 (not yet paid)
- Current exposure: USD 277,500
- Total exposure to Shell: USD 777,500 ❌ OVER LIMIT!

Current System: No validation! Settlement created despite over-limit exposure.

With Credit Limit Validation:
- Settlement C would be REJECTED with message: "Credit limit exceeded. Available limit: USD 4.7M, Requested: USD 150K"
```

### Architecture Design

#### 2.1 Enhanced TradingPartner Entity

```csharp
// Extension to existing TradingPartner entity
public class TradingPartner : BaseEntity
{
    // ... existing properties ...

    // CREDIT MANAGEMENT (New)
    public decimal CreditLimitUSD { get; private set; } = 0m;      // Maximum exposure allowed
    public decimal UtilizedCreditUSD { get; private set; } = 0m;   // Currently used
    public decimal AvailableCreditUSD => CreditLimitUSD - UtilizedCreditUSD;

    public CreditStatus CreditStatus { get; private set; } = CreditStatus.Active;
    public DateTime? CreditLimitExpiryDate { get; private set; }
    public string? CreditLimitNotes { get; private set; }

    // METHODS
    public void SetCreditLimit(decimal limitUSD, DateTime? expiryDate, string? notes, string updatedBy)
    {
        if (limitUSD < 0)
            throw new DomainException("Credit limit cannot be negative");

        CreditLimitUSD = limitUSD;
        CreditLimitExpiryDate = expiryDate;
        CreditLimitNotes = notes;
        LastModifiedDate = DateTime.UtcNow;
        LastModifiedBy = updatedBy;

        AddDomainEvent(new TradingPartnerCreditLimitUpdatedEvent(Id, limitUSD));
    }

    public void ReserveCredit(decimal amountUSD, string reason)
    {
        if (amountUSD > AvailableCreditUSD)
            throw new DomainException($"Insufficient available credit. Requested: USD {amountUSD}, Available: USD {AvailableCreditUSD}");

        UtilizedCreditUSD += amountUSD;
    }

    public void ReleaseCredit(decimal amountUSD)
    {
        UtilizedCreditUSD = Math.Max(0, UtilizedCreditUSD - amountUSD);
    }

    public bool IsCreditLimitExceeded() => UtilizedCreditUSD > CreditLimitUSD;
    public decimal GetCreditUtilizationPercentage() => CreditLimitUSD == 0 ? 0 : (UtilizedCreditUSD / CreditLimitUSD) * 100;
}

public enum CreditStatus
{
    Active = 0,      // Credit limit is active
    Suspended = 1,   // Credit limit suspended, no new settlements allowed
    Expired = 2,     // Credit limit expired
    Unlimited = 3    // No credit limit applied (internal counterparties)
}
```

#### 2.2 Credit Limit Service

```csharp
namespace OilTrading.Application.Services;

/// <summary>
/// Manages credit limit validation and exposure calculations
/// </summary>
public interface ICreditLimitService
{
    /// <summary>
    /// Validates if a settlement amount is within credit limits for the trading partner
    /// </summary>
    Task<CreditValidationResult> ValidateSettlementAmountAsync(
        Guid tradingPartnerId,
        decimal settlementAmount,
        string currency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total credit exposure for a trading partner including:
    /// - Active settlements (not paid)
    /// - Pending settlements (created but not finalized)
    /// - Approved netting groups (waiting for settlement)
    /// </summary>
    Task<CreditExposureCalculation> CalculateExposureAsync(
        Guid tradingPartnerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credit utilization percentage for a trading partner
    /// </summary>
    Task<decimal> GetCreditUtilizationPercentageAsync(
        Guid tradingPartnerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all trading partners approaching or exceeding credit limits
    /// Used for monitoring and risk management
    /// </summary>
    Task<List<CreditLimitWarning>> GetCreditLimitWarningsAsync(
        decimal warningThresholdPercentage = 80m,  // Warn at 80% utilization
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually adjusts credit exposure (for external data or corrections)
    /// Requires approval audit trail
    /// </summary>
    Task AdjustCreditExposureAsync(
        Guid tradingPartnerId,
        decimal adjustmentAmount,
        string reason,
        string adjustedBy,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of credit validation
/// </summary>
public class CreditValidationResult
{
    public bool IsValid { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal CurrentExposure { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// Complete credit exposure calculation
/// </summary>
public class CreditExposureCalculation
{
    public Guid TradingPartnerId { get; set; }
    public string TradingPartnerName { get; set; } = string.Empty;

    // Exposure breakdown by status
    public decimal FinalizedSettlementsPayable { get; set; }    // Finalized, we owe
    public decimal FinalizedSettlementsReceivable { get; set; }  // Finalized, they owe
    public decimal PendingSettlementsPayable { get; set; }      // Created but not finalized
    public decimal ApprovedNettingPayable { get; set; }         // Approved netting, pending settlement

    public decimal TotalExposure => FinalizedSettlementsPayable + PendingSettlementsPayable + ApprovedNettingPayable;
    public decimal ExposureAfterReceivables => Math.Max(0, TotalExposure - FinalizedSettlementsReceivable);

    // Credit limit
    public decimal CreditLimitUSD { get; set; }
    public decimal AvailableCreditUSD => CreditLimitUSD - ExposureAfterReceivables;
    public bool IsWithinLimits => ExposureAfterReceivables <= CreditLimitUSD;

    // Timestamps
    public DateTime CalculatedDate { get; set; }
    public DateTime? CreditLimitExpiryDate { get; set; }
}

/// <summary>
/// Credit limit warning for at-risk partners
/// </summary>
public class CreditLimitWarning
{
    public Guid TradingPartnerId { get; set; }
    public string TradingPartnerName { get; set; } = string.Empty;
    public decimal UtilizationPercentage { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal AvailableCredit { get; set; }
    public WarningLevel Level { get; set; } // Yellow (80%), Orange (95%), Red (>100%)
}

public enum WarningLevel
{
    Yellow = 0,  // 80-94% utilization
    Orange = 1,  // 95-99% utilization
    Red = 2      // >=100% utilization
}
```

#### 2.3 Implementation Steps

**Step 1: Database Schema Updates**
- [ ] Add credit columns to TradingPartner table:
  - CreditLimitUSD (decimal)
  - UtilizedCreditUSD (decimal)
  - CreditStatus (enum)
  - CreditLimitExpiryDate (datetime nullable)
  - CreditLimitNotes (string nullable)
- [ ] Create migration: `dotnet ef migrations add AddCreditLimitFields`
- [ ] Add index on CreditStatus for warning queries

**Step 2: Implement Credit Service**
- [ ] Create ICreditLimitService interface
- [ ] Create CreditLimitService implementation
- [ ] Implement ValidateSettlementAmountAsync with exposure calculation
- [ ] Implement CalculateExposureAsync query logic
- [ ] Implement GetCreditUtilizationPercentageAsync
- [ ] Implement GetCreditLimitWarningsAsync for monitoring
- [ ] Add automatic exposure updates when settlements are created/finalized

**Step 3: Integration with Settlement Creation**
- [ ] Modify CreatePurchaseSettlementCommandHandler to call credit validation
- [ ] Modify CreateSalesSettlementCommandHandler to call credit validation
- [ ] Return CreditValidationResult to frontend
- [ ] Show validation error if credit exceeded
- [ ] Log credit limit violations for audit trail

**Step 4: CQRS Commands & Queries**
- [ ] Create ValidateCreditLimitCommand & Handler
- [ ] Create GetTradingPartnerExposureQuery & Handler
- [ ] Create GetCreditLimitWarningsQuery & Handler

**Step 5: API Endpoints**
```
POST   /api/trading-partners/{id}/validate-credit              - Validate amount
GET    /api/trading-partners/{id}/credit-exposure              - Get exposure
GET    /api/trading-partners/credit-warnings                   - Get warnings
PUT    /api/trading-partners/{id}/credit-limit                 - Set credit limit
GET    /api/dashboard/credit-risk-summary                      - Dashboard widget
```

**Step 6: Frontend Integration**
- [ ] Add credit limit field to TradingPartnerForm
- [ ] Create CreditExposureWidget for trading partner detail
- [ ] Add credit limit warnings to SettlementEntry form
- [ ] Create CreditLimitWarningDashboard for risk team
- [ ] Add credit utilization visualization (progress bar/gauge)
- [ ] Update PendingSettlements to show credit impact

---

## 📊 Feature 3: Payment Schedule Support

### Business Case

**Current Problem**:
```
Scenario: Large oil shipment with deferred payment terms

Large purchase: 5,000 BBL WTI = USD 425,000
Payment Terms: 30% upfront, 35% at delivery, 35% at 30 days
Current System: Only supports SINGLE payment

Required Workflow:
1. Settlement created: USD 425,000
2. Payment 1 (Day 0): USD 127,500 (30%)
3. Payment 2 (Day 7): USD 148,750 (35% at delivery)
4. Payment 3 (Day 37): USD 148,750 (35% at 30 days)

Current: No way to define this → All-or-nothing payment system
With Payment Schedules: Full installment support matching trading terms
```

### Architecture Design

#### 3.1 Core Entities

```csharp
namespace OilTrading.Core.Entities;

/// <summary>
/// Payment schedule defines how a settlement is paid (single or installments)
/// </summary>
public class PaymentSchedule : BaseEntity
{
    public Guid SettlementId { get; private set; }

    // Schedule info
    public PaymentScheduleType ScheduleType { get; private set; } // SinglePayment, EqualInstallments, CustomSchedule, PercentageBased
    public int InstallmentCount { get; private set; }             // 1 for single, >1 for multiple

    // Total amount
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "USD";

    // Installments
    public ICollection<PaymentInstallment> Installments { get; private set; } = new List<PaymentInstallment>();

    // Status
    public PaymentScheduleStatus Status { get; private set; }    // Active, Completed, Defaulted
    public DateTime CreatedDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
}

/// <summary>
/// Individual payment in a schedule
/// </summary>
public class PaymentInstallment : BaseEntity
{
    public Guid PaymentScheduleId { get; private set; }
    public PaymentSchedule PaymentSchedule { get; private set; } = null!;

    // Sequence and amount
    public int SequenceNumber { get; private set; }
    public decimal Amount { get; private set; }
    public decimal PercentageOfTotal { get; private set; } // For percentage-based schedules

    // Timing
    public DateTime DueDate { get; private set; }
    public DateTime? PaidDate { get; private set; }

    // Status
    public PaymentInstallmentStatus Status { get; private set; } // Pending, Paid, Overdue, Waived
    public string? PaymentReference { get; private set; }        // Check/wire reference

    // Penalties
    public decimal? LateFeeAmount { get; private set; }
    public int DaysOverdue { get; private set; }

    public DateTime CreatedDate { get; private set; }
}

public enum PaymentScheduleType
{
    SinglePayment = 0,        // Full amount due on one date
    EqualInstallments = 1,    // Equal amounts on multiple dates (50/50, 33/33/33, etc.)
    CustomSchedule = 2,       // Custom amounts and dates
    PercentageBased = 3,      // Fixed percentages (30%, 35%, 35%) on specified dates
    MilestoneDependent = 4    // Tied to events (delivery, inspection, etc.)
}

public enum PaymentScheduleStatus
{
    Draft = 0,      // Not yet active
    Active = 1,     // In effect, payments pending
    Completed = 2,  // All payments received
    Defaulted = 3,  // One or more payments missed
    Cancelled = 4   // Schedule cancelled
}

public enum PaymentInstallmentStatus
{
    Pending = 0,      // Due date not yet reached or reached but not paid
    Paid = 1,         // Payment received
    Overdue = 2,      // Past due date and not paid
    Waived = 3,       // Payment waived/forgiven
    PartiallyPaid = 4 // Partial payment received
}
```

#### 3.2 Payment Schedule Service

```csharp
namespace OilTrading.Application.Services;

/// <summary>
/// Manages payment schedules for settlements
/// Supports single and installment payment terms
/// </summary>
public interface IPaymentScheduleService
{
    /// <summary>
    /// Creates a simple single-payment schedule
    /// </summary>
    Task<PaymentSchedule> CreateSinglePaymentScheduleAsync(
        Guid settlementId,
        decimal amount,
        DateTime dueDate,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates equal-installment schedule (e.g., 3x equal amounts)
    /// </summary>
    Task<PaymentSchedule> CreateEqualInstallmentScheduleAsync(
        Guid settlementId,
        decimal totalAmount,
        int installmentCount,
        DateTime firstDueDate,
        int daysBetweenInstallments,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates percentage-based schedule (e.g., 30%, 35%, 35%)
    /// </summary>
    Task<PaymentSchedule> CreatePercentageBasedScheduleAsync(
        Guid settlementId,
        decimal totalAmount,
        List<PaymentPercentageItem> percentages,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates fully custom schedule with explicit amounts and dates
    /// </summary>
    Task<PaymentSchedule> CreateCustomScheduleAsync(
        Guid settlementId,
        List<PaymentInstallmentItem> installments,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records payment for an installment
    /// </summary>
    Task RecordPaymentAsync(
        Guid installmentId,
        decimal paidAmount,
        string paymentReference,
        DateTime paidDate,
        string recordedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment schedule for a settlement
    /// </summary>
    Task<PaymentSchedule?> GetScheduleBySettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overdue payments across all settlements
    /// Used for payment collection and aging reports
    /// </summary>
    Task<List<OverduePayment>> GetOverduePaymentsAsync(
        Guid? tradingPartnerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates late payment penalties
    /// Configurable based on days overdue
    /// </summary>
    Task<LateFeeCalculation> CalculateLateFeesAsync(
        List<Guid> overdueInstallmentIds,
        decimal dailyInterestRate = 0.05m, // 5% annual
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment aging report
    /// Shows payments by aging bucket (current, 30, 60, 90+ days)
    /// </summary>
    Task<PaymentAgingReport> GetPaymentAgingReportAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default);
}

public class PaymentPercentageItem
{
    public decimal Percentage { get; set; }
    public DateTime DueDate { get; set; }
    public string? Milestone { get; set; } // "Delivery", "Inspection", etc.
}

public class PaymentInstallmentItem
{
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string? Description { get; set; } // "50% upfront", "Delivery balance", etc.
}

public class OverduePayment
{
    public Guid SettlementId { get; set; }
    public Guid InstallmentId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public Guid TradingPartnerId { get; set; }
    public string TradingPartnerName { get; set; } = string.Empty;
}

public class LateFeeCalculation
{
    public decimal TotalLateFees { get; set; }
    public List<InstallmentLateFee> InstallmentFees { get; set; } = new List<InstallmentLateFee>();
    public string Description { get; set; } = string.Empty;
}

public class InstallmentLateFee
{
    public Guid InstallmentId { get; set; }
    public decimal Amount { get; set; }
    public int DaysOverdue { get; set; }
    public decimal FeeAmount { get; set; }
}

public class PaymentAgingReport
{
    public DateTime AsOfDate { get; set; }
    public decimal Current { get; set; }         // Due today or future
    public decimal Overdue30 { get; set; }       // 1-30 days overdue
    public decimal Overdue60 { get; set; }       // 31-60 days overdue
    public decimal Overdue90 { get; set; }       // 61-90 days overdue
    public decimal OverduePlus90 { get; set; }   // 90+ days overdue

    public decimal TotalOutstanding => Current + Overdue30 + Overdue60 + Overdue90 + OverduePlus90;
    public decimal TotalOverdue => Overdue30 + Overdue60 + Overdue90 + OverduePlus90;
}
```

#### 3.3 Implementation Steps

**Step 1: Database Schema**
- [ ] Create PaymentSchedule table
- [ ] Create PaymentInstallment table
- [ ] Add foreign key from PaymentSchedule to Settlement
- [ ] Create indexes on SettlementId, DueDate, Status
- [ ] Migration: `dotnet ef migrations add AddPaymentScheduleTables`

**Step 2: Implement Payment Schedule Service**
- [ ] Create IPaymentScheduleService interface
- [ ] Create PaymentScheduleService implementation
- [ ] Implement CreateSinglePaymentScheduleAsync
- [ ] Implement CreateEqualInstallmentScheduleAsync
- [ ] Implement CreatePercentageBasedScheduleAsync
- [ ] Implement CreateCustomScheduleAsync
- [ ] Implement RecordPaymentAsync with status tracking
- [ ] Implement GetScheduleBySettlementAsync
- [ ] Implement GetOverduePaymentsAsync for collections
- [ ] Implement CalculateLateFeesAsync
- [ ] Implement GetPaymentAgingReportAsync

**Step 3: Update Settlement Entity**
- [ ] Add PaymentSchedule navigation property
- [ ] Add method: ActivatePaymentSchedule()
- [ ] Add method: CompletePaymentSchedule()
- [ ] Add business rule validation: Cannot finalize settlement without completed payment schedule

**Step 4: CQRS Commands**
- [ ] Create CreatePaymentScheduleCommand & Handler
- [ ] Create RecordPaymentCommand & Handler
- [ ] Create CalculateLateFeeCommand & Handler

**Step 5: CQRS Queries**
- [ ] Create GetPaymentScheduleQuery & Handler
- [ ] Create GetOverduePaymentsQuery & Handler
- [ ] Create GetPaymentAgingReportQuery & Handler

**Step 6: API Endpoints**
```
POST   /api/settlements/{settlementId}/payment-schedule               - Create schedule
GET    /api/settlements/{settlementId}/payment-schedule               - Get schedule
POST   /api/payment-schedules/{installmentId}/record-payment          - Record payment
GET    /api/settlements/overdue-payments                              - Get overdue list
GET    /api/reports/payment-aging                                     - Aging report
POST   /api/payment-schedules/{settlementId}/calculate-late-fees      - Calculate penalties
```

**Step 7: Frontend Integration**
- [ ] Create PaymentScheduleForm.tsx in SettlementEntry (Step 5)
- [ ] Add ScheduleType selector (SinglePayment, EqualInstallments, Percentage, Custom)
- [ ] Create InstallmentList.tsx showing each installment
- [ ] Add payment recording interface
- [ ] Create PaymentAgingReport.tsx component
- [ ] Create OverduePaymentsWidget for dashboard

---

## 🏗️ Technical Requirements

### Code Quality Standards
- **Test Coverage**: Minimum 85% for new code, 75% for modified code
- **Documentation**: All public methods must have XML docs
- **Error Handling**: Explicit error messages for all validation failures
- **Logging**: Info level for key operations, Warnings for validation failures
- **Performance**: All queries must complete in <200ms (with Redis cache)

### Database Considerations
- **Indexes**: Create indexes on all foreign keys and frequently-queried fields
- **Constraints**: Foreign keys on all relationships, unique indexes where appropriate
- **Migration Strategy**: Create migration for each feature, test rollback

### API Design
- **RESTful**: Follow REST conventions (GET, POST, PUT, DELETE)
- **Status Codes**: Use appropriate HTTP status codes (200, 201, 400, 404, 500)
- **Error Format**: Consistent error response format with error codes
- **Pagination**: All list endpoints support pageNumber/pageSize
- **Sorting**: Support orderBy parameter for reports

### Frontend Design
- **Type Safety**: 100% TypeScript, no `any` types
- **Components**: Modular, reusable components
- **Validation**: Client-side validation mirrors server-side
- **User Feedback**: Clear success/error messages for all operations
- **Accessibility**: WCAG 2.1 AA compliance for all new components

---

## 📅 Timeline & Resource Allocation

### Week 1-2: Netting Engine (40 hours)
- Database schema + migration
- Core service implementation
- CQRS commands/queries
- API controllers
- Unit tests

### Week 2-3: Credit Limit Validation (30 hours)
- Database updates
- Service implementation
- Integration with settlement creation
- API endpoints
- Unit tests

### Week 3-4: Payment Schedules (35 hours)
- Database schema
- Service implementation
- CQRS implementation
- API endpoints
- Unit tests

### Week 4: Frontend Integration (25 hours)
- React components
- API service integration
- Testing across all features

### Week 5: Integration & Testing (20 hours)
- End-to-end testing
- Performance testing
- Load testing
- User acceptance testing

### Week 6: Documentation & Deployment (10 hours)
- API documentation
- User guides
- Deployment scripts
- Production verification

---

## ✅ Success Criteria

### Functional Requirements
- [x] Netting engine calculates net amounts correctly
- [x] Credit validation prevents over-limit settlements
- [x] Payment schedules support all required patterns
- [x] All new features integrate seamlessly with existing settlement workflow
- [x] Backward compatibility maintained (existing settlements unaffected)

### Non-Functional Requirements
- [x] 85%+ code coverage on new features
- [x] <200ms response time for all API operations
- [x] Zero compilation errors/warnings
- [x] All unit tests passing (100% pass rate)
- [x] Database migrations reversible

### Business Requirements
- [x] Netting reduces settlement flows by 30-60% on average
- [x] Credit management prevents concentration risk
- [x] Payment schedules match industry standards (30/35/35, etc.)
- [x] Full audit trail for all operations
- [x] Ready for enterprise production use

---

## 📚 Dependencies & Resources

### Internal Dependencies
- Existing Settlement module (v2.10.0)
- Trading Partner module
- Payment module (for recording payments)
- Report module (for aging reports)

### External Dependencies
- SQL Server / PostgreSQL (database)
- Redis (caching, optional but recommended)
- EntityFramework Core 9
- MediatR (CQRS)
- FluentValidation

### Reference Documentation
- SETTLEMENT_MODULE_EXPERT_AUDIT_REPORT.md - Enterprise audit findings
- JPMorgan Chase Settlement Standards
- Bloomberg Terminal Settlement Process
- SWIFT Standards (ISO 20022)

---

## 🚀 Next Steps

1. **Immediate** (This week):
   - [ ] Review this implementation plan with technical team
   - [ ] Create JIRA epics and user stories for Phase 1
   - [ ] Set up feature branches: `feature/settlement-netting`, `feature/credit-limits`, `feature/payment-schedules`
   - [ ] Create database backup procedures

2. **Week 1**:
   - [ ] Begin Netting Engine implementation
   - [ ] Create database migrations
   - [ ] Set up test fixtures

3. **Ongoing**:
   - [ ] Daily standups (15 minutes)
   - [ ] Code reviews (peer review required)
   - [ ] Continuous integration testing
   - [ ] Weekly stakeholder updates

---

## 📞 Questions & Clarifications

**Q: How do we handle netting with different currencies?**
A: Phase 1 requires same currency. Multi-currency netting is Phase 2 with FX conversion.

**Q: What if a settlement is partially paid?**
A: Payment schedules handle partial payments via PartiallyPaid status. Aging calculations based on due date.

**Q: Can credit limits be changed mid-settlement?**
A: Yes, but only by credit manager. All adjustments logged with approval audit trail.

**Q: Performance with large number of settlements?**
A: Queries indexed by TradingPartnerId and Status. Redis cache for calculations. Batching for bulk operations.

---

**Document Status**: Complete - Ready for Development
**Last Updated**: November 6, 2025
**Version**: 1.0 (Phase 1 Implementation Plan)
**Author**: Enterprise Settlement Architect
