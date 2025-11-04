using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class ContractExecutionReportRepository : Repository<ContractExecutionReport>, IContractExecutionReportRepository
{
    public ContractExecutionReportRepository(ApplicationDbContext context) : base(context) { }

    public async Task<ContractExecutionReport?> GetByContractIdAsync(Guid contractId, string contractType)
    {
        return await _dbSet
            .Where(r => r.ContractId == contractId && r.ContractType == contractType)
            .OrderByDescending(r => r.ReportGeneratedDate)
            .FirstOrDefaultAsync();
    }

    public IQueryable<ContractExecutionReport> GetAllAsQueryable()
    {
        return _dbSet.AsQueryable();
    }
}
