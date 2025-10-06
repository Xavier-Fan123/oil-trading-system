using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.SalesContracts;

/// <summary>
/// Query to get sales contract summary statistics
/// </summary>
public class GetSalesContractSummaryQuery : IRequest<SalesContractsSummaryDto>
{
}