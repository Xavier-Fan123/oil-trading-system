using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.ContractExecutionReports;

public class GetContractExecutionReportQueryHandler : IRequestHandler<GetContractExecutionReportQuery, ContractExecutionReportDto?>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IContractExecutionReportService _reportService;

    public GetContractExecutionReportQueryHandler(
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IContractExecutionReportService reportService)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _reportService = reportService;
    }

    public async Task<ContractExecutionReportDto?> Handle(
        GetContractExecutionReportQuery request,
        CancellationToken cancellationToken)
    {
        if (request.IsPurchaseContract)
        {
            var contract = await _purchaseContractRepository.GetByIdAsync(request.ContractId);
            if (contract == null)
                return null;

            return await _reportService.GeneratePurchaseContractExecutionReportAsync(contract, cancellationToken);
        }
        else
        {
            var contract = await _salesContractRepository.GetByIdAsync(request.ContractId);
            if (contract == null)
                return null;

            return await _reportService.GenerateSalesContractExecutionReportAsync(contract, cancellationToken);
        }
    }
}
