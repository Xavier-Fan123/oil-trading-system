using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Handler for bulk finalize settlements command
/// </summary>
public class BulkFinalizeSettlementsCommandHandler : IRequestHandler<BulkFinalizeSettlementsCommand, BulkOperationResultDto>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BulkFinalizeSettlementsCommandHandler> _logger;

    public BulkFinalizeSettlementsCommandHandler(
        IMediator mediator,
        ILogger<BulkFinalizeSettlementsCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BulkOperationResultDto> Handle(BulkFinalizeSettlementsCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResultDto();

        if (request.SettlementIds == null || request.SettlementIds.Count == 0)
        {
            _logger.LogWarning("Bulk finalize command received with no settlement IDs");
            return result;
        }

        _logger.LogInformation("Processing bulk finalize for {Count} settlements", request.SettlementIds.Count);

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

                // Attempt to finalize the settlement
                var finalizeCommand = new FinalizeSettlementCommand
                {
                    SettlementId = settlementId,
                    FinalizedBy = request.FinalizedBy
                };

                await _mediator.Send(finalizeCommand, cancellationToken);

                result.SuccessCount++;
                result.Details.Add(new BulkOperationDetailDto
                {
                    SettlementId = settlementIdStr,
                    Status = "success"
                });

                _logger.LogInformation("Successfully finalized settlement {SettlementId}", settlementId);
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

                _logger.LogError(ex, "Failed to finalize settlement {SettlementId}", settlementIdStr);
            }
        }

        _logger.LogInformation(
            "Bulk finalize completed: {SuccessCount} successful, {FailureCount} failed",
            result.SuccessCount,
            result.FailureCount);

        return result;
    }
}
