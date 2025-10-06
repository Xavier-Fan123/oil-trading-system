using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 将纸质合同分配给交易组命令处理器 - Assign Paper Contract to Trade Group Command Handler
/// </summary>
public class AssignPaperContractToTradeGroupCommandHandler : IRequestHandler<AssignPaperContractToTradeGroupCommand, bool>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignPaperContractToTradeGroupCommandHandler> _logger;

    public AssignPaperContractToTradeGroupCommandHandler(
        ITradeGroupRepository tradeGroupRepository,
        IPaperContractRepository paperContractRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignPaperContractToTradeGroupCommandHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _paperContractRepository = paperContractRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(AssignPaperContractToTradeGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning paper contract {PaperContractId} to trade group {TradeGroupId} by {AssignedBy}",
            request.PaperContractId, request.TradeGroupId, request.AssignedBy);

        try
        {
            // Get the trade group
            var tradeGroup = await _tradeGroupRepository.GetByIdAsync(request.TradeGroupId, cancellationToken);
            if (tradeGroup == null)
            {
                _logger.LogWarning("Trade group {TradeGroupId} not found", request.TradeGroupId);
                return false;
            }

            // Get the paper contract
            var paperContract = await _paperContractRepository.GetByIdAsync(request.PaperContractId, cancellationToken);
            if (paperContract == null)
            {
                _logger.LogWarning("Paper contract {PaperContractId} not found", request.PaperContractId);
                return false;
            }

            // Check if already assigned
            if (paperContract.TradeGroupId == request.TradeGroupId)
            {
                _logger.LogWarning("Paper contract {PaperContractId} is already assigned to trade group {TradeGroupId}",
                    request.PaperContractId, request.TradeGroupId);
                return true;
            }

            // Assign the paper contract to the trade group
            paperContract.AssignToTradeGroup(request.TradeGroupId, request.AssignedBy);

            // Save changes
            await _paperContractRepository.UpdateAsync(paperContract);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully assigned paper contract {PaperContractId} to trade group {TradeGroupId}",
                request.PaperContractId, request.TradeGroupId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error assigning paper contract {PaperContractId} to trade group {TradeGroupId}",
                request.PaperContractId, request.TradeGroupId);
            throw;
        }
    }
}