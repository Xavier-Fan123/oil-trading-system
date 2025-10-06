using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 从交易组移除纸质合同命令处理器 - Remove Paper Contract from Trade Group Command Handler
/// </summary>
public class RemovePaperContractFromTradeGroupCommandHandler : IRequestHandler<RemovePaperContractFromTradeGroupCommand, bool>
{
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemovePaperContractFromTradeGroupCommandHandler> _logger;

    public RemovePaperContractFromTradeGroupCommandHandler(
        IPaperContractRepository paperContractRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemovePaperContractFromTradeGroupCommandHandler> logger)
    {
        _paperContractRepository = paperContractRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RemovePaperContractFromTradeGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Removing paper contract {PaperContractId} from trade group by {RemovedBy}",
            request.PaperContractId, request.RemovedBy);

        try
        {
            // Get the paper contract
            var paperContract = await _paperContractRepository.GetByIdAsync(request.PaperContractId, cancellationToken);
            if (paperContract == null)
            {
                _logger.LogWarning("Paper contract {PaperContractId} not found", request.PaperContractId);
                return false;
            }

            // Check if it's assigned to any trade group
            if (paperContract.TradeGroupId == null)
            {
                _logger.LogWarning("Paper contract {PaperContractId} is not assigned to any trade group",
                    request.PaperContractId);
                return true;
            }

            var previousTradeGroupId = paperContract.TradeGroupId;

            // Remove from trade group
            paperContract.RemoveFromTradeGroup(request.RemovedBy);

            // Save changes
            await _paperContractRepository.UpdateAsync(paperContract);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully removed paper contract {PaperContractId} from trade group {TradeGroupId}",
                request.PaperContractId, previousTradeGroupId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error removing paper contract {PaperContractId} from trade group",
                request.PaperContractId);
            throw;
        }
    }
}