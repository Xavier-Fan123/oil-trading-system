using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.SalesContracts;

/// <summary>
/// Handler for getting sales contract summary statistics
/// </summary>
public class GetSalesContractSummaryQueryHandler : IRequestHandler<GetSalesContractSummaryQuery, SalesContractsSummaryDto>
{
    public async Task<SalesContractsSummaryDto> Handle(GetSalesContractSummaryQuery request, CancellationToken cancellationToken)
    {
        // For now, return mock data until we have proper repository pattern
        // This matches the structure expected by the frontend
        await Task.Delay(50, cancellationToken); // Simulate async operation

        return new SalesContractsSummaryDto
        {
            TotalContracts = 15,
            TotalValue = 45850000,
            EstimatedProfit = 2450000,
            ContractsByStatus = new List<ContractStatusSummaryDto>
            {
                new() { Status = "Draft", Count = 3, Value = 8500000 },
                new() { Status = "PendingApproval", Count = 4, Value = 12300000 },
                new() { Status = "Active", Count = 6, Value = 18750000 },
                new() { Status = "Completed", Count = 2, Value = 6300000 }
            },
            TopCustomers = new List<TopCustomerDto>
            {
                new()
                {
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "China National Petroleum Corporation",
                    ContractCount = 3,
                    TotalValue = 15200000
                },
                new()
                {
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Indian Oil Corporation",
                    ContractCount = 2,
                    TotalValue = 12100000
                },
                new()
                {
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Reliance Industries",
                    ContractCount = 2,
                    TotalValue = 8750000
                }
            },
            MonthlyBreakdown = new List<MonthlyBreakdownDto>
            {
                new() { Month = "2025-02", Contracts = 2, Value = 6300000, Profit = 320000 },
                new() { Month = "2025-03", Contracts = 5, Value = 16800000, Profit = 850000 },
                new() { Month = "2025-04", Contracts = 4, Value = 12150000, Profit = 640000 },
                new() { Month = "2025-05", Contracts = 3, Value = 8950000, Profit = 480000 },
                new() { Month = "2025-06", Contracts = 1, Value = 1650000, Profit = 160000 }
            }
        };
    }
}