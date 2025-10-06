using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 关闭交易组命令处理器 - Close Trade Group Command Handler
/// </summary>
public class CloseTradeGroupCommandHandler : IRequestHandler<CloseTradeGroupCommand, bool>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CloseTradeGroupCommandHandler> _logger;

    public CloseTradeGroupCommandHandler(
        ITradeGroupRepository tradeGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<CloseTradeGroupCommandHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(CloseTradeGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to close trade group with ID: {TradeGroupId}", request.TradeGroupId);

        var tradeGroup = await _tradeGroupRepository.GetWithContractsAsync(request.TradeGroupId, cancellationToken);
        
        if (tradeGroup == null)
        {
            throw new NotFoundException($"Trade group with ID {request.TradeGroupId} not found");
        }

        try
        {
            tradeGroup.Close(request.ClosedBy);
            await _tradeGroupRepository.UpdateAsync(tradeGroup);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully closed trade group {GroupName} (ID: {TradeGroupId})", 
                tradeGroup.GroupName, request.TradeGroupId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close trade group {TradeGroupId}: {ErrorMessage}", 
                request.TradeGroupId, ex.Message);
            throw;
        }
    }
}