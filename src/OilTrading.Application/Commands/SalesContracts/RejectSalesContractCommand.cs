using MediatR;

namespace OilTrading.Application.Commands.SalesContracts;

public class RejectSalesContractCommand : IRequest
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string? RejectedBy { get; set; }
}