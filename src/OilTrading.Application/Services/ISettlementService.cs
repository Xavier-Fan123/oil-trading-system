using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;
using ApplicationSettlement = OilTrading.Application.DTOs.Settlement;
using CoreSettlementType = OilTrading.Core.Entities.SettlementType;
using CoreSettlementStatus = OilTrading.Core.Entities.SettlementStatus;
using CorePaymentStatus = OilTrading.Core.Entities.PaymentStatus;
using DtoSettlementType = OilTrading.Application.DTOs.SettlementType;
using DtoSettlementStatus = OilTrading.Application.DTOs.SettlementStatus;
using DtoPaymentStatus = OilTrading.Application.DTOs.PaymentStatus;
using DtoPayment = OilTrading.Application.DTOs.Payment;
using CorePayment = OilTrading.Core.Entities.Payment;
using DtoBankAccount = OilTrading.Application.DTOs.BankAccount;
using CoreBankAccount = OilTrading.Core.Entities.BankAccount;

namespace OilTrading.Application.Services;

public interface ISettlementService
{
    // Settlement operations
    Task<SettlementResult> CreateSettlementAsync(SettlementRequest request);
    Task<SettlementResult> ProcessSettlementAsync(Guid settlementId);
    Task<SettlementResult> CancelSettlementAsync(Guid settlementId, string reason);
    Task<SettlementResult> UpdateSettlementAsync(Guid settlementId, SettlementUpdateRequest request);
    
    // Settlement queries
    Task<ApplicationSettlement> GetSettlementAsync(Guid settlementId);
    Task<ApplicationSettlement> GetSettlementByIdAsync(Guid settlementId);
    Task<List<ApplicationSettlement>> GetSettlementsByContractAsync(Guid contractId);
    Task<List<ApplicationSettlement>> GetSettlementsForContractAsync(Guid contractId);
    Task<List<ApplicationSettlement>> GetPendingSettlementsAsync();
    Task<SettlementSummary> GetSettlementSummaryAsync(DateTime startDate, DateTime endDate);
    
    // Automatic settlement
    Task<List<SettlementResult>> ProcessDueSettlementsAsync();
    Task<SettlementScheduleResult> ScheduleSettlementAsync(Guid contractId, SettlementScheduleRequest request);
    Task<List<SettlementSchedule>> GetScheduledSettlementsAsync(DateTime? fromDate = null);
    
    // Reconciliation
    Task<ReconciliationResult> ReconcileSettlementAsync(Guid settlementId);
    Task<List<ReconciliationIssue>> GetReconciliationIssuesAsync();
    Task<ReconciliationSummary> GetReconciliationSummaryAsync(DateTime date);
    
    // Payment processing
    Task<PaymentResult> InitiatePaymentAsync(Guid settlementId, PaymentRequest request);
    Task<PaymentResult> ConfirmPaymentReceiptAsync(Guid settlementId, PaymentConfirmationRequest request);
    Task<List<DTOs.Payment>> GetPaymentHistoryAsync(Guid? contractId = null, Guid? settlementId = null);
    
    // Settlement matching
    Task<SettlementMatchingResult> MatchSettlementsAsync(SettlementMatchingRequest request);
    Task<List<SettlementMatchingRecommendation>> GetMatchingRecommendationsAsync();
    
    // Reports and analytics
    Task<SettlementAnalytics> GetSettlementAnalyticsAsync(DateTime startDate, DateTime endDate);
    Task<CashFlowForecast> GenerateCashFlowForecastAsync(int forecastDays);
    Task<List<SettlementAlert>> GetSettlementAlertsAsync();
}


public class SettlementScheduleRequest
{
    public SettlementFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? NumberOfPayments { get; set; }
    public Money PaymentAmount { get; set; } = null!;
    public bool EnableAutomaticProcessing { get; set; } = false;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class PaymentRequest
{
    public PaymentMethod Method { get; set; }
    public Money Amount { get; set; } = null!;
    public string PaymentReference { get; set; } = string.Empty;
    public DtoBankAccount? PayerAccount { get; set; }
    public DtoBankAccount? PayeeAccount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Instructions { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PaymentConfirmationRequest
{
    public string PaymentReference { get; set; } = string.Empty;
    public Money ConfirmedAmount { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? BankReference { get; set; }
    public CorePaymentStatus Status { get; set; }
    public string? Comments { get; set; }
}

public class SettlementMatchingRequest
{
    public List<Guid> SettlementIds { get; set; } = new();
    public SettlementMatchingCriteria Criteria { get; set; } = new();
    public bool AutoApplyMatches { get; set; } = false;
}

public class Settlement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public CoreSettlementType Type { get; set; }
    public Money Amount { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public CoreSettlementStatus Status { get; set; } = CoreSettlementStatus.Pending;
    public Guid PayerPartyId { get; set; }
    public Guid PayeePartyId { get; set; }
    public SettlementTerms Terms { get; set; } = new();
    public string? Description { get; set; }
    public List<DtoPayment> Payments { get; set; } = new();
    public List<SettlementAdjustment> Adjustments { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime? ProcessedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class SettlementTerms
{
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public SettlementMethod Method { get; set; } = SettlementMethod.TelegraphicTransfer;
    public string Currency { get; set; } = "USD";
    public decimal? DiscountRate { get; set; }
    public int? EarlyPaymentDays { get; set; }
    public decimal? LateFeeRate { get; set; }
    public bool EnableAutomaticProcessing { get; set; } = false;
    public Dictionary<string, object> CustomTerms { get; set; } = new();
}

public class SettlementAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SettlementAdjustmentType Type { get; set; }
    public Money Amount { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public Guid AdjustedBy { get; set; }
    public string? Reference { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}




public class SettlementSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractId { get; set; }
    public SettlementFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public Money PaymentAmount { get; set; } = null!;
    public int PaymentNumber { get; set; }
    public int? TotalPayments { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EnableAutomaticProcessing { get; set; }
    public List<Guid> GeneratedSettlements { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class SettlementResult
{
    public bool IsSuccessful { get; set; }
    public Guid? SettlementId { get; set; }
    public Guid? Id => SettlementId; // Alias for compatibility
    public string? SettlementNumber { get; set; }
    public CoreSettlementType? Type { get; set; }
    public DateTime? DueDate { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SettlementSummary
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSettlements { get; set; }
    public Money TotalAmount { get; set; } = null!;
    public Dictionary<CoreSettlementStatus, int> SettlementsByStatus { get; set; } = new();
    public Dictionary<string, Money> AmountsByCurrency { get; set; } = new();
    public Dictionary<CoreSettlementType, int> SettlementsByType { get; set; } = new();
    public List<SettlementTrend> Trends { get; set; } = new();
}

public class SettlementTrend
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public Money Amount { get; set; } = null!;
}

public class SettlementScheduleResult
{
    public bool IsSuccessful { get; set; }
    public Guid? ScheduleId { get; set; }
    public List<DateTime> GeneratedDueDates { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ReconciliationResult
{
    public bool IsReconciled { get; set; }
    public List<ReconciliationIssue> Issues { get; set; } = new();
    public Money ReconciledAmount { get; set; } = null!;
    public Money VarianceAmount { get; set; } = null!;
    public DateTime ReconciliationDate { get; set; } = DateTime.UtcNow;
    public string? Comments { get; set; }
}

public class ReconciliationIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SettlementId { get; set; }
    public ReconciliationIssueType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Money? AmountDifference { get; set; }
    public ReconciliationSeverity Severity { get; set; }
    public bool IsResolved { get; set; }
    public DateTime DetectedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedDate { get; set; }
    public string? Resolution { get; set; }
}

public class ReconciliationSummary
{
    public DateTime Date { get; set; }
    public int TotalSettlements { get; set; }
    public int ReconciledSettlements { get; set; }
    public int UnreconciledSettlements { get; set; }
    public Money TotalVariance { get; set; } = null!;
    public List<ReconciliationIssue> OutstandingIssues { get; set; } = new();
    public double ReconciliationRate => TotalSettlements > 0 ? (double)ReconciledSettlements / TotalSettlements * 100 : 0;
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
    public List<Guid> SettlementIds { get; set; } = new();
    public Money MatchedAmount { get; set; } = null!;
    public SettlementMatchType MatchType { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class SettlementMatchingRecommendation
{
    public List<Guid> SettlementIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public SettlementMatchType MatchType { get; set; }
    public Dictionary<string, object> MatchingCriteria { get; set; } = new();
}

public class SettlementMatchingCriteria
{
    public bool MatchByAmount { get; set; } = true;
    public decimal AmountTolerancePercentage { get; set; } = 0.01m; // 1%
    public bool MatchByDate { get; set; } = true;
    public int DateToleranceDays { get; set; } = 3;
    public bool MatchByReference { get; set; } = true;
    public bool MatchByCounterparty { get; set; } = true;
    public List<string> CustomCriteria { get; set; } = new();
}

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
    public Dictionary<CoreSettlementType, int> VolumeByType { get; set; } = new();
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

public class CashFlowForecast
{
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
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

public class SettlementAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SettlementAlertType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public SettlementAlertSeverity Severity { get; set; }
    public Guid? SettlementId { get; set; }
    public DateTime TriggeredDate { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}



public enum SettlementFrequency
{
    OneTime,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually
}

public enum PaymentMethod
{
    TelegraphicTransfer,
    SWIFT,
    ACH,
    Check,
    LetterOfCredit,
    DocumentaryCollection,
    CreditCard,
    Digital
}

public enum PaymentTerms
{
    Prepaid,
    COD,
    Net15,
    Net30,
    Net45,
    Net60,
    Net90,
    Custom
}

public enum SettlementMethod
{
    TelegraphicTransfer,
    LetterOfCredit,
    DocumentaryCollection,
    Cash,
    Netting
}


public enum SettlementAdjustmentType
{
    QuantityAdjustment,
    PriceAdjustment,
    QualityDiscount,
    LateFee,
    EarlyPaymentDiscount,
    TaxAdjustment,
    Other
}

public enum ReconciliationIssueType
{
    AmountMismatch,
    DateMismatch,
    MissingPayment,
    DuplicatePayment,
    CurrencyMismatch,
    ReferenceIncorrect
}

public enum ReconciliationSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum SettlementMatchType
{
    Exact,
    Partial,
    Split,
    Consolidated
}

public enum SettlementAlertType
{
    PaymentOverdue,
    PaymentFailed,
    ReconciliationIssue,
    CashFlowRisk,
    LargeCashMovement,
    ComplianceViolation
}

public enum SettlementAlertSeverity
{
    Info,
    Warning,
    High,
    Critical
}