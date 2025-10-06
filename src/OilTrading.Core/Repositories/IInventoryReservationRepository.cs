using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IInventoryReservationRepository
{
    // Basic CRUD operations
    Task<InventoryReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryReservation> AddAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Contract-based queries
    Task<List<InventoryReservation>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetByContractTypeAsync(string contractType, CancellationToken cancellationToken = default);

    // Product and location queries
    Task<List<InventoryReservation>> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetByProductAndLocationAsync(string productCode, string locationCode, CancellationToken cancellationToken = default);

    // Status-based queries
    Task<List<InventoryReservation>> GetActiveReservationsAsync(CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetByStatusAsync(InventoryReservationStatus status, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetExpiringReservationsAsync(DateTime? beforeDate = null, CancellationToken cancellationToken = default);

    // Date-based queries
    Task<List<InventoryReservation>> GetByReservationDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetByExpiryDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // User-based queries
    Task<List<InventoryReservation>> GetByReservedByAsync(string reservedBy, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetByReleasedByAsync(string releasedBy, CancellationToken cancellationToken = default);

    // Complex queries
    Task<List<InventoryReservation>> SearchReservationsAsync(
        Guid? contractId = null,
        string? contractType = null,
        string? productCode = null,
        string? locationCode = null,
        InventoryReservationStatus? status = null,
        DateTime? reservationDateFrom = null,
        DateTime? reservationDateTo = null,
        DateTime? expiryDateFrom = null,
        DateTime? expiryDateTo = null,
        string? reservedBy = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    // Aggregation queries
    Task<decimal> GetTotalReservedQuantityAsync(
        string? productCode = null,
        string? locationCode = null,
        InventoryReservationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<int> GetReservationCountAsync(
        string? productCode = null,
        string? locationCode = null,
        InventoryReservationStatus? status = null,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, decimal>> GetReservedQuantityByProductAsync(
        string? locationCode = null,
        InventoryReservationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, decimal>> GetReservedQuantityByLocationAsync(
        string? productCode = null,
        InventoryReservationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<InventoryReservationStatus, int>> GetReservationCountByStatusAsync(
        string? productCode = null,
        string? locationCode = null,
        CancellationToken cancellationToken = default);

    // Utilization and analytics
    Task<List<InventoryReservation>> GetLongRunningReservationsAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetUnderutilizedReservationsAsync(decimal utilizationThreshold = 50, CancellationToken cancellationToken = default);
    Task<List<InventoryReservation>> GetRecentReservationsAsync(int count = 10, CancellationToken cancellationToken = default);

    // Validation and business rules
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveReservationForContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<bool> HasConflictingReservationAsync(string productCode, string locationCode, decimal quantity, CancellationToken cancellationToken = default);

    // Batch operations
    Task<List<InventoryReservation>> GetMultipleByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task UpdateMultipleAsync(IEnumerable<InventoryReservation> reservations, CancellationToken cancellationToken = default);
    Task DeleteMultipleAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // Maintenance operations
    Task<int> CleanupExpiredReservationsAsync(DateTime? olderThan = null, CancellationToken cancellationToken = default);
    Task<int> ArchiveOldReservationsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}