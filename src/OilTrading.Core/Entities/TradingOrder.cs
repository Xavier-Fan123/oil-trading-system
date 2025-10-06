using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class TradingOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public TradingOrderType OrderType { get; set; }
    public TradingOrderStatus Status { get; set; }
    public int TraderId { get; set; }
    public int TradingPartnerId { get; set; }
    public int ProductId { get; set; }
    public Quantity Quantity { get; set; } = new(0, QuantityUnit.MT);
    public Money? Price { get; set; }
    public TradingOrderPriceType PriceType { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExecutionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DeliveryTerms DeliveryTerms { get; set; }
    public string? DeliveryLocation { get; set; }
    public DateTime? DeliveryStart { get; set; }
    public DateTime? DeliveryEnd { get; set; }
    public string? Notes { get; set; }
    
    // Approval workflow
    public TradingOrderApprovalStatus ApprovalStatus { get; set; }
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    // Risk checks
    public bool RiskCheckPassed { get; set; }
    public string? RiskCheckNotes { get; set; }
    public DateTime? RiskCheckDate { get; set; }
    public string? RiskCheckedBy { get; set; }
    
    // Execution details
    public Money? ExecutedPrice { get; set; }
    public Quantity? ExecutedQuantity { get; set; }
    public string? ExecutionNotes { get; set; }
    public string? ExecutedBy { get; set; }
    
    // Navigation properties
    public User? Trader { get; set; }
    public TradingPartner TradingPartner { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<TradingOrderApproval> Approvals { get; set; } = new List<TradingOrderApproval>();
    public ICollection<TradingOrderExecution> Executions { get; set; } = new List<TradingOrderExecution>();
}

public class TradingOrderApproval : BaseEntity
{
    public int TradingOrderId { get; set; }
    public TradingOrder TradingOrder { get; set; } = null!;
    public string ApproverRole { get; set; } = string.Empty; // Trader, Manager, RiskManager, Compliance
    public string ApproverUserId { get; set; } = string.Empty;
    public string ApproverName { get; set; } = string.Empty;
    public TradingOrderApprovalDecision Decision { get; set; }
    public DateTime DecisionDate { get; set; }
    public string? Comments { get; set; }
    public bool IsRequired { get; set; }
    public int ApprovalLevel { get; set; } // 1, 2, 3 for multi-level approval
}

public class TradingOrderExecution : BaseEntity
{
    public int TradingOrderId { get; set; }
    public TradingOrder TradingOrder { get; set; } = null!;
    public Quantity ExecutedQuantity { get; set; } = new(0, QuantityUnit.MT);
    public Money ExecutedPrice { get; set; } = new(0, "USD");
    public DateTime ExecutionTime { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string? ExecutionMethod { get; set; } // Manual, Electronic, Phone
    public string? CounterpartyReference { get; set; }
    public string? Notes { get; set; }
    public TradingExecutionStatus ExecutionStatus { get; set; }
}

public enum TradingOrderType
{
    Buy = 1,
    Sell = 2
}

public enum TradingOrderStatus
{
    Draft = 1,
    Submitted = 2,
    PendingApproval = 3,
    Approved = 4,
    Rejected = 5,
    Executed = 6,
    PartiallyExecuted = 7,
    Cancelled = 8,
    Expired = 9
}

public enum TradingOrderPriceType
{
    Fixed = 1,          // Fixed price
    Market = 2,         // Market price at execution
    IndexBased = 3,     // Based on price index
    Formula = 4         // Price formula
}

public enum TradingOrderApprovalStatus
{
    NotRequired = 1,
    Pending = 2,
    InProgress = 3,
    Approved = 4,
    Rejected = 5
}

public enum TradingOrderApprovalDecision
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Escalated = 4
}

public enum TradingExecutionStatus
{
    Executed = 1,
    Failed = 2,
    Cancelled = 3,
    Settled = 4
}