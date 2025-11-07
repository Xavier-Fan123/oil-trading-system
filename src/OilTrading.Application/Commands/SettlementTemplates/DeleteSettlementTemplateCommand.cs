using MediatR;

namespace OilTrading.Application.Commands.SettlementTemplates;

/// <summary>
/// Command to delete (soft-delete) a settlement template
/// </summary>
public class DeleteSettlementTemplateCommand : IRequest<bool>
{
    /// <summary>
    /// Template ID to delete
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// User performing the deletion
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User name for audit
    /// </summary>
    public string DeletedBy { get; set; } = string.Empty;
}
