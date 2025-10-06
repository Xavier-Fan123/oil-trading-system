using OilTrading.Core.Common;

namespace OilTrading.Core.Agents;

/// <summary>
/// 智能代理基础接口
/// </summary>
public interface IAgent
{
    /// <summary>
    /// 代理唯一标识
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// 代理名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 代理类型
    /// </summary>
    AgentType Type { get; }
    
    /// <summary>
    /// 代理状态
    /// </summary>
    AgentStatus Status { get; }
    
    /// <summary>
    /// 代理优先级 (1-100, 数字越高优先级越高)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// 初始化代理
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 启动代理
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 停止代理
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 处理消息
    /// </summary>
    Task<AgentResponse> ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行决策
    /// </summary>
    Task<AgentDecision> MakeDecisionAsync(DecisionContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取代理健康状态
    /// </summary>
    Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 代理类型枚举
/// </summary>
public enum AgentType
{
    RiskManagement = 1,
    PriceAnalysis = 2,
    InventoryOptimization = 3,
    ComplianceMonitoring = 4,
    TradingStrategy = 5,
    PortfolioOptimization = 6,
    MarketDataAnalysis = 7,
    SupplyChainOptimization = 8
}

/// <summary>
/// 代理状态枚举
/// </summary>
public enum AgentStatus
{
    Inactive = 0,
    Initializing = 1,
    Active = 2,
    Processing = 3,
    Error = 4,
    Suspended = 5
}

/// <summary>
/// 代理健康状态
/// </summary>
public class AgentHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastCheckTime { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int ProcessedMessages { get; set; }
    public int ErrorCount { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}