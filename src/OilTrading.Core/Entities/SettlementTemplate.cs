namespace OilTrading.Core.Entities;

/// <summary>
/// Represents a reusable template for settlement creation
/// Allows users to save common settlement configurations for quick-create functionality
/// </summary>
public class SettlementTemplate : BaseEntity
{
    /// <summary>
    /// Template name (e.g., "Standard TT NET30")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description (e.g., "Telegraphic transfer with 30-day credit")
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who created the template
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Current version number - incremented on each update
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Whether the template is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the template is shared with all users (public) or private to creator
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// JSON-serialized template configuration including:
    /// - documentType: "BillOfLading"
    /// - paymentTerms: "NET 30"
    /// - creditPeriodDays: 30
    /// - settlementType: "TT"
    /// - prepaymentPercentage: 0
    /// - defaultCharges: array of charge objects
    /// - deliveryTerms: "FOB"
    /// </summary>
    public string TemplateConfiguration { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this template has been used
    /// </summary>
    public int TimesUsed { get; set; } = 0;

    /// <summary>
    /// Last time this template was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Navigation property for template usage tracking
    /// </summary>
    public List<SettlementTemplateUsage> Usages { get; set; } = new();

    /// <summary>
    /// Navigation property for template permissions/sharing
    /// </summary>
    public List<SettlementTemplatePermission> Permissions { get; set; } = new();

    /// <summary>
    /// Create a new version of this template
    /// </summary>
    public void CreateNewVersion()
    {
        Version++;
        SetUpdatedBy(CreatedBy ?? "System");
    }

    /// <summary>
    /// Record usage of this template
    /// </summary>
    public void RecordUsage()
    {
        TimesUsed++;
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark template as inactive
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        SetUpdatedBy(deactivatedBy);
    }

    /// <summary>
    /// Mark template as active
    /// </summary>
    public void Activate(string activatedBy)
    {
        IsActive = true;
        SetUpdatedBy(activatedBy);
    }

    /// <summary>
    /// Share template with all users
    /// </summary>
    public void MakePublic(string changedBy)
    {
        IsPublic = true;
        SetUpdatedBy(changedBy);
    }

    /// <summary>
    /// Make template private to creator only
    /// </summary>
    public void MakePrivate(string changedBy)
    {
        IsPublic = false;
        SetUpdatedBy(changedBy);
    }
}
