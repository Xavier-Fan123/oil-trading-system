using MediatR;

namespace OilTrading.Application.Commands.SalesContracts;

public class DeleteSalesContractCommand : IRequest
{
    public Guid Id { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}