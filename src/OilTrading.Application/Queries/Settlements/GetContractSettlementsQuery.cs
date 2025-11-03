using MediatR;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// CQRS Query to retrieve all settlements for a contract
/// Returns paginated list of settlements for one-to-many relationship support
/// </summary>
public class GetContractSettlementsQuery : IRequest<List<SettlementDto>>
{
    public Guid ContractId { get; set; }
    public bool IsPurchaseSettlement { get; set; } = true;
}
