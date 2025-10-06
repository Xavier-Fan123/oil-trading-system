using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IInventoryLocationRepository : IRepository<InventoryLocation>
{
    Task<IEnumerable<InventoryLocation>> GetByTypeAsync(InventoryLocationType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryLocation>> GetByCountryAsync(string country, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryLocation>> GetActiveLocationsAsync(CancellationToken cancellationToken = default);
    Task<InventoryLocation?> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryLocation>> GetWithInventoryAsync(CancellationToken cancellationToken = default);
}

public interface IInventoryPositionRepository : IRepository<InventoryPosition>
{
    Task<IEnumerable<InventoryPosition>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryPosition>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<InventoryPosition?> GetByLocationAndProductAsync(Guid locationId, Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryPosition>> GetByStatusAsync(InventoryStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryPosition>> GetLowInventoryAsync(decimal thresholdQuantity, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalValueByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
}

public interface IInventoryMovementRepository : IRepository<InventoryMovement>
{
    Task<IEnumerable<InventoryMovement>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovement>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovement>> GetByTypeAsync(InventoryMovementType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovement>> GetByStatusAsync(InventoryMovementStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovement>> GetByContractAsync(Guid? purchaseContractId, Guid? salesContractId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryMovement>> GetPendingMovementsAsync(CancellationToken cancellationToken = default);
}