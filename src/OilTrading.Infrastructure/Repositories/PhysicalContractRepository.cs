using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class PhysicalContractRepository : Repository<PhysicalContract>, IPhysicalContractRepository
{
    public PhysicalContractRepository(ApplicationDbContext context) : base(context) { }

    public override async Task<PhysicalContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PhysicalContract?> GetByContractNumberAsync(string contractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .FirstOrDefaultAsync(x => x.ContractNumber == contractNumber, cancellationToken);
    }

    public override async Task<IReadOnlyList<PhysicalContract>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .OrderByDescending(x => x.ContractDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetByPartnerAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Where(x => x.TradingPartnerId == partnerId)
            .OrderByDescending(x => x.ContractDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetByTypeAsync(PhysicalContractType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Where(x => x.ContractType == type)
            .OrderByDescending(x => x.ContractDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.Status == PhysicalContractStatus.Active)
            .OrderByDescending(x => x.ContractDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetActiveContractsForPositionAsync(CancellationToken cancellationToken = default)
    {
        // Optimized query for position calculations - removed unnecessary Include
        return await _dbSet
            .Where(x => x.Status == PhysicalContractStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetUnsettledContractsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Where(x => !x.IsFullySettled && x.Status != PhysicalContractStatus.Cancelled)
            .OrderByDescending(x => x.ContractDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetByLaycanPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Where(x => x.LaycanStart >= startDate && x.LaycanEnd <= endDate)
            .OrderBy(x => x.LaycanStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateContractNumberAsync(PhysicalContractType type, CancellationToken cancellationToken = default)
    {
        var prefix = type == PhysicalContractType.Purchase ? "PC" : "SC";
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        
        var pattern = $"{prefix}{year:D4}{month:D2}";
        
        var lastContract = await _dbSet
            .Where(x => x.ContractNumber.StartsWith(pattern))
            .OrderByDescending(x => x.ContractNumber)
            .Select(x => x.ContractNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastContract))
        {
            return $"{pattern}001";
        }

        var lastNumberStr = lastContract.Substring(pattern.Length);
        if (int.TryParse(lastNumberStr, out int lastNumber))
        {
            return $"{pattern}{(lastNumber + 1):D3}";
        }

        return $"{pattern}001";
    }

    public async Task<decimal> CalculatePartnerExposureAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        var exposure = await _dbSet
            .Where(x => x.TradingPartnerId == partnerId && 
                       !x.IsFullySettled && 
                       x.Status != PhysicalContractStatus.Cancelled)
            .SumAsync(x => x.OutstandingAmount ?? 0, cancellationToken);

        return exposure;
    }

    public async Task<IReadOnlyList<PhysicalContract>> GetByProductAndPeriodAsync(
        string productType, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.TradingPartner)
            .Where(x => x.ProductType == productType && 
                       x.LaycanStart >= startDate && 
                       x.LaycanEnd <= endDate)
            .OrderBy(x => x.LaycanStart)
            .ToListAsync(cancellationToken);
    }
}