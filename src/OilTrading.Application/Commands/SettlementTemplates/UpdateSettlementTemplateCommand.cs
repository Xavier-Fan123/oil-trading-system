using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.SettlementTemplates;

/// <summary>
/// Command to update an existing settlement template
/// </summary>
public class UpdateSettlementTemplateCommand : IRequest<SettlementTemplateDto>
{
    /// <summary>
    /// Template ID to update
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Updated template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Updated template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Updated template configuration (JSON)
    /// </summary>
    public string TemplateConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Updated public/private status
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// User performing the update
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User name for audit
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}
