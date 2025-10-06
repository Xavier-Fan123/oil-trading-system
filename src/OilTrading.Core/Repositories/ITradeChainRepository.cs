using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

/// <summary>
/// Repository interface for TradeChain entities
/// </summary>
public interface ITradeChainRepository : IRepository<TradeChain>
{
    /// <summary>
    /// Gets a trade chain by its chain ID
    /// </summary>
    Task<TradeChain?> GetByChainIdAsync(string chainId);

    /// <summary>
    /// Gets trade chains by purchase contract ID
    /// </summary>
    Task<List<TradeChain>> GetByPurchaseContractIdAsync(Guid purchaseContractId);

    /// <summary>
    /// Gets trade chains by sales contract ID
    /// </summary>
    Task<List<TradeChain>> GetBySalesContractIdAsync(Guid salesContractId);

    /// <summary>
    /// Gets trade chains by supplier ID
    /// </summary>
    Task<List<TradeChain>> GetBySupplierIdAsync(Guid supplierId);

    /// <summary>
    /// Gets trade chains by customer ID
    /// </summary>
    Task<List<TradeChain>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>
    /// Gets trade chains by product ID
    /// </summary>
    Task<List<TradeChain>> GetByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets trade chains by status
    /// </summary>
    Task<List<TradeChain>> GetByStatusAsync(TradeChainStatus status);

    /// <summary>
    /// Gets trade chains by type
    /// </summary>
    Task<List<TradeChain>> GetByTypeAsync(TradeChainType type);

    /// <summary>
    /// Gets trade chains created within a date range
    /// </summary>
    Task<List<TradeChain>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets trade chains with delivery dates in a specific range
    /// </summary>
    Task<List<TradeChain>> GetByDeliveryDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets active trade chains (not completed or cancelled)
    /// </summary>
    Task<List<TradeChain>> GetActiveAsync();

    /// <summary>
    /// Gets trade chains requiring attention (alerts, delays, etc.)
    /// </summary>
    Task<List<TradeChain>> GetRequiringAttentionAsync();

    /// <summary>
    /// Gets trade chain summaries with pagination
    /// </summary>
    Task<(List<TradeChainSummary> Items, int TotalCount)> GetSummariesAsync(
        int page = 1, 
        int pageSize = 20,
        TradeChainStatus? status = null,
        TradeChainType? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets trade chains with performance metrics
    /// </summary>
    Task<List<TradeChain>> GetWithPerformanceMetricsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Searches trade chains by various criteria
    /// </summary>
    Task<List<TradeChain>> SearchAsync(TradeChainSearchCriteria criteria);

    /// <summary>
    /// Gets trade chain analytics for dashboard
    /// </summary>
    Task<TradeChainAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets trade chains that are overdue or delayed
    /// </summary>
    Task<List<TradeChain>> GetOverdueAsync();

    /// <summary>
    /// Gets trade chains with incomplete operations
    /// </summary>
    Task<List<TradeChain>> GetWithIncompleteOperationsAsync();
}

/// <summary>
/// Search criteria for trade chains
/// </summary>
public class TradeChainSearchCriteria
{
    public string? ChainId { get; set; }
    public string? ChainName { get; set; }
    public List<TradeChainStatus>? Statuses { get; set; }
    public List<TradeChainType>? Types { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? DeliveryStartDate { get; set; }
    public DateTime? DeliveryEndDate { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? CreatedBy { get; set; }
    public string? SearchText { get; set; } // Free text search
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Analytics data for trade chains
/// </summary>
public class TradeChainAnalytics
{
    public int TotalChains { get; set; }
    public int ActiveChains { get; set; }
    public int CompletedChains { get; set; }
    public int CancelledChains { get; set; }
    
    public Dictionary<TradeChainStatus, int> ChainsByStatus { get; set; } = new();
    public Dictionary<TradeChainType, int> ChainsByType { get; set; } = new();
    
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal TotalRealizedPnL { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    
    public decimal AverageProfitMargin { get; set; }
    public decimal AverageCompletionTime { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    
    public List<TradeChainTrend> MonthlyTrends { get; set; } = new();
    public List<TradeChainPerformanceByType> PerformanceByType { get; set; } = new();
    
    public DateTime AnalyzedFrom { get; set; }
    public DateTime AnalyzedTo { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Monthly trend data for trade chains
/// </summary>
public class TradeChainTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int ChainCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal AverageProfitMargin { get; set; }
}

/// <summary>
/// Performance metrics by trade chain type
/// </summary>
public class TradeChainPerformanceByType
{
    public TradeChainType Type { get; set; }
    public int Count { get; set; }
    public decimal AverageProfitMargin { get; set; }
    public decimal AverageCompletionTime { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal TotalValue { get; set; }
}