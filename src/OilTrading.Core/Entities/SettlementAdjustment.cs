using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class SettlementAdjustment : BaseEntity
{
    private SettlementAdjustment() { } // For EF Core

    public SettlementAdjustment(
        SettlementAdjustmentType type,
        Money amount,
        string reason,
        Guid adjustedBy,
        string? reference = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Adjustment reason cannot be empty");

        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Reason = reason.Trim();
        AdjustmentDate = DateTime.UtcNow;
        AdjustedBy = adjustedBy;
        Reference = reference?.Trim();
    }

    public SettlementAdjustmentType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;
    public DateTime AdjustmentDate { get; private set; }
    public Guid AdjustedBy { get; private set; }
    public string? Reference { get; private set; }

    // Navigation Properties
    public User AdjusterUser { get; private set; } = null!;

    // Business Methods
    public decimal GetAdjustmentValue()
    {
        return Type switch
        {
            SettlementAdjustmentType.QuantityAdjustment => Amount.Amount,
            SettlementAdjustmentType.PriceAdjustment => Amount.Amount,
            SettlementAdjustmentType.QualityDiscount => -Amount.Amount, // Discounts reduce the amount
            SettlementAdjustmentType.LateFee => Amount.Amount,
            SettlementAdjustmentType.EarlyPaymentDiscount => -Amount.Amount,
            SettlementAdjustmentType.TaxAdjustment => Amount.Amount,
            SettlementAdjustmentType.AmountIncrease => Amount.Amount,
            SettlementAdjustmentType.AmountDecrease => -Amount.Amount,
            SettlementAdjustmentType.DueDateChange => 0, // No financial impact
            SettlementAdjustmentType.Other => Amount.Amount,
            _ => Amount.Amount
        };
    }

    public bool IsFinancialAdjustment()
    {
        return Type != SettlementAdjustmentType.DueDateChange;
    }

    public string GetAdjustmentDescription()
    {
        return Type switch
        {
            SettlementAdjustmentType.QuantityAdjustment => $"Quantity adjustment: {Reason}",
            SettlementAdjustmentType.PriceAdjustment => $"Price adjustment: {Reason}",
            SettlementAdjustmentType.QualityDiscount => $"Quality discount: {Reason}",
            SettlementAdjustmentType.LateFee => $"Late fee: {Reason}",
            SettlementAdjustmentType.EarlyPaymentDiscount => $"Early payment discount: {Reason}",
            SettlementAdjustmentType.TaxAdjustment => $"Tax adjustment: {Reason}",
            SettlementAdjustmentType.AmountIncrease => $"Amount increase: {Reason}",
            SettlementAdjustmentType.AmountDecrease => $"Amount decrease: {Reason}",
            SettlementAdjustmentType.DueDateChange => $"Due date change: {Reason}",
            SettlementAdjustmentType.Other => $"Other adjustment: {Reason}",
            _ => Reason
        };
    }
}

public enum SettlementAdjustmentType
{
    QuantityAdjustment = 1,
    PriceAdjustment = 2,
    QualityDiscount = 3,
    LateFee = 4,
    EarlyPaymentDiscount = 5,
    TaxAdjustment = 6,
    AmountIncrease = 7,
    AmountDecrease = 8,
    DueDateChange = 9,
    Other = 10
}