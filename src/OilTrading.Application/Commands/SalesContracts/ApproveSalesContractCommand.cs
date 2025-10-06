using MediatR;

namespace OilTrading.Application.Commands.SalesContracts;

public class ApproveSalesContractCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Comments { get; set; }
    public string? ApprovedBy { get; set; }
}