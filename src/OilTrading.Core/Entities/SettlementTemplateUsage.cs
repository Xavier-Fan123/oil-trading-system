namespace OilTrading.Core.Entities;

/// <summary>
/// Tracks usage of a settlement template
/// Records when and where a template was applied
/// </summary>
public class SettlementTemplateUsage : BaseEntity
{
    /// <summary>
    /// Reference to the template that was used
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Reference to the settlement created from this template
    /// </summary>
    public Guid SettlementId { get; set; }

    /// <summary>
    /// User who applied the template
    /// </summary>
    public string AppliedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the template was applied
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the template
    /// </summary>
    public SettlementTemplate? Template { get; set; }
}
