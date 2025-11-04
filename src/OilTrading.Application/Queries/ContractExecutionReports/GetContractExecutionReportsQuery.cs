using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Common;

namespace OilTrading.Application.Queries.ContractExecutionReports;

public class GetContractExecutionReportsQuery : IRequest<PagedResult<ContractExecutionReportDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? ContractType { get; set; } // null = all, "Purchase", "Sales"
    public string? ExecutionStatus { get; set; } // "OnTrack", "Delayed", "Completed", "Cancelled"
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? TradingPartnerId { get; set; }
    public Guid? ProductId { get; set; }
    public string? SortBy { get; set; } = "ReportGeneratedDate"; // Field to sort by
    public bool SortDescending { get; set; } = true;
}
