using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TradeChain entities
/// </summary>
public class TradeChainRepository : Repository<TradeChain>, ITradeChainRepository
{
    public TradeChainRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TradeChain?> GetByChainIdAsync(string chainId)
    {
        return await _context.TradingChains
            .FirstOrDefaultAsync(tc => tc.ChainId == chainId);
    }

    public async Task<List<TradeChain>> GetByPurchaseContractIdAsync(Guid purchaseContractId)
    {
        return await _context.TradingChains
            .Where(tc => tc.PurchaseContractId == purchaseContractId)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetBySalesContractIdAsync(Guid salesContractId)
    {
        return await _context.TradingChains
            .Where(tc => tc.SalesContractId == salesContractId)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetBySupplierIdAsync(Guid supplierId)
    {
        return await _context.TradingChains
            .Where(tc => tc.SupplierId == supplierId)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.TradingChains
            .Where(tc => tc.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetByProductIdAsync(Guid productId)
    {
        return await _context.TradingChains
            .Where(tc => tc.ProductId == productId)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetByStatusAsync(TradeChainStatus status)
    {
        return await _context.TradingChains
            .Where(tc => tc.Status == status)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetByTypeAsync(TradeChainType type)
    {
        return await _context.TradingChains
            .Where(tc => tc.Type == type)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.TradingChains
            .Where(tc => tc.CreatedAt >= startDate && tc.CreatedAt <= endDate)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetByDeliveryDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.TradingChains
            .Where(tc => 
                (tc.ExpectedDeliveryStart >= startDate && tc.ExpectedDeliveryStart <= endDate) ||
                (tc.ExpectedDeliveryEnd >= startDate && tc.ExpectedDeliveryEnd <= endDate) ||
                (tc.ActualDeliveryStart >= startDate && tc.ActualDeliveryStart <= endDate) ||
                (tc.ActualDeliveryEnd >= startDate && tc.ActualDeliveryEnd <= endDate))
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetActiveAsync()
    {
        return await _context.TradingChains
            .Where(tc => tc.Status != TradeChainStatus.Completed && tc.Status != TradeChainStatus.Cancelled)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetRequiringAttentionAsync()
    {
        var now = DateTime.UtcNow;
        
        return await _context.TradingChains
            .Where(tc => 
                // Overdue delivery
                (tc.ExpectedDeliveryEnd < now && tc.ActualDeliveryEnd == null) ||
                // Long duration without progress
                (tc.CreatedAt < now.AddDays(-30) && tc.Status == TradeChainStatus.Initiated) ||
                // Missing sales contract for old purchase
                (tc.PurchaseContractId != null && tc.SalesContractId == null && tc.CreatedAt < now.AddDays(-7)))
            .ToListAsync();
    }

    public async Task<(List<TradeChainSummary> Items, int TotalCount)> GetSummariesAsync(
        int page = 1, 
        int pageSize = 20,
        TradeChainStatus? status = null,
        TradeChainType? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.TradingChains.AsQueryable();

        // Apply filters
        if (status.HasValue)
            query = query.Where(tc => tc.Status == status.Value);

        if (type.HasValue)
            query = query.Where(tc => tc.Type == type.Value);

        if (startDate.HasValue)
            query = query.Where(tc => tc.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(tc => tc.CreatedAt <= endDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(tc => tc.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tc => tc.GetSummary())
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<TradeChain>> GetWithPerformanceMetricsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.TradingChains
            .Where(tc => tc.CreatedAt >= startDate && tc.CreatedAt <= endDate)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> SearchAsync(TradeChainSearchCriteria criteria)
    {
        var query = _context.TradingChains.AsQueryable();

        // Apply filters based on criteria
        if (!string.IsNullOrEmpty(criteria.ChainId))
            query = query.Where(tc => tc.ChainId.Contains(criteria.ChainId));

        if (!string.IsNullOrEmpty(criteria.ChainName))
            query = query.Where(tc => tc.ChainName.Contains(criteria.ChainName));

        if (criteria.Statuses?.Any() == true)
            query = query.Where(tc => criteria.Statuses.Contains(tc.Status));

        if (criteria.Types?.Any() == true)
            query = query.Where(tc => criteria.Types.Contains(tc.Type));

        if (criteria.SupplierId.HasValue)
            query = query.Where(tc => tc.SupplierId == criteria.SupplierId.Value);

        if (criteria.CustomerId.HasValue)
            query = query.Where(tc => tc.CustomerId == criteria.CustomerId.Value);

        if (criteria.ProductId.HasValue)
            query = query.Where(tc => tc.ProductId == criteria.ProductId.Value);

        if (criteria.StartDate.HasValue)
            query = query.Where(tc => tc.CreatedAt >= criteria.StartDate.Value);

        if (criteria.EndDate.HasValue)
            query = query.Where(tc => tc.CreatedAt <= criteria.EndDate.Value);

        if (criteria.DeliveryStartDate.HasValue)
            query = query.Where(tc => tc.ExpectedDeliveryStart >= criteria.DeliveryStartDate.Value);

        if (criteria.DeliveryEndDate.HasValue)
            query = query.Where(tc => tc.ExpectedDeliveryEnd <= criteria.DeliveryEndDate.Value);

        if (criteria.MinValue.HasValue)
            query = query.Where(tc => tc.PurchaseValue!.Amount >= criteria.MinValue.Value);

        if (criteria.MaxValue.HasValue)
            query = query.Where(tc => tc.PurchaseValue!.Amount <= criteria.MaxValue.Value);

        if (!string.IsNullOrEmpty(criteria.CreatedBy))
            query = query.Where(tc => tc.CreatedBy.Contains(criteria.CreatedBy));

        if (!string.IsNullOrEmpty(criteria.SearchText))
        {
            query = query.Where(tc => 
                tc.ChainId.Contains(criteria.SearchText) ||
                tc.ChainName.Contains(criteria.SearchText) ||
                tc.Notes!.Contains(criteria.SearchText));
        }

        // Apply sorting
        query = criteria.SortBy?.ToLower() switch
        {
            "chainid" => criteria.SortDescending ? query.OrderByDescending(tc => tc.ChainId) : query.OrderBy(tc => tc.ChainId),
            "chainname" => criteria.SortDescending ? query.OrderByDescending(tc => tc.ChainName) : query.OrderBy(tc => tc.ChainName),
            "status" => criteria.SortDescending ? query.OrderByDescending(tc => tc.Status) : query.OrderBy(tc => tc.Status),
            "type" => criteria.SortDescending ? query.OrderByDescending(tc => tc.Type) : query.OrderBy(tc => tc.Type),
            "tradedate" => criteria.SortDescending ? query.OrderByDescending(tc => tc.TradeDate) : query.OrderBy(tc => tc.TradeDate),
            "value" => criteria.SortDescending ? query.OrderByDescending(tc => tc.PurchaseValue!.Amount) : query.OrderBy(tc => tc.PurchaseValue!.Amount),
            _ => criteria.SortDescending ? query.OrderByDescending(tc => tc.CreatedAt) : query.OrderBy(tc => tc.CreatedAt)
        };

        return await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();
    }

    public async Task<TradeChainAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var chains = await _context.TradingChains
            .Where(tc => tc.CreatedAt >= startDate && tc.CreatedAt <= endDate)
            .ToListAsync();

        var analytics = new TradeChainAnalytics
        {
            AnalyzedFrom = startDate,
            AnalyzedTo = endDate,
            GeneratedAt = DateTime.UtcNow,
            TotalChains = chains.Count,
            ActiveChains = chains.Count(tc => tc.Status != TradeChainStatus.Completed && tc.Status != TradeChainStatus.Cancelled),
            CompletedChains = chains.Count(tc => tc.Status == TradeChainStatus.Completed),
            CancelledChains = chains.Count(tc => tc.Status == TradeChainStatus.Cancelled)
        };

        // Chains by status
        analytics.ChainsByStatus = chains
            .GroupBy(tc => tc.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Chains by type
        analytics.ChainsByType = chains
            .GroupBy(tc => tc.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        // Financial metrics
        analytics.TotalPurchaseValue = chains.Where(tc => tc.PurchaseValue != null).Sum(tc => tc.PurchaseValue!.Amount);
        analytics.TotalSalesValue = chains.Where(tc => tc.SalesValue != null).Sum(tc => tc.SalesValue!.Amount);
        analytics.TotalRealizedPnL = chains.Where(tc => tc.RealizedPnL != null).Sum(tc => tc.RealizedPnL!.Amount);
        analytics.TotalUnrealizedPnL = chains.Where(tc => tc.UnrealizedPnL != null).Sum(tc => tc.UnrealizedPnL!.Amount);

        // Performance metrics
        var completedChains = chains.Where(tc => tc.Status == TradeChainStatus.Completed).ToList();
        if (completedChains.Any())
        {
            analytics.AverageProfitMargin = completedChains
                .Where(tc => tc.RealizedPnL != null && tc.SalesValue != null && tc.SalesValue.Amount > 0)
                .Average(tc => (tc.RealizedPnL!.Amount / tc.SalesValue!.Amount) * 100);

            analytics.AverageCompletionTime = (decimal)completedChains
                .Where(tc => tc.UpdatedAt.HasValue)
                .Average(tc => (tc.UpdatedAt!.Value - tc.CreatedAt).TotalDays);

            var deliveredChains = completedChains.Where(tc => tc.ActualDeliveryEnd.HasValue && tc.ExpectedDeliveryEnd.HasValue).ToList();
            if (deliveredChains.Any())
            {
                analytics.OnTimeDeliveryRate = (decimal)deliveredChains
                    .Count(tc => tc.ActualDeliveryEnd <= tc.ExpectedDeliveryEnd) / deliveredChains.Count * 100;
            }
        }

        // Monthly trends
        analytics.MonthlyTrends = chains
            .GroupBy(tc => new { tc.CreatedAt.Year, tc.CreatedAt.Month })
            .Select(g => new TradeChainTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                ChainCount = g.Count(),
                TotalValue = g.Where(tc => tc.PurchaseValue != null).Sum(tc => tc.PurchaseValue!.Amount),
                TotalPnL = g.Where(tc => tc.RealizedPnL != null).Sum(tc => tc.RealizedPnL!.Amount),
                AverageProfitMargin = g.Where(tc => tc.RealizedPnL != null && tc.SalesValue != null && tc.SalesValue.Amount > 0)
                    .Average(tc => (tc.RealizedPnL!.Amount / tc.SalesValue!.Amount) * 100)
            })
            .OrderBy(t => t.Year).ThenBy(t => t.Month)
            .ToList();

        // Performance by type
        analytics.PerformanceByType = chains
            .GroupBy(tc => tc.Type)
            .Select(g => new TradeChainPerformanceByType
            {
                Type = g.Key,
                Count = g.Count(),
                AverageProfitMargin = g.Where(tc => tc.RealizedPnL != null && tc.SalesValue != null && tc.SalesValue.Amount > 0)
                    .Average(tc => (tc.RealizedPnL!.Amount / tc.SalesValue!.Amount) * 100),
                AverageCompletionTime = (decimal)g.Average(tc => (DateTime.UtcNow - tc.CreatedAt).TotalDays),
                OnTimeDeliveryRate = g.Where(tc => tc.ActualDeliveryEnd.HasValue && tc.ExpectedDeliveryEnd.HasValue)
                    .Count(tc => tc.ActualDeliveryEnd <= tc.ExpectedDeliveryEnd) / (decimal)Math.Max(1, g.Count()) * 100,
                TotalValue = g.Where(tc => tc.PurchaseValue != null).Sum(tc => tc.PurchaseValue!.Amount)
            })
            .ToList();

        return analytics;
    }

    public async Task<List<TradeChain>> GetOverdueAsync()
    {
        var now = DateTime.UtcNow;
        
        return await _context.TradingChains
            .Where(tc => 
                tc.Status != TradeChainStatus.Completed && 
                tc.Status != TradeChainStatus.Cancelled &&
                tc.ExpectedDeliveryEnd < now && 
                tc.ActualDeliveryEnd == null)
            .ToListAsync();
    }

    public async Task<List<TradeChain>> GetWithIncompleteOperationsAsync()
    {
        // This is a simplified implementation
        // In reality, you'd define what constitutes "incomplete operations"
        var now = DateTime.UtcNow;
        
        return await _context.TradingChains
            .Where(tc => 
                tc.Status == TradeChainStatus.Contracted &&
                tc.CreatedAt < now.AddDays(-7) && // Older than 7 days
                tc.Operations.Count < 3) // Has fewer than expected operations
            .ToListAsync();
    }
}