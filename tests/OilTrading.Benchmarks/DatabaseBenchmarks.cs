using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class DatabaseBenchmarks
{
    private ApplicationDbContext _context = null!;
    private List<Product> _products = null!;
    private List<PurchaseContract> _contracts = null!;
    private readonly Faker _faker = new();

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"BenchmarkDb_{Guid.NewGuid()}"));

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Generate test data
        _products = GenerateProducts(1000);
        _contracts = GenerateContracts(500);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
    }

    [Benchmark(Description = "Insert 1000 Products")]
    public async Task InsertProducts()
    {
        var products = GenerateProducts(1000);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "Insert 500 Purchase Contracts")]
    public async Task InsertPurchaseContracts()
    {
        var contracts = GenerateContracts(500);
        _context.PurchaseContracts.AddRange(contracts);
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "Query Products by Code")]
    public async Task<Product?> QueryProductByCode()
    {
        // Seed some data first
        if (!await _context.Products.AnyAsync())
        {
            _context.Products.AddRange(_products.Take(100));
            await _context.SaveChangesAsync();
        }

        var randomCode = _products[_faker.Random.Int(0, 99)].ProductCode;
        return await _context.Products
            .FirstOrDefaultAsync(p => p.ProductCode == randomCode);
    }

    [Benchmark(Description = "Query Products with Complex Filter")]
    public async Task<List<Product>> QueryProductsWithComplexFilter()
    {
        // Seed some data first
        if (!await _context.Products.AnyAsync())
        {
            _context.Products.AddRange(_products.Take(100));
            await _context.SaveChangesAsync();
        }

        return await _context.Products
            .Where(p => p.Type == ProductType.CrudeOil)
            .Where(p => p.Density > 800 && p.Density < 900)
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductName)
            .Take(50)
            .ToListAsync();
    }

    [Benchmark(Description = "Bulk Update Products")]
    public async Task BulkUpdateProducts()
    {
        // Seed some data first
        if (!await _context.Products.AnyAsync())
        {
            _context.Products.AddRange(_products.Take(100));
            await _context.SaveChangesAsync();
        }

        var products = await _context.Products.Take(50).ToListAsync();
        foreach (var product in products)
        {
            product.ProductName = $"{product.ProductName} - Updated";
        }
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "Query with Navigation Properties")]
    public async Task<List<PurchaseContract>> QueryWithNavigationProperties()
    {
        // Seed data with relationships
        if (!await _context.PurchaseContracts.AnyAsync())
        {
            _context.Products.AddRange(_products.Take(50));
            await _context.SaveChangesAsync();
            
            _context.PurchaseContracts.AddRange(_contracts.Take(20));
            await _context.SaveChangesAsync();
        }

        return await _context.PurchaseContracts
            .Include(c => c.Product)
            .Include(c => c.TradingPartner)
            .Where(c => c.Status == ContractStatus.Active)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark(Description = "Soft Delete Operations")]
    public async Task SoftDeleteOperations()
    {
        // Create fresh products for deletion
        var productsToDelete = GenerateProducts(50);
        _context.Products.AddRange(productsToDelete);
        await _context.SaveChangesAsync();

        // Soft delete all products
        foreach (var product in productsToDelete)
        {
            product.SoftDelete("benchmark-user");
        }
        await _context.SaveChangesAsync();
    }

    private List<Product> GenerateProducts(int count)
    {
        var products = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            products.Add(new Product
            {
                ProductName = _faker.Commerce.ProductName(),
                ProductCode = _faker.Random.AlphaNumeric(8).ToUpper(),
                Type = _faker.PickRandom<ProductType>(),
                Grade = _faker.Commerce.ProductAdjective(),
                Specification = _faker.Lorem.Sentence(),
                UnitOfMeasure = _faker.PickRandom("BBL", "MT", "GAL"),
                Density = _faker.Random.Decimal(750, 1000),
                Origin = _faker.Address.Country(),
                IsActive = true
            });
        }

        return products;
    }

    private List<PurchaseContract> GenerateContracts(int count)
    {
        var contracts = new List<PurchaseContract>();
        for (int i = 0; i < count; i++)
        {
            var contractNumber = ContractNumber.Create(
                _faker.Date.Recent().Year,
                _faker.PickRandom<ContractType>(),
                _faker.Random.Int(1, 9999));
            
            var quantity = new Quantity(
                _faker.Random.Decimal(1000, 50000),
                _faker.PickRandom<QuantityUnit>());

            contracts.Add(new PurchaseContract(
                contractNumber,
                _faker.PickRandom<ContractType>(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                quantity));
        }

        return contracts;
    }
}