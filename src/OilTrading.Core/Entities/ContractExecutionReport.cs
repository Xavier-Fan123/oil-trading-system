using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

/// <summary>
/// Contract Execution Report entity for tracking contract lifecycle and performance metrics
/// </summary>
public class ContractExecutionReport : BaseEntity
{
    private ContractExecutionReport() { } // For EF Core

    public ContractExecutionReport(
        Guid contractId,
        string contractNumber,
        string contractType, // "Purchase" or "Sales"
        DateTime reportGeneratedDate)
    {
        ContractId = contractId;
        ContractNumber = contractNumber;
        ContractType = contractType;
        ReportGeneratedDate = reportGeneratedDate;
    }

    // Contract Identification
    public Guid ContractId { get; private set; }
    public string ContractNumber { get; private set; } = string.Empty;
    public string ContractType { get; private set; } = string.Empty; // "Purchase" or "Sales"
    public DateTime ReportGeneratedDate { get; private set; }

    // Contract Basic Information
    public Guid? TradingPartnerId { get; private set; }
    public string TradingPartnerName { get; private set; } = string.Empty;
    public Guid? ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string ContractStatus { get; private set; } = string.Empty;

    // Execution Metrics
    public decimal? ContractValue { get; private set; }
    public string? Currency { get; private set; }
    public decimal? ExecutedQuantity { get; private set; }
    public decimal ExecutionPercentage { get; private set; } // 0-100

    // Dates
    public DateTime? CreatedDate { get; private set; }
    public DateTime? ActivatedDate { get; private set; }
    public DateTime? LaycanStart { get; private set; }
    public DateTime? LaycanEnd { get; private set; }
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public DateTime? SettlementDate { get; private set; }
    public DateTime? CompletionDate { get; private set; }

    // Settlement Information
    public int SettlementCount { get; private set; }
    public decimal TotalSettledAmount { get; private set; }
    public decimal PaidSettledAmount { get; private set; }
    public decimal UnpaidSettledAmount { get; private set; }
    public string PaymentStatus { get; private set; } = string.Empty;

    // Shipping/Logistics Information
    public int ShippingOperationCount { get; private set; }
    public string? LoadPort { get; private set; }
    public string? DischargePort { get; private set; }
    public string? DeliveryTerms { get; private set; }

    // Performance Indicators
    public int DaysToActivation { get; private set; }
    public int DaysToCompletion { get; private set; }
    public bool IsOnSchedule { get; private set; }
    public string ExecutionStatus { get; private set; } = string.Empty; // "OnTrack", "Delayed", "Completed", "Cancelled"

    // Pricing Information
    public decimal? BenchmarkPrice { get; private set; }
    public decimal? AdjustmentPrice { get; private set; }
    public decimal? FinalPrice { get; private set; }
    public decimal? PricingFormula { get; private set; }
    public bool IsPriceFinalized { get; private set; }

    // Risk & Compliance
    public bool HasRiskViolations { get; private set; }
    public string? RiskNotes { get; private set; }
    public bool IsCompliant { get; private set; }

    // Additional Notes
    public string? Notes { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    // Business Methods
    public void UpdateExecutionMetrics(
        decimal executedQuantity,
        string paymentStatus,
        int shippingOperationCount,
        decimal totalSettledAmount,
        decimal paidSettledAmount)
    {
        ExecutedQuantity = executedQuantity;
        PaymentStatus = paymentStatus;
        ShippingOperationCount = shippingOperationCount;
        TotalSettledAmount = totalSettledAmount;
        PaidSettledAmount = paidSettledAmount;
        UnpaidSettledAmount = totalSettledAmount - paidSettledAmount;

        if (Quantity > 0)
        {
            ExecutionPercentage = (executedQuantity / Quantity) * 100;
        }

        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdateSettlementInfo(
        int settlementCount,
        decimal totalSettledAmount,
        decimal paidSettledAmount)
    {
        SettlementCount = settlementCount;
        TotalSettledAmount = totalSettledAmount;
        PaidSettledAmount = paidSettledAmount;
        UnpaidSettledAmount = totalSettledAmount - paidSettledAmount;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdateDeliveryInfo(
        DateTime? actualDeliveryDate,
        string executionStatus,
        bool isOnSchedule)
    {
        ActualDeliveryDate = actualDeliveryDate;
        ExecutionStatus = executionStatus;
        IsOnSchedule = isOnSchedule;

        if (actualDeliveryDate.HasValue && CreatedDate.HasValue)
        {
            DaysToCompletion = (int)(actualDeliveryDate.Value - CreatedDate.Value).TotalDays;
        }

        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdatePricingInfo(
        decimal? benchmarkPrice,
        decimal? adjustmentPrice,
        decimal? finalPrice,
        bool isPriceFinalized)
    {
        BenchmarkPrice = benchmarkPrice;
        AdjustmentPrice = adjustmentPrice;
        FinalPrice = finalPrice;
        IsPriceFinalized = isPriceFinalized;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdateRiskStatus(bool hasRiskViolations, string? riskNotes, bool isCompliant)
    {
        HasRiskViolations = hasRiskViolations;
        RiskNotes = riskNotes;
        IsCompliant = isCompliant;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void AddNote(string note)
    {
        Notes = string.IsNullOrEmpty(Notes) ? note : $"{Notes}\n{note}";
        LastUpdatedDate = DateTime.UtcNow;
    }

    public int CalculateDaysToActivation()
    {
        if (CreatedDate.HasValue && ActivatedDate.HasValue)
        {
            DaysToActivation = (int)(ActivatedDate.Value - CreatedDate.Value).TotalDays;
            return DaysToActivation;
        }
        return 0;
    }

    public void SetBasicInfo(
        Guid? tradingPartnerId,
        string tradingPartnerName,
        Guid? productId,
        string productName,
        decimal quantity,
        string quantityUnit,
        string status,
        DateTime? createdDate,
        DateTime? activatedDate,
        DateTime? laycanStart,
        DateTime? laycanEnd,
        decimal? contractValue,
        string? currency,
        string? loadPort,
        string? dischargePort,
        string? deliveryTerms)
    {
        TradingPartnerId = tradingPartnerId;
        TradingPartnerName = tradingPartnerName;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        QuantityUnit = quantityUnit;
        ContractStatus = status;
        CreatedDate = createdDate;
        ActivatedDate = activatedDate;
        LaycanStart = laycanStart;
        LaycanEnd = laycanEnd;
        ContractValue = contractValue;
        Currency = currency;
        LoadPort = loadPort;
        DischargePort = dischargePort;
        DeliveryTerms = deliveryTerms;

        if (createdDate.HasValue && activatedDate.HasValue)
        {
            DaysToActivation = (int)(activatedDate.Value - createdDate.Value).TotalDays;
        }

        LastUpdatedDate = DateTime.UtcNow;
    }
}
