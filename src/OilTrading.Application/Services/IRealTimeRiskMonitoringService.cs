using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public interface IRealTimeRiskMonitoringService
{
    // Real-time risk monitoring
    Task<RealTimeRiskSnapshot> GetRealTimeRiskSnapshotAsync();
    Task<List<RiskAlert>> GetActiveRiskAlertsAsync();
    Task<RiskMonitoringStatus> GetMonitoringStatusAsync();
    
    // VaR monitoring
    Task<RealTimeVaRMetrics> GetRealTimeVaRAsync();
    Task<VaRBreachAlert?> CheckVaRBreachAsync();
    Task<List<VaRTrend>> GetVaRTrendsAsync(int hours = 24);
    
    // Risk limit monitoring
    Task<List<RiskLimitStatus>> GetRiskLimitStatusesAsync();
    Task<RiskLimitBreachResult> CheckRiskLimitsAsync();
    Task<List<RiskLimitAlert>> GetRiskLimitAlertsAsync();
    
    // Position risk monitoring
    Task<List<PositionRisk>> GetPositionRisksAsync();
    Task<ConcentrationRiskMetrics> GetConcentrationRiskAsync();
    Task<CounterpartyRiskSummary> GetCounterpartyRiskAsync();
    
    // Real-time alerting
    Task<AlertResult> CreateRiskAlertAsync(RiskAlertRequest request);
    Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy);
    Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string resolution);
    
    // Risk dashboard data
    Task<RiskDashboardData> GetRiskDashboardDataAsync();
    Task<List<RiskMetricTimeSeries>> GetRiskTimeSeriesAsync(string metricName, TimeSpan period);
    
    // Risk monitoring configuration
    Task ConfigureRiskThresholdsAsync(RiskThresholdConfiguration config);
    Task<List<RiskThreshold>> GetRiskThresholdsAsync();
    Task EnableRiskMonitoringAsync(string riskType);
    Task DisableRiskMonitoringAsync(string riskType);
    
    // Stress testing integration
    Task<StressTestResult> RunRealTimeStressTestAsync(StressTestScenario scenario);
    Task<List<StressTestAlert>> GetStressTestAlertsAsync();
    
    // Risk reporting
    Task<RiskReport> GenerateRealTimeRiskReportAsync();
    Task<byte[]> ExportRiskDataAsync(RiskExportRequest request);
    
    // Additional methods needed by RiskCheckAttribute
    Task<SystemRiskStatus> GetSystemRiskStatusAsync();
    Task<RealTimeVaRMetrics> GetRealTimeRiskAsync();
    Task<List<StressTestResult>> RunRealTimeStressTestAsync();
    Task<MonteCarloResult> RunMonteCarloSimulationAsync(int iterations);
    Task<decimal> CalculateCorrelationRiskAsync();
    Task TriggerRiskAlertAsync(RiskAlert alert);
    
    // Additional methods needed by RiskCheckMiddleware
    Task<OperationRiskCheckResult> CheckOperationRiskAsync(OperationDetails details);
}

public class MonteCarloResult
{
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal ExpectedShortfall95 { get; set; }
    public decimal ExpectedShortfall99 { get; set; }
    public decimal WorstCaseLoss { get; set; }
    public decimal BestCaseGain { get; set; }
    public int Iterations { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public enum SystemRiskStatus
{
    Normal = 1,
    Elevated = 2,
    High = 3,
    Emergency = 4
}

public class OperationRiskCheckResult
{
    public bool PassesAllChecks { get; set; }
    public decimal OverallRiskScore { get; set; }
    public List<string> Violations { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class OperationDetails
{
    public string OperationType { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class RealTimeRiskSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public RealTimeVaRMetrics VaR { get; set; } = new();
    public ConcentrationRiskMetrics Concentration { get; set; } = new();
    public CounterpartyRiskSummary Counterparty { get; set; } = new();
    public LiquidityRiskMetrics Liquidity { get; set; } = new();
    public OperationalRiskMetrics Operational { get; set; } = new();
    public int ActiveAlertsCount { get; set; }
    public RiskHealthStatus OverallStatus { get; set; } = RiskHealthStatus.Normal;
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
}

public class RealTimeVaRMetrics
{
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal ExpectedShortfall95 { get; set; }
    public decimal ExpectedShortfall99 { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal DailyPnL { get; set; }
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    public VaRModelType ModelUsed { get; set; }
    public Dictionary<string, decimal> ComponentVaR { get; set; } = new();
    public bool IsBreaching { get; set; }
    public decimal BreachAmount { get; set; }
}

public class VaRTrend
{
    public DateTime Timestamp { get; set; }
    public decimal VaR95 { get; set; }
    public decimal VaR99 { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal PnL { get; set; }
}

public class VaRBreachAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime BreachTime { get; set; } = DateTime.UtcNow;
    public decimal VaRLimit { get; set; }
    public decimal ActualLoss { get; set; }
    public decimal BreachAmount { get; set; }
    public VaRConfidenceLevel ConfidenceLevel { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedTime { get; set; }
}

public class RiskLimitStatus
{
    public Guid LimitId { get; set; }
    public string LimitName { get; set; } = string.Empty;
    public RiskLimitType LimitType { get; set; }
    public decimal LimitValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public RiskLimitSeverity Status { get; set; }
    public bool IsBreaching { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? ProductType { get; set; }
    public Guid? TraderId { get; set; }
    public Guid? CounterpartyId { get; set; }
}

public class RiskLimitBreachResult
{
    public bool HasBreaches { get; set; }
    public List<RiskLimitBreach> Breaches { get; set; } = new();
    public int TotalBreaches { get; set; }
    public int CriticalBreaches { get; set; }
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;
}

public class RiskLimitBreach
{
    public Guid LimitId { get; set; }
    public string LimitName { get; set; } = string.Empty;
    public decimal LimitValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal ExcessAmount { get; set; }
    public RiskLimitSeverity Severity { get; set; }
    public DateTime BreachTime { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
}

public class RiskLimitAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LimitId { get; set; }
    public string LimitName { get; set; } = string.Empty;
    public RiskAlertType AlertType { get; set; }
    public RiskAlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public bool IsResolved { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PositionRisk
{
    public Guid PositionId { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public Quantity Position { get; set; } = null!;
    public Money MarketValue { get; set; } = null!;
    public decimal DeltaEquivalent { get; set; }
    public decimal VaRContribution { get; set; }
    public decimal Beta { get; set; }
    public RiskRating RiskRating { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ConcentrationRiskMetrics
{
    public decimal HerfindahlIndex { get; set; }
    public decimal TopConcentrationPercentage { get; set; }
    public int NumberOfPositions { get; set; }
    public List<ConcentrationBreakdown> ConcentrationByProduct { get; set; } = new();
    public List<ConcentrationBreakdown> ConcentrationByCounterparty { get; set; } = new();
    public List<ConcentrationBreakdown> ConcentrationByTrader { get; set; } = new();
    public ConcentrationRiskLevel RiskLevel { get; set; }
}

public class ConcentrationBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public Money Value { get; set; } = null!;
    public int Count { get; set; }
}

public class CounterpartyRiskSummary
{
    public int TotalCounterparties { get; set; }
    public Money TotalExposure { get; set; } = null!;
    public decimal AverageRating { get; set; }
    public List<CounterpartyRiskDetail> TopRisks { get; set; } = new();
    public int CounterpartiesAboveThreshold { get; set; }
    public Money PotentialLoss { get; set; } = null!;
}

public class CounterpartyRiskDetail
{
    public Guid CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = string.Empty;
    public Money Exposure { get; set; } = null!;
    public decimal CreditRating { get; set; }
    public decimal ProbabilityOfDefault { get; set; }
    public Money PotentialLoss { get; set; } = null!;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class LiquidityRiskMetrics
{
    public decimal LiquidityRatio { get; set; }
    public TimeSpan AverageLiquidationTime { get; set; }
    public Money IlliquidAssets { get; set; } = null!;
    public decimal LiquidityBuffer { get; set; }
    public LiquidityRiskLevel RiskLevel { get; set; }
}

public class OperationalRiskMetrics
{
    public int ActiveIncidents { get; set; }
    public int SystemDowntime { get; set; } // minutes
    public decimal ProcessingErrorRate { get; set; }
    public Money PotentialOperationalLoss { get; set; } = null!;
    public OperationalRiskLevel RiskLevel { get; set; }
}

public class RiskAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RiskAlertType Type { get; set; }
    public RiskAlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public bool IsResolved { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}

public class RiskAlertRequest
{
    public RiskAlertType Type { get; set; }
    public RiskAlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
    public bool RequireAcknowledgment { get; set; } = true;
}

public class AlertResult
{
    public bool IsSuccessful { get; set; }
    public Guid? AlertId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> NotificationsSent { get; set; } = new();
}

public class RiskMonitoringStatus
{
    public bool IsActive { get; set; }
    public DateTime LastUpdate { get; set; }
    public Dictionary<string, bool> MonitoringModules { get; set; } = new();
    public List<string> ActiveMonitors { get; set; } = new();
    public List<string> FailedMonitors { get; set; } = new();
    public RiskSystemHealth SystemHealth { get; set; } = new();
}

public class RiskSystemHealth
{
    public bool IsHealthy { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, string> ComponentStatus { get; set; } = new();
}

public class RiskDashboardData
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public RealTimeRiskSnapshot RiskSnapshot { get; set; } = new();
    public List<RiskAlert> RecentAlerts { get; set; } = new();
    public List<RiskLimitStatus> LimitStatuses { get; set; } = new();
    public List<RiskTrendPoint> Trends { get; set; } = new();
    public Dictionary<string, decimal> KeyMetrics { get; set; } = new();
}

public class RiskTrendPoint
{
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class RiskMetricTimeSeries
{
    public string MetricName { get; set; } = string.Empty;
    public List<TimeSeriesPoint> DataPoints { get; set; } = new();
    public TimeSpan Period { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class TimeSeriesPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class RiskThresholdConfiguration
{
    public List<RiskThreshold> Thresholds { get; set; } = new();
    public bool EnableAutomaticAdjustment { get; set; }
    public TimeSpan ReviewPeriod { get; set; } = TimeSpan.FromDays(30);
}

public class RiskThreshold
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public RiskMetricType MetricType { get; set; }
    public decimal WarningThreshold { get; set; }
    public decimal CriticalThreshold { get; set; }
    public string? ApplicableProduct { get; set; }
    public Guid? ApplicableTrader { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
}

public class StressTestResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public Money PotentialLoss { get; set; } = null!;
    public decimal PortfolioImpactPercentage { get; set; }
    public List<StressTestComponentResult> ComponentResults { get; set; } = new();
    public bool ExceedsRiskTolerance { get; set; }
    public StressTestSeverity Severity { get; set; }
    public decimal WorstCaseLoss { get; set; }
}

public class StressTestComponentResult
{
    public string Component { get; set; } = string.Empty;
    public Money Impact { get; set; } = null!;
    public decimal ImpactPercentage { get; set; }
}

public class StressTestAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ScenarioName { get; set; } = string.Empty;
    public Money PotentialLoss { get; set; } = null!;
    public decimal ImpactPercentage { get; set; }
    public StressTestSeverity Severity { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
}

public class StressTestScenario
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, decimal> PriceShocks { get; set; } = new();
    public Dictionary<string, decimal> VolumeShocks { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class RiskReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public RealTimeRiskSnapshot Summary { get; set; } = new();
    public List<RiskAlert> Alerts { get; set; } = new();
    public List<RiskLimitStatus> LimitStatuses { get; set; } = new();
    public byte[]? DetailedAnalysis { get; set; }
}

public class RiskExportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> MetricNames { get; set; } = new();
    public RiskExportFormat Format { get; set; } = RiskExportFormat.CSV;
    public bool IncludeAlerts { get; set; } = true;
    public bool IncludeLimitStatuses { get; set; } = true;
}

// Enums
public enum RiskHealthStatus
{
    Normal,
    Warning,
    Critical,
    Unknown
}

public enum VaRModelType
{
    HistoricalSimulation,
    MonteCarlo,
    GARCH,
    Hybrid
}

public enum VaRConfidenceLevel
{
    Confidence95 = 95,
    Confidence99 = 99
}

public enum RiskLimitType
{
    VaR,
    NotionalExposure,
    DeltaEquivalent,
    CounterpartyExposure,
    ConcentrationLimit,
    StopLoss
}

public enum RiskLimitSeverity
{
    Normal,
    Warning,
    Critical,
    Breach
}

public enum RiskAlertType
{
    VaRBreach,
    LimitExceeded,
    ConcentrationRisk,
    CounterpartyRisk,
    LiquidityRisk,
    OperationalRisk,
    SystemError,
    StressTestFailure
}

public enum RiskAlertSeverity
{
    Info,
    Warning,
    High,
    Critical
}

public enum RiskRating
{
    Low,
    Medium,
    High,
    VeryHigh
}

public enum ConcentrationRiskLevel
{
    Low,
    Medium,
    High,
    Excessive
}

public enum LiquidityRiskLevel
{
    Liquid,
    ModeratelyLiquid,
    Illiquid,
    VeryIlliquid
}

public enum OperationalRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RiskMetricType
{
    VaR95,
    VaR99,
    ExpectedShortfall,
    NotionalExposure,
    DeltaEquivalent,
    ConcentrationIndex,
    CounterpartyExposure
}

public enum StressTestSeverity
{
    Low,
    Medium,
    High,
    Extreme
}

public enum RiskExportFormat
{
    CSV,
    Excel,
    JSON,
    PDF
}