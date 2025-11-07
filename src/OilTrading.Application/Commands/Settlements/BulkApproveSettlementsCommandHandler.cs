using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Application.Queries.Settlements;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Handler for bulk approve settlements command
/// </summary>
public class BulkApproveSettlementsCommandHandler : IRequestHandler<BulkApproveSettlementsCommand, BulkOperationResultDto>
{
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<BulkApproveSettlementsCommandHandler> _logger;

    public BulkApproveSettlementsCommandHandler(
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository,
        IMediator mediator,
        ILogger<BulkApproveSettlementsCommandHandler> logger)
    {
        _purchaseSettlementRepository = purchaseSettlementRepository;
        _salesSettlementRepository = salesSettlementRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BulkOperationResultDto> Handle(BulkApproveSettlementsCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResultDto();

        if (request.SettlementIds == null || request.SettlementIds.Count == 0)
        {
            _logger.LogWarning("Bulk approve command received with no settlement IDs");
            return result;
        }

        _logger.LogInformation("Processing bulk approve for {Count} settlements", request.SettlementIds.Count);

        foreach (var settlementIdStr in request.SettlementIds)
        {
            try
            {
                // Convert string ID to Guid
                if (!Guid.TryParse(settlementIdStr, out var settlementId))
                {
                    result.FailureCount++;
                    result.Details.Add(new BulkOperationDetailDto
                    {
                        SettlementId = settlementIdStr,
                        Status = "failure",
                        Message = "Invalid settlement ID format"
                    });
                    _logger.LogWarning("Invalid settlement ID format: {SettlementId}", settlementIdStr);
                    continue;
                }

                // Attempt to approve the settlement
                var approveCommand = new ApproveSettlementCommand
                {
                    SettlementId = settlementId,
                    ApprovedBy = request.ApprovedBy
                };

                await _mediator.Send(approveCommand, cancellationToken);

                result.SuccessCount++;
                result.Details.Add(new BulkOperationDetailDto
                {
                    SettlementId = settlementIdStr,
                    Status = "success"
                });

                _logger.LogInformation("Successfully approved settlement {SettlementId}", settlementId);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Details.Add(new BulkOperationDetailDto
                {
                    SettlementId = settlementIdStr,
                    Status = "failure",
                    Message = ex.Message
                });

                _logger.LogError(ex, "Failed to approve settlement {SettlementId}", settlementIdStr);
            }
        }

        _logger.LogInformation(
            "Bulk approve completed: {SuccessCount} successful, {FailureCount} failed",
            result.SuccessCount,
            result.FailureCount);

        return result;
    }
}
