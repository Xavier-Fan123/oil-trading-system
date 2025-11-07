using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OilTrading.Application.Commands.Settlements;
using OilTrading.Core.Entities;

namespace OilTrading.Application.EventHandlers;

/// <summary>
/// Event handler for automatically creating settlements when contracts complete
/// Subscribes to PurchaseContractCompletionNotification and SalesContractCompletionNotification
/// Creates initial settlement in Draft status with auto-populated contract data
/// </summary>
public class AutoSettlementEventHandler :
    INotificationHandler<PurchaseContractCompletionNotification>,
    INotificationHandler<SalesContractCompletionNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AutoSettlementEventHandler> _logger;
    private readonly AutoSettlementOptions _options;

    public AutoSettlementEventHandler(
        IMediator mediator,
        ILogger<AutoSettlementEventHandler> logger,
        IOptions<AutoSettlementOptions> options)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? new AutoSettlementOptions();
    }

    /// <summary>
    /// Handle purchase contract completion - create AP settlement automatically
    /// </summary>
    public async Task Handle(PurchaseContractCompletionNotification @event, CancellationToken cancellationToken)
    {
        if (!_options.EnableAutoSettlementOnCompletion)
        {
            _logger.LogDebug(
                "Auto-settlement disabled. Skipping settlement creation for purchase contract {ContractNumber}",
                @event.ContractNumber);
            return;
        }

        try
        {
            _logger.LogInformation(
                "Purchase contract completed. Creating auto-settlement for contract {ContractId} ({ContractNumber})",
                @event.ContractId,
                @event.ContractNumber);

            var command = new CreatePurchaseSettlementCommand
            {
                PurchaseContractId = @event.ContractId,
                ExternalContractNumber = "", // Will be populated from contract data in handler
                DocumentNumber = "", // Can be generated or left empty initially
                DocumentType = _options.DefaultDocumentType ?? DocumentType.BillOfLading,
                DocumentDate = DateTime.UtcNow,
                ActualQuantityMT = 0, // Will be populated when settlement form is filled
                ActualQuantityBBL = 0, // Will be populated when settlement form is filled
                Notes = $"Auto-generated settlement for contract {@event.ContractNumber}",
                SettlementCurrency = _options.DefaultCurrency ?? "USD",
                AutoCalculatePrices = _options.AutoCalculatePrices,
                AutoTransitionStatus = _options.AutoTransitionStatus,
                CreatedBy = "AutoSettlementService"
            };

            var settlementId = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Successfully created auto-settlement {SettlementId} for purchase contract {ContractId}",
                settlementId,
                @event.ContractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create auto-settlement for purchase contract {ContractId} ({ContractNumber})",
                @event.ContractId,
                @event.ContractNumber);

            if (_options.FailOnError)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Handle sales contract completion - create AR settlement automatically
    /// </summary>
    public async Task Handle(SalesContractCompletionNotification @event, CancellationToken cancellationToken)
    {
        if (!_options.EnableAutoSettlementOnCompletion)
        {
            _logger.LogDebug(
                "Auto-settlement disabled. Skipping settlement creation for sales contract {ContractNumber}",
                @event.ContractNumber);
            return;
        }

        try
        {
            _logger.LogInformation(
                "Sales contract completed. Creating auto-settlement for contract {ContractId} ({ContractNumber})",
                @event.ContractId,
                @event.ContractNumber);

            var command = new CreateSalesSettlementCommand
            {
                SalesContractId = @event.ContractId,
                ExternalContractNumber = "", // Will be populated from contract data in handler
                DocumentNumber = "", // Can be generated or left empty initially
                DocumentType = _options.DefaultDocumentType ?? DocumentType.BillOfLading,
                DocumentDate = DateTime.UtcNow,
                ActualQuantityMT = 0, // Will be populated when settlement form is filled
                ActualQuantityBBL = 0, // Will be populated when settlement form is filled
                Notes = $"Auto-generated settlement for contract {@event.ContractNumber}",
                SettlementCurrency = _options.DefaultCurrency ?? "USD",
                AutoCalculatePrices = _options.AutoCalculatePrices,
                AutoTransitionStatus = _options.AutoTransitionStatus,
                CreatedBy = "AutoSettlementService"
            };

            var settlementId = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Successfully created auto-settlement {SettlementId} for sales contract {ContractId}",
                settlementId,
                @event.ContractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create auto-settlement for sales contract {ContractId} ({ContractNumber})",
                @event.ContractId,
                @event.ContractNumber);

            if (_options.FailOnError)
            {
                throw;
            }
        }
    }
}

/// <summary>
/// Configuration options for auto-settlement feature
/// </summary>
public class AutoSettlementOptions
{
    /// <summary>
    /// Enable/disable automatic settlement creation when contracts complete
    /// Default: true
    /// </summary>
    public bool EnableAutoSettlementOnCompletion { get; set; } = true;

    /// <summary>
    /// Automatically calculate settlement prices after creation
    /// Default: false (user must manually enter B/L quantities and pricing info)
    /// </summary>
    public bool AutoCalculatePrices { get; set; } = false;

    /// <summary>
    /// Automatically transition settlement status through workflow (Draft -> DataEntered -> Calculated -> ...)
    /// Default: false (user must manually progress through settlement steps)
    /// </summary>
    public bool AutoTransitionStatus { get; set; } = false;

    /// <summary>
    /// Default document type for auto-generated settlements
    /// Default: BillOfLading
    /// </summary>
    public DocumentType? DefaultDocumentType { get; set; } = DocumentType.BillOfLading;

    /// <summary>
    /// Default currency for auto-generated settlements
    /// Default: USD
    /// </summary>
    public string? DefaultCurrency { get; set; } = "USD";

    /// <summary>
    /// Throw exception if auto-settlement creation fails, preventing contract completion
    /// Default: false (log error but allow contract to complete)
    /// </summary>
    public bool FailOnError { get; set; } = false;
}
