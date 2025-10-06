using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

/// <summary>
/// 交易组仓库实现 - Trade Group Repository Implementation
/// </summary>
public class TradeGroupRepository : Repository<TradeGroup>, ITradeGroupRepository
{
    public TradeGroupRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TradeGroup>> GetByStrategyTypeAsync(StrategyType strategyType, CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Where(tg => tg.StrategyType == strategyType)
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .OrderBy(tg => tg.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TradeGroup>> GetActiveTradeGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Where(tg => tg.Status == TradeGroupStatus.Active)
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .OrderBy(tg => tg.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TradeGroup?> GetByNameAsync(string groupName, CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .FirstOrDefaultAsync(tg => tg.GroupName == groupName, cancellationToken);
    }

    public async Task<TradeGroup?> GetWithContractsAsync(Guid tradeGroupId, CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Include(tg => tg.PaperContracts.Where(pc => pc.Status != PaperContractStatus.Cancelled))
            .Include(tg => tg.PurchaseContracts.Where(pc => pc.Status != ContractStatus.Cancelled))
            .Include(tg => tg.SalesContracts.Where(sc => sc.Status != ContractStatus.Cancelled))
            .FirstOrDefaultAsync(tg => tg.Id == tradeGroupId, cancellationToken);
    }

    public async Task<IEnumerable<TradeGroup>> GetTradeGroupsWithOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .Where(tg => tg.Status == TradeGroupStatus.Active &&
                        (tg.PaperContracts.Any(pc => pc.Status == PaperContractStatus.Open) ||
                         tg.PurchaseContracts.Any(pc => pc.Status == ContractStatus.Active) ||
                         tg.SalesContracts.Any(sc => sc.Status == ContractStatus.Active)))
            .OrderBy(tg => tg.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TradeGroupRiskSummary>> GetTradeGroupRiskSummariesAsync(CancellationToken cancellationToken = default)
    {
        // This is a complex query that calculates risk metrics for each trade group
        // In a real implementation, this might use stored procedures or computed views for performance
        var tradeGroups = await _context.TradeGroups
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .Where(tg => tg.Status == TradeGroupStatus.Active)
            .ToListAsync(cancellationToken);

        var summaries = new List<TradeGroupRiskSummary>();

        foreach (var group in tradeGroups)
        {
            var summary = new TradeGroupRiskSummary
            {
                TradeGroupId = group.Id,
                GroupName = group.GroupName,
                StrategyType = group.StrategyType,
                Status = group.Status,
                NetPnL = group.GetNetPnL(),
                TotalValue = group.GetTotalValue(),
                ContractCount = group.PaperContracts.Count + group.PurchaseContracts.Count + group.SalesContracts.Count,
                LastUpdated = group.UpdatedAt ?? group.CreatedAt
            };

            // For now, set VaR values to 0 - these would be calculated by the risk service
            // In a real implementation, this might call the risk calculation service
            summary.VaR95 = 0;
            summary.VaR99 = 0;

            summaries.Add(summary);
        }

        return summaries;
    }

    public override async Task<TradeGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .FirstOrDefaultAsync(tg => tg.Id == id, cancellationToken);
    }

    public override async Task<IReadOnlyList<TradeGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TradeGroups
            .Include(tg => tg.PaperContracts)
            .Include(tg => tg.PurchaseContracts)
            .Include(tg => tg.SalesContracts)
            .OrderBy(tg => tg.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}