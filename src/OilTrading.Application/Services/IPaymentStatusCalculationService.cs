using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Service interface for calculating contract payment status based on settlement information.
///
/// Payment status is derived from settlement dates and payment information:
/// - NotDue: No settlement is due yet
/// - Due: At least one settlement is due today/soon
/// - PartiallyPaid: Some settlements are paid, others are unpaid
/// - Paid: All settlements for the contract are paid
/// - Overdue: At least one settlement's due date has passed without payment
/// </summary>
public interface IPaymentStatusCalculationService
{
    /// <summary>
    /// Calculate payment status for a purchase contract based on its related settlements.
    /// Returns null if the contract has no settlements.
    /// </summary>
    Task<ContractPaymentStatus?> CalculatePurchaseContractPaymentStatusAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate payment status for a sales contract based on its related settlements.
    /// Returns null if the contract has no settlements.
    /// </summary>
    Task<ContractPaymentStatus?> CalculateSalesContractPaymentStatusAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the earliest due date from all unpaid settlements for a contract.
    /// Returns null if no unpaid settlements exist or none have a due date.
    /// </summary>
    Task<DateTime?> GetEarliestUnpaidDueDateAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total amount still unpaid for a contract (sum of unpaid settlements).
    /// </summary>
    Task<decimal> GetUnpaidAmountAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total paid amount for a contract (sum of paid settlements).
    /// </summary>
    Task<decimal> GetPaidAmountAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive payment status details for reporting/display purposes.
    /// Includes status, amounts, dates, and breakdown by settlement.
    /// </summary>
    Task<PaymentStatusDetailsDto> GetPaymentStatusDetailsAsync(
        Guid contractId,
        bool isPurchaseContract,
        CancellationToken cancellationToken = default);
}
