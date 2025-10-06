using OilTrading.Core.Entities;

namespace OilTrading.Application.DTOs;

public class TradingPartnerDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public TradingPartnerType PartnerType { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    
    // Credit Management
    public decimal CreditLimit { get; set; }
    public DateTime CreditLimitValidUntil { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal CreditUtilization { get; set; }
    
    // Statistics
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    
    // Financial Reports
    public List<FinancialReportDto> FinancialReports { get; set; } = [];
    
    // Calculated fields
    public bool IsCreditExceeded => CurrentExposure > CreditLimit;
    public bool IsCreditExpired => CreditLimitValidUntil < DateTime.UtcNow;
}

public class CreateTradingPartnerDto
{
    public string CompanyName { get; set; } = string.Empty;
    public TradingPartnerType PartnerType { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreditLimitValidUntil { get; set; }
    public int PaymentTermDays { get; set; } = 30;
    public List<CreateFinancialReportDto> FinancialReports { get; set; } = [];
}

public class UpdateTradingPartnerDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal CreditLimit { get; set; }
    public DateTime CreditLimitValidUntil { get; set; }
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public List<UpdateFinancialReportDto> FinancialReports { get; set; } = [];
}

public class TradingPartnerListDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public TradingPartnerType PartnerType { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentExposure { get; set; }
    public decimal CreditUtilization { get; set; }
    public bool IsActive { get; set; }
    public bool IsCreditExceeded { get; set; }
}

public class TradingPartnerImportDto
{
    public string CompanyName { get; set; } = string.Empty;
    public TradingPartnerType PartnerType { get; set; }
    public decimal CreditLimit { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}