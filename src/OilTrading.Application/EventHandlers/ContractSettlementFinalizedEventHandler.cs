using OilTrading.Core.Entities;
using OilTrading.Core.Events;
using OilTrading.Core.Repositories;
using OilTrading.Application.Services;

namespace OilTrading.Application.EventHandlers;

/// <summary>
/// Domain event handler service for ContractSettlementFinalizedEvent.
/// Handles business logic that must execute when a settlement is finalized.
///
/// This service processes the ContractSettlementFinalizedEvent by:
/// 1. Calculating updated contract payment status
/// 2. Updating contract status if all settlements are paid (Complete)
/// 3. Persisting changes back to the database
///
/// Responsibilities:
/// 1. Update contract payment status based on finalized settlement
/// 2. Determine if contract should transition to "Completed" state
/// 3. Maintain consistency between settlement and contract status
/// 4. Enable any downstream workflows (e.g., invoicing, accounting integrations)
///
/// Usage:
/// This handler is called from FinalizeSettlementCommandHandler after a settlement is finalized.
/// It updates the associated contract's status based on the payment status of all its settlements.
/// </summary>
public class ContractSettlementFinalizedEventHandler
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IContractSettlementRepository _settlementRepository;
    private readonly IPaymentStatusCalculationService _paymentStatusCalculationService;
    private readonly IUnitOfWork _unitOfWork;

    public ContractSettlementFinalizedEventHandler(
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IContractSettlementRepository settlementRepository,
        IPaymentStatusCalculationService paymentStatusCalculationService,
        IUnitOfWork unitOfWork)
    {
        _purchaseContractRepository = purchaseContractRepository ?? throw new ArgumentNullException(nameof(purchaseContractRepository));
        _salesContractRepository = salesContractRepository ?? throw new ArgumentNullException(nameof(salesContractRepository));
        _settlementRepository = settlementRepository ?? throw new ArgumentNullException(nameof(settlementRepository));
        _paymentStatusCalculationService = paymentStatusCalculationService ?? throw new ArgumentNullException(nameof(paymentStatusCalculationService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Handles the ContractSettlementFinalizedEvent domain event.
    ///
    /// Process:
    /// 1. Retrieve the finalized settlement
    /// 2. Determine which type of contract (Purchase or Sales)
    /// 3. Calculate updated payment status
    /// 4. Update contract status accordingly:
    ///    - If all settlements are paid → Contract Status = "Completed"
    ///    - If some settlements are paid → Contract Status = "Active" (with partial settlement tracking)
    /// 5. Save changes to database
    ///
    /// Returns true if contract status was updated, false otherwise.
    /// </summary>
    public async Task<bool> HandleSettlementFinalizedAsync(
        ContractSettlementFinalizedEvent @event,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Retrieve settlement to get contract ID
            var settlement = await _settlementRepository.GetByIdAsync(@event.SettlementId, cancellationToken);
            if (settlement == null)
            {
                // Settlement not found, cannot process - this should not happen in normal operation
                return false;
            }

            // Determine contract type and retrieve contract
            var purchaseContract = await _purchaseContractRepository.GetByIdAsync(settlement.ContractId, cancellationToken);

            if (purchaseContract != null)
            {
                // Handle purchase contract settlement finalization
                return await HandlePurchaseContractSettlementFinalizedAsync(
                    purchaseContract,
                    settlement.ContractId,
                    cancellationToken);
            }
            else
            {
                // Try to find sales contract
                var salesContract = await _salesContractRepository.GetByIdAsync(settlement.ContractId, cancellationToken);

                if (salesContract != null)
                {
                    // Handle sales contract settlement finalization
                    return await HandleSalesContractSettlementFinalizedAsync(
                        salesContract,
                        settlement.ContractId,
                        cancellationToken);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            // Log exception but don't throw - event handlers should be resilient
            // In production, this should be logged to a monitoring system
            System.Diagnostics.Debug.WriteLine($"Error handling ContractSettlementFinalizedEvent: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handles purchase contract status update after settlement finalization.
    /// </summary>
    private async Task<bool> HandlePurchaseContractSettlementFinalizedAsync(
        PurchaseContract purchaseContract,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        // Calculate updated payment status for the contract
        var paymentStatus = await _paymentStatusCalculationService
            .CalculatePurchaseContractPaymentStatusAsync(contractId, cancellationToken);

        // Determine new contract status based on payment status
        if (paymentStatus == ContractPaymentStatus.Paid)
        {
            // All settlements for this contract are paid
            // Contract can transition to Completed if no more settlements are expected
            try
            {
                purchaseContract.Complete();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                // Contract may already be completed or in a state that doesn't allow completion
                return false;
            }
        }

        // No status update needed - contract remains Active with partial settlement tracking
        return false;
    }

    /// <summary>
    /// Handles sales contract status update after settlement finalization.
    /// </summary>
    private async Task<bool> HandleSalesContractSettlementFinalizedAsync(
        SalesContract salesContract,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        // Calculate updated payment status for the contract
        var paymentStatus = await _paymentStatusCalculationService
            .CalculateSalesContractPaymentStatusAsync(contractId, cancellationToken);

        // Determine new contract status based on payment status
        if (paymentStatus == ContractPaymentStatus.Paid)
        {
            // All settlements for this contract are collected
            // Contract can transition to Completed if no more settlements are expected
            try
            {
                salesContract.Complete();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                // Contract may already be completed or in a state that doesn't allow completion
                return false;
            }
        }

        // No status update needed - contract remains Active with partial settlement tracking
        return false;
    }
}
