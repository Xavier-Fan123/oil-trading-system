using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// Trade Group Tag Association Entity
/// Purpose: Establishes many-to-many relationship between TradeGroup and Tag for strategy classification and risk management
/// </summary>
public class TradeGroupTag : BaseEntity
{
    private TradeGroupTag() { } // For EF Core

    public TradeGroupTag(Guid tradeGroupId, Guid tagId, string assignedBy = "System")
    {
        TradeGroupId = tradeGroupId;
        TagId = tagId;
        AssignedAt = DateTime.UtcNow;
        AssignedBy = assignedBy;
        IsActive = true;
    }

    /// <summary>
    /// Trade Group ID
    /// </summary>
    public Guid TradeGroupId { get; private set; }

    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// Assignment timestamp
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Assigned by user
    /// </summary>
    public string AssignedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Is this tag assignment active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Optional notes for this tag assignment
    /// </summary>
    public string? Notes { get; private set; }

    // Navigation Properties
    /// <summary>
    /// Associated trade group
    /// </summary>
    public TradeGroup TradeGroup { get; private set; } = null!;

    /// <summary>
    /// Associated tag
    /// </summary>
    public Tag Tag { get; private set; } = null!;

    // Business Methods

    /// <summary>
    /// Deactivate the tag assignment
    /// </summary>
    /// <param name="deactivatedBy">User who deactivated</param>
    /// <param name="reason">Reason for deactivation</param>
    public void Deactivate(string deactivatedBy = "System", string? reason = null)
    {
        if (!IsActive)
            throw new DomainException("Tag assignment is already inactive");

        IsActive = false;
        Notes = string.IsNullOrEmpty(reason) ? Notes : $"{Notes} | Deactivated: {reason}";
        SetUpdatedBy(deactivatedBy);
    }

    /// <summary>
    /// Reactivate the tag assignment
    /// </summary>
    /// <param name="reactivatedBy">User who reactivated</param>
    /// <param name="reason">Reason for reactivation</param>
    public void Reactivate(string reactivatedBy = "System", string? reason = null)
    {
        if (IsActive)
            throw new DomainException("Tag assignment is already active");

        IsActive = true;
        Notes = string.IsNullOrEmpty(reason) ? Notes : $"{Notes} | Reactivated: {reason}";
        SetUpdatedBy(reactivatedBy);
    }

    /// <summary>
    /// Update notes
    /// </summary>
    /// <param name="notes">New notes</param>
    /// <param name="updatedBy">User who updated</param>
    public void UpdateNotes(string? notes, string updatedBy = "System")
    {
        Notes = notes?.Trim();
        SetUpdatedBy(updatedBy);
    }

    /// <summary>
    /// Check if the tag assignment is valid
    /// </summary>
    /// <returns>Whether the assignment is valid</returns>
    public bool IsValidAssignment()
    {
        return TradeGroupId != Guid.Empty && 
               TagId != Guid.Empty && 
               IsActive &&
               !string.IsNullOrWhiteSpace(AssignedBy);
    }
}