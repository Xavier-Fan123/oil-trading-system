using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class InventoryPositionRepository : Repository<InventoryPosition>, IInventoryPositionRepository
{
    public InventoryPositionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<InventoryPosition>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.LocationId == locationId)
            .OrderBy(x => x.Product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryPosition>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Location.LocationName)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryPosition?> GetByLocationAndProductAsync(Guid locationId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Location)
            .FirstOrDefaultAsync(x => x.LocationId == locationId && x.ProductId == productId, cancellationToken);
    }

    public async Task<IEnumerable<InventoryPosition>> GetByStatusAsync(InventoryStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.Status == status)
            .OrderBy(x => x.Location.LocationName)
            .ThenBy(x => x.Product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryPosition>> GetLowInventoryAsync(decimal thresholdQuantity, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.Location)
            .Where(x => x.Quantity.Value <= thresholdQuantity)
            .OrderBy(x => x.Quantity.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalValueByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var positions = await _dbSet
            .Where(x => x.LocationId == locationId)
            .ToListAsync(cancellationToken);

        return positions.Sum(x => x.TotalValue.Amount);
    }
}