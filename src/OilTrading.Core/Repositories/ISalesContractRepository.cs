using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Common;
using System.Linq.Expressions;

namespace OilTrading.Core.Repositories;

public interface ISalesContractRepository : IRepository<SalesContract>
{
    Task<SalesContract?> GetByContractNumberAsync(ContractNumber contractNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetByTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetByTraderAsync(Guid traderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetByStatusAsync(ContractStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetByLinkedPurchaseContractAsync(Guid purchaseContractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetByLaycanDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default);
    Task<bool> ContractNumberExistsAsync(ContractNumber contractNumber, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalContractValueByTraderAsync(Guid traderId, string currency = "USD", CancellationToken cancellationToken = default);
    Task<ContractNumber> GetNextContractNumberAsync(int year, ValueObjects.ContractType contractType, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalProfitMarginByTraderAsync(Guid traderId, string currency = "USD", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesContract>> GetContractsByYearAsync(int year, CancellationToken cancellationToken = default);
    
    // Additional methods for CQRS support
    Task<SalesContract?> GetByIdWithIncludesAsync(Guid id, string[] includeProperties, CancellationToken cancellationToken = default);
    Task<PagedResult<SalesContract>> GetPagedAsync(
        Expression<Func<SalesContract, bool>>? filter = null,
        Expression<Func<SalesContract, object>>? orderBy = null,
        bool orderByDescending = false,
        int page = 1,
        int pageSize = 20,
        string[]? includeProperties = null,
        CancellationToken cancellationToken = default);

    // External contract number lookup methods
    Task<IReadOnlyList<SalesContract>> GetByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    Task<SalesContract?> GetSingleByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);

    Task<bool> ExternalContractNumberExistsAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default);
}