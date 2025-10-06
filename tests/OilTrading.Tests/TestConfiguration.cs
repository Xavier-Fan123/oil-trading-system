using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OilTrading.Infrastructure.Data;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Repositories;
using OilTrading.Application.Services;
using OilTrading.Infrastructure.Services;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using Moq;
using FluentAssertions;

namespace OilTrading.Tests;

/// <summary>
/// Test configuration and setup for the Oil Trading System test suite
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Creates an in-memory database context for testing
    /// </summary>
    public static ApplicationDbContext CreateInMemoryDbContext(string databaseName = "TestDatabase")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Creates a service collection with all dependencies for integration testing
    /// </summary>
    public static IServiceCollection CreateTestServiceCollection()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors());

        // Add repositories
        services.AddScoped<IPurchaseContractRepository, PurchaseContractRepository>();
        services.AddScoped<ISalesContractRepository, SalesContractRepository>();
        services.AddScoped<IShippingOperationRepository, ShippingOperationRepository>();
        services.AddScoped<ITradingPartnerRepository, TradingPartnerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add application services
        services.AddScoped<IRiskCalculationService, RiskCalculationService>();
        services.AddScoped<IContractNumberGenerator, ContractNumberGenerator>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Mock external services that shouldn't be tested in unit tests
        var mockCacheService = new Mock<ICacheService>();
        services.AddSingleton(mockCacheService.Object);

        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        services.AddSingleton(mockCacheInvalidationService.Object);

        return services;
    }

    /// <summary>
    /// Seeds test data into the database context
    /// </summary>
    public static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Clear existing data
        context.PurchaseContracts.RemoveRange(context.PurchaseContracts);
        context.SalesContracts.RemoveRange(context.SalesContracts);
        context.ShippingOperations.RemoveRange(context.ShippingOperations);
        context.TradingPartners.RemoveRange(context.TradingPartners);
        context.Products.RemoveRange(context.Products);
        context.Users.RemoveRange(context.Users);
        context.MarketPrices.RemoveRange(context.MarketPrices);

        // Add test products
        var products = CreateTestProducts();
        context.Products.AddRange(products);

        // Add test users
        var users = CreateTestUsers();
        context.Users.AddRange(users);

        // Add test trading partners
        var tradingPartners = CreateTestTradingPartners();
        context.TradingPartners.AddRange(tradingPartners);

        // Add test market data
        var marketData = CreateTestMarketData();
        context.MarketPrices.AddRange(marketData);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates test products for seeding
    /// </summary>
    public static List<Product> CreateTestProducts()
    {
        return new List<Product>
        {
            new() {
                Name = "Brent Crude",
                ProductCode = "BRENT",
                Type = ProductType.CrudeOil,
                Grade = "Sweet Light",
                Density = 0.835m,
                Origin = "North Sea",
                UnitOfMeasure = "BBL",
                IsActive = true
            },
            new() {
                Name = "WTI Crude",
                ProductCode = "WTI",
                Type = ProductType.CrudeOil,
                Grade = "Light Sweet",
                Density = 0.827m,
                Origin = "USA",
                UnitOfMeasure = "BBL",
                IsActive = true
            },
            new() {
                Name = "Dubai Crude",
                ProductCode = "DUBAI",
                Type = ProductType.CrudeOil,
                Grade = "Medium Sour",
                Density = 0.871m,
                Origin = "UAE",
                UnitOfMeasure = "BBL",
                IsActive = true
            },
            new() {
                Name = "Diesel",
                ProductCode = "ULSD",
                Type = ProductType.RefinedProducts,
                Grade = "Ultra Low Sulfur",
                Density = 0.832m,
                Origin = "Various",
                UnitOfMeasure = "GAL",
                IsActive = true
            },
            new() {
                Name = "Gasoline",
                ProductCode = "RBOB",
                Type = ProductType.RefinedProducts,
                Grade = "Regular",
                Density = 0.745m,
                Origin = "Various",
                UnitOfMeasure = "GAL",
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Creates test users for seeding
    /// </summary>
    public static List<User> CreateTestUsers()
    {
        return new List<User>
        {
            new() {
                Email = "trader1@oiltrading.com",
                FirstName = "John",
                LastName = "Trader",
                PasswordHash = "hashed_password_123",
                Role = UserRole.Trader,
                IsActive = true
            },
            new() {
                Email = "trader2@oiltrading.com",
                FirstName = "Jane",
                LastName = "Smith",
                PasswordHash = "hashed_password_456",
                Role = UserRole.Trader,
                IsActive = true
            },
            new() {
                Email = "riskmanager@oiltrading.com",
                FirstName = "Risk",
                LastName = "Manager",
                PasswordHash = "hashed_password_789",
                Role = UserRole.RiskManager,
                IsActive = true
            },
            new() {
                Email = "admin@oiltrading.com",
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = "hashed_password_admin",
                Role = UserRole.Administrator,
                IsActive = true
            },
            new() {
                Email = "viewer@oiltrading.com",
                FirstName = "Report",
                LastName = "Viewer",
                PasswordHash = "hashed_password_viewer",
                Role = UserRole.Viewer,
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Creates test trading partners for seeding
    /// </summary>
    public static List<TradingPartner> CreateTestTradingPartners()
    {
        return new List<TradingPartner>
        {
            new() {
                Name = "Shell Trading",
                Type = TradingPartnerType.Supplier,
                ContactEmail = "trading@shell.com",
                ContactPhone = "+1-555-0101",
                Address = "Houston, TX",
                IsActive = true
            },
            new() {
                Name = "BP Trading",
                Type = TradingPartnerType.Supplier,
                ContactEmail = "trading@bp.com",
                ContactPhone = "+1-555-0102",
                Address = "London, UK",
                IsActive = true
            },
            new() {
                Name = "ExxonMobil",
                Type = TradingPartnerType.Customer,
                ContactEmail = "trading@exxonmobil.com",
                ContactPhone = "+1-555-0103",
                Address = "Irving, TX",
                IsActive = true
            },
            new() {
                Name = "Chevron",
                Type = TradingPartnerType.Customer,
                ContactEmail = "trading@chevron.com",
                ContactPhone = "+1-555-0104",
                Address = "San Ramon, CA",
                IsActive = true
            },
            new() {
                Name = "Total Energies",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@totalenergies.com",
                ContactPhone = "+33-1-55-55-0105",
                Address = "Paris, France",
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Creates test market data for seeding
    /// </summary>
    public static List<MarketPrice> CreateTestMarketData()
    {
        var marketData = new List<MarketPrice>();
        var baseDate = DateTime.UtcNow.AddDays(-365);
        var random = new Random(42); // Fixed seed for reproducible tests

        var products = new[] { "Brent", "WTI", "Dubai", "ULSD", "RBOB" };
        var basePrices = new[] { 85.0, 82.0, 83.0, 2.5, 2.3 };

        for (int day = 0; day < 365; day++)
        {
            var date = baseDate.AddDays(day);
            
            for (int i = 0; i < products.Length; i++)
            {
                var basePrice = basePrices[i];
                var volatility = basePrice * 0.02; // 2% daily volatility
                var change = (random.NextDouble() - 0.5) * 2 * volatility;
                var price = basePrice + change;

                marketData.Add(new MarketPrice
                {
                    ProductName = products[i],
                    Price = (decimal)Math.Max(price, basePrice * 0.5), // Prevent negative prices
                    PriceDate = date,
                    Source = i < 3 ? "ICE" : "NYMEX",
                    Currency = "USD",
                    // Unit property removed from MarketPrice entity
                });
            }
        }

        return marketData;
    }

    /// <summary>
    /// Creates a test purchase contract
    /// </summary>
    public static PurchaseContract CreateTestPurchaseContract(
        Guid? supplierId = null,
        Guid? productId = null,
        Guid? traderId = null,
        decimal quantity = 1000m,
        decimal? price = 85.50m)
    {
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, Random.Shared.Next(1, 9999));
        var contractQuantity = Quantity.MetricTons(quantity);

        var contract = new PurchaseContract(
            contractNumber,
            ContractType.CARGO,
            supplierId ?? Guid.NewGuid(),
            productId ?? Guid.NewGuid(),
            traderId ?? Guid.NewGuid(),
            contractQuantity);

        if (price.HasValue)
        {
            var priceFormula = PriceFormula.Fixed(price.Value);
            var contractValue = Money.Dollar(price.Value * quantity);
            contract.UpdatePricing(priceFormula, contractValue);
        }

        var laycanStart = DateTime.UtcNow.AddDays(30);
        var laycanEnd = laycanStart.AddDays(15);
        contract.UpdateLaycan(laycanStart, laycanEnd);
        contract.UpdatePorts("Houston", "Rotterdam");

        return contract;
    }

    /// <summary>
    /// Creates a test sales contract
    /// </summary>
    public static SalesContract CreateTestSalesContract(
        Guid? customerId = null,
        Guid? productId = null,
        Guid? traderId = null,
        decimal quantity = 800m,
        decimal? price = 87.00m)
    {
        var contractNumber = ContractNumber.Create(2024, ContractType.CARGO, Random.Shared.Next(1, 9999));
        var contractQuantity = Quantity.MetricTons(quantity);

        var contract = new SalesContract(
            contractNumber,
            ContractType.CARGO,
            customerId ?? Guid.NewGuid(),
            productId ?? Guid.NewGuid(),
            traderId ?? Guid.NewGuid(),
            contractQuantity);

        if (price.HasValue)
        {
            var priceFormula = PriceFormula.Fixed(price.Value);
            var contractValue = Money.Dollar(price.Value * quantity);
            contract.UpdatePricing(priceFormula, contractValue);
        }

        var laycanStart = DateTime.UtcNow.AddDays(30);
        var laycanEnd = laycanStart.AddDays(15);
        contract.UpdateLaycan(laycanStart, laycanEnd);
        contract.UpdatePorts("Houston", "Rotterdam");

        return contract;
    }

    /// <summary>
    /// Asserts that two decimal values are approximately equal (for financial calculations)
    /// </summary>
    public static void AssertFinanciallyEqual(decimal expected, decimal actual, decimal tolerance = 0.01m)
    {
        var difference = Math.Abs(expected - actual);
        if (difference > tolerance)
        {
            throw new Xunit.Sdk.XunitException($"Expected {expected}, but got {actual}. Difference of {difference} exceeds tolerance of {tolerance}.");
        }
    }

    /// <summary>
    /// Asserts that two money values are approximately equal
    /// </summary>
    public static void AssertMoneyEqual(Money expected, Money actual, decimal tolerance = 0.01m)
    {
        actual.Currency.Should().Be(expected.Currency);
        AssertFinanciallyEqual(expected.Amount, actual.Amount, tolerance);
    }

    /// <summary>
    /// Generates test return data for risk calculations
    /// </summary>
    public static List<decimal> GenerateTestReturns(int count, double mean = 0.0, double stdDev = 0.02, int? seed = 42)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var returns = new List<decimal>();

        for (int i = 0; i < count; i++)
        {
            // Generate normal distribution using Box-Muller transform
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            var normalValue = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var returnValue = mean + stdDev * normalValue;
            
            returns.Add((decimal)returnValue);
        }

        return returns;
    }
}

/// <summary>
/// Base test class with common test utilities
/// </summary>
public abstract class BaseTestClass : IDisposable
{
    protected ApplicationDbContext DbContext { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }

    protected BaseTestClass()
    {
        var services = TestConfiguration.CreateTestServiceCollection();
        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected async Task SeedTestDataAsync()
    {
        await TestConfiguration.SeedTestDataAsync(DbContext);
    }

    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public virtual void Dispose()
    {
        DbContext?.Dispose();
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}