using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface ISettlementRepository
{
    // Basic CRUD operations
    Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Settlement?> GetBySettlementNumberAsync(string settlementNumber, CancellationToken cancellationToken = default);
    Task<Settlement> AddAsync(Settlement settlement, CancellationToken cancellationToken = default);
    Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Settlement queries
    Task<IEnumerable<Settlement>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetByStatusAsync(SettlementStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetByTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetPendingSettlementsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetOverdueSettlementsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetDueSettlementsAsync(DateTime? dueDate = null, CancellationToken cancellationToken = default);

    // Date range queries
    Task<IEnumerable<Settlement>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Settlement>> GetByCreatedDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);

    // Payment-related queries
    Task<IEnumerable<Settlement>> GetWithPendingPaymentsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetByPaymentStatusAsync(PaymentStatus paymentStatus, CancellationToken cancellationToken = default);

    // Settlement type queries
    Task<IEnumerable<Settlement>> GetByTypeAsync(SettlementType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Settlement>> GetByTypesAsync(SettlementType[] types, CancellationToken cancellationToken = default);

    // Amount-based queries
    Task<IEnumerable<Settlement>> GetByAmountRangeAsync(
        decimal minAmount, 
        decimal maxAmount, 
        string currency = "USD", 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Settlement>> GetLargeSettlementsAsync(
        decimal threshold, 
        string currency = "USD", 
        CancellationToken cancellationToken = default);

    // Advanced search
    Task<IEnumerable<Settlement>> SearchSettlementsAsync(
        string? settlementNumber = null,
        Guid? contractId = null,
        SettlementStatus? status = null,
        SettlementType? type = null,
        Guid? tradingPartnerId = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? currency = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    // Aggregation methods
    Task<decimal> GetTotalSettlementAmountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        SettlementStatus? status = null,
        string currency = "USD",
        CancellationToken cancellationToken = default);

    Task<int> GetSettlementCountAsync(
        SettlementStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<SettlementStatus, int>> GetSettlementCountByStatusAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, decimal>> GetSettlementAmountByCurrencyAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default);

    // Performance and analytics
    Task<IEnumerable<Settlement>> GetRecentSettlementsAsync(
        int count = 10, 
        CancellationToken cancellationToken = default);

    Task<double> GetAverageSettlementTimeAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Settlement>> GetSettlementsWithAdjustmentsAsync(CancellationToken cancellationToken = default);

    // Batch operations
    Task<IEnumerable<Settlement>> GetMultipleByIdsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);
    
    Task UpdateMultipleAsync(
        IEnumerable<Settlement> settlements, 
        CancellationToken cancellationToken = default);

    // Validation and business rules
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SettlementNumberExistsAsync(string settlementNumber, CancellationToken cancellationToken = default);
    Task<bool> HasPendingSettlementsForContractAsync(Guid contractId, CancellationToken cancellationToken = default);
}