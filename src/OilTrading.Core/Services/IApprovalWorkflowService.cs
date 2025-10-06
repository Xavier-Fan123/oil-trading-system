using OilTrading.Core.Entities;

namespace OilTrading.Core.Services;

public interface IApprovalWorkflowService
{
    Task<ApprovalWorkflowResult> StartWorkflowAsync(int orderId, string workflowType, string initiatedBy);
    Task<ApprovalWorkflowResult> ProcessApprovalAsync(int orderId, int levelId, ApprovalDecisionRequest decision);
    Task<ApprovalWorkflowStatus> GetWorkflowStatusAsync(int orderId);
    Task<IEnumerable<PendingApprovalItem>> GetPendingApprovalsAsync(string approverRole, string approverUserId);
    
    // Workflow management
    Task<ApprovalWorkflow> CreateWorkflowAsync(CreateApprovalWorkflowRequest request);
    Task<ApprovalWorkflow> UpdateWorkflowAsync(int workflowId, UpdateApprovalWorkflowRequest request);
    Task<IEnumerable<ApprovalWorkflow>> GetActiveWorkflowsAsync(string workflowType);
    Task<ApprovalWorkflow?> GetApplicableWorkflowAsync(TradingOrder order);
    
    // Escalation and notifications
    Task ProcessEscalationsAsync();
    Task SendApprovalNotificationsAsync();
}

public class ApprovalDecisionRequest
{
    public TradingOrderApprovalDecision Decision { get; set; }
    public string ApproverUserId { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class ApprovalWorkflowResult
{
    public bool IsSuccessful { get; set; }
    public string Message { get; set; } = string.Empty;
    public ApprovalWorkflowStatus CurrentStatus { get; set; } = new();
    public int? NextLevelId { get; set; }
    public string? NextApproverRole { get; set; }
    public bool IsComplete { get; set; }
    public bool IsApproved { get; set; }
}

public class ApprovalWorkflowStatus
{
    public int OrderId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public TradingOrderApprovalStatus OverallStatus { get; set; }
    public int CurrentLevel { get; set; }
    public string CurrentLevelName { get; set; } = string.Empty;
    public string? CurrentApproverRole { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IEnumerable<ApprovalLevelStatus> LevelStatuses { get; set; } = new List<ApprovalLevelStatus>();
    public IEnumerable<string> PendingRoles { get; set; } = new List<string>();
}

public class ApprovalLevelStatus
{
    public int Level { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public TradingOrderApprovalDecision Status { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? Comments { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEscalated { get; set; }
    public DateTime? EscalationDate { get; set; }
}

public class PendingApprovalItem
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public TradingOrderType OrderType { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TradingPartnerName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public int DaysWaiting { get; set; }
    public string RequiredRole { get; set; } = string.Empty;
    public int ApprovalLevel { get; set; }
    public bool IsEscalated { get; set; }
    public decimal? RiskAmount { get; set; }
}

public class CreateApprovalWorkflowRequest
{
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public WorkflowConditions Conditions { get; set; } = new();
    public IEnumerable<CreateApprovalWorkflowLevel> Levels { get; set; } = new List<CreateApprovalWorkflowLevel>();
}

public class CreateApprovalWorkflowLevel
{
    public int Level { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public int RequiredApprovers { get; set; } = 1;
    public bool IsParallel { get; set; }
    public int TimeoutHours { get; set; } = 24;
    public bool IsOptional { get; set; }
    public string? EscalationRole { get; set; }
    public int EscalationHours { get; set; } = 48;
}

public class UpdateApprovalWorkflowRequest
{
    public string? WorkflowName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public WorkflowConditions? Conditions { get; set; }
}