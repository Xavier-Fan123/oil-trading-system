using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class InventoryMovementRepository : Repository<InventoryMovement>, IInventoryMovementRepository
{
    public InventoryMovementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<InventoryMovement>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => x.FromLocationId == locationId || x.ToLocationId == locationId)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovement>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovement>> GetByTypeAsync(InventoryMovementType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => x.MovementType == type)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovement>> GetByStatusAsync(InventoryMovementStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => x.Status == status)
            .OrderBy(x => x.PlannedDate ?? x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => x.MovementDate >= startDate && x.MovementDate <= endDate)
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovement>> GetByContractAsync(Guid? purchaseContractId, Guid? salesContractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => (purchaseContractId.HasValue && x.PurchaseContractId == purchaseContractId) ||
                       (salesContractId.HasValue && x.SalesContractId == salesContractId))
            .OrderByDescending(x => x.MovementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryMovement>> GetPendingMovementsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.Product)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Where(x => x.Status == InventoryMovementStatus.Planned || x.Status == InventoryMovementStatus.InProgress)
            .OrderBy(x => x.PlannedDate ?? x.MovementDate)
            .ToListAsync(cancellationToken);
    }
}