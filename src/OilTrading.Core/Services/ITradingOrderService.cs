using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Services;

public interface ITradingOrderService
{
    Task<TradingOrder> CreateOrderAsync(CreateTradingOrderRequest request);
    Task<TradingOrder> UpdateOrderAsync(int orderId, UpdateTradingOrderRequest request);
    Task<bool> CancelOrderAsync(int orderId, string cancelledBy, string reason);
    Task<TradingOrder?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<TradingOrder>> GetOrdersAsync(TradingOrderFilter filter);
    
    // Order execution
    Task<TradingOrderExecution> ExecuteOrderAsync(int orderId, ExecuteTradingOrderRequest request);
    Task<TradingOrderExecution> PartialExecuteOrderAsync(int orderId, PartialExecuteTradingOrderRequest request);
    Task<IEnumerable<TradingOrderExecution>> GetOrderExecutionsAsync(int orderId);
    
    // Order management
    Task<bool> SubmitOrderForApprovalAsync(int orderId, string submittedBy);
    Task<OrderValidationResult> ValidateOrderAsync(int orderId);
    Task<IEnumerable<TradingOrder>> GetOrdersRequiringApprovalAsync(string approverRole);
    Task<IEnumerable<TradingOrder>> GetExecutableOrdersAsync();
    
    // Reporting
    Task<TradingOrderSummary> GetOrderSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<TradingOrderMetrics>> GetTradingMetricsAsync(string period);
}

public class CreateTradingOrderRequest
{
    public TradingOrderType OrderType { get; set; }
    public int TraderId { get; set; }
    public int TradingPartnerId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.MT;
    public decimal? Price { get; set; }
    public string? Currency { get; set; } = "USD";
    public TradingOrderPriceType PriceType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DeliveryTerms DeliveryTerms { get; set; }
    public string? DeliveryLocation { get; set; }
    public DateTime? DeliveryStart { get; set; }
    public DateTime? DeliveryEnd { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTradingOrderRequest
{
    public decimal? Quantity { get; set; }
    public QuantityUnit? QuantityUnit { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DeliveryLocation { get; set; }
    public DateTime? DeliveryStart { get; set; }
    public DateTime? DeliveryEnd { get; set; }
    public string? Notes { get; set; }
}

public class ExecuteTradingOrderRequest
{
    public decimal? ExecutedQuantity { get; set; }
    public decimal ExecutedPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string ExecutedBy { get; set; } = string.Empty;
    public string? ExecutionMethod { get; set; }
    public string? CounterpartyReference { get; set; }
    public string? Notes { get; set; }
}

public class PartialExecuteTradingOrderRequest
{
    public decimal ExecutedQuantity { get; set; }
    public decimal ExecutedPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string ExecutedBy { get; set; } = string.Empty;
    public string? ExecutionMethod { get; set; }
    public string? CounterpartyReference { get; set; }
    public string? Notes { get; set; }
}

public class TradingOrderFilter
{
    public TradingOrderStatus? Status { get; set; }
    public TradingOrderType? OrderType { get; set; }
    public int? TraderId { get; set; }
    public int? TradingPartnerId { get; set; }
    public int? ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class OrderValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();
    public bool RiskCheckPassed { get; set; }
    public bool CreditCheckPassed { get; set; }
    public bool ComplianceCheckPassed { get; set; }
    public string? ValidationSummary { get; set; }
}

public class TradingOrderSummary
{
    public DateTime AsOf { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ExecutedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal ExecutedVolume { get; set; }
    public decimal TotalValue { get; set; }
    public decimal ExecutedValue { get; set; }
    public IEnumerable<OrderSummaryByProduct> OrdersByProduct { get; set; } = new List<OrderSummaryByProduct>();
    public IEnumerable<OrderSummaryByPartner> OrdersByPartner { get; set; } = new List<OrderSummaryByPartner>();
}

public class OrderSummaryByProduct
{
    public string ProductName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalValue { get; set; }
}

public class OrderSummaryByPartner
{
    public string PartnerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalValue { get; set; }
}

public class TradingOrderMetrics
{
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOrders { get; set; }
    public decimal ExecutionRate { get; set; }
    public decimal AverageExecutionTime { get; set; } // Hours
    public decimal TotalVolume { get; set; }
    public decimal TotalValue { get; set; }
}