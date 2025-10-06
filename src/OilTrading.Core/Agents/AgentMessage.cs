namespace OilTrading.Core.Agents;

/// <summary>
/// 代理消息类
/// </summary>
public class AgentMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public string Subject { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool RequiresResponse { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// 代理响应类
/// </summary>
public class AgentResponse
{
    public string ResponseId { get; set; } = Guid.NewGuid().ToString();
    public string OriginalMessageId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public ResponseStatus Status { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<AgentRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// 代理决策类
/// </summary>
public class AgentDecision
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString();
    public string AgentId { get; set; } = string.Empty;
    public DecisionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<AgentAction> RecommendedActions { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Reasoning { get; set; }
    public List<string> SupportingEvidence { get; set; } = new();
}

/// <summary>
/// 决策上下文
/// </summary>
public class DecisionContext
{
    public string ContextId { get; set; } = Guid.NewGuid().ToString();
    public DecisionScope Scope { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<BusinessRule> BusinessRules { get; set; } = new();
    public RiskParameters? RiskParameters { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan? TimeHorizon { get; set; }
}

/// <summary>
/// 代理推荐
/// </summary>
public class AgentRecommendation
{
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
    public RecommendationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public RecommendationPriority Priority { get; set; }
    public List<AgentAction> Actions { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// 代理动作
/// </summary>
public class AgentAction
{
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    public ActionType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public double RiskScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 业务规则
/// </summary>
public class BusinessRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public RuleType Type { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 风险参数
/// </summary>
public class RiskParameters
{
    public double VaRThreshold { get; set; }
    public double MaxExposure { get; set; }
    public double ConcentrationLimit { get; set; }
    public TimeSpan MonitoringWindow { get; set; }
    public List<string> RestrictedProducts { get; set; } = new();
    public Dictionary<string, double> ProductLimits { get; set; } = new();
}

#region Enums

public enum MessageType
{
    Request = 1,
    Response = 2,
    Notification = 3,
    Alert = 4,
    Command = 5,
    Query = 6,
    Event = 7
}

public enum MessagePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

public enum ResponseStatus
{
    Success = 1,
    Error = 2,
    Timeout = 3,
    Rejected = 4,
    Partial = 5
}

public enum DecisionType
{
    RiskAdjustment = 1,
    PositionRebalancing = 2,
    PriceOptimization = 3,
    InventoryAllocation = 4,
    ComplianceAction = 5,
    TradingStrategy = 6,
    HedgingDecision = 7
}

public enum DecisionScope
{
    Contract = 1,
    Portfolio = 2,
    Product = 3,
    TradingPartner = 4,
    Global = 5
}

public enum RecommendationType
{
    Risk = 1,
    Optimization = 2,
    Compliance = 3,
    Strategy = 4,
    Alert = 5
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum ActionType
{
    Approve = 1,
    Reject = 2,
    Modify = 3,
    Monitor = 4,
    Hedge = 5,
    Rebalance = 6,
    Alert = 7,
    Report = 8
}

public enum RuleType
{
    Risk = 1,
    Compliance = 2,
    Business = 3,
    Trading = 4,
    Operational = 5
}

#endregion