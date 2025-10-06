using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetPnLSummaryQuery : IRequest<PnLSummaryDto>
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}