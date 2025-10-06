using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

/// <summary>
/// 库存分配结果DTO
/// </summary>
public class InventoryAllocationResult
{
    public Guid AllocationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity AllocatedQuantity { get; set; } = new(0, QuantityUnit.MT);
    public Quantity RemainingRequirement { get; set; } = new(0, QuantityUnit.MT);
    public Quantity TotalAllocated { get; set; } = new(0, QuantityUnit.MT);
    public bool IsFullyAllocated { get; set; }
    public bool IsSuccessful { get; set; }
    public string AllocationStrategy { get; set; } = string.Empty;
    public DateTime AllocationTime { get; set; }
    public List<InventoryAllocation> Allocations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 库存分配请求DTO
/// </summary>
public class InventoryAllocationRequest
{
    public Guid ProductId { get; set; }
    public Quantity RequiredQuantity { get; set; } = new(0, QuantityUnit.MT);
    public List<Guid> PreferredLocationIds { get; set; } = new();
    public InventoryAllocationStrategy Strategy { get; set; } = InventoryAllocationStrategy.FIFO;
    public DateTime RequiredByDate { get; set; }
    public string? ContractReference { get; set; }
    public bool AllowPartialAllocation { get; set; } = true;
    public decimal MaximumCost { get; set; }
}

/// <summary>
/// 库存分配策略枚举
/// </summary>
public enum InventoryAllocationStrategy
{
    FIFO = 1,           // 先进先出
    LIFO = 2,           // 后进先出
    LowestCost = 3,     // 最低成本
    NearestLocation = 4, // 最近位置
    OptimalMix = 5      // 最优组合
}

/// <summary>
/// 库存可用性检查结果
/// </summary>
public class InventoryAvailabilityResult
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Quantity TotalAvailable { get; set; } = new(0, QuantityUnit.MT);
    public Quantity RequestedQuantity { get; set; } = new(0, QuantityUnit.MT);
    public bool IsAvailable { get; set; }
    public List<LocationAvailability> LocationBreakdown { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
}

/// <summary>
/// 位置可用性详情
/// </summary>
public class LocationAvailability
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public Quantity Available { get; set; } = new(0, QuantityUnit.MT);
    public Quantity Reserved { get; set; } = new(0, QuantityUnit.MT);
    public decimal AverageCost { get; set; }
    public DateTime LastUpdated { get; set; }
}