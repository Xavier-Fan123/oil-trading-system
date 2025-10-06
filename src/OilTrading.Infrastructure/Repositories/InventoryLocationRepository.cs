using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class InventoryLocationRepository : Repository<InventoryLocation>, IInventoryLocationRepository
{
    public InventoryLocationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<InventoryLocation>> GetByTypeAsync(InventoryLocationType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.LocationType == type && x.IsActive)
            .OrderBy(x => x.LocationName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryLocation>> GetByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.Country.ToLower() == country.ToLower() && x.IsActive)
            .OrderBy(x => x.LocationName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryLocation>> GetActiveLocationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .OrderBy(x => x.LocationName)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryLocation?> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.LocationCode == locationCode, cancellationToken);
    }

    public async Task<IEnumerable<InventoryLocation>> GetWithInventoryAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Inventories)
                .ThenInclude(i => i.Product)
            .Where(x => x.IsActive)
            .OrderBy(x => x.LocationName)
            .ToListAsync(cancellationToken);
    }
}