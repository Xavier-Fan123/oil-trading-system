using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using System.Linq.Expressions;

namespace OilTrading.Core.Repositories;

public interface IShippingOperationRepository : IRepository<ShippingOperation>
{
    Task<ShippingOperation?> GetByShippingNumberAsync(string shippingNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetByContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetByVesselAsync(string vesselName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetByStatusAsync(ShippingStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetByScheduleRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetActiveShipmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetDelayedShipmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetByLoadPortAsync(string loadPort, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingOperation>> GetByDischargePortAsync(string dischargePort, CancellationToken cancellationToken = default);
    Task<bool> ShippingNumberExistsAsync(string shippingNumber, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalQuantityByContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<ShippingOperation?> GetLastShippingByYearAsync(int year, CancellationToken cancellationToken = default);
    
    // Additional methods for CQRS support
    Task<ShippingOperation?> GetByIdWithIncludesAsync(Guid id, string[] includeProperties, CancellationToken cancellationToken = default);
    Task<PagedResult<ShippingOperation>> GetPagedAsync(
        Expression<Func<ShippingOperation, bool>>? filter = null,
        Expression<Func<ShippingOperation, object>>? orderBy = null,
        bool orderByDescending = false,
        int page = 1,
        int pageSize = 20,
        string[]? includeProperties = null,
        CancellationToken cancellationToken = default);
}