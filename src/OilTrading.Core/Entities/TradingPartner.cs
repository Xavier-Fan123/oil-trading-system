using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public class TradingPartner : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public TradingPartnerType Type { get; set; }
    public TradingPartnerType PartnerType { get; set; }
    public string? ContactPerson { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string ContactInfo { get; set; } = string.Empty;
    
    // Credit Management
    public decimal CreditLimit { get; set; }
    public DateTime CreditLimitValidUntil { get; set; }
    public int PaymentTermDays { get; set; } = 30;
    public decimal CurrentExposure { get; set; }
    public string CreditRating { get; set; } = string.Empty;
    
    // Statistics
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public string? BlockReason { get; set; }
    
    // Navigation Properties
    public ICollection<PurchaseContract> PurchaseContracts { get; set; } = [];
    public ICollection<SalesContract> SalesContracts { get; set; } = [];
    public ICollection<PhysicalContract> PhysicalContracts { get; set; } = new List<PhysicalContract>();
    public ICollection<FinancialReport> FinancialReports { get; set; } = [];
    public ICollection<PaymentRiskAlert> PaymentRiskAlerts { get; set; } = [];
}

public enum TradingPartnerType
{
    Supplier = 1,
    Customer = 2,
    Both = 3,
    Trader = 4,
    EndUser = 5
}