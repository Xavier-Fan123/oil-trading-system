using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

/// <summary>
/// 库存转移请求DTO
/// </summary>
public class InventoryTransferRequest
{
    public Guid ProductId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public Quantity TransferQuantity { get; set; } = new(0, QuantityUnit.MT);
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime RequestedDate { get; set; }
    public Guid RequestedBy { get; set; }
    public Money? TransferCost { get; set; }
    public Dictionary<string, object> TransferMetadata { get; set; } = new();
}

/// <summary>
/// 库存转移结果DTO
/// </summary>
public class InventoryTransferResult
{
    public Guid TransferId { get; set; }
    public bool IsSuccessful { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Quantity ActualTransferredQuantity { get; set; } = new(0, QuantityUnit.MT);
    public DateTime CompletedAt { get; set; }
    public Dictionary<string, object> ResultMetadata { get; set; } = new();
}

/// <summary>
/// 库存收货请求DTO
/// </summary>
public class InventoryReceiptRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity ReceivedQuantity { get; set; } = new(0, QuantityUnit.MT);
    public Money UnitCost { get; set; } = new(0, "USD");
    public string? SupplierReference { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime ReceivedDate { get; set; }
    public Guid ReceivedBy { get; set; }
    public string QualityNotes { get; set; } = string.Empty;
    public Dictionary<string, object> QualityMetrics { get; set; } = new();
}

/// <summary>
/// 库存出货请求DTO
/// </summary>
public class InventoryDeliveryRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity DeliveryQuantity { get; set; } = new(0, QuantityUnit.MT);
    public string? CustomerReference { get; set; }
    public string? DeliveryOrderNumber { get; set; }
    public DateTime DeliveryDate { get; set; }
    public Guid DeliveredBy { get; set; }
    public string DeliveryNotes { get; set; } = string.Empty;
    public Dictionary<string, object> DeliveryMetadata { get; set; } = new();
}

/// <summary>
/// 库存调整请求DTO
/// </summary>
public class InventoryAdjustmentRequest
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public Quantity AdjustmentQuantity { get; set; } = new(0, QuantityUnit.MT); // 正数为增加，负数为减少
    public InventoryAdjustmentReason Reason { get; set; }
    public string ReasonDescription { get; set; } = string.Empty;
    public Guid AdjustedBy { get; set; }
    public string? ApprovalReference { get; set; }
    public Dictionary<string, object> AdjustmentMetadata { get; set; } = new();
}

/// <summary>
/// 库存调整原因枚举
/// </summary>
public enum InventoryAdjustmentReason
{
    PhysicalCount = 1,      // 实物盘点
    QualityIssue = 2,       // 质量问题
    Damage = 3,             // 损坏
    Theft = 4,              // 盗窃
    SystemError = 5,        // 系统错误
    Evaporation = 6,        // 蒸发损失
    Contamination = 7,      // 污染
    Other = 99              // 其他
}

/// <summary>
/// 库存历史记录DTO
/// </summary>
public class InventoryHistory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty; // Receipt, Delivery, Transfer, Adjustment
    public Quantity QuantityBefore { get; set; } = new(0, QuantityUnit.MT);
    public Quantity QuantityChange { get; set; } = new(0, QuantityUnit.MT);
    public Quantity QuantityAfter { get; set; } = new(0, QuantityUnit.MT);
    public string? ReferenceNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public Guid TransactionBy { get; set; }
    public string TransactionByName { get; set; } = string.Empty;
    public Dictionary<string, object> TransactionMetadata { get; set; } = new();
}