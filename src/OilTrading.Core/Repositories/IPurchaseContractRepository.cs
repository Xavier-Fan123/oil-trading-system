using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using System.Linq.Expressions;

namespace OilTrading.Core.Repositories;

public interface IPurchaseContractRepository : IRepository<PurchaseContract>
{
    Task<PurchaseContract?> GetByContractNumberAsync(ContractNumber contractNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetByTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetByTraderAsync(Guid traderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetByStatusAsync(ContractStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetByLaycanDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetContractsWithAvailableQuantityAsync(CancellationToken cancellationToken = default);
    Task<bool> ContractNumberExistsAsync(ContractNumber contractNumber, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalContractValueByTraderAsync(Guid traderId, string currency = "USD", CancellationToken cancellationToken = default);
    Task<ContractNumber> GetNextContractNumberAsync(int year, ValueObjects.ContractType contractType, CancellationToken cancellationToken = default);
    
    // Additional methods for CQRS support
    Task<PurchaseContract?> GetByIdWithIncludesAsync(Guid id, string[] includeProperties, CancellationToken cancellationToken = default);
    Task<PagedResult<PurchaseContract>> GetPagedAsync(
        Expression<Func<PurchaseContract, bool>>? filter = null,
        Expression<Func<PurchaseContract, object>>? orderBy = null,
        bool orderByDescending = false,
        int page = 1,
        int pageSize = 20,
        string[]? includeProperties = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseContract>> GetContractsByYearAsync(int year, CancellationToken cancellationToken = default);

    // External contract number lookup methods
    Task<IReadOnlyList<PurchaseContract>> GetByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    Task<PurchaseContract?> GetSingleByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    Task<bool> ExternalContractNumberExistsAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);
}