using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.ContractExecutionReports;

public class GetContractExecutionReportQuery : IRequest<ContractExecutionReportDto?>
{
    public Guid ContractId { get; set; }
    public bool IsPurchaseContract { get; set; } = true;
}
