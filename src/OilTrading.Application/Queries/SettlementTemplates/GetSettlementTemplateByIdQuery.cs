using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.SettlementTemplates;

/// <summary>
/// Query to get a specific settlement template by ID
/// </summary>
public class GetSettlementTemplateByIdQuery : IRequest<SettlementTemplateDto?>
{
    /// <summary>
    /// Template ID to retrieve
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Current user ID (for permission checking)
    /// </summary>
    public Guid UserId { get; set; }
}
