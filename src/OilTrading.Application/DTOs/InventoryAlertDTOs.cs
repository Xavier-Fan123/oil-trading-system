using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.DTOs;

/// <summary>
/// 库存警报DTO
/// </summary>
public class InventoryAlert
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public InventoryAlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public InventoryAlertSeverity Severity { get; set; }
    public Quantity CurrentQuantity { get; set; } = new(0, QuantityUnit.MT);
    public Quantity ThresholdQuantity { get; set; } = new(0, QuantityUnit.MT);
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public string? AcknowledgmentNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 库存警报类型枚举
/// </summary>
public enum InventoryAlertType
{
    LowStock = 1,           // 低库存
    HighStock = 2,          // 高库存
    ZeroStock = 3,          // 零库存
    ExpiringStock = 4,      // 即将过期库存
    OverCapacity = 5,       // 超出容量
    QualityIssue = 6,       // 质量问题
    ReorderPoint = 7,       // 再订货点
    SafetyStockBreach = 8   // 安全库存不足
}

/// <summary>
/// 库存警报严重级别
/// </summary>
public enum InventoryAlertSeverity
{
    Low = 1,        // 低
    Medium = 2,     // 中
    High = 3,       // 高
    Critical = 4    // 严重
}

/// <summary>
/// 库存警报配置
/// </summary>
public class InventoryAlertConfiguration
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public InventoryAlertType AlertType { get; set; }
    public Quantity LowStockThreshold { get; set; } = new(0, QuantityUnit.MT);
    public Quantity HighStockThreshold { get; set; } = new(0, QuantityUnit.MT);
    public Quantity SafetyStockLevel { get; set; } = new(0, QuantityUnit.MT);
    public Quantity ReorderPoint { get; set; } = new(0, QuantityUnit.MT);
    public bool IsEnabled { get; set; } = true;
    public List<string> NotificationRecipients { get; set; } = new();
    public Dictionary<string, object> AlertSettings { get; set; } = new();
}

/// <summary>
/// 库存警报统计
/// </summary>
public class InventoryAlertSummary
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }
    public int AcknowledgedAlerts { get; set; }
    public int UnacknowledgedAlerts { get; set; }
    public Dictionary<InventoryAlertType, int> AlertsByType { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// 库存警报处理请求
/// </summary>
public class ProcessInventoryAlertRequest
{
    public Guid AlertId { get; set; }
    public Guid ProcessedBy { get; set; }
    public string Action { get; set; } = string.Empty; // Acknowledge, Resolve, Escalate
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, object> ActionParameters { get; set; } = new();
}