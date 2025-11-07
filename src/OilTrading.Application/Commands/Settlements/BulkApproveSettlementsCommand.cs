using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Command to bulk approve multiple settlements
/// </summary>
public class BulkApproveSettlementsCommand : IRequest<BulkOperationResultDto>
{
    /// <summary>
    /// List of settlement IDs to approve
    /// </summary>
    public List<string> SettlementIds { get; set; } = new();

    /// <summary>
    /// User approving the settlements
    /// </summary>
    public string ApprovedBy { get; set; } = string.Empty;
}
