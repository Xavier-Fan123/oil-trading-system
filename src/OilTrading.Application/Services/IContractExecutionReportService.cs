using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

public interface IContractExecutionReportService
{
    /// <summary>
    /// Generates an execution report for a purchase contract
    /// </summary>
    Task<ContractExecutionReportDto?> GeneratePurchaseContractExecutionReportAsync(
        PurchaseContract contract,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an execution report for a sales contract
    /// </summary>
    Task<ContractExecutionReportDto?> GenerateSalesContractExecutionReportAsync(
        SalesContract contract,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates execution reports for all contracts
    /// </summary>
    Task GenerateAllExecutionReportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates execution report for a specific contract by ID
    /// </summary>
    Task<ContractExecutionReportDto?> GenerateExecutionReportByContractIdAsync(
        Guid contractId,
        bool isPurchaseContract,
        CancellationToken cancellationToken = default);
}
