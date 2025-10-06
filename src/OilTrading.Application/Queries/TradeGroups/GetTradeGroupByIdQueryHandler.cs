using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Queries.TradeGroups;

/// <summary>
/// 根据ID获取交易组查询处理器 - Get Trade Group By ID Query Handler
/// </summary>
public class GetTradeGroupByIdQueryHandler : IRequestHandler<GetTradeGroupByIdQuery, TradeGroupDto>
{
    public async Task<TradeGroupDto> Handle(GetTradeGroupByIdQuery request, CancellationToken cancellationToken)
    {
        // For now, return mock data until we have proper repository pattern implementation
        await Task.Delay(50, cancellationToken);

        var mockTradeGroups = GenerateMockTradeGroupDetails();
        var tradeGroup = mockTradeGroups.FirstOrDefault(g => g.Id == request.Id);

        if (tradeGroup == null)
        {
            throw new NotFoundException($"Trade group with ID {request.Id} not found");
        }

        return tradeGroup;
    }

    private static List<TradeGroupDto> GenerateMockTradeGroupDetails()
    {
        var tradeGroupId1 = Guid.NewGuid();
        var tradeGroupId2 = Guid.NewGuid();

        return new List<TradeGroupDto>
        {
            new()
            {
                Id = tradeGroupId1,
                GroupName = "Brent Calendar Spread Q2-Q3",
                StrategyType = 2, // CalendarSpread
                StrategyTypeName = "Calendar Spread",
                Description = "Q2/Q3 Brent calendar spread strategy to capture contango structure",
                Status = 1, // Active
                StatusName = "Active",
                ExpectedRiskLevel = 2, // Medium
                ExpectedRiskLevelName = "Medium",
                MaxAllowedLoss = 500000m,
                TargetProfit = 200000m,
                TotalValue = 25000000m,
                NetPnL = 85000m,
                UnrealizedPnL = 65000m,
                RealizedPnL = 20000m,
                TotalContracts = 4,
                ActiveContracts = 4,
                PurchaseContracts = new List<ContractSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "PC-2025-001",
                        ContractType = "Purchase",
                        Status = "Active",
                        Quantity = 1000m,
                        QuantityUnit = "MT",
                        ContractValue = 8500000m,
                        Currency = "USD",
                        LaycanStart = DateTime.UtcNow.AddDays(30),
                        LaycanEnd = DateTime.UtcNow.AddDays(45),
                        CreatedAt = DateTime.UtcNow.AddDays(-15)
                    }
                },
                SalesContracts = new List<ContractSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "SC-2025-002",
                        ContractType = "Sales",
                        Status = "Active",
                        Quantity = 1000m,
                        QuantityUnit = "MT",
                        ContractValue = 8600000m,
                        Currency = "USD",
                        LaycanStart = DateTime.UtcNow.AddDays(90),
                        LaycanEnd = DateTime.UtcNow.AddDays(105),
                        CreatedAt = DateTime.UtcNow.AddDays(-14)
                    }
                },
                PaperContracts = new List<ContractSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "PC-FUT-001",
                        ContractType = "Paper",
                        Status = "Open",
                        Quantity = 500m,
                        QuantityUnit = "BBL",
                        ContractValue = 4000000m,
                        Currency = "USD",
                        CreatedAt = DateTime.UtcNow.AddDays(-13)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "PC-FUT-002",
                        ContractType = "Paper",
                        Status = "Open",
                        Quantity = -500m, // Short position
                        QuantityUnit = "BBL",
                        ContractValue = -3900000m,
                        Currency = "USD",
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    }
                },
                Tags = new List<TagSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Calendar Spread",
                        Description = "Time-based spread strategy",
                        Color = "#3B82F6",
                        Category = (TagCategory)2, // TradingStrategy
                        CategoryDisplayName = "Trading Strategy",
                        CategoryName = "Trading Strategy",
                        Priority = 1,
                        AssignedAt = DateTime.UtcNow.AddDays(-15),
                        AssignedBy = "trader1@company.com"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Medium Risk",
                        Description = "Medium risk level classification",
                        Color = "#F59E0B",
                        Category = (TagCategory)1, // RiskLevel
                        CategoryDisplayName = "Risk Level",
                        CategoryName = "Risk Level",
                        Priority = 2,
                        AssignedAt = DateTime.UtcNow.AddDays(-15),
                        AssignedBy = "riskmanager@company.com"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Q2-Q3 2025",
                        Description = "Second to third quarter strategy",
                        Color = "#10B981",
                        Category = (TagCategory)6, // MarketCondition
                        CategoryDisplayName = "Market Condition",
                        CategoryName = "Market Condition",
                        Priority = 0,
                        AssignedAt = DateTime.UtcNow.AddDays(-14),
                        AssignedBy = "trader1@company.com"
                    }
                },
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                CreatedBy = "trader1@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedBy = "trader1@company.com"
            },
            new()
            {
                Id = tradeGroupId2,
                GroupName = "WTI-Brent Arbitrage",
                StrategyType = 8, // Arbitrage
                StrategyTypeName = "Arbitrage",
                Description = "Exploit price differential between WTI and Brent crude oil",
                Status = 1, // Active
                StatusName = "Active",
                ExpectedRiskLevel = 3, // High
                ExpectedRiskLevelName = "High",
                MaxAllowedLoss = 1000000m,
                TargetProfit = 500000m,
                TotalValue = 45000000m,
                NetPnL = -125000m,
                UnrealizedPnL = -150000m,
                RealizedPnL = 25000m,
                TotalContracts = 6,
                ActiveContracts = 5,
                PurchaseContracts = new List<ContractSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "PC-2025-003",
                        ContractType = "Purchase",
                        Status = "Active",
                        Quantity = 2000m,
                        QuantityUnit = "MT",
                        ContractValue = 17000000m,
                        Currency = "USD",
                        LaycanStart = DateTime.UtcNow.AddDays(20),
                        LaycanEnd = DateTime.UtcNow.AddDays(35),
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    }
                },
                SalesContracts = new List<ContractSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "SC-2025-004",
                        ContractType = "Sales",
                        Status = "Active",
                        Quantity = 2000m,
                        QuantityUnit = "MT",
                        ContractValue = 17200000m,
                        Currency = "USD",
                        LaycanStart = DateTime.UtcNow.AddDays(25),
                        LaycanEnd = DateTime.UtcNow.AddDays(40),
                        CreatedAt = DateTime.UtcNow.AddDays(-9)
                    }
                },
                PaperContracts = new List<ContractSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "PC-FUT-003",
                        ContractType = "Paper",
                        Status = "Open",
                        Quantity = 1000m,
                        QuantityUnit = "BBL",
                        ContractValue = 5400000m,
                        Currency = "USD",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContractNumber = "PC-FUT-004",
                        ContractType = "Paper",
                        Status = "Open",
                        Quantity = -1000m, // Short position
                        QuantityUnit = "BBL",
                        ContractValue = -5600000m,
                        Currency = "USD",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    }
                },
                Tags = new List<TagSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Arbitrage",
                        Description = "Price differential exploitation strategy",
                        Color = "#8B5CF6",
                        Category = (TagCategory)2, // TradingStrategy
                        CategoryDisplayName = "Trading Strategy",
                        CategoryName = "Trading Strategy",
                        Priority = 1,
                        AssignedAt = DateTime.UtcNow.AddDays(-10),
                        AssignedBy = "trader2@company.com"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "High Risk",
                        Description = "High risk level classification",
                        Color = "#EF4444",
                        Category = (TagCategory)1, // RiskLevel
                        CategoryDisplayName = "Risk Level",
                        CategoryName = "Risk Level",
                        Priority = 3,
                        AssignedAt = DateTime.UtcNow.AddDays(-10),
                        AssignedBy = "riskmanager@company.com"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Intercommodity",
                        Description = "Different commodity types",
                        Color = "#F97316",
                        Category = (TagCategory)2, // TradingStrategy
                        CategoryDisplayName = "Trading Strategy",
                        CategoryName = "Trading Strategy",
                        Priority = 1,
                        AssignedAt = DateTime.UtcNow.AddDays(-9),
                        AssignedBy = "trader2@company.com"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "WTI-Brent",
                        Description = "WTI to Brent crude spread",
                        Color = "#14B8A6",
                        Category = (TagCategory)7, // ProductClass
                        CategoryDisplayName = "Product Class",
                        CategoryName = "Product Class",
                        Priority = 0,
                        AssignedAt = DateTime.UtcNow.AddDays(-9),
                        AssignedBy = "trader2@company.com"
                    }
                },
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "trader2@company.com",
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedBy = "trader2@company.com"
            }
        };
    }
}