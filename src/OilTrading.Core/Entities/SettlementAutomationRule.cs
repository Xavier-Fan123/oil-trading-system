using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// Settlement Automation Rule - Defines rules for automatic settlement creation based on conditions
/// Supports complex rule expressions with multiple conditions and orchestration strategies
///
/// Architecture: Aggregate root for settlement automation rules
/// - Rules can be created, enabled/disabled, tested, and executed
/// - Maintains audit trail of all rule modifications
/// - Supports versioning for rule evolution
/// - Provides execution history and analytics
/// </summary>
public class SettlementAutomationRule : BaseEntity
{
    private SettlementAutomationRule() { } // For EF Core

    public SettlementAutomationRule(
        string name,
        string description,
        SettlementRuleType ruleType,
        string priority = "Normal",
        string createdBy = "System")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        RuleType = ruleType;
        Priority = priority ?? "Normal";
        CreatedBy = createdBy;

        Status = RuleStatus.Active;
        IsEnabled = true;
        CreatedDate = DateTime.UtcNow;
        ExecutionCount = 0;
        SuccessCount = 0;
        FailureCount = 0;

        // Initialize collections
        Conditions = new List<SettlementRuleCondition>();
        Actions = new List<SettlementRuleAction>();
        ExecutionHistory = new List<RuleExecutionRecord>();
    }

    // Core attributes
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SettlementRuleType RuleType { get; private set; }

    // Priority determines execution order (Critical, High, Normal, Low)
    public string Priority { get; private set; } = "Normal";

    // Status and lifecycle
    public RuleStatus Status { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? LastModifiedDate { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public string? LastModifiedBy { get; private set; }

    // Versioning for rule evolution
    public int RuleVersion { get; private set; } = 1;

    // Execution scope
    /// <summary>
    /// Scope of rule application: All, PurchaseOnly, SalesOnly, ByPartner, ByProduct, ByQuantityRange
    /// </summary>
    public SettlementRuleScope Scope { get; private set; } = SettlementRuleScope.All;

    /// <summary>
    /// Scope filter value (e.g., partner name, product type)
    /// Only used if Scope requires specific filtering
    /// </summary>
    public string? ScopeFilter { get; private set; }

    // Trigger conditions
    /// <summary>
    /// When rule should be evaluated: OnContractCompletion, OnSettlementCreation, OnSchedule, OnManualTrigger
    /// </summary>
    public SettlementRuleTrigger Trigger { get; private set; } = SettlementRuleTrigger.OnContractCompletion;

    /// <summary>
    /// If trigger is OnSchedule, define the schedule (cron format)
    /// Example: "0 9 * * *" for daily at 9 AM
    /// </summary>
    public string? ScheduleExpression { get; private set; }

    // Orchestration settings
    /// <summary>
    /// How multiple matching settlements should be created: Sequential, Parallel, Grouped
    /// </summary>
    public SettlementOrchestrationStrategy OrchestrationStrategy { get; private set; } = SettlementOrchestrationStrategy.Sequential;

    /// <summary>
    /// Maximum number of settlements to create in single execution
    /// </summary>
    public int? MaxSettlementsPerExecution { get; private set; }

    /// <summary>
    /// For Grouped strategy: grouping dimension (ByPartner, ByProduct, ByMonth)
    /// </summary>
    public string? GroupingDimension { get; private set; }

    // Execution tracking
    public int ExecutionCount { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime? LastExecutedDate { get; private set; }
    public int? LastExecutionSettlementCount { get; private set; }
    public string? LastExecutionError { get; private set; }

    // Audit and metadata
    public string? Notes { get; private set; }
    public DateTime? DisabledDate { get; private set; }
    public string? DisabledReason { get; private set; }

    // Collections - Rule definition components
    public ICollection<SettlementRuleCondition> Conditions { get; private set; } = new List<SettlementRuleCondition>();
    public ICollection<SettlementRuleAction> Actions { get; private set; } = new List<SettlementRuleAction>();
    public ICollection<RuleExecutionRecord> ExecutionHistory { get; private set; } = new List<RuleExecutionRecord>();

    // Methods

    /// <summary>
    /// Enable the rule for execution
    /// </summary>
    public void Enable()
    {
        if (!IsEnabled)
        {
            IsEnabled = true;
            DisabledDate = null;
            DisabledReason = null;
            LastModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Disable the rule temporarily
    /// </summary>
    public void Disable(string? reason = null)
    {
        if (IsEnabled)
        {
            IsEnabled = false;
            DisabledDate = DateTime.UtcNow;
            DisabledReason = reason;
            LastModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Update rule basic information
    /// </summary>
    public void UpdateBasicInfo(string name, string description, string priority, string? notes, string modifiedBy)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Priority = priority ?? "Normal";
        Notes = notes;
        LastModifiedBy = modifiedBy;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Update rule trigger configuration
    /// </summary>
    public void UpdateTrigger(SettlementRuleTrigger trigger, string? scheduleExpression, string modifiedBy)
    {
        Trigger = trigger;
        ScheduleExpression = scheduleExpression;
        LastModifiedBy = modifiedBy;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Update execution scope
    /// </summary>
    public void UpdateScope(SettlementRuleScope scope, string? filter, string modifiedBy)
    {
        Scope = scope;
        ScopeFilter = filter;
        LastModifiedBy = modifiedBy;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Update orchestration settings
    /// </summary>
    public void UpdateOrchestration(
        SettlementOrchestrationStrategy strategy,
        int? maxSettlements,
        string? groupingDimension,
        string modifiedBy)
    {
        OrchestrationStrategy = strategy;
        MaxSettlementsPerExecution = maxSettlements;
        GroupingDimension = groupingDimension;
        LastModifiedBy = modifiedBy;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Record successful execution
    /// </summary>
    public void RecordSuccessfulExecution(int settlementCount)
    {
        ExecutionCount++;
        SuccessCount++;
        LastExecutedDate = DateTime.UtcNow;
        LastExecutionSettlementCount = settlementCount;
        LastExecutionError = null;
    }

    /// <summary>
    /// Record failed execution
    /// </summary>
    public void RecordFailedExecution(string errorMessage)
    {
        ExecutionCount++;
        FailureCount++;
        LastExecutedDate = DateTime.UtcNow;
        LastExecutionError = errorMessage;
        LastExecutionSettlementCount = null;
    }

    /// <summary>
    /// Increment rule version when conditions/actions change
    /// </summary>
    public void IncrementVersion(string modifiedBy)
    {
        RuleVersion++;
        LastModifiedBy = modifiedBy;
        LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Validate rule has sufficient configuration
    /// </summary>
    public bool IsValid()
    {
        // Must have at least one condition
        if (Conditions.Count == 0)
            return false;

        // Must have at least one action
        if (Actions.Count == 0)
            return false;

        // If OnSchedule trigger, must have schedule expression
        if (Trigger == SettlementRuleTrigger.OnSchedule && string.IsNullOrEmpty(ScheduleExpression))
            return false;

        return true;
    }
}

/// <summary>
/// Enumerates settlement rule types - determines what the rule creates
/// </summary>
public enum SettlementRuleType
{
    /// <summary>Auto-settlement creation on contract completion</summary>
    AutoSettlement = 1,

    /// <summary>Automatic settlement approval based on conditions</summary>
    AutoApproval = 2,

    /// <summary>Automatic settlement finalization</summary>
    AutoFinalization = 3,

    /// <summary>Charge auto-creation based on conditions</summary>
    ChargeCalculation = 4,

    /// <summary>Payment matching and reconciliation</summary>
    PaymentMatching = 5,

    /// <summary>Settlement consolidation/netting</summary>
    Consolidation = 6
}

/// <summary>
/// Enumerates rule lifecycle status
/// </summary>
public enum RuleStatus
{
    Draft = 1,
    Testing = 2,
    Active = 3,
    Deprecated = 4,
    Archived = 5
}

/// <summary>
/// Enumerates execution scope for rules
/// </summary>
public enum SettlementRuleScope
{
    /// <summary>Rule applies to all settlements</summary>
    All = 1,

    /// <summary>Rule applies only to purchase settlements</summary>
    PurchaseOnly = 2,

    /// <summary>Rule applies only to sales settlements</summary>
    SalesOnly = 3,

    /// <summary>Rule applies to specific trading partner (filter required)</summary>
    ByPartner = 4,

    /// <summary>Rule applies to specific product type (filter required)</summary>
    ByProduct = 5,

    /// <summary>Rule applies to specific quantity range (filter required)</summary>
    ByQuantityRange = 6
}

/// <summary>
/// Enumerates rule execution triggers
/// </summary>
public enum SettlementRuleTrigger
{
    /// <summary>Execute when contract marked as completed</summary>
    OnContractCompletion = 1,

    /// <summary>Execute when new settlement created</summary>
    OnSettlementCreation = 2,

    /// <summary>Execute on defined schedule (cron expression)</summary>
    OnSchedule = 3,

    /// <summary>Execute manually by user</summary>
    OnManualTrigger = 4
}

/// <summary>
/// Enumerates settlement orchestration strategies
/// </summary>
public enum SettlementOrchestrationStrategy
{
    /// <summary>Process settlements one by one</summary>
    Sequential = 1,

    /// <summary>Process settlements in parallel (async)</summary>
    Parallel = 2,

    /// <summary>Group settlements before processing</summary>
    Grouped = 3,

    /// <summary>Create single consolidated settlement</summary>
    Consolidated = 4
}

/// <summary>
/// Rule Condition - Defines when a rule should apply
/// Uses expression trees for flexible evaluation
/// </summary>
public class SettlementRuleCondition : BaseEntity
{
    private SettlementRuleCondition() { } // For EF Core

    public SettlementRuleCondition(
        Guid ruleId,
        string field,
        string operatorType,
        string value,
        int sequenceNumber)
    {
        RuleId = ruleId;
        Field = field ?? throw new ArgumentNullException(nameof(field));
        OperatorType = operatorType ?? throw new ArgumentNullException(nameof(operatorType));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        SequenceNumber = sequenceNumber;
        LogicalOperator = "AND"; // Default conjunction
    }

    public Guid RuleId { get; private set; }

    /// <summary>
    /// Field to evaluate (e.g., "SettlementAmount", "ContractType", "DaysOverdue")
    /// </summary>
    public string Field { get; private set; } = string.Empty;

    /// <summary>
    /// Operator type: Equals, NotEquals, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual,
    /// Contains, StartsWith, EndsWith, In, Between, IsNull, IsNotNull
    /// </summary>
    public string OperatorType { get; private set; } = string.Empty;

    /// <summary>
    /// Value to compare against (can be literal, formula, or reference)
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Order in which this condition appears in the rule
    /// </summary>
    public int SequenceNumber { get; private set; }

    /// <summary>
    /// Logical operator connecting this to next condition: AND, OR
    /// </summary>
    public string LogicalOperator { get; private set; } = "AND";

    /// <summary>
    /// Is this condition part of a grouped sub-expression (for complex logic)
    /// </summary>
    public string? GroupReference { get; private set; }

    // Navigation
    public SettlementAutomationRule Rule { get; private set; } = null!;
}

/// <summary>
/// Rule Action - Defines what happens when rule conditions are met
/// </summary>
public class SettlementRuleAction : BaseEntity
{
    private SettlementRuleAction() { } // For EF Core

    public SettlementRuleAction(
        Guid ruleId,
        string actionType,
        int sequenceNumber)
    {
        RuleId = ruleId;
        ActionType = actionType ?? throw new ArgumentNullException(nameof(actionType));
        SequenceNumber = sequenceNumber;
    }

    public Guid RuleId { get; private set; }

    /// <summary>
    /// Action to perform: CreateSettlement, ApproveSettlement, FinalizeSettlement,
    /// AddCharge, SendNotification, UpdateStatus, CreatePayment
    /// </summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>
    /// Order in which action executes
    /// </summary>
    public int SequenceNumber { get; private set; }

    /// <summary>
    /// Action configuration in JSON format (flexible parameters based on action type)
    /// Example for CreateSettlement: { "includeCharges": true, "autoApprove": false }
    /// Example for AddCharge: { "chargeType": "Demurrage", "amount": 5000 }
    /// </summary>
    public string? Parameters { get; private set; }

    /// <summary>
    /// Whether action should halt execution if it fails
    /// </summary>
    public bool StopOnFailure { get; private set; } = true;

    /// <summary>
    /// Notification template ID if action involves notification
    /// </summary>
    public string? NotificationTemplateId { get; private set; }

    // Navigation
    public SettlementAutomationRule Rule { get; private set; } = null!;
}

/// <summary>
/// Rule Execution Record - Audit trail of rule execution
/// </summary>
public class RuleExecutionRecord : BaseEntity
{
    private RuleExecutionRecord() { } // For EF Core

    public RuleExecutionRecord(Guid ruleId, string triggerSource)
    {
        RuleId = ruleId;
        TriggerSource = triggerSource ?? "Manual";
        ExecutionStartTime = DateTime.UtcNow;
        Status = ExecutionStatus.Running;
    }

    public Guid RuleId { get; private set; }

    /// <summary>
    /// Source that triggered execution: ContractCompletion, ScheduledJob, Manual, API
    /// </summary>
    public string TriggerSource { get; private set; } = string.Empty;

    /// <summary>
    /// When execution started
    /// </summary>
    public DateTime ExecutionStartTime { get; private set; }

    /// <summary>
    /// When execution completed
    /// </summary>
    public DateTime? ExecutionEndTime { get; private set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public int? ExecutionDurationMs { get; private set; }

    /// <summary>
    /// Overall execution status
    /// </summary>
    public ExecutionStatus Status { get; private set; }

    /// <summary>
    /// Number of settlements created/modified
    /// </summary>
    public int SettlementCount { get; private set; }

    /// <summary>
    /// Number of conditions evaluated
    /// </summary>
    public int ConditionsEvaluated { get; private set; }

    /// <summary>
    /// Number of actions executed
    /// </summary>
    public int ActionsExecuted { get; private set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Detailed execution log for debugging
    /// </summary>
    public string? DetailedLog { get; private set; }

    /// <summary>
    /// IDs of settlements created/modified during execution
    /// </summary>
    public List<Guid>? AffectedSettlementIds { get; private set; }

    // Methods

    public void Complete(int settlementCount, int conditionsEvaluated, int actionsExecuted)
    {
        ExecutionEndTime = DateTime.UtcNow;
        ExecutionDurationMs = (int)(ExecutionEndTime.Value - ExecutionStartTime).TotalMilliseconds;
        Status = ExecutionStatus.Completed;
        SettlementCount = settlementCount;
        ConditionsEvaluated = conditionsEvaluated;
        ActionsExecuted = actionsExecuted;
    }

    public void Failed(string errorMessage)
    {
        ExecutionEndTime = DateTime.UtcNow;
        ExecutionDurationMs = (int)(ExecutionEndTime.Value - ExecutionStartTime).TotalMilliseconds;
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void AddLog(string logEntry)
    {
        DetailedLog = (DetailedLog ?? string.Empty) + Environment.NewLine + logEntry;
    }
}

/// <summary>
/// Execution status for rule execution records
/// </summary>
public enum ExecutionStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4,
    Cancelled = 5
}
