using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;

namespace OilTrading.Application.Queries.ShippingOperations;

public class GetShippingOperationsQuery : IRequest<PagedResult<ShippingOperationSummaryDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? ShippingNumber { get; set; }
    public string? Status { get; set; }
    public Guid? ContractId { get; set; }
    public string? VesselName { get; set; }
    public DateTime? LoadPortETAFrom { get; set; }
    public DateTime? LoadPortETATo { get; set; }
    public DateTime? DischargePortETAFrom { get; set; }
    public DateTime? DischargePortETATo { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}