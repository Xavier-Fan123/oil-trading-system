using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Command to bulk finalize multiple settlements
/// </summary>
public class BulkFinalizeSettlementsCommand : IRequest<BulkOperationResultDto>
{
    /// <summary>
    /// List of settlement IDs to finalize
    /// </summary>
    public List<string> SettlementIds { get; set; } = new();

    /// <summary>
    /// User finalizing the settlements
    /// </summary>
    public string FinalizedBy { get; set; } = string.Empty;
}
