using OilTrading.Application.TransactionOperations;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace OilTrading.Application.Services;

public interface IContractTransactionService
{
    Task<ContractCreationResult> CreateContractWithValidationAsync(CreateContractTransactionRequest request);
    Task<bool> ValidateContractCreationAsync(CreateContractTransactionRequest request);
}

public class ContractTransactionService : IContractTransactionService
{
    private readonly ITransactionCoordinatorService _transactionCoordinator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IContractNumberGenerator _contractNumberGenerator;
    private readonly ILogger<ContractTransactionService> _logger;

    public ContractTransactionService(
        ITransactionCoordinatorService transactionCoordinator,
        IServiceProvider serviceProvider,
        IContractNumberGenerator contractNumberGenerator,
        ILogger<ContractTransactionService> logger)
    {
        _transactionCoordinator = transactionCoordinator;
        _serviceProvider = serviceProvider;
        _contractNumberGenerator = contractNumberGenerator;
        _logger = logger;
    }

    public async Task<ContractCreationResult> CreateContractWithValidationAsync(CreateContractTransactionRequest request)
    {
        _logger.LogInformation("Starting contract creation transaction for supplier {SupplierId}, product {ProductId}", 
            request.SupplierId, request.ProductId);

        try
        {
            // Pre-validation
            var validationResult = await ValidateContractCreationAsync(request);
            if (!validationResult)
            {
                return new ContractCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Contract validation failed"
                };
            }

            // Generate contract number
            var contractNumberString = await _contractNumberGenerator.GenerateAsync(
                Core.ValueObjects.ContractType.CARGO, 
                DateTime.UtcNow.Year);
            var contractNumber = Core.ValueObjects.ContractNumber.Parse(contractNumberString);

            // Create transaction context
            var context = _transactionCoordinator.CreateTransactionContext(
                "CreatePurchaseContract", 
                request.InitiatedBy);

            // Set contract data in context
            context.SetData("ContractType", "Purchase");
            context.SetData("ContractData", new PurchaseContractCreationData
            {
                ContractNumber = contractNumber.Value,
                SupplierId = request.SupplierId,
                ProductId = request.ProductId,
                TraderId = request.TraderId,
                ContractQuantity = request.Quantity,
                PriceFormula = request.PriceFormula,
                DeliveryTerms = request.DeliveryTerms,
                DeliveryStartDate = request.LaycanStart,
                DeliveryEndDate = request.LaycanEnd,
                TonBarrelRatio = 7.6m, // Default ratio
                PriceBenchmarkId = Guid.NewGuid(), // Default benchmark
                ExternalContractNumber = null
            });

            // Create transaction operations
            var operations = new ITransactionOperation[]
            {
                new ContractCreationOperation(
                    _serviceProvider.GetRequiredService<Core.Repositories.IPurchaseContractRepository>(),
                    _serviceProvider.GetRequiredService<Core.Repositories.ISalesContractRepository>(),
                    _serviceProvider.GetRequiredService<ILogger<ContractCreationOperation>>()),
                
                new InventoryReservationOperation(
                    _serviceProvider.GetRequiredService<IContractInventoryService>(),
                    _serviceProvider.GetRequiredService<ILogger<InventoryReservationOperation>>()),
                
                new RiskLimitCheckOperation(
                    _serviceProvider.GetRequiredService<IRealTimeRiskMonitoringService>(),
                    _serviceProvider.GetRequiredService<ILogger<RiskLimitCheckOperation>>()),
                
                new AuditLogOperation(
                    _serviceProvider.GetRequiredService<IAuditLogService>(),
                    _serviceProvider.GetRequiredService<ILogger<AuditLogOperation>>())
            };

            // Execute the distributed transaction
            var transactionResult = await _transactionCoordinator.ExecuteTransactionAsync(operations, context);

            var result = new ContractCreationResult
            {
                IsSuccess = transactionResult.IsSuccess,
                TransactionId = transactionResult.TransactionId,
                ErrorMessage = transactionResult.ErrorMessage,
                Duration = transactionResult.Duration
            };

            if (transactionResult.IsSuccess)
            {
                result.ContractId = context.GetData<Guid>("ContractId");
                result.ContractNumber = contractNumber.Value;
                result.RequiresApproval = context.GetData<bool>("RequiresRiskApproval");
                
                if (result.RequiresApproval)
                {
                    result.ApprovalReason = "Risk limit violations detected";
                    var violations = context.GetData<List<RiskLimitViolation>>("RiskViolations");
                    result.RiskViolations = violations ?? new List<RiskLimitViolation>();
                }

                _logger.LogInformation("Contract creation transaction completed successfully. Contract ID: {ContractId}", 
                    result.ContractId);
            }
            else
            {
                _logger.LogError("Contract creation transaction failed: {ErrorMessage}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in contract creation transaction");
            return new ContractCreationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<bool> ValidateContractCreationAsync(CreateContractTransactionRequest request)
    {
        // Basic validation
        if (request.SupplierId == Guid.Empty)
        {
            _logger.LogWarning("Contract validation failed: Invalid supplier ID");
            return false;
        }

        if (request.ProductId == Guid.Empty)
        {
            _logger.LogWarning("Contract validation failed: Invalid product ID");
            return false;
        }

        if (request.TraderId == Guid.Empty)
        {
            _logger.LogWarning("Contract validation failed: Invalid trader ID");
            return false;
        }

        if (request.Quantity.Value <= 0)
        {
            _logger.LogWarning("Contract validation failed: Invalid quantity {Quantity}", request.Quantity.Value);
            return false;
        }

        if (request.LaycanStart >= request.LaycanEnd)
        {
            _logger.LogWarning("Contract validation failed: Invalid laycan dates");
            return false;
        }

        if (request.LaycanStart < DateTime.UtcNow.Date)
        {
            _logger.LogWarning("Contract validation failed: Laycan start date is in the past");
            return false;
        }

        // Additional business validations could be added here
        // - Supplier validation
        // - Product availability
        // - Price formula validation
        // - Trading partner limits

        return true;
    }
}

public class CreateContractTransactionRequest
{
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TraderId { get; set; }
    public Quantity Quantity { get; set; } = null!;
    public PriceFormula PriceFormula { get; set; } = null!;
    public DeliveryTerms DeliveryTerms { get; set; }
    public DateTime LaycanStart { get; set; }
    public DateTime LaycanEnd { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ContractCreationResult
{
    public bool IsSuccess { get; set; }
    public Guid TransactionId { get; set; }
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public string? ApprovalReason { get; set; }
    public List<RiskLimitViolation> RiskViolations { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Warnings { get; set; } = new();
}