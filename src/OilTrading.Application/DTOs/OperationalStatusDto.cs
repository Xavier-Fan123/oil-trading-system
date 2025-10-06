namespace OilTrading.Application.DTOs;

public class OperationalStatusDto
{
    public int ActiveShipments { get; set; }
    public int PendingDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    
    public int ContractsAwaitingExecution { get; set; }
    public int ContractsInLaycan { get; set; }
    
    public List<LaycanDto> UpcomingLaycans { get; set; } = new();
    
    public SystemHealthDto SystemHealth { get; set; } = new();
    public decimal CacheHitRatio { get; set; }
    
    public DateTime LastDataRefresh { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class LaycanDto
{
    public string ContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public string Product { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class SystemHealthDto
{
    public string DatabaseStatus { get; set; } = string.Empty;
    public string CacheStatus { get; set; } = string.Empty;
    public string MarketDataStatus { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty;
}

public class AlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class KpiSummaryDto
{
    public decimal TotalExposure { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal VaR95 { get; set; }
    public int PortfolioCount { get; set; }
    
    public decimal ExposureUtilization { get; set; }
    public decimal RiskUtilization { get; set; }
    
    public DateTime CalculatedAt { get; set; }
}