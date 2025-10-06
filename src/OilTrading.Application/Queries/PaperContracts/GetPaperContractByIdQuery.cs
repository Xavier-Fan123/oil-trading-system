using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetPaperContractByIdQuery : IRequest<PaperContractDto?>
{
    public Guid Id { get; set; }
}