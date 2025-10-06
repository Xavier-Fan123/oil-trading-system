using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Application.TransactionOperations;

namespace OilTrading.Application.Services;

/// <summary>
/// Factory for creating and configuring transaction operations
/// </summary>
public class TransactionOperationFactory : ITransactionOperationFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionOperationFactory> _logger;

    public TransactionOperationFactory(
        IServiceProvider serviceProvider,
        ILogger<TransactionOperationFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ITransactionOperation[] CreateContractCreationOperations(string contractType)
    {
        _logger.LogDebug("Creating contract creation operations for type: {ContractType}", contractType);

        var operations = new List<ITransactionOperation>();

        // Order is important - operations will be executed in this sequence
        // and compensated in reverse order

        // 1. Create contract (must be first)
        operations.Add(CreateContractCreationOperation());

        // 2. Reserve inventory (depends on contract being created)
        operations.Add(CreateInventoryReservationOperation());

        // 3. Check risk limits (depends on contract and inventory)
        operations.Add(CreateRiskLimitCheckOperation());

        // 4. Create settlement (depends on contract and risk approval)
        operations.Add(CreateSettlementOperation());

        // 5. Log audit trail (typically last, rarely compensated)
        operations.Add(CreateAuditLogOperation());

        _logger.LogInformation("Created {Count} operations for contract creation", operations.Count);
        return operations.ToArray();
    }

    public ITransactionOperation[] CreateContractApprovalOperations()
    {
        var operations = new List<ITransactionOperation>
        {
            CreateRiskLimitCheckOperation(),
            CreateAuditLogOperation()
        };

        return operations.ToArray();
    }

    public ITransactionOperation[] CreateContractCancellationOperations()
    {
        var operations = new List<ITransactionOperation>
        {
            CreateInventoryReservationOperation(), // Will release reservations
            CreateSettlementOperation(),           // Will cancel/adjust settlements
            CreateAuditLogOperation()
        };

        return operations.ToArray();
    }

    public ITransactionOperation[] CreateSettlementProcessingOperations()
    {
        var operations = new List<ITransactionOperation>
        {
            CreateSettlementOperation(),
            CreateAuditLogOperation()
        };

        return operations.ToArray();
    }

    public ITransactionOperation[] CreateInventoryMovementOperations()
    {
        var operations = new List<ITransactionOperation>
        {
            CreateInventoryReservationOperation(),
            CreateAuditLogOperation()
        };

        return operations.ToArray();
    }

    public ContractCreationOperation CreateContractCreationOperation()
    {
        return new ContractCreationOperation(
            _serviceProvider.GetRequiredService<IPurchaseContractRepository>(),
            _serviceProvider.GetRequiredService<ISalesContractRepository>(),
            _serviceProvider.GetRequiredService<ILogger<ContractCreationOperation>>())
        {
            Order = 1
        };
    }

    public InventoryReservationOperation CreateInventoryReservationOperation()
    {
        return new InventoryReservationOperation(
            _serviceProvider.GetRequiredService<IContractInventoryService>(),
            _serviceProvider.GetRequiredService<ILogger<InventoryReservationOperation>>())
        {
            Order = 2
        };
    }

    public RiskLimitCheckOperation CreateRiskLimitCheckOperation()
    {
        return new RiskLimitCheckOperation(
            _serviceProvider.GetRequiredService<IRealTimeRiskMonitoringService>(),
            _serviceProvider.GetRequiredService<ILogger<RiskLimitCheckOperation>>())
        {
            Order = 3
        };
    }

    public SettlementOperation CreateSettlementOperation()
    {
        return new SettlementOperation(
            _serviceProvider.GetRequiredService<ISettlementService>(),
            _serviceProvider.GetRequiredService<ILogger<SettlementOperation>>())
        {
            Order = 4
        };
    }

    public AuditLogOperation CreateAuditLogOperation()
    {
        return new AuditLogOperation(
            _serviceProvider.GetRequiredService<IAuditLogService>(),
            _serviceProvider.GetRequiredService<ILogger<AuditLogOperation>>())
        {
            Order = 100 // Always last
        };
    }

    public ITransactionOperation[] CreateCustomOperations(params string[] operationNames)
    {
        var operations = new List<ITransactionOperation>();

        foreach (var operationName in operationNames)
        {
            ITransactionOperation operation = operationName.ToLower() switch
            {
                "contract" or "contractcreation" => CreateContractCreationOperation(),
                "inventory" or "inventoryreservation" => CreateInventoryReservationOperation(),
                "risk" or "risklimitcheck" => CreateRiskLimitCheckOperation(),
                "settlement" => CreateSettlementOperation(),
                "audit" or "auditlog" => CreateAuditLogOperation(),
                _ => throw new ArgumentException($"Unknown operation name: {operationName}")
            };

            operations.Add(operation);
        }

        return operations.ToArray();
    }

    public CompensationContext CreateCompensationContext(Guid transactionId, string transactionName, string reason, string initiatedBy)
    {
        return new CompensationContext
        {
            TransactionId = transactionId,
            TransactionName = transactionName,
            CompensationReason = reason,
            InitiatedBy = initiatedBy,
            Strategy = CompensationStrategy.BestEffort,
            MaxRetryAttempts = 3,
            OperationTimeout = TimeSpan.FromMinutes(5)
        };
    }

    public CompensationContext CreateCompensationContext(Guid transactionId, string transactionName, string reason, string initiatedBy, CompensationStrategy strategy)
    {
        var context = CreateCompensationContext(transactionId, transactionName, reason, initiatedBy);
        context.Strategy = strategy;

        // Adjust settings based on strategy
        switch (strategy)
        {
            case CompensationStrategy.FailFast:
                context.MaxRetryAttempts = 1;
                context.OperationTimeout = TimeSpan.FromMinutes(2);
                break;
            
            case CompensationStrategy.AllOrNothing:
                context.MaxRetryAttempts = 5;
                context.OperationTimeout = TimeSpan.FromMinutes(10);
                break;
            
            case CompensationStrategy.ManualIntervention:
                context.MaxRetryAttempts = 1;
                context.OperationTimeout = TimeSpan.FromMinutes(1);
                break;
            
            case CompensationStrategy.BestEffort:
            default:
                // Use default settings
                break;
        }

        return context;
    }
}

/// <summary>
/// Interface for transaction operation factory
/// </summary>
public interface ITransactionOperationFactory
{
    /// <summary>
    /// Creates all operations needed for contract creation
    /// </summary>
    ITransactionOperation[] CreateContractCreationOperations(string contractType);

    /// <summary>
    /// Creates operations for contract approval workflow
    /// </summary>
    ITransactionOperation[] CreateContractApprovalOperations();

    /// <summary>
    /// Creates operations for contract cancellation
    /// </summary>
    ITransactionOperation[] CreateContractCancellationOperations();

    /// <summary>
    /// Creates operations for settlement processing
    /// </summary>
    ITransactionOperation[] CreateSettlementProcessingOperations();

    /// <summary>
    /// Creates operations for inventory movements
    /// </summary>
    ITransactionOperation[] CreateInventoryMovementOperations();

    /// <summary>
    /// Creates specific operation instances
    /// </summary>
    ContractCreationOperation CreateContractCreationOperation();
    InventoryReservationOperation CreateInventoryReservationOperation();
    RiskLimitCheckOperation CreateRiskLimitCheckOperation();
    SettlementOperation CreateSettlementOperation();
    AuditLogOperation CreateAuditLogOperation();

    /// <summary>
    /// Creates custom combination of operations
    /// </summary>
    ITransactionOperation[] CreateCustomOperations(params string[] operationNames);

    /// <summary>
    /// Creates compensation context with default settings
    /// </summary>
    CompensationContext CreateCompensationContext(Guid transactionId, string transactionName, string reason, string initiatedBy);

    /// <summary>
    /// Creates compensation context with specific strategy
    /// </summary>
    CompensationContext CreateCompensationContext(Guid transactionId, string transactionName, string reason, string initiatedBy, CompensationStrategy strategy);
}

/// <summary>
/// Extension methods for easier transaction operation usage
/// </summary>
public static class TransactionOperationExtensions
{
    public static async Task<TransactionResult> ExecuteContractCreationAsync(
        this ITransactionCoordinatorService coordinator,
        ITransactionOperationFactory factory,
        string contractType,
        object contractData,
        string initiatedBy)
    {
        var context = coordinator.CreateTransactionContext($"Create{contractType}Contract", initiatedBy);
        context.Data["ContractType"] = contractType;
        context.Data["ContractData"] = contractData;

        var operations = factory.CreateContractCreationOperations(contractType);
        return await coordinator.ExecuteTransactionAsync(operations, context);
    }

    public static async Task<TransactionResult> ExecuteContractApprovalAsync(
        this ITransactionCoordinatorService coordinator,
        ITransactionOperationFactory factory,
        Guid contractId,
        string contractType,
        string initiatedBy,
        bool allowRiskOverride = false)
    {
        var context = coordinator.CreateTransactionContext($"Approve{contractType}Contract", initiatedBy);
        context.Data["ContractId"] = contractId;
        context.Data["ContractType"] = contractType;
        
        if (allowRiskOverride)
        {
            context.Data["AllowRiskOverride"] = true;
        }

        var operations = factory.CreateContractApprovalOperations();
        return await coordinator.ExecuteTransactionAsync(operations, context);
    }

    public static async Task<TransactionResult> ExecuteContractCancellationAsync(
        this ITransactionCoordinatorService coordinator,
        ITransactionOperationFactory factory,
        Guid contractId,
        string contractType,
        string reason,
        string initiatedBy)
    {
        var context = coordinator.CreateTransactionContext($"Cancel{contractType}Contract", initiatedBy);
        context.Data["ContractId"] = contractId;
        context.Data["ContractType"] = contractType;
        context.Data["CancellationReason"] = reason;

        var operations = factory.CreateContractCancellationOperations();
        return await coordinator.ExecuteTransactionAsync(operations, context);
    }
}