using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;

namespace OilTrading.Application.Queries.PurchaseContracts;

public class GetAvailablePurchaseContractsQuery : IRequest<PagedResult<AvailablePurchaseContractDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? ProductId { get; set; }
    public Guid? TraderId { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class AvailablePurchaseContractDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string TraderName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public decimal? ContractValue { get; set; }
    public string? ContractValueCurrency { get; set; }
    public DateTime? LaycanStart { get; set; }
    public DateTime? LaycanEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}