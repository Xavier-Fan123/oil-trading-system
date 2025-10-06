using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetPaperContractByIdQueryHandler : IRequestHandler<GetPaperContractByIdQuery, PaperContractDto?>
{
    private readonly IPaperContractRepository _repository;

    public GetPaperContractByIdQueryHandler(IPaperContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaperContractDto?> Handle(GetPaperContractByIdQuery request, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (contract == null)
            return null;

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
            IsSpread = contract.IsSpread,
            Leg1Product = contract.Leg1Product,
            Leg2Product = contract.Leg2Product,
            SpreadValue = contract.SpreadValue,
            VaRValue = contract.VaRValue,
            Volatility = contract.Volatility,
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