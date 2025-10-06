using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// 合同标签关联实体 - Contract-Tag Association Entity
/// Purpose: 建立合同与标签的多对多关系，支持合同的标签化管理
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
    /// 合同ID - Contract ID
    /// </summary>
    public Guid ContractId { get; private set; }

    /// <summary>
    /// 合同类型 - Contract type (PurchaseContract, SalesContract, etc.)
    /// </summary>
    public string ContractType { get; private set; } = string.Empty;

    /// <summary>
    /// 标签ID - Tag ID
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// 备注 - Notes for this tag assignment
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// 分配者 - Assigned by user
    /// </summary>
    public string AssignedBy { get; private set; } = string.Empty;

    /// <summary>
    /// 分配时间 - Assignment timestamp
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