namespace OilTrading.Core.Enums;

/// <summary>
/// Settlement type enumeration for contracts.
/// Specifies the type of settlement/payment event in a contract lifecycle.
/// </summary>
public enum SettlementType
{
    /// <summary>
    /// Contract Payment - Standard contract settlement payment
    /// </summary>
    ContractPayment = 1,

    /// <summary>
    /// Partial Payment - Partial payment of contract amount
    /// </summary>
    PartialPayment = 2,

    /// <summary>
    /// Adjustment - Settlement adjustment or correction
    /// </summary>
    Adjustment = 3,

    /// <summary>
    /// Refund - Refund or return of payment
    /// </summary>
    Refund = 4,

    /// <summary>
    /// Prepayment - Advance payment against future deliveries
    /// </summary>
    Prepayment = 5,

    /// <summary>
    /// Final Payment - Final settlement payment for contract completion
    /// </summary>
    FinalPayment = 6
}
