using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.TransactionOperations;

public class InventoryReservationOperation : ITransactionOperation
{
    private readonly IContractInventoryService _contractInventoryService;
    private readonly ILogger<InventoryReservationOperation> _logger;

    private Guid? _createdReservationId;
    private Guid? _contractId;
    private string _contractType = string.Empty;

    public string OperationName => "InventoryReservation";
    public int Order { get; set; } = 2;
    public bool RequiresCompensation => true;

    public InventoryReservationOperation(
        IContractInventoryService contractInventoryService,
        ILogger<InventoryReservationOperation> logger)
    {
        _contractInventoryService = contractInventoryService;
        _logger = logger;
    }

    public async Task<OperationResult> ExecuteAsync(TransactionContext context)
    {
        _logger.LogInformation("Executing inventory reservation operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // Get contract ID from previous operation or context
            if (context.Data.TryGetValue("ContractId", out var contractIdObj))
            {
                _contractId = (Guid)contractIdObj;
            }
            else if (context.Data.TryGetValue("ContractData", out var contractDataObj))
            {
                // Extract contract ID from contract data if available
                _contractId = ExtractContractIdFromData(contractDataObj);
            }

            if (!_contractId.HasValue)
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Contract ID not found in transaction context"
                };
            }

            // Get contract type
            if (!context.Data.TryGetValue("ContractType", out var contractTypeObj))
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Contract type not found in transaction context"
                };
            }

            _contractType = contractTypeObj.ToString() ?? "";

            // Reserve inventory for the contract
            var reservationResult = await _contractInventoryService.ReserveInventoryForContractAsync(_contractId.Value, _contractType);

            if (!reservationResult.IsSuccessful)
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = reservationResult.ErrorMessage ?? "Failed to reserve inventory"
                };
            }

            _createdReservationId = reservationResult.ReservationId;

            _logger.LogInformation("Reserved inventory (Reservation ID: {ReservationId}) for contract {ContractId} in transaction {TransactionId}", 
                _createdReservationId, _contractId, context.TransactionId);

            return new OperationResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object> 
                { 
                    ["ReservationId"] = _createdReservationId,
                    ["ReservedQuantity"] = reservationResult.ReservedQuantity?.Value ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing inventory reservation operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OperationResult> CompensateAsync(TransactionContext context)
    {
        _logger.LogInformation("Compensating inventory reservation operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            if (!_contractId.HasValue)
            {
                _logger.LogWarning("No contract ID available for compensation in transaction {TransactionId}", context.TransactionId);
                return new OperationResult { IsSuccess = true }; // Nothing to compensate
            }

            // Release the inventory reservation
            var releaseResult = await _contractInventoryService.ReleaseInventoryReservationAsync(
                _contractId.Value, 
                "Transaction compensation");

            if (!releaseResult.IsSuccessful)
            {
                _logger.LogError("Failed to release inventory reservation for contract {ContractId} during compensation: {Error}", 
                    _contractId.Value, releaseResult.ErrorMessage);
                
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = releaseResult.ErrorMessage ?? "Failed to release inventory reservation"
                };
            }

            _logger.LogInformation("Released inventory reservation for contract {ContractId} in compensation for transaction {TransactionId}", 
                _contractId, context.TransactionId);

            return new OperationResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating inventory reservation operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private Guid? ExtractContractIdFromData(object contractData)
    {
        // Try to extract contract ID from different contract data types
        if (contractData is PurchaseContractCreationData purchaseData)
        {
            // For new contracts, we might need to generate or get the ID differently
            // This is a placeholder - in real implementation, the ID would be available
            return null;
        }
        
        if (contractData is SalesContractCreationData salesData)
        {
            return null;
        }

        return null;
    }
}

