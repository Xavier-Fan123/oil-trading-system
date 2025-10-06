using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.TradeGroups;

/// <summary>
/// 分配合约到交易组命令处理器 - Assign Contract to Trade Group Command Handler
/// </summary>
public class AssignContractToTradeGroupCommandHandler : IRequestHandler<AssignContractToTradeGroupCommand, bool>
{
    private readonly ITradeGroupRepository _tradeGroupRepository;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignContractToTradeGroupCommandHandler> _logger;

    public AssignContractToTradeGroupCommandHandler(
        ITradeGroupRepository tradeGroupRepository,
        IPaperContractRepository paperContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignContractToTradeGroupCommandHandler> logger)
    {
        _tradeGroupRepository = tradeGroupRepository;
        _paperContractRepository = paperContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(AssignContractToTradeGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning {ContractType} {ContractId} to trade group {TradeGroupId}", 
            request.ContractType, request.ContractId, request.TradeGroupId);

        // Verify trade group exists
        var tradeGroup = await _tradeGroupRepository.GetByIdAsync(request.TradeGroupId);
        if (tradeGroup == null)
        {
            throw new NotFoundException($"Trade group with ID {request.TradeGroupId} not found");
        }

        // Assign contract based on type
        switch (request.ContractType.ToLower())
        {
            case "papercontract":
                await AssignPaperContract(request, cancellationToken);
                break;
            case "purchasecontract":
                await AssignPurchaseContract(request, cancellationToken);
                break;
            case "salescontract":
                await AssignSalesContract(request, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unknown contract type: {request.ContractType}");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully assigned {ContractType} {ContractId} to trade group {TradeGroupId}", 
            request.ContractType, request.ContractId, request.TradeGroupId);

        return true;
    }

    private async Task AssignPaperContract(AssignContractToTradeGroupCommand request, CancellationToken cancellationToken)
    {
        var contract = await _paperContractRepository.GetByIdAsync(request.ContractId);
        if (contract == null)
        {
            throw new NotFoundException($"Paper contract with ID {request.ContractId} not found");
        }

        contract.AssignToTradeGroup(request.TradeGroupId, request.UpdatedBy);
        await _paperContractRepository.UpdateAsync(contract);
    }

    private async Task AssignPurchaseContract(AssignContractToTradeGroupCommand request, CancellationToken cancellationToken)
    {
        var contract = await _purchaseContractRepository.GetByIdAsync(request.ContractId);
        if (contract == null)
        {
            throw new NotFoundException($"Purchase contract with ID {request.ContractId} not found");
        }

        contract.AssignToTradeGroup(request.TradeGroupId, request.UpdatedBy);
        await _purchaseContractRepository.UpdateAsync(contract);
    }

    private async Task AssignSalesContract(AssignContractToTradeGroupCommand request, CancellationToken cancellationToken)
    {
        var contract = await _salesContractRepository.GetByIdAsync(request.ContractId);
        if (contract == null)
        {
            throw new NotFoundException($"Sales contract with ID {request.ContractId} not found");
        }

        contract.AssignToTradeGroup(request.TradeGroupId, request.UpdatedBy);
        await _salesContractRepository.UpdateAsync(contract);
    }
}