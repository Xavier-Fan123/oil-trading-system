using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 获取交易组查询处理器 - Get Trade Groups Query Handler
/// </summary>
public class GetTradeGroupsQueryHandler : IRequestHandler<GetTradeGroupsQuery, PagedResult<TradeGroupSummaryDto>>
{
    public async Task<PagedResult<TradeGroupSummaryDto>> Handle(GetTradeGroupsQuery request, CancellationToken cancellationToken)
    {
        // For now, return mock data until we have proper repository pattern implementation
        await Task.Delay(50, cancellationToken);

        var mockTradeGroups = GenerateMockTradeGroups();

        // Apply filters
        var filteredGroups = mockTradeGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
        {
            filteredGroups = filteredGroups.Where(g => 
                g.GroupName.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                (g.Description != null && g.Description.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase)));
        }

        if (request.StrategyType.HasValue)
        {
            filteredGroups = filteredGroups.Where(g => g.StrategyType == request.StrategyType.Value);
        }

        if (request.Status.HasValue)
        {
            filteredGroups = filteredGroups.Where(g => g.Status == request.Status.Value);
        }

        if (request.RiskLevel.HasValue)
        {
            filteredGroups = filteredGroups.Where(g => g.ExpectedRiskLevel == request.RiskLevel.Value);
        }

        if (request.CreatedFrom.HasValue)
        {
            filteredGroups = filteredGroups.Where(g => g.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            filteredGroups = filteredGroups.Where(g => g.CreatedAt <= request.CreatedTo.Value);
        }

        // Apply sorting
        filteredGroups = request.SortBy?.ToLower() switch
        {
            "groupname" => request.SortDirection?.ToLower() == "asc" 
                ? filteredGroups.OrderBy(g => g.GroupName) 
                : filteredGroups.OrderByDescending(g => g.GroupName),
            "strategytype" => request.SortDirection?.ToLower() == "asc" 
                ? filteredGroups.OrderBy(g => g.StrategyType) 
                : filteredGroups.OrderByDescending(g => g.StrategyType),
            "status" => request.SortDirection?.ToLower() == "asc" 
                ? filteredGroups.OrderBy(g => g.Status) 
                : filteredGroups.OrderByDescending(g => g.Status),
            "totalvalue" => request.SortDirection?.ToLower() == "asc" 
                ? filteredGroups.OrderBy(g => g.TotalValue) 
                : filteredGroups.OrderByDescending(g => g.TotalValue),
            "netpnl" => request.SortDirection?.ToLower() == "asc" 
                ? filteredGroups.OrderBy(g => g.NetPnL) 
                : filteredGroups.OrderByDescending(g => g.NetPnL),
            _ => request.SortDirection?.ToLower() == "asc" 
                ? filteredGroups.OrderBy(g => g.CreatedAt) 
                : filteredGroups.OrderByDescending(g => g.CreatedAt)
        };

        var totalCount = filteredGroups.Count();
        var items = filteredGroups
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<TradeGroupSummaryDto>(items, totalCount, request.Page, request.PageSize);
    }

    private static List<TradeGroupSummaryDto> GenerateMockTradeGroups()
    {
        return new List<TradeGroupSummaryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GroupName = "Brent Calendar Spread Q2-Q3",
                StrategyType = 2, // CalendarSpread
                StrategyTypeName = "Calendar Spread",
                Description = "Q2/Q3 Brent calendar spread strategy to capture contango",
                Status = 1, // Active
                StatusName = "Active",
                ExpectedRiskLevel = 2, // Medium
                ExpectedRiskLevelName = "Medium",
                MaxAllowedLoss = 500000m,
                TargetProfit = 200000m,
                TotalContracts = 4,
                ActiveContracts = 4,
                TotalValue = 25000000m,
                NetPnL = 85000m,
                TagCount = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                CreatedBy = "trader1@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedBy = "trader1@company.com"
            },
            new()
            {
                Id = Guid.NewGuid(),
                GroupName = "WTI-Brent Arbitrage",
                StrategyType = 8, // Arbitrage
                StrategyTypeName = "Arbitrage",
                Description = "Exploit price differential between WTI and Brent crude",
                Status = 1, // Active
                StatusName = "Active",
                ExpectedRiskLevel = 3, // High
                ExpectedRiskLevelName = "High",
                MaxAllowedLoss = 1000000m,
                TargetProfit = 500000m,
                TotalContracts = 6,
                ActiveContracts = 5,
                TotalValue = 45000000m,
                NetPnL = -125000m,
                TagCount = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "trader2@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedBy = "trader2@company.com"
            },
            new()
            {
                Id = Guid.NewGuid(),
                GroupName = "Crack Spread Hedging",
                StrategyType = 9, // CrackSpread
                StrategyTypeName = "Crack Spread",
                Description = "Hedge refining margins through crack spread positions",
                Status = 1, // Active
                StatusName = "Active",
                ExpectedRiskLevel = 2, // Medium
                ExpectedRiskLevelName = "Medium",
                MaxAllowedLoss = 750000m,
                TargetProfit = 300000m,
                TotalContracts = 8,
                ActiveContracts = 7,
                TotalValue = 32000000m,
                NetPnL = 156000m,
                TagCount = 5,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                CreatedBy = "trader3@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedBy = "trader3@company.com"
            },
            new()
            {
                Id = Guid.NewGuid(),
                GroupName = "Asia-Europe Location Spread",
                StrategyType = 4, // LocationSpread
                StrategyTypeName = "Location Spread",
                Description = "Capture location differential between Asian and European markets",
                Status = 2, // Closed
                StatusName = "Closed",
                ExpectedRiskLevel = 2, // Medium
                ExpectedRiskLevelName = "Medium",
                MaxAllowedLoss = 400000m,
                TargetProfit = 180000m,
                TotalContracts = 3,
                ActiveContracts = 0,
                TotalValue = 0m,
                NetPnL = 125000m,
                TagCount = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                CreatedBy = "trader1@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedBy = "trader1@company.com"
            },
            new()
            {
                Id = Guid.NewGuid(),
                GroupName = "Inventory Basis Hedge",
                StrategyType = 5, // BasisHedge
                StrategyTypeName = "Basis Hedge",
                Description = "Hedge physical inventory exposure using futures",
                Status = 1, // Active
                StatusName = "Active",
                ExpectedRiskLevel = 1, // Low
                ExpectedRiskLevelName = "Low",
                MaxAllowedLoss = 200000m,
                TargetProfit = 75000m,
                TotalContracts = 2,
                ActiveContracts = 2,
                TotalValue = 18000000m,
                NetPnL = 45000m,
                TagCount = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                CreatedBy = "riskmanager@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedBy = "riskmanager@company.com"
            }
        };
    }
}