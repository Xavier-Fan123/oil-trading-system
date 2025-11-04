using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Command to update a charge in a settlement
/// </summary>
public class UpdateChargeCommand : IRequest<SettlementChargeDto>
{
    public Guid SettlementId { get; set; }
    public Guid ChargeId { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string UpdatedBy { get; set; } = "System";
    public bool IsPurchaseSettlement { get; set; } = true;
}

/// <summary>
/// Handler for UpdateChargeCommand
/// </summary>
public class UpdateChargeCommandHandler : IRequestHandler<UpdateChargeCommand, SettlementChargeDto>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public UpdateChargeCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<SettlementChargeDto> Handle(UpdateChargeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Route to appropriate service based on settlement type
            var chargeDto = request.IsPurchaseSettlement
                ? await _purchaseSettlementService.UpdateChargeAsync(
                    request.SettlementId,
                    request.ChargeId,
                    request.Description,
                    request.Amount,
                    request.UpdatedBy,
                    cancellationToken)
                : await _salesSettlementService.UpdateChargeAsync(
                    request.SettlementId,
                    request.ChargeId,
                    request.Description,
                    request.Amount,
                    request.UpdatedBy,
                    cancellationToken);

            return chargeDto;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
