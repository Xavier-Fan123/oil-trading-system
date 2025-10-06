using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetOpenPositionsQueryHandler : IRequestHandler<GetOpenPositionsQuery, IEnumerable<PaperContractListDto>>
{
    private readonly IPaperContractRepository _repository;

    public GetOpenPositionsQueryHandler(IPaperContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaperContractListDto>> Handle(GetOpenPositionsQuery request, CancellationToken cancellationToken)
    {
        var contracts = await _repository.GetOpenPositionsAsync(cancellationToken);

        return contracts.Select(c => new PaperContractListDto
        {
            Id = c.Id,
            ContractMonth = c.ContractMonth,
            ProductType = c.ProductType,
            Position = c.Position.ToString(),
            Quantity = c.Quantity,
            EntryPrice = c.EntryPrice,
            CurrentPrice = c.CurrentPrice,
            UnrealizedPnL = c.UnrealizedPnL,
            Status = c.Status.ToString(),
            TradeDate = c.TradeDate
        });
    }
}