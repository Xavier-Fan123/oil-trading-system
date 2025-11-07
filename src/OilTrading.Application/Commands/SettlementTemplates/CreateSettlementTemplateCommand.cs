using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.SettlementTemplates;

/// <summary>
/// Command to create a new settlement template
/// </summary>
public class CreateSettlementTemplateCommand : IRequest<SettlementTemplateDto>
{
    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Template configuration (JSON)
    /// </summary>
    public string TemplateConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Whether template is public
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// User creating the template
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User name for audit
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}
