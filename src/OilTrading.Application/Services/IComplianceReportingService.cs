using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public interface IComplianceReportingService
{
    // Report generation
    Task<ComplianceReportResult> GenerateReportAsync(ComplianceReportRequest request);
    Task<List<ComplianceReport>> GetAvailableReportsAsync();
    Task<ComplianceReport?> GetReportAsync(Guid reportId);
    Task<byte[]> ExportReportAsync(Guid reportId, ComplianceExportFormat format);
    
    // Scheduled reporting
    Task<ComplianceScheduleResult> ScheduleReportAsync(ComplianceReportSchedule schedule);
    Task<List<ComplianceReportSchedule>> GetScheduledReportsAsync();
    Task<bool> UpdateScheduleAsync(Guid scheduleId, ComplianceReportSchedule schedule);
    Task<bool> DeleteScheduleAsync(Guid scheduleId);
    
    // Compliance monitoring
    Task<List<ComplianceViolation>> GetComplianceViolationsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<ComplianceStatus> GetComplianceStatusAsync();
    Task<ComplianceDashboardData> GetComplianceDashboardAsync();
    
    // Regulatory reporting
    Task<RegulatoryReportResult> GenerateRegulatoryReportAsync(RegulatoryReportRequest request);
    Task<List<RegulatoryReportTemplate>> GetRegulatoryTemplatesAsync();
    Task<bool> SubmitRegulatoryReportAsync(Guid reportId, RegulatorySubmissionRequest request);
    
    // Audit support
    Task<AuditReportResult> GenerateAuditReportAsync(AuditReportRequest request);
    Task<AuditTrail> GetAuditTrailAsync(DateTime startDate, DateTime endDate, string? entityType = null);
    Task<List<AuditFinding>> GetAuditFindingsAsync();
    
    // Compliance rules management
    Task<ComplianceRuleResult> CreateComplianceRuleAsync(ComplianceRuleRequest request);
    Task<List<ComplianceRule>> GetComplianceRulesAsync();
    Task<bool> UpdateComplianceRuleAsync(Guid ruleId, ComplianceRuleRequest request);
    Task<bool> DeleteComplianceRuleAsync(Guid ruleId);
    
    // Compliance checking
    Task<ComplianceCheckResult> RunComplianceCheckAsync(ComplianceCheckRequest request);
    Task<List<ComplianceCheckResult>> RunAllComplianceChecksAsync();
    Task<ComplianceMetrics> GetComplianceMetricsAsync(DateTime startDate, DateTime endDate);
    
    // Document management
    Task<DocumentResult> UploadComplianceDocumentAsync(ComplianceDocumentRequest request);
    Task<List<ComplianceDocument>> GetComplianceDocumentsAsync(string? category = null);
    Task<byte[]?> DownloadDocumentAsync(Guid documentId);
    
    // Notification and alerting
    Task<bool> ConfigureComplianceAlertsAsync(ComplianceAlertConfiguration config);
    Task<List<ComplianceAlert>> GetComplianceAlertsAsync();
    Task<bool> AcknowledgeComplianceAlertAsync(Guid alertId, string acknowledgedBy);
}

public class ComplianceReportRequest
{
    public ComplianceReportType ReportType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public List<string> IncludeDataTypes { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ComplianceReportFormat Format { get; set; } = ComplianceReportFormat.PDF;
    public string? RequestedBy { get; set; }
    public bool IncludeConfidentialData { get; set; } = false;
}

public class ComplianceReportSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ComplianceReportType ReportType { get; set; }
    public ComplianceScheduleFrequency Frequency { get; set; }
    public DateTime NextRunDate { get; set; }
    public ComplianceReportFormat Format { get; set; } = ComplianceReportFormat.PDF;
    public List<string> Recipients { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

public class RegulatoryReportRequest
{
    public RegulatoryAuthority Authority { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public DateTime ReportingPeriodStart { get; set; }
    public DateTime ReportingPeriodEnd { get; set; }
    public Dictionary<string, object> ReportData { get; set; } = new();
    public bool RequiresDigitalSignature { get; set; } = false;
    public string? SubmissionDeadline { get; set; }
}

public class AuditReportRequest
{
    public AuditScope Scope { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> AuditAreas { get; set; } = new();
    public AuditReportType Type { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public bool IncludeRecommendations { get; set; } = true;
}

public class ComplianceRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceRuleType RuleType { get; set; }
    public string RuleExpression { get; set; } = string.Empty;
    public ComplianceRuleSeverity Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> ApplicableEntities { get; set; } = new();
    public Dictionary<string, object> RuleParameters { get; set; } = new();
}

public class ComplianceCheckRequest
{
    public List<Guid>? SpecificRuleIds { get; set; }
    public List<string>? EntityTypes { get; set; }
    public DateTime? CheckDate { get; set; }
    public bool IncludeInactiveRules { get; set; } = false;
}

public class ComplianceDocumentRequest
{
    public string DocumentName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public byte[] DocumentContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public ComplianceDocumentType DocumentType { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class RegulatorySubmissionRequest
{
    public RegulatoryAuthority Authority { get; set; }
    public string SubmissionMethod { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string SubmissionNotes { get; set; } = string.Empty;
    public bool RequiresAcknowledgment { get; set; } = true;
}

public class ComplianceReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReportName { get; set; } = string.Empty;
    public ComplianceReportType ReportType { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ComplianceReportStatus Status { get; set; } = ComplianceReportStatus.Generated;
    public string GeneratedBy { get; set; } = string.Empty;
    public ComplianceReportFormat Format { get; set; }
    public byte[]? ReportContent { get; set; }
    public ComplianceReportSummary Summary { get; set; } = new();
    public List<ComplianceViolation> Violations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ComplianceReportSummary
{
    public int TotalEntitiesChecked { get; set; }
    public int CompliantEntities { get; set; }
    public int NonCompliantEntities { get; set; }
    public int TotalViolations { get; set; }
    public int CriticalViolations { get; set; }
    public decimal ComplianceScore { get; set; }
    public List<string> KeyFindings { get; set; } = new();
}

public class ComplianceViolation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceViolationSeverity Severity { get; set; }
    public DateTime DetectedDate { get; set; } = DateTime.UtcNow;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public ComplianceViolationStatus Status { get; set; } = ComplianceViolationStatus.Open;
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ResolvedBy { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class ComplianceStatus
{
    public DateTime AsOfDate { get; set; } = DateTime.UtcNow;
    public decimal OverallComplianceScore { get; set; }
    public int TotalActiveRules { get; set; }
    public int OpenViolations { get; set; }
    public int CriticalViolations { get; set; }
    public Dictionary<string, decimal> ComplianceByArea { get; set; } = new();
    public List<ComplianceViolation> RecentViolations { get; set; } = new();
    public List<string> RequiredActions { get; set; } = new();
}

public class ComplianceDashboardData
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public ComplianceStatus Status { get; set; } = new();
    public List<ComplianceAlert> RecentAlerts { get; set; } = new();
    public List<ComplianceReport> RecentReports { get; set; } = new();
    public ComplianceTrendData Trends { get; set; } = new();
    public Dictionary<string, int> ViolationsByType { get; set; } = new();
    public List<UpcomingDeadline> UpcomingDeadlines { get; set; } = new();
}

public class ComplianceTrendData
{
    public List<ComplianceTrendPoint> ComplianceScoreTrend { get; set; } = new();
    public List<ComplianceTrendPoint> ViolationTrend { get; set; } = new();
    public TimeSpan TrendPeriod { get; set; } = TimeSpan.FromDays(30);
}

public class ComplianceTrendPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

public class UpcomingDeadline
{
    public string DeadlineType { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public ComplianceUrgency Urgency { get; set; }
}

public class RegulatoryReportTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RegulatoryAuthority Authority { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceScheduleFrequency Frequency { get; set; }
    public List<ReportField> RequiredFields { get; set; } = new();
    public string TemplateVersion { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReportField
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? ValidationRule { get; set; }
    public string? Description { get; set; }
}

public class AuditTrail
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<AuditEntry> Entries { get; set; } = new();
    public int TotalEntries { get; set; }
    public Dictionary<string, int> EntriesByType { get; set; } = new();
}

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Changes { get; set; } = new();
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class AuditFinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FindingType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuditFindingSeverity Severity { get; set; }
    public DateTime IdentifiedDate { get; set; } = DateTime.UtcNow;
    public string IdentifiedBy { get; set; } = string.Empty;
    public AuditFindingStatus Status { get; set; } = AuditFindingStatus.Open;
    public string? RecommendedAction { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public string? AssignedTo { get; set; }
}

public class ComplianceRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceRuleType RuleType { get; set; }
    public string RuleExpression { get; set; } = string.Empty;
    public ComplianceRuleSeverity Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> ApplicableEntities { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> RuleParameters { get; set; } = new();
    public ComplianceRuleStats Stats { get; set; } = new();
}

public class ComplianceRuleStats
{
    public int TotalChecks { get; set; }
    public int ViolationsDetected { get; set; }
    public DateTime? LastChecked { get; set; }
    public decimal ViolationRate { get; set; }
}

public class ComplianceCheckResult
{
    public Guid CheckId { get; set; } = Guid.NewGuid();
    public DateTime CheckDate { get; set; } = DateTime.UtcNow;
    public bool IsCompliant { get; set; }
    public List<ComplianceViolation> ViolationsFound { get; set; } = new();
    public int RulesChecked { get; set; }
    public int EntitiesChecked { get; set; }
    public TimeSpan CheckDuration { get; set; }
    public Dictionary<string, object> CheckMetadata { get; set; } = new();
}

public class ComplianceMetrics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AverageComplianceScore { get; set; }
    public int TotalViolations { get; set; }
    public int ResolvedViolations { get; set; }
    public Dictionary<ComplianceViolationSeverity, int> ViolationsBySeverity { get; set; } = new();
    public Dictionary<string, int> ViolationsByType { get; set; } = new();
    public TimeSpan AverageResolutionTime { get; set; }
    public List<ComplianceMetricTrend> Trends { get; set; } = new();
}

public class ComplianceMetricTrend
{
    public DateTime Date { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class ComplianceDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DocumentName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ComplianceDocumentType DocumentType { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public ComplianceDocumentStatus Status { get; set; } = ComplianceDocumentStatus.Active;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? FilePath { get; set; }
}

public class ComplianceAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ComplianceAlertType AlertType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceAlertSeverity Severity { get; set; }
    public DateTime TriggeredDate { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
    public Dictionary<string, object> AlertData { get; set; } = new();
}

public class ComplianceAlertConfiguration
{
    public List<ComplianceAlertRule> AlertRules { get; set; } = new();
    public List<string> NotificationRecipients { get; set; } = new();
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnableInAppNotifications { get; set; } = true;
    public TimeSpan AlertRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
}

public class ComplianceAlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RuleName { get; set; } = string.Empty;
    public ComplianceAlertType AlertType { get; set; }
    public string TriggerCondition { get; set; } = string.Empty;
    public ComplianceAlertSeverity Severity { get; set; }
    public bool IsActive { get; set; } = true;
}

// Result classes
public class ComplianceReportResult
{
    public bool IsSuccessful { get; set; }
    public Guid? ReportId { get; set; }
    public string? ErrorMessage { get; set; }
    public ComplianceReport? Report { get; set; }
}

public class ComplianceScheduleResult
{
    public bool IsSuccessful { get; set; }
    public Guid? ScheduleId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? NextRunDate { get; set; }
}

public class RegulatoryReportResult
{
    public bool IsSuccessful { get; set; }
    public Guid? ReportId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SubmissionConfirmation { get; set; }
    public Dictionary<string, object> ValidationResults { get; set; } = new();
}

public class AuditReportResult
{
    public bool IsSuccessful { get; set; }
    public Guid? ReportId { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? ReportContent { get; set; }
    public AuditReportSummary? Summary { get; set; }
}

public class AuditReportSummary
{
    public int TotalAuditEntries { get; set; }
    public int FindingsCount { get; set; }
    public int CriticalFindings { get; set; }
    public List<string> KeyRecommendations { get; set; } = new();
}

public class ComplianceRuleResult
{
    public bool IsSuccessful { get; set; }
    public Guid? RuleId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public class DocumentResult
{
    public bool IsSuccessful { get; set; }
    public Guid? DocumentId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
}

// Enums
public enum ComplianceReportType
{
    RiskCompliance,
    RegulatoryCompliance,
    AuditReport,
    ViolationSummary,
    ComplianceMetrics,
    TradeReporting,
    PositionReporting,
    TransactionReporting
}

public enum ComplianceReportFormat
{
    PDF,
    Excel,
    CSV,
    XML,
    JSON
}

public enum ComplianceExportFormat
{
    PDF,
    Excel,
    Word,
    CSV
}

public enum ComplianceScheduleFrequency
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually,
    OnDemand
}

public enum ComplianceReportStatus
{
    Generated,
    InReview,
    Approved,
    Submitted,
    Archived
}

public enum RegulatoryAuthority
{
    CFTC,
    SEC,
    FCA,
    ESMA,
    MAS,
    FSA,
    Other
}

public enum AuditScope
{
    FullAudit,
    RiskManagement,
    TradingOperations,
    Compliance,
    FinancialReporting,
    OperationalRisk
}

public enum AuditReportType
{
    Internal,
    External,
    Regulatory,
    SelfAssessment
}

public enum ComplianceRuleType
{
    RiskLimit,
    PositionLimit,
    TradeValidation,
    DocumentationRequirement,
    ReportingRequirement,
    Custom
}

public enum ComplianceRuleSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ComplianceViolationSeverity
{
    Minor,
    Moderate,
    Major,
    Critical
}

public enum ComplianceViolationStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
    Waived
}

public enum ComplianceDocumentType
{
    Policy,
    Procedure,
    Certificate,
    License,
    Agreement,
    Report,
    Evidence,
    Other
}

public enum ComplianceDocumentStatus
{
    Active,
    Expired,
    Pending,
    Archived
}

public enum ComplianceAlertType
{
    ViolationDetected,
    DeadlineApproaching,
    DocumentExpiring,
    RuleChanged,
    SystemError,
    SubmissionRequired
}

public enum ComplianceAlertSeverity
{
    Info,
    Warning,
    High,
    Critical
}

public enum ComplianceUrgency
{
    Low,
    Medium,
    High,
    Urgent
}

public enum AuditFindingSeverity
{
    Observation,
    Minor,
    Moderate,
    Significant,
    Critical
}

public enum AuditFindingStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}