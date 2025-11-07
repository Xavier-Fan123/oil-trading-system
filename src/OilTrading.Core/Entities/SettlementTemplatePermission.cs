namespace OilTrading.Core.Entities;

/// <summary>
/// Manages permissions and sharing for settlement templates
/// Allows templates to be shared with specific users or teams
/// </summary>
public class SettlementTemplatePermission : BaseEntity
{
    /// <summary>
    /// Reference to the template being shared
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// User ID who has permission to access this template
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Permission level for this user
    /// </summary>
    public TemplatePermissionLevel PermissionLevel { get; set; } = TemplatePermissionLevel.View;

    /// <summary>
    /// When this permission was granted
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who granted this permission
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for the template
    /// </summary>
    public SettlementTemplate? Template { get; set; }
}

/// <summary>
/// Permission levels for settlement template sharing
/// </summary>
public enum TemplatePermissionLevel
{
    /// <summary>
    /// Can view template but not use it
    /// </summary>
    View = 0,

    /// <summary>
    /// Can view and use template to create settlements
    /// </summary>
    Use = 1,

    /// <summary>
    /// Can view, use, and edit template
    /// </summary>
    Edit = 2,

    /// <summary>
    /// Full permissions - can view, use, edit, and delete
    /// </summary>
    Admin = 3
}
