using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 更新交易组风险参数命令处理器 - Update Trade Group Risk Parameters Command Handler
/// </summary>
public class UpdateTradeGroupRiskParametersCommandHandler : IRequestHandler<UpdateTradeGroupRiskParametersCommand, bool>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTradeGroupRiskParametersCommandHandler> _logger;

    public UpdateTradeGroupRiskParametersCommandHandler(
        ITradeGroupRepository tradeGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTradeGroupRiskParametersCommandHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateTradeGroupRiskParametersCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating risk parameters for trade group {TradeGroupId} by {UpdatedBy}",
            request.TradeGroupId, request.UpdatedBy);

        try
        {
            // Get the trade group
            var tradeGroup = await _tradeGroupRepository.GetByIdAsync(request.TradeGroupId, cancellationToken);
            if (tradeGroup == null)
            {
                _logger.LogWarning("Trade group {TradeGroupId} not found", request.TradeGroupId);
                return false;
            }

            // Update risk parameters if provided
            if (request.ExpectedRiskLevel.HasValue || request.MaxAllowedLoss.HasValue || request.TargetProfit.HasValue)
            {
                var riskLevel = request.ExpectedRiskLevel.HasValue 
                    ? (RiskLevel)request.ExpectedRiskLevel.Value 
                    : tradeGroup.ExpectedRiskLevel ?? RiskLevel.Medium; // Default fallback

                var maxLoss = request.MaxAllowedLoss ?? tradeGroup.MaxAllowedLoss;
                var targetProfit = request.TargetProfit ?? tradeGroup.TargetProfit;

                tradeGroup.SetRiskParameters(riskLevel, maxLoss, targetProfit, request.UpdatedBy);
            }

            // Save changes
            await _tradeGroupRepository.UpdateAsync(tradeGroup);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated risk parameters for trade group {TradeGroupId}",
                request.TradeGroupId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error updating risk parameters for trade group {TradeGroupId}",
                request.TradeGroupId);
            throw;
        }
    }
}