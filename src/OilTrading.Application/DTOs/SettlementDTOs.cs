using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

namespace OilTrading.Application.DTOs;

// Note: SettlementType enum moved to OilTrading.Core.Enums.SettlementType
// SettlementStatus enum removed as part of settlement consolidation from 3 modules to 2

public enum PaymentStatus
{
    Pending = 1,
    Initiated = 2,
    InProgress = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
    Expired = 7
}

public enum PaymentMethod
{
    TelegraphicTransfer = 1,
    LetterOfCredit = 2,
    CashInAdvance = 3,
    DocumentaryCollection = 4,
    OpenAccount = 5,
    BankGuarantee = 6
}

// REMOVED: Generic Settlement DTOs have been consolidated
// - Settlement DTO class removed (part of generic settlement consolidation)
// - SettlementRequest removed (use PurchaseSettlement/SalesSettlement specific DTOs instead)
// - SettlementUpdateRequest removed (use specific settlement update DTOs instead)

public class SettlementResult
{
    public bool IsSuccessful { get; set; }
    public Guid? SettlementId { get; set; }
    public string? SettlementNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SettlementSummary
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSettlements { get; set; }
    public Money TotalAmount { get; set; } = null!;
    // SettlementsByStatus removed - use specific settlement summaries for Purchase/SalesSettlements
    public Dictionary<string, int> SettlementsByStatusName { get; set; } = new();
    public Dictionary<string, int> SettlementsByType { get; set; } = new();
    public Dictionary<string, Money> AmountsByCurrency { get; set; } = new();
    public List<SettlementTrend> Trends { get; set; } = new();
}

public class SettlementTrend
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public Money Amount { get; set; } = null!;
}

public class SettlementScheduleRequest
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? NumberOfPayments { get; set; }
    public SettlementFrequency Frequency { get; set; }
    public Money PaymentAmount { get; set; } = null!;
}

public class SettlementScheduleResult
{
    public bool IsSuccessful { get; set; }
    public Guid? ScheduleId { get; set; }
    public List<DateTime> GeneratedDueDates { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class SettlementSchedule
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public Money Amount { get; set; } = null!;
    public SettlementFrequency Frequency { get; set; }
    public bool IsProcessed { get; set; }
}

public enum SettlementFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    SemiAnnually = 5,
    Annually = 6
}

// Payment DTO for API responses
public class Payment
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public Money Amount { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? BankReference { get; set; }
    public string? Instructions { get; set; }
    public DateTime? InitiatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? FailureReason { get; set; }
}

// Bank account DTO
public class BankAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string? RoutingNumber { get; set; }
    public string? BranchCode { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; } = new();
}

// Payment related DTOs
public class PaymentRequest
{
    public PaymentMethod Method { get; set; }
    public Money Amount { get; set; } = null!;
    public BankAccount? PayerAccount { get; set; }
    public BankAccount? PayeeAccount { get; set; }
    public string? Instructions { get; set; }
}

public class PaymentResult
{
    public bool IsSuccessful { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PaymentReference { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PaymentConfirmationRequest
{
    public string PaymentReference { get; set; } = string.Empty;
    public OilTrading.Application.DTOs.PaymentStatus Status { get; set; }
    public string? BankReference { get; set; }
    public string? Comments { get; set; }
}

// Reconciliation DTOs
public class ReconciliationResult
{
    public bool IsReconciled { get; set; }
    public List<ReconciliationIssue> Issues { get; set; } = new();
    public Money ReconciledAmount { get; set; } = null!;
    public Money VarianceAmount { get; set; } = null!;
    public DateTime ReconciliationDate { get; set; }
}

public class ReconciliationIssue
{
    public Guid SettlementId { get; set; }
    public ReconciliationIssueType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Money AmountDifference { get; set; } = null!;
    public ReconciliationSeverity Severity { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class ReconciliationSummary
{
    public DateTime Date { get; set; }
    public int TotalSettlements { get; set; }
    public int ReconciledSettlements { get; set; }
    public int UnreconciledSettlements { get; set; }
    public Money TotalVariance { get; set; } = null!;
    public List<ReconciliationIssue> OutstandingIssues { get; set; } = new();
}

public enum ReconciliationIssueType
{
    AmountMismatch = 1,
    DateMismatch = 2,
    CurrencyMismatch = 3,
    MissingPayment = 4,
    DuplicatePayment = 5,
    UnexpectedPayment = 6
}

public enum ReconciliationSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

// Settlement matching DTOs
public class SettlementMatchingRequest
{
    public List<Guid> SettlementIds { get; set; } = new();
    public SettlementMatchingCriteria Criteria { get; set; } = new();
}

public class SettlementMatchingResult
{
    public bool IsSuccessful { get; set; }
    public List<SettlementMatch> Matches { get; set; } = new();
    public List<Guid> UnmatchedSettlements { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class SettlementMatch
{
    public List<Guid> MatchedSettlementIds { get; set; } = new();
    public Money TotalAmount { get; set; } = null!;
    public decimal MatchConfidence { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}

public class SettlementMatchingCriteria
{
    public decimal AmountTolerancePercentage { get; set; } = 1.0m;
    public int DateToleranceDays { get; set; } = 5;
    public bool RequireSameCurrency { get; set; } = true;
    public bool RequireSameCounterparty { get; set; } = false;
}

public class SettlementMatchingRecommendation
{
    public List<Guid> SettlementIds { get; set; } = new();
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Money PotentialAmount { get; set; } = null!;
}

// Analytics DTOs
public class SettlementAnalytics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SettlementVolumeMetrics Volume { get; set; } = new();
    public SettlementPerformanceMetrics Performance { get; set; } = new();
    public CashFlowMetrics CashFlow { get; set; } = new();
    public List<SettlementTrend> Trends { get; set; } = new();
    public Dictionary<string, decimal> KPIs { get; set; } = new();
}

public class SettlementVolumeMetrics
{
    public int TotalSettlements { get; set; }
    public Money TotalAmount { get; set; } = null!;
    public Money AverageSettlementAmount { get; set; } = null!;
    public Money LargestSettlement { get; set; } = null!;
    public Money SmallestSettlement { get; set; } = null!;
    public Dictionary<SettlementType, int> VolumeByType { get; set; } = new();
    public Dictionary<string, Money> VolumeByCounterparty { get; set; } = new();
}

public class SettlementPerformanceMetrics
{
    public double OnTimePaymentRate { get; set; }
    public TimeSpan AveragePaymentDelay { get; set; }
    public double ReconciliationRate { get; set; }
    public int FailedPayments { get; set; }
    public Money LostToLateFees { get; set; } = null!;
    public Money SavedFromEarlyPaymentDiscounts { get; set; } = null!;
}

public class CashFlowMetrics
{
    public Money NetCashFlow { get; set; } = null!;
    public Money TotalInflows { get; set; } = null!;
    public Money TotalOutflows { get; set; } = null!;
    public List<CashFlowPeriod> PeriodBreakdown { get; set; } = new();
}

public class CashFlowPeriod
{
    public DateTime Date { get; set; }
    public Money Inflows { get; set; } = null!;
    public Money Outflows { get; set; } = null!;
    public Money NetFlow { get; set; } = null!;
}

// Cash flow forecast DTOs
public class CashFlowForecast
{
    public DateTime GeneratedDate { get; set; }
    public int ForecastDays { get; set; }
    public List<CashFlowForecastPeriod> Periods { get; set; } = new();
    public ForecastAccuracy Accuracy { get; set; } = new();
    public List<CashFlowRisk> Risks { get; set; } = new();
}

public class CashFlowForecastPeriod
{
    public DateTime Date { get; set; }
    public Money PredictedInflows { get; set; } = null!;
    public Money PredictedOutflows { get; set; } = null!;
    public Money PredictedNetFlow { get; set; } = null!;
    public Money CumulativeBalance { get; set; } = null!;
    public decimal ConfidenceLevel { get; set; }
}

public class ForecastAccuracy
{
    public decimal HistoricalAccuracy { get; set; }
    public TimeSpan ForecastHorizon { get; set; }
    public List<string> AssumptionsMade { get; set; } = new();
}

public class CashFlowRisk
{
    public string Description { get; set; } = string.Empty;
    public Money PotentialImpact { get; set; } = null!;
    public decimal Probability { get; set; }
    public DateTime PotentialDate { get; set; }
    public string Mitigation { get; set; } = string.Empty;
}

// Settlement alerts DTOs
public class SettlementAlert
{
    public SettlementAlertType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public SettlementAlertSeverity Severity { get; set; }
    public Guid SettlementId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public enum SettlementAlertType
{
    PaymentOverdue = 1,
    LargeCashMovement = 2,
    ReconciliationFailure = 3,
    PaymentFailure = 4,
    UnusualActivity = 5
}

public enum SettlementAlertSeverity
{
    Info = 1,
    Warning = 2,
    High = 3,
    Critical = 4
}