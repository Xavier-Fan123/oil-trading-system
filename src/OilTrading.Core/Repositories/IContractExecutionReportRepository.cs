using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IContractExecutionReportRepository : IRepository<ContractExecutionReport>
{
    /// <summary>
    /// Get report by contract ID and type
    /// </summary>
    Task<ContractExecutionReport?> GetByContractIdAsync(Guid contractId, string contractType);

    /// <summary>
    /// Get all reports as queryable for advanced filtering
    /// </summary>
    IQueryable<ContractExecutionReport> GetAllAsQueryable();
}
