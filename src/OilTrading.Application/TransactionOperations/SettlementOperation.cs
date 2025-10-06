using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;
using CoreSettlementType = OilTrading.Core.Entities.SettlementType;
using CoreSettlementStatus = OilTrading.Core.Entities.SettlementStatus;
using DtoSettlementType = OilTrading.Application.DTOs.SettlementType;
using DtoSettlementStatus = OilTrading.Application.DTOs.SettlementStatus;

namespace OilTrading.Application.TransactionOperations;

public class SettlementOperation : ITransactionOperation
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<SettlementOperation> _logger;

    private Guid? _createdSettlementId;
    private Guid? _contractId;
    private string _contractType = string.Empty;

    public string OperationName => "Settlement";
    public int Order { get; set; } = 4;
    public bool RequiresCompensation => true;

    public SettlementOperation(
        ISettlementService settlementService,
        ILogger<SettlementOperation> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    public async Task<OperationResult> ExecuteAsync(TransactionContext context)
    {
        _logger.LogInformation("Executing settlement operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // Get contract information from context
            if (!context.Data.TryGetValue("ContractId", out var contractIdObj) ||
                !context.Data.TryGetValue("ContractType", out var contractTypeObj))
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Contract ID or type not found in transaction context"
                };
            }

            _contractId = (Guid)contractIdObj;
            _contractType = contractTypeObj.ToString() ?? "";

            // Check if settlement should be created based on contract terms
            var shouldCreateSettlement = await ShouldCreateInitialSettlementAsync(_contractId.Value, _contractType);

            if (!shouldCreateSettlement)
            {
                _logger.LogInformation("No immediate settlement required for contract {ContractId} in transaction {TransactionId}", 
                    _contractId, context.TransactionId);
                
                return new OperationResult
                {
                    IsSuccess = true,
                    Data = new Dictionary<string, object> { ["SettlementCreated"] = false }
                };
            }

            // Create settlement for the contract
            var settlementRequest = CreateSettlementRequest(_contractId.Value, _contractType, context);
            var settlement = await _settlementService.CreateSettlementAsync(settlementRequest);

            _createdSettlementId = settlement.Id;

            _logger.LogInformation("Created settlement {SettlementId} for contract {ContractId} in transaction {TransactionId}", 
                settlement.Id, _contractId, context.TransactionId);

            return new OperationResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object> 
                { 
                    ["SettlementId"] = settlement.Id,
                    ["SettlementCreated"] = true,
                    ["SettlementType"] = settlement.Type.ToString(),
                    ["DueDate"] = settlement.DueDate
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing settlement operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OperationResult> CompensateAsync(TransactionContext context)
    {
        _logger.LogInformation("Compensating settlement operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            if (!_createdSettlementId.HasValue)
            {
                _logger.LogWarning("No settlement ID available for compensation in transaction {TransactionId}", context.TransactionId);
                return new OperationResult { IsSuccess = true }; // Nothing to compensate
            }

            // Get the settlement to check its current status
            var settlement = await _settlementService.GetSettlementByIdAsync(_createdSettlementId.Value);
            if (settlement == null)
            {
                _logger.LogWarning("Settlement {SettlementId} not found for compensation in transaction {TransactionId}", 
                    _createdSettlementId, context.TransactionId);
                return new OperationResult { IsSuccess = true };
            }

            // Handle compensation based on settlement status
            if (settlement.Status == DtoSettlementStatus.Pending)
            {
                // Cancel the settlement if it hasn't been processed yet
                await _settlementService.CancelSettlementAsync(_createdSettlementId.Value, "Transaction compensation");
                
                _logger.LogInformation("Cancelled settlement {SettlementId} for compensation in transaction {TransactionId}", 
                    _createdSettlementId, context.TransactionId);
            }
            else if (settlement.Status == DtoSettlementStatus.Processing || settlement.Status == DtoSettlementStatus.Completed)
            {
                // Create a reversal settlement for processed settlements
                var reversalRequest = new SettlementRequest
                {
                    ContractId = settlement.ContractId,
                    Type = DtoSettlementType.Adjustment,
                    Amount = new Money(-settlement.Amount.Amount, settlement.Amount.Currency),
                    DueDate = DateTime.UtcNow.AddDays(1),
                    Description = $"Reversal of settlement {_createdSettlementId} due to transaction compensation",
                    CreatedBy = "System"
                };

                var reversalSettlement = await _settlementService.CreateSettlementAsync(reversalRequest);
                
                _logger.LogInformation("Created reversal settlement {ReversalSettlementId} for settlement {SettlementId} in compensation for transaction {TransactionId}", 
                    reversalSettlement.Id, _createdSettlementId, context.TransactionId);
            }
            else if (settlement.Status == DtoSettlementStatus.Completed)
            {
                // For completed settlements, we may need manual intervention
                _logger.LogError("Cannot automatically compensate completed settlement {SettlementId} in transaction {TransactionId}. Manual intervention required.", 
                    _createdSettlementId, context.TransactionId);
                
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Cannot automatically compensate completed settlement. Manual intervention required."
                };
            }

            return new OperationResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating settlement operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<bool> ShouldCreateInitialSettlementAsync(Guid contractId, string contractType)
    {
        // Business logic to determine if an initial settlement should be created
        // This could depend on contract terms, payment schedules, etc.
        
        // For demonstration, we'll create settlements for contracts with certain settlement types
        // In real implementation, this would check contract terms
        
        try
        {
            // Check if there are already existing settlements for this contract
            var existingSettlements = await _settlementService.GetSettlementsForContractAsync(contractId);
            
            // Don't create if settlements already exist
            if (existingSettlements.Any())
            {
                return false;
            }

            // For purchase contracts, typically create initial settlement
            // For sales contracts, might wait for delivery confirmation
            return contractType.Equals("Purchase", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking settlement requirements for contract {ContractId}", contractId);
            return false; // Err on the side of caution
        }
    }

    private SettlementRequest CreateSettlementRequest(Guid contractId, string contractType, TransactionContext context)
    {
        // Extract contract data to determine settlement amount and terms
        var contractData = context.Data.GetValueOrDefault("ContractData");
        
        Money settlementAmount;
        DateTime dueDate;
        string description;

        if (contractType.Equals("Purchase", StringComparison.OrdinalIgnoreCase) && 
            contractData is PurchaseContractCreationData purchaseData)
        {
            // Calculate settlement amount based on contract value
            var contractValue = CalculateContractValue(purchaseData);
            settlementAmount = new Money(contractValue, "USD");
            dueDate = DateTime.UtcNow.AddDays(30); // Standard payment terms
            description = $"Initial settlement for purchase contract {purchaseData.ContractNumber}";
        }
        else if (contractType.Equals("Sales", StringComparison.OrdinalIgnoreCase) && 
                 contractData is SalesContractCreationData salesData)
        {
            var contractValue = CalculateContractValue(salesData);
            settlementAmount = new Money(contractValue, "USD");
            dueDate = DateTime.UtcNow.AddDays(15); // Faster collection for sales
            description = $"Initial settlement for sales contract {salesData.ContractNumber}";
        }
        else
        {
            // Default settlement
            settlementAmount = new Money(1000000, "USD"); // $1M default
            dueDate = DateTime.UtcNow.AddDays(30);
            description = $"Initial settlement for contract {contractId}";
        }

        return new SettlementRequest
        {
            ContractId = contractId,
            Type = (OilTrading.Application.DTOs.SettlementType)SettlementType.ContractPayment,
            Amount = settlementAmount,
            DueDate = dueDate,
            Description = description,
            CreatedBy = context.InitiatedBy
        };
    }

    private decimal CalculateContractValue(PurchaseContractCreationData data)
    {
        // Simplified contract value calculation
        if (data.PriceFormula.IsFixedPrice && data.PriceFormula.BasePrice != null)
        {
            return data.PriceFormula.BasePrice.Amount * data.ContractQuantity.Value;
        }
        
        // For floating prices, use an estimated value
        return 75m * data.ContractQuantity.Value; // $75 per unit estimate
    }

    private decimal CalculateContractValue(SalesContractCreationData data)
    {
        // Simplified contract value calculation
        if (data.PriceFormula.IsFixedPrice && data.PriceFormula.BasePrice != null)
        {
            return data.PriceFormula.BasePrice.Amount * data.ContractQuantity.Value;
        }
        
        // For floating prices, use an estimated value
        return 75m * data.ContractQuantity.Value; // $75 per unit estimate
    }
}