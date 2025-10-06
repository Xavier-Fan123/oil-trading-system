using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class TradingPartnerRepository : Repository<TradingPartner>, ITradingPartnerRepository
{
    public TradingPartnerRepository(ApplicationDbContext context) : base(context) { }

    public async Task<TradingPartner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<TradingPartner>> GetByTypeAsync(TradingPartnerType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.Type == type || x.Type == TradingPartnerType.Both)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TradingPartner>> GetActivePartnersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TradingPartner>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive && (x.Type == TradingPartnerType.Supplier || x.Type == TradingPartnerType.Both))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TradingPartner>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive && (x.Type == TradingPartnerType.Customer || x.Type == TradingPartnerType.Both))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TradingPartner>> GetByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.Country == country && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(x => x.Code == code);
        
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<TradingPartner?> GetByCompanyCodeAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.CompanyCode == companyCode, cancellationToken);
    }

    public async Task<IReadOnlyList<TradingPartner>> GetByCreditStatusAsync(bool exceeded, CancellationToken cancellationToken = default)
    {
        if (exceeded)
        {
            return await _dbSet
                .Where(x => x.IsActive && x.CurrentExposure > x.CreditLimit)
                .OrderByDescending(x => x.CurrentExposure - x.CreditLimit)
                .ToListAsync(cancellationToken);
        }
        else
        {
            return await _dbSet
                .Where(x => x.IsActive && x.CurrentExposure <= x.CreditLimit)
                .OrderBy(x => x.CompanyName)
                .ToListAsync(cancellationToken);
        }
    }

    public async Task UpdateExposureAsync(Guid partnerId, decimal exposure, CancellationToken cancellationToken = default)
    {
        var partner = await _dbSet.FindAsync(new object[] { partnerId }, cancellationToken);
        if (partner != null)
        {
            partner.CurrentExposure = exposure;
            partner.SetUpdatedBy("System");
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string> GenerateCompanyCodeAsync(CancellationToken cancellationToken = default)
    {
        var lastCode = await _dbSet
            .Where(x => x.CompanyCode != null && x.CompanyCode.StartsWith("TP"))
            .OrderByDescending(x => x.CompanyCode)
            .Select(x => x.CompanyCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastCode))
        {
            return "TP00001";
        }

        if (int.TryParse(lastCode.Substring(2), out int lastNumber))
        {
            return $"TP{(lastNumber + 1):D5}";
        }

        return "TP00001";
    }
}