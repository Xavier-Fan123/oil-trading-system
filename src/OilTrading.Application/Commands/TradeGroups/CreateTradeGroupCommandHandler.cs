using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 创建交易组命令处理器 - Create Trade Group Command Handler
/// </summary>
public class CreateTradeGroupCommandHandler : IRequestHandler<CreateTradeGroupCommand, Guid>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTradeGroupCommandHandler> _logger;

    public CreateTradeGroupCommandHandler(
        ITradeGroupRepository tradeGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateTradeGroupCommandHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateTradeGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating trade group: {GroupName} with strategy {StrategyType}", 
            request.GroupName, request.StrategyType);

        // Check if group name already exists
        var existingGroup = await _tradeGroupRepository.GetByNameAsync(request.GroupName, cancellationToken);
        if (existingGroup != null)
        {
            throw new InvalidOperationException($"Trade group with name '{request.GroupName}' already exists");
        }

        // Create the trade group
        var tradeGroup = new TradeGroup(
            request.GroupName,
            (StrategyType)request.StrategyType,
            request.Description,
            request.CreatedBy);

        // Set risk parameters if provided
        if (request.ExpectedRiskLevel.HasValue)
        {
            tradeGroup.SetRiskParameters(
                (RiskLevel)request.ExpectedRiskLevel.Value,
                request.MaxAllowedLoss,
                request.TargetProfit,
                request.CreatedBy);
        }

        // Save to repository
        await _tradeGroupRepository.AddAsync(tradeGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trade group created successfully with ID: {TradeGroupId}", tradeGroup.Id);

        return tradeGroup.Id;
    }
}