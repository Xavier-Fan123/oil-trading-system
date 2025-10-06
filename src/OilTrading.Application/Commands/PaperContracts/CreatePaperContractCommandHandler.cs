using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.PaperContracts;

public class CreatePaperContractCommandHandler : IRequestHandler<CreatePaperContractCommand, PaperContractDto>
{
    private readonly IPaperContractRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePaperContractCommandHandler> _logger;

    public CreatePaperContractCommandHandler(
        IPaperContractRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreatePaperContractCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaperContractDto> Handle(CreatePaperContractCommand request, CancellationToken cancellationToken)
    {
        // Parse position enum
        if (!Enum.TryParse<PositionType>(request.Position, true, out var positionType))
        {
            throw new ArgumentException($"Invalid position type: {request.Position}");
        }

        // Create the paper contract entity
        var paperContract = new PaperContract
        {
            ContractMonth = request.ContractMonth,
            ProductType = request.ProductType,
            Position = positionType,
            Quantity = request.Quantity,
            LotSize = request.LotSize,
            EntryPrice = request.EntryPrice,
            TradeDate = request.TradeDate,
            Status = PaperContractStatus.Open,
            TradeReference = request.TradeReference,
            CounterpartyName = request.CounterpartyName,
            Notes = request.Notes
        };
        paperContract.SetId(Guid.NewGuid());
        paperContract.SetCreated(request.CreatedBy);

        await _repository.AddAsync(paperContract, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Paper contract created: {ContractId} - {ProductType} {ContractMonth} {Position} {Quantity}",
            paperContract.Id, paperContract.ProductType, paperContract.ContractMonth, 
            paperContract.Position, paperContract.Quantity);

        // Return DTO
        return new PaperContractDto
        {
            Id = paperContract.Id,
            ContractMonth = paperContract.ContractMonth,
            ProductType = paperContract.ProductType,
            Position = paperContract.Position.ToString(),
            Quantity = paperContract.Quantity,
            LotSize = paperContract.LotSize,
            EntryPrice = paperContract.EntryPrice,
            CurrentPrice = paperContract.CurrentPrice,
            TradeDate = paperContract.TradeDate,
            SettlementDate = paperContract.SettlementDate,
            Status = paperContract.Status.ToString(),
            RealizedPnL = paperContract.RealizedPnL,
            UnrealizedPnL = paperContract.UnrealizedPnL,
            DailyPnL = paperContract.DailyPnL,
            LastMTMDate = paperContract.LastMTMDate,
            TradeReference = paperContract.TradeReference,
            CounterpartyName = paperContract.CounterpartyName,
            Notes = paperContract.Notes,
            CreatedAt = paperContract.CreatedAt,
            CreatedBy = paperContract.CreatedBy
        };
    }
}