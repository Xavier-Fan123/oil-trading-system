using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetPaperContractsQuery : IRequest<IEnumerable<PaperContractListDto>>
{
    public string? ProductType { get; set; }
    public string? ContractMonth { get; set; }
    public string? Status { get; set; }
}