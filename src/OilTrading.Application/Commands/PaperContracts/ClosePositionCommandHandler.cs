using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.PaperContracts;

public class ClosePositionCommandHandler : IRequestHandler<ClosePositionCommand, PaperContractDto?>
{
    private readonly IPaperContractRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClosePositionCommandHandler> _logger;

    public ClosePositionCommandHandler(
        IPaperContractRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ClosePositionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaperContractDto?> Handle(ClosePositionCommand request, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(request.ContractId, cancellationToken);
        
        if (contract == null)
        {
            _logger.LogWarning("Paper contract not found: {ContractId}", request.ContractId);
            return null;
        }

        if (contract.Status != PaperContractStatus.Open)
        {
            throw new InvalidOperationException($"Cannot close contract {request.ContractId}. Current status: {contract.Status}");
        }

        // Close the position
        contract.ClosePosition(request.ClosingPrice, request.CloseDate);
        contract.SetUpdatedBy(request.ClosedBy);

        await _repository.UpdateAsync(contract, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Paper contract closed: {ContractId} - Closing Price: {ClosingPrice}, Realized P&L: {RealizedPnL}",
            contract.Id, request.ClosingPrice, contract.RealizedPnL);

        // Return DTO
        return new PaperContractDto
        {
            Id = contract.Id,
            ContractMonth = contract.ContractMonth,
            ProductType = contract.ProductType,
            Position = contract.Position.ToString(),
            Quantity = contract.Quantity,
            LotSize = contract.LotSize,
            EntryPrice = contract.EntryPrice,
            CurrentPrice = contract.CurrentPrice,
            TradeDate = contract.TradeDate,
            SettlementDate = contract.SettlementDate,
            Status = contract.Status.ToString(),
            RealizedPnL = contract.RealizedPnL,
            UnrealizedPnL = contract.UnrealizedPnL,
            DailyPnL = contract.DailyPnL,
            LastMTMDate = contract.LastMTMDate,
            TradeReference = contract.TradeReference,
            CounterpartyName = contract.CounterpartyName,
            Notes = contract.Notes,
            CreatedAt = contract.CreatedAt,
            CreatedBy = contract.CreatedBy,
            UpdatedAt = contract.UpdatedAt,
            UpdatedBy = contract.UpdatedBy
        };
    }
}