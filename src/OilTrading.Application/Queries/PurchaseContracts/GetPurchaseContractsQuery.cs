using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetPurchaseContractsQuery : IRequest<PagedResult<PurchaseContractListDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? ContractNumber { get; set; }
    public string? ExternalContractNumber { get; set; }
    public string? Status { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? TraderId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public DateTime? LaycanFrom { get; set; }
    public DateTime? LaycanTo { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}