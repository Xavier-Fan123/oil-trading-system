using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Contracts;

/// <summary>
/// Query to resolve a contract from its external contract number
/// Supports disambiguation when multiple contracts match
/// </summary>
public class ResolveContractByExternalNumberQuery : IRequest<ContractResolutionResultDto>
{
    /// <summary>
    /// The external contract number to search for
    /// </summary>
    public string ExternalContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Expected contract type to narrow results (Purchase or Sales)
    /// </summary>
    public string? ExpectedContractType { get; set; }

    /// <summary>
    /// Optional: Expected trading partner ID to narrow results
    /// </summary>
    public Guid? ExpectedTradingPartnerId { get; set; }

    /// <summary>
    /// Optional: Expected product ID to narrow results
    /// </summary>
    public Guid? ExpectedProductId { get; set; }
}
