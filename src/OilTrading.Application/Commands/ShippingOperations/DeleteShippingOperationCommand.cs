using MediatR;

namespace OilTrading.Application.Commands.ShippingOperations;

public class DeleteShippingOperationCommand : IRequest
{
    public Guid Id { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}