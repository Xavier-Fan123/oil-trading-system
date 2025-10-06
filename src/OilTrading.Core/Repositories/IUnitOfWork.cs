namespace OilTrading.Core.Repositories;

public interface IUnitOfWork : IDisposable
{
    IPurchaseContractRepository PurchaseContracts { get; }
    ISalesContractRepository SalesContracts { get; }
    ITradingPartnerRepository TradingPartners { get; }
    IProductRepository Products { get; }
    IUserRepository Users { get; }
    IShippingOperationRepository ShippingOperations { get; }
    IPricingEventRepository PricingEvents { get; }
    IFinancialReportRepository FinancialReports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}