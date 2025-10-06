using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// 交易组标签关联实体 - Trade Group Tag Association Entity
/// Purpose: 建立TradeGroup和Tag之间的多对多关系，用于策略分类和风险管理
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
    /// 交易组ID - Trade Group ID
    /// </summary>
    public Guid TradeGroupId { get; private set; }

    /// <summary>
    /// 标签ID - Tag ID
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// 分配时间 - Assignment timestamp
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// 分配人 - Assigned by user
    /// </summary>
    public string AssignedBy { get; private set; } = string.Empty;

    /// <summary>
    /// 是否活跃 - Is this tag assignment active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 备注 - Optional notes for this tag assignment
    /// </summary>
    public string? Notes { get; private set; }

    // Navigation Properties
    /// <summary>
    /// 关联的交易组 - Associated trade group
    /// </summary>
    public TradeGroup TradeGroup { get; private set; } = null!;

    /// <summary>
    /// 关联的标签 - Associated tag
    /// </summary>
    public Tag Tag { get; private set; } = null!;

    // Business Methods

    /// <summary>
    /// 停用标签分配 - Deactivate the tag assignment
    /// </summary>
    /// <param name="deactivatedBy">停用操作执行人</param>
    /// <param name="reason">停用原因</param>
    public void Deactivate(string deactivatedBy = "System", string? reason = null)
    {
        if (!IsActive)
            throw new DomainException("Tag assignment is already inactive");

        IsActive = false;
        Notes = string.IsNullOrEmpty(reason) ? Notes : $"{Notes} | Deactivated: {reason}";
        SetUpdatedBy(deactivatedBy);
    }

    /// <summary>
    /// 重新激活标签分配 - Reactivate the tag assignment
    /// </summary>
    /// <param name="reactivatedBy">重新激活操作执行人</param>
    /// <param name="reason">重新激活原因</param>
    public void Reactivate(string reactivatedBy = "System", string? reason = null)
    {
        if (IsActive)
            throw new DomainException("Tag assignment is already active");

        IsActive = true;
        Notes = string.IsNullOrEmpty(reason) ? Notes : $"{Notes} | Reactivated: {reason}";
        SetUpdatedBy(reactivatedBy);
    }

    /// <summary>
    /// 更新备注 - Update notes
    /// </summary>
    /// <param name="notes">新的备注</param>
    /// <param name="updatedBy">更新操作执行人</param>
    public void UpdateNotes(string? notes, string updatedBy = "System")
    {
        Notes = notes?.Trim();
        SetUpdatedBy(updatedBy);
    }

    /// <summary>
    /// 检查标签分配的有效性 - Check if the tag assignment is valid
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValidAssignment()
    {
        return TradeGroupId != Guid.Empty && 
               TagId != Guid.Empty && 
               IsActive &&
               !string.IsNullOrWhiteSpace(AssignedBy);
    }
}