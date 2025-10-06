using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;

namespace OilTrading.Application.DTOs;

public class ContractCreationData
{
    public string ContractNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid TraderId { get; set; }
    public Quantity ContractQuantity { get; set; } = null!;
    public PriceFormula PriceFormula { get; set; } = null!;
    public DeliveryTerms DeliveryTerms { get; set; }
    public SettlementType SettlementType { get; set; }
    public DateTime DeliveryStartDate { get; set; }
    public DateTime DeliveryEndDate { get; set; }
    public DateTime? PricingPeriodStart { get; set; }
    public DateTime? PricingPeriodEnd { get; set; }
    public Money? Premium { get; set; }
    public Money? Discount { get; set; }
    public string CreatedBy { get; set; } = "System";
    public string? Notes { get; set; }
    public Dictionary<string, object> CustomProperties { get; set; } = new();
    
    // Common properties used by transaction services
    public Guid? SupplierId { get; set; }  // For purchase contracts
    public Guid? CustomerId { get; set; }  // For sales contracts
    public Quantity? Quantity { get; set; }  // Alternative name for ContractQuantity
    public DateTime? LaycanStart { get; set; }  // Alternative name for DeliveryStartDate
    public DateTime? LaycanEnd { get; set; }    // Alternative name for DeliveryEndDate
}

public class PurchaseContractCreationData : ContractCreationData
{
    public new Guid SupplierId { get; set; } // Override base class to make non-nullable
    public decimal TonBarrelRatio { get; set; } = 7.6m;
    public Guid? PriceBenchmarkId { get; set; }
    public string? ExternalContractNumber { get; set; }
}

public class SalesContractCreationData : ContractCreationData
{
    public new Guid CustomerId { get; set; } // Override base class to make non-nullable
}

public class ContractUpdateData
{
    public Guid ContractId { get; set; }
    public string? UpdateReason { get; set; }
    public string UpdatedBy { get; set; } = "System";
    public Dictionary<string, object> UpdatedFields { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ContractApprovalData
{
    public Guid ContractId { get; set; }
    public bool IsApproved { get; set; }
    public string? Comments { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovalDate { get; set; } = DateTime.UtcNow;
    public UserRole ApproverRole { get; set; }
    public List<string> RequiredChecks { get; set; } = new();
}

public class ContractCancellationData
{
    public Guid ContractId { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public string CancelledBy { get; set; } = string.Empty;
    public DateTime CancellationDate { get; set; } = DateTime.UtcNow;
    public Money? CancellationFee { get; set; }
    public bool NotifyCounterparty { get; set; } = true;
}