using Microsoft.EntityFrameworkCore.Storage;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IPurchaseContractRepository? _purchaseContracts;
    private ISalesContractRepository? _salesContracts;
    private ITradingPartnerRepository? _tradingPartners;
    private IProductRepository? _products;
    private IUserRepository? _users;
    private IShippingOperationRepository? _shippingOperations;
    private IPricingEventRepository? _pricingEvents;
    private IFinancialReportRepository? _financialReports;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IPurchaseContractRepository PurchaseContracts =>
        _purchaseContracts ??= new PurchaseContractRepository(_context);

    public ISalesContractRepository SalesContracts =>
        _salesContracts ??= new SalesContractRepository(_context);

    public ITradingPartnerRepository TradingPartners =>
        _tradingPartners ??= new TradingPartnerRepository(_context);

    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IShippingOperationRepository ShippingOperations =>
        _shippingOperations ??= new ShippingOperationRepository(_context);

    public IPricingEventRepository PricingEvents =>
        _pricingEvents ??= new PricingEventRepository(_context);

    public IFinancialReportRepository FinancialReports =>
        _financialReports ??= new FinancialReportRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already in progress");

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}