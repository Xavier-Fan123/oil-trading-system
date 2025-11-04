using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Command to add a charge to a settlement
/// </summary>
public class AddChargeCommand : IRequest<SettlementChargeDto>
{
    public Guid SettlementId { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? IncurredDate { get; set; }
    public string? ReferenceDocument { get; set; }
    public string? Notes { get; set; }
    public string AddedBy { get; set; } = "System";
    public bool IsPurchaseSettlement { get; set; } = true;
}

/// <summary>
/// Handler for AddChargeCommand
/// </summary>
public class AddChargeCommandHandler : IRequestHandler<AddChargeCommand, SettlementChargeDto>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public AddChargeCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<SettlementChargeDto> Handle(AddChargeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Route to appropriate service based on settlement type
            var chargeDto = request.IsPurchaseSettlement
                ? await _purchaseSettlementService.AddChargeAsync(
                    request.SettlementId,
                    request.ChargeType,
                    request.Description,
                    request.Amount,
                    request.Currency,
                    request.IncurredDate,
                    request.ReferenceDocument,
                    request.Notes,
                    request.AddedBy,
                    cancellationToken)
                : await _salesSettlementService.AddChargeAsync(
                    request.SettlementId,
                    request.ChargeType,
                    request.Description,
                    request.Amount,
                    request.Currency,
                    request.IncurredDate,
                    request.ReferenceDocument,
                    request.Notes,
                    request.AddedBy,
                    cancellationToken);

            return chargeDto;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
