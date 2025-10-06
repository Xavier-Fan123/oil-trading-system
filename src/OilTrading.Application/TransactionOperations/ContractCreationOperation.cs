using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Enums;
using SettlementType = OilTrading.Core.Entities.SettlementType;

namespace OilTrading.Application.TransactionOperations;

public class ContractCreationOperation : ITransactionOperation
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ILogger<ContractCreationOperation> _logger;

    private Guid? _createdContractId;
    private string _contractType = string.Empty;

    public string OperationName => "ContractCreation";
    public int Order { get; set; } = 1;
    public bool RequiresCompensation => true;

    public ContractCreationOperation(
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ILogger<ContractCreationOperation> logger)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _logger = logger;
    }

    public async Task<OperationResult> ExecuteAsync(TransactionContext context)
    {
        _logger.LogInformation("Executing contract creation operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // Get contract creation parameters from context
            if (!context.Data.TryGetValue("ContractType", out var contractTypeObj) ||
                !context.Data.TryGetValue("ContractData", out var contractDataObj))
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Missing contract type or data in transaction context"
                };
            }

            _contractType = contractTypeObj.ToString() ?? "";
            
            if (_contractType.Equals("Purchase", StringComparison.OrdinalIgnoreCase))
            {
                var contractData = contractDataObj as PurchaseContractCreationData;
                if (contractData == null)
                {
                    return new OperationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid purchase contract data"
                    };
                }

                var contract = CreatePurchaseContract(contractData);
                await _purchaseContractRepository.AddAsync(contract);
                _createdContractId = contract.Id;

                _logger.LogInformation("Created purchase contract {ContractId} in transaction {TransactionId}", 
                    contract.Id, context.TransactionId);

                return new OperationResult
                {
                    IsSuccess = true,
                    Data = new Dictionary<string, object> { ["ContractId"] = contract.Id }
                };
            }
            else if (_contractType.Equals("Sales", StringComparison.OrdinalIgnoreCase))
            {
                var contractData = contractDataObj as SalesContractCreationData;
                if (contractData == null)
                {
                    return new OperationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid sales contract data"
                    };
                }

                var contract = CreateSalesContract(contractData);
                await _salesContractRepository.AddAsync(contract);
                _createdContractId = contract.Id;

                _logger.LogInformation("Created sales contract {ContractId} in transaction {TransactionId}", 
                    contract.Id, context.TransactionId);

                return new OperationResult
                {
                    IsSuccess = true,
                    Data = new Dictionary<string, object> { ["ContractId"] = contract.Id }
                };
            }

            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unknown contract type: {_contractType}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing contract creation operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OperationResult> CompensateAsync(TransactionContext context)
    {
        _logger.LogInformation("Compensating contract creation operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            if (!_createdContractId.HasValue)
            {
                _logger.LogWarning("No contract ID available for compensation in transaction {TransactionId}", context.TransactionId);
                return new OperationResult { IsSuccess = true }; // Nothing to compensate
            }

            if (_contractType.Equals("Purchase", StringComparison.OrdinalIgnoreCase))
            {
                var contract = await _purchaseContractRepository.GetByIdAsync(_createdContractId.Value);
                if (contract != null)
                {
                    // Mark contract as cancelled instead of deleting to maintain audit trail
                    contract.Cancel("Transaction compensation");
                    await _purchaseContractRepository.UpdateAsync(contract);
                    
                    _logger.LogInformation("Cancelled purchase contract {ContractId} for compensation in transaction {TransactionId}", 
                        _createdContractId.Value, context.TransactionId);
                }
            }
            else if (_contractType.Equals("Sales", StringComparison.OrdinalIgnoreCase))
            {
                var contract = await _salesContractRepository.GetByIdAsync(_createdContractId.Value);
                if (contract != null)
                {
                    // Mark contract as cancelled instead of deleting to maintain audit trail
                    contract.Cancel("Transaction compensation");
                    await _salesContractRepository.UpdateAsync(contract);
                    
                    _logger.LogInformation("Cancelled sales contract {ContractId} for compensation in transaction {TransactionId}", 
                        _createdContractId.Value, context.TransactionId);
                }
            }

            return new OperationResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating contract creation operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private PurchaseContract CreatePurchaseContract(PurchaseContractCreationData data)
    {
        var contractNumber = ContractNumber.Parse(data.ContractNumber);
        return new PurchaseContract(
            contractNumber,
            ContractType.CARGO, // Default to CARGO type
            data.SupplierId,
            data.ProductId,
            data.TraderId,
            data.ContractQuantity,
            data.TonBarrelRatio,
            data.PriceBenchmarkId,
            data.ExternalContractNumber);
    }

    private SalesContract CreateSalesContract(SalesContractCreationData data)
    {
        var contractNumber = ContractNumber.Parse(data.ContractNumber);
        return new SalesContract(
            contractNumber,
            ContractType.CARGO, // Default to CARGO type
            data.CustomerId,
            data.ProductId,
            data.TraderId,
            data.ContractQuantity);
    }
}

