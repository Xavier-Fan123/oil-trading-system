using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.TransactionOperations;

public class AuditLogOperation : ITransactionOperation
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogOperation> _logger;

    private Guid? _createdAuditLogId;

    public string OperationName => "AuditLog";
    public int Order { get; set; } = 100; // Typically run last
    public bool RequiresCompensation => false; // Audit logs are rarely rolled back

    public AuditLogOperation(
        IAuditLogService auditLogService,
        ILogger<AuditLogOperation> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<OperationResult> ExecuteAsync(TransactionContext context)
    {
        _logger.LogInformation("Executing audit log operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // Create comprehensive audit log entry
            var auditLog = CreateAuditLogEntry(context);
            
            await _auditLogService.LogOperationAsync(auditLog);
            _createdAuditLogId = auditLog.OperationId;

            _logger.LogInformation("Created audit log entry {AuditLogId} for transaction {TransactionId}", 
                _createdAuditLogId, context.TransactionId);

            return new OperationResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object> { ["AuditLogId"] = _createdAuditLogId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing audit log operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OperationResult> CompensateAsync(TransactionContext context)
    {
        _logger.LogInformation("Compensating audit log operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // Create compensation audit log entry
            if (_createdAuditLogId.HasValue)
            {
                var compensationLog = new OperationAuditLog(
                    Guid.NewGuid(),
                    context.TransactionId,
                    "AuditLogCompensation",
                    "Compensation",
                    DateTime.UtcNow,
                    true,
                    new Dictionary<string, object>
                    {
                        ["CompensatedAuditLogId"] = _createdAuditLogId,
                        ["CompensationReason"] = "Transaction rolled back",
                        ["OriginalTransactionStatus"] = context.Status.ToString()
                    });

                await _auditLogService.LogOperationAsync(compensationLog);

                _logger.LogInformation("Created compensation audit log for transaction {TransactionId}", context.TransactionId);
            }

            return new OperationResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating audit log operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private OperationAuditLog CreateAuditLogEntry(TransactionContext context)
    {
        var operationType = DetermineOperationType(context);
        var entityType = DetermineEntityType(context);
        var entityId = ExtractEntityId(context);

        return new OperationAuditLog(
            Guid.NewGuid(),
            context.TransactionId,
            context.TransactionName,
            operationType,
            DateTime.UtcNow,
            true, // At this point, we assume success since this is the final operation
            new Dictionary<string, object>
            {
                ["EntityType"] = entityType,
                ["EntityId"] = entityId,
                ["InitiatedBy"] = context.InitiatedBy,
                ["TransactionStartTime"] = context.StartTime,
                ["OperationCount"] = context.Steps.Count,
                ["ContractType"] = context.Data.GetValueOrDefault("ContractType", "Unknown"),
                ["ContractData"] = SerializeContractData(context.Data.GetValueOrDefault("ContractData")),
                ["RiskOverride"] = context.Data.ContainsKey("AllowRiskOverride"),
                ["BusinessJustification"] = context.Data.GetValueOrDefault("BusinessJustification", ""),
                ["ClientIP"] = context.Data.GetValueOrDefault("ClientIP", ""),
                ["UserAgent"] = context.Data.GetValueOrDefault("UserAgent", ""),
                ["SessionId"] = context.Data.GetValueOrDefault("SessionId", "")
            });
    }

    private string DetermineOperationType(TransactionContext context)
    {
        var transactionName = context.TransactionName.ToLower();
        
        if (transactionName.Contains("create") || transactionName.Contains("add"))
            return "Create";
        if (transactionName.Contains("update") || transactionName.Contains("modify"))
            return "Update";
        if (transactionName.Contains("delete") || transactionName.Contains("remove"))
            return "Delete";
        if (transactionName.Contains("approve"))
            return "Approve";
        if (transactionName.Contains("cancel"))
            return "Cancel";
        
        return "Other";
    }

    private string DetermineEntityType(TransactionContext context)
    {
        if (context.Data.TryGetValue("ContractType", out var contractType))
        {
            return $"{contractType}Contract";
        }
        
        var transactionName = context.TransactionName.ToLower();
        if (transactionName.Contains("contract"))
            return "Contract";
        if (transactionName.Contains("settlement"))
            return "Settlement";
        if (transactionName.Contains("inventory"))
            return "Inventory";
        if (transactionName.Contains("payment"))
            return "Payment";
        
        return "Unknown";
    }

    private string ExtractEntityId(TransactionContext context)
    {
        if (context.Data.TryGetValue("ContractId", out var contractId))
        {
            return contractId.ToString() ?? "";
        }
        
        // Check for other entity IDs
        var possibleIds = new[] { "SettlementId", "PaymentId", "InventoryId", "ReservationId" };
        
        foreach (var idKey in possibleIds)
        {
            if (context.Data.TryGetValue(idKey, out var id))
            {
                return id.ToString() ?? "";
            }
        }
        
        return "";
    }

    private string SerializeContractData(object? contractData)
    {
        if (contractData == null)
            return "";

        try
        {
            // Create a simplified version of contract data for audit purposes
            if (contractData is PurchaseContractCreationData purchaseData)
            {
                return $"PurchaseContract: {purchaseData.ContractNumber}, Supplier: {purchaseData.SupplierId}, " +
                       $"Product: {purchaseData.ProductId}, Quantity: {purchaseData.ContractQuantity.Value} {purchaseData.ContractQuantity.Unit}, " +
                       $"Delivery: {purchaseData.DeliveryStartDate:yyyy-MM-dd} to {purchaseData.DeliveryEndDate:yyyy-MM-dd}";
            }
            
            if (contractData is SalesContractCreationData salesData)
            {
                return $"SalesContract: {salesData.ContractNumber}, Customer: {salesData.CustomerId}, " +
                       $"Product: {salesData.ProductId}, Quantity: {salesData.ContractQuantity.Value} {salesData.ContractQuantity.Unit}, " +
                       $"Delivery: {salesData.DeliveryStartDate:yyyy-MM-dd} to {salesData.DeliveryEndDate:yyyy-MM-dd}";
            }

            return contractData.ToString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize contract data for audit log");
            return "SerializationFailed";
        }
    }
}