using MediatR;

namespace OilTrading.Application.Commands.ShippingOperations;

public class RecordLiftingOperationCommand : IRequest
{
    public Guid ShippingOperationId { get; set; }
    public DateTime? NorDate { get; set; }
    public DateTime? BillOfLadingDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string? ActualQuantityUnit { get; set; }
    public string? Notes { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}