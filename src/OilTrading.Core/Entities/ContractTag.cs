using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// Contract-Tag Association Entity
/// Purpose: Establishes many-to-many relationship between contracts and tags for contract tag management
/// </summary>
public class ContractTag : BaseEntity
{
    private ContractTag() { } // For EF Core

    public ContractTag(Guid contractId, string contractType, Guid tagId, string? notes = null, string assignedBy = "System")
    {
        ContractId = contractId;
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        TagId = tagId;
        Notes = notes?.Trim();
        AssignedBy = assignedBy;
        AssignedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Contract ID
    /// </summary>
    public Guid ContractId { get; private set; }

    /// <summary>
    /// Contract type (PurchaseContract, SalesContract, etc.)
    /// </summary>
    public string ContractType { get; private set; } = string.Empty;

    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// Notes for this tag assignment
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Assigned by user
    /// </summary>
    public string AssignedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Assignment timestamp
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    // Navigation Properties
    public Tag Tag { get; private set; } = null!;

    // Business Methods
    public void UpdateNotes(string? notes, string updatedBy = "System")
    {
        Notes = notes?.Trim();
        SetUpdatedBy(updatedBy);
    }

    public bool IsForContract(Guid contractId, string contractType)
    {
        return ContractId == contractId && 
               string.Equals(ContractType, contractType, StringComparison.OrdinalIgnoreCase);
    }
}