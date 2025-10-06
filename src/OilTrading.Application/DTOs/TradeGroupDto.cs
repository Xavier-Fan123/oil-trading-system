namespace OilTrading.Application.DTOs;

/// <summary>
/// 交易组详细信息DTO - Trade Group Detailed DTO
/// </summary>
public class TradeGroupDto
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int StrategyType { get; set; }
    public string StrategyTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? ExpectedRiskLevel { get; set; }
    public string? ExpectedRiskLevelName { get; set; }
    public decimal? MaxAllowedLoss { get; set; }
    public decimal? TargetProfit { get; set; }

    // 关联的合约信息
    public ICollection<ContractSummaryDto> PurchaseContracts { get; set; } = new List<ContractSummaryDto>();
    public ICollection<ContractSummaryDto> SalesContracts { get; set; } = new List<ContractSummaryDto>();
    public ICollection<ContractSummaryDto> PaperContracts { get; set; } = new List<ContractSummaryDto>();

    // 关联的标签
    public ICollection<TagSummaryDto> Tags { get; set; } = new List<TagSummaryDto>();

    // 风险和收益指标
    public decimal TotalValue { get; set; }
    public decimal NetPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public int TotalContracts { get; set; }
    public int ActiveContracts { get; set; }

    // 审计信息
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 交易组汇总信息DTO - Trade Group Summary DTO
/// </summary>
public class TradeGroupSummaryDto
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int StrategyType { get; set; }
    public string StrategyTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? ExpectedRiskLevel { get; set; }
    public string? ExpectedRiskLevelName { get; set; }
    public decimal? MaxAllowedLoss { get; set; }
    public decimal? TargetProfit { get; set; }

    // 统计信息
    public int TotalContracts { get; set; }
    public int ActiveContracts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal NetPnL { get; set; }
    public int TagCount { get; set; }

    // 审计信息
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 合约汇总信息DTO - Contract Summary DTO for Trade Groups
/// </summary>
public class ContractSummaryDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public decimal? ContractValue { get; set; }
    public string? Currency { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}


/// <summary>
/// 更新交易组DTO - Update Trade Group DTO
/// </summary>
public class UpdateTradeGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ExpectedRiskLevel { get; set; }
    public decimal? MaxAllowedLoss { get; set; }
    public decimal? TargetProfit { get; set; }
}

/// <summary>
/// 分配合约到交易组DTO - Assign Contract to Trade Group DTO
/// </summary>
public class AssignContractToTradeGroupDto
{
    public Guid TradeGroupId { get; set; }
    public Guid ContractId { get; set; }
    public string ContractType { get; set; } = string.Empty; // "PurchaseContract", "SalesContract", "PaperContract"
}

/// <summary>
/// 交易组标签管理DTO - Trade Group Tag Management DTO
/// </summary>
public class TradeGroupTagManagementDto
{
    public Guid TradeGroupId { get; set; }
    public List<Guid> TagIds { get; set; } = new List<Guid>();
    public string? Notes { get; set; }
}