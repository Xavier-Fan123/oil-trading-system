using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OilTrading.Infrastructure.Data;

/// <summary>
/// Seeds the database with sample data for testing and demonstration purposes
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // DEVELOPMENT MODE: Always clear and re-seed for complete data integrity
            // This ensures seeded contracts always have all required fields
            _logger.LogInformation("Clearing existing data to ensure fresh seeding...");
            await _context.PurchaseContracts.ExecuteDeleteAsync();
            await _context.SalesContracts.ExecuteDeleteAsync();
            await _context.ShippingOperations.ExecuteDeleteAsync();
            await _context.Users.ExecuteDeleteAsync();
            await _context.TradingPartners.ExecuteDeleteAsync();
            await _context.Products.ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Old data cleared. Starting fresh seeding...");

            // Seed Products
            await SeedProductsAsync();
            await _context.SaveChangesAsync();

            // Seed Trading Partners
            await SeedTradingPartnersAsync();
            await _context.SaveChangesAsync();

            // Seed Users
            await SeedUsersAsync();
            await _context.SaveChangesAsync();

            // Seed Purchase Contracts
            await SeedPurchaseContractsAsync();
            await _context.SaveChangesAsync();

            // Seed Sales Contracts
            await SeedSalesContractsAsync();
            await _context.SaveChangesAsync();

            // Seed Shipping Operations
            await SeedShippingOperationsAsync();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully with all required fields populated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedProductsAsync()
    {
        _logger.LogInformation("Seeding products...");

        var products = new[]
        {
            new Product
            {
                Code = "BRENT",
                Name = "Brent Crude Oil",
                ProductName = "Brent Crude Oil",
                ProductCode = "BRENT",
                Type = ProductType.CrudeOil,
                ProductType = ProductType.CrudeOil,
                Description = "Brent crude oil from North Sea",
                Grade = "Light Sweet",
                Specification = "API 35, Sulfur 0.37%",
                UnitOfMeasure = "BBL",
                Density = 0.827m,
                Origin = "North Sea",
                IsActive = true
            },
            new Product
            {
                Code = "WTI",
                Name = "West Texas Intermediate",
                ProductName = "West Texas Intermediate",
                ProductCode = "WTI",
                Type = ProductType.CrudeOil,
                ProductType = ProductType.CrudeOil,
                Description = "WTI crude oil from Texas",
                Grade = "Light Sweet",
                Specification = "API 39.6, Sulfur 0.24%",
                UnitOfMeasure = "BBL",
                Density = 0.815m,
                Origin = "United States",
                IsActive = true
            },
            new Product
            {
                Code = "MGO",
                Name = "Marine Gas Oil",
                ProductName = "Marine Gas Oil",
                ProductCode = "MGO",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Description = "Marine Gas Oil per ISO 8217:2017",
                Grade = "0.50% S",
                Specification = "ISO 8217 MGO 0.5% S",
                UnitOfMeasure = "MT",
                Density = 0.890m,
                Origin = "Multiple Sources",
                IsActive = true
            },
            new Product
            {
                Code = "HFO380",
                Name = "Heavy Fuel Oil 380cSt",
                ProductName = "Heavy Fuel Oil 380cSt",
                ProductCode = "HFO380",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Description = "Heavy Fuel Oil 380 centistoke per ISO 8217:2017",
                Grade = "3.5% S",
                Specification = "ISO 8217 HFO 380 3.5% S",
                UnitOfMeasure = "MT",
                Density = 0.991m,
                Origin = "Multiple Sources",
                IsActive = true
            }
        };

        await _context.Products.AddRangeAsync(products);
        _logger.LogInformation("Added {Count} products", products.Length);
    }

    private async Task SeedTradingPartnersAsync()
    {
        _logger.LogInformation("Seeding trading partners...");

        var partners = new[]
        {
            new TradingPartner
            {
                Code = "SINOPEC",
                Name = "China National Petroleum Corporation",
                CompanyCode = "SINOPEC",
                CompanyName = "China National Petroleum Corporation",
                Type = TradingPartnerType.Supplier,
                PartnerType = TradingPartnerType.Supplier,
                CreditLimit = 50000000m,
                Country = "China",
                ContactEmail = "contact@sinopec.com",
                ContactPhone = "+86-10-8359-1000",
                Address = "Beijing, China",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },
            new TradingPartner
            {
                Code = "PETRONAS",
                Name = "PETRONAS Trading Sdn Bhd",
                CompanyCode = "PETRONAS",
                CompanyName = "PETRONAS Trading Sdn Bhd",
                Type = TradingPartnerType.Supplier,
                PartnerType = TradingPartnerType.Supplier,
                CreditLimit = 30000000m,
                Country = "Malaysia",
                ContactEmail = "trading@petronas.com",
                ContactPhone = "+60-3-2091-3000",
                Address = "Kuala Lumpur, Malaysia",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },
            new TradingPartner
            {
                Code = "ARAMCO",
                Name = "Saudi Aramco Trading",
                CompanyCode = "ARAMCO",
                CompanyName = "Saudi Aramco Trading",
                Type = TradingPartnerType.Supplier,
                PartnerType = TradingPartnerType.Supplier,
                CreditLimit = 100000000m,
                Country = "Saudi Arabia",
                ContactEmail = "trading@aramco.com",
                ContactPhone = "+966-1-801-0000",
                Address = "Riyadh, Saudi Arabia",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },
            new TradingPartner
            {
                Code = "PLTW",
                Name = "PT Pertamina Trading",
                CompanyCode = "PLTW",
                CompanyName = "PT Pertamina Trading",
                Type = TradingPartnerType.Supplier,
                PartnerType = TradingPartnerType.Supplier,
                CreditLimit = 25000000m,
                Country = "Indonesia",
                ContactEmail = "trading@pertamina.com",
                ContactPhone = "+62-21-2922-8600",
                Address = "Jakarta, Indonesia",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },
            new TradingPartner
            {
                Code = "VITOL",
                Name = "Vitol Asia Pte Ltd",
                CompanyCode = "VITOL",
                CompanyName = "Vitol Asia Pte Ltd",
                Type = TradingPartnerType.Customer,
                PartnerType = TradingPartnerType.Customer,
                CreditLimit = 40000000m,
                Country = "Singapore",
                ContactEmail = "trading@vitol.com",
                ContactPhone = "+65-6832-3000",
                Address = "Singapore",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },
            new TradingPartner
            {
                Code = "TRAFIGURA",
                Name = "Trafigura Trading Ltd",
                CompanyCode = "TRAFIGURA",
                CompanyName = "Trafigura Trading Ltd",
                Type = TradingPartnerType.Customer,
                PartnerType = TradingPartnerType.Customer,
                CreditLimit = 35000000m,
                Country = "Singapore",
                ContactEmail = "trading@trafigura.com",
                ContactPhone = "+65-6878-8000",
                Address = "Singapore",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },
            new TradingPartner
            {
                Code = "GLENCORE",
                Name = "Glencore Energy Ltd",
                CompanyCode = "GLENCORE",
                CompanyName = "Glencore Energy Ltd",
                Type = TradingPartnerType.Customer,
                PartnerType = TradingPartnerType.Customer,
                CreditLimit = 50000000m,
                Country = "Switzerland",
                ContactEmail = "trading@glencore.com",
                ContactPhone = "+41-41-709-01-11",
                Address = "Baar, Switzerland",
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(1),
                IsActive = true
            }
        };

        await _context.TradingPartners.AddRangeAsync(partners);
        _logger.LogInformation("Added {Count} trading partners", partners.Length);
    }

    private async Task SeedUsersAsync()
    {
        _logger.LogInformation("Seeding users...");

        var users = new[]
        {
            new User
            {
                Name = "trader01",
                Email = "trader01@oiltrading.com",
                FirstName = "John",
                LastName = "Trader",
                PasswordHash = "dummy_hash",
                Role = OilTrading.Core.Entities.UserRole.Trader,
                IsActive = true
            },
            new User
            {
                Name = "trader02",
                Email = "trader02@oiltrading.com",
                FirstName = "Jane",
                LastName = "Dealer",
                PasswordHash = "dummy_hash",
                Role = OilTrading.Core.Entities.UserRole.Trader,
                IsActive = true
            },
            new User
            {
                Name = "approver01",
                Email = "approver01@oiltrading.com",
                FirstName = "Mike",
                LastName = "Manager",
                PasswordHash = "dummy_hash",
                Role = OilTrading.Core.Entities.UserRole.RiskManager,
                IsActive = true
            },
            new User
            {
                Name = "accountant01",
                Email = "accountant01@oiltrading.com",
                FirstName = "Sarah",
                LastName = "Settlement",
                PasswordHash = "dummy_hash",
                Role = OilTrading.Core.Entities.UserRole.Administrator,
                IsActive = true
            }
        };

        await _context.Users.AddRangeAsync(users);
        _logger.LogInformation("Added {Count} users", users.Length);
    }

    private async Task SeedPurchaseContractsAsync()
    {
        _logger.LogInformation("Seeding purchase contracts...");

        // Get seeded data
        var brentProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "BRENT")
            ?? throw new InvalidOperationException("BRENT product not found");
        var wtiProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "WTI")
            ?? throw new InvalidOperationException("WTI product not found");
        var trader01 = await _context.Users.FirstOrDefaultAsync(u => u.Name == "trader01")
            ?? throw new InvalidOperationException("trader01 user not found");
        var sinopec = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "SINOPEC")
            ?? throw new InvalidOperationException("SINOPEC partner not found");
        var petronas = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "PETRONAS")
            ?? throw new InvalidOperationException("PETRONAS partner not found");

        var contracts = new List<PurchaseContract>();

        // Contract 1: Brent from Sinopec - 50000 BBL @ USD 85.50/BBL
        var contract1 = new PurchaseContract(
            ContractNumber.Parse("PC-2025-001"),
            ContractType.CARGO,
            sinopec.Id,
            brentProduct.Id,
            trader01.Id,
            new Quantity(50000, QuantityUnit.BBL),
            7.6m,
            null,
            "EXT-SINOPEC-001"
        );
        var price1 = 85.50m;
        var value1 = new Money(50000 * price1, "USD");
        contract1.UpdatePricing(PriceFormula.Fixed(price1), value1);
        contract1.UpdateDeliveryTerms(DeliveryTerms.FOB);
        contract1.UpdateLaycan(new DateTime(2025, 12, 1), new DateTime(2025, 12, 15));
        contract1.UpdatePorts("Ras Tanura, Saudi Arabia", "Singapore");
        contract1.UpdateSettlementType(SettlementType.ContractPayment);
        contract1.UpdatePaymentTerms("TT 30 days after B/L presentation", 30);
        contract1.UpdateQualitySpecifications("API 38.0° min, Sulfur 0.37% max");
        contract1.UpdateInspectionAgency("SGS");
        contract1.AddNotes("Sample Brent crude contract - PC-2025-001");
        contracts.Add(contract1);

        // Contract 2: WTI from Petronas - 30000 BBL @ USD 78.25/BBL
        var contract2 = new PurchaseContract(
            ContractNumber.Parse("PC-2025-002"),
            ContractType.CARGO,
            petronas.Id,
            wtiProduct.Id,
            trader01.Id,
            new Quantity(30000, QuantityUnit.BBL),
            7.6m,
            null,
            "EXT-PETRONAS-001"
        );
        var price2 = 78.25m;
        var value2 = new Money(30000 * price2, "USD");
        contract2.UpdatePricing(PriceFormula.Fixed(price2), value2);
        contract2.UpdateDeliveryTerms(DeliveryTerms.CIF);
        contract2.UpdateLaycan(new DateTime(2026, 1, 1), new DateTime(2026, 1, 20));
        contract2.UpdatePorts("Corpus Christi, USA", "Rotterdam, Netherlands");
        contract2.UpdateSettlementType(SettlementType.ContractPayment);
        contract2.UpdatePaymentTerms("10% prepayment, balance TT 45 days after B/L", 45);
        contract2.SetPrepaymentPercentage(10);
        contract2.UpdateQualitySpecifications("API 39.6° min, Sulfur 0.24% max");
        contract2.UpdateInspectionAgency("SGS");
        contract2.AddNotes("Sample WTI crude contract - PC-2025-002");
        contracts.Add(contract2);

        // Contract 3: Brent from Sinopec (second one) - 25000 BBL @ USD 84.75/BBL
        var contract3 = new PurchaseContract(
            ContractNumber.Parse("PC-2025-003"),
            ContractType.CARGO,
            sinopec.Id,
            brentProduct.Id,
            trader01.Id,
            new Quantity(25000, QuantityUnit.BBL),
            7.6m,
            null,
            "EXT-SINOPEC-002"
        );
        var price3 = 84.75m;
        var value3 = new Money(25000 * price3, "USD");
        contract3.UpdatePricing(PriceFormula.Fixed(price3), value3);
        contract3.UpdateDeliveryTerms(DeliveryTerms.FOB);
        contract3.UpdateLaycan(new DateTime(2026, 1, 10), new DateTime(2026, 1, 25));
        contract3.UpdatePorts("Ras Tanura, Saudi Arabia", "Singapore");
        contract3.UpdateSettlementType(SettlementType.ContractPayment);
        contract3.UpdatePaymentTerms("LC at sight, 60 days tenor", 60);
        contract3.UpdateQualitySpecifications("API 38.0° min, Sulfur 0.37% max");
        contract3.UpdateInspectionAgency("SGS");
        contract3.AddNotes("Sample Brent crude contract - PC-2025-003");
        contracts.Add(contract3);

        await _context.PurchaseContracts.AddRangeAsync(contracts);
        _logger.LogInformation("Added {Count} complete sample purchase contracts with all required fields", contracts.Count);
    }

    private async Task SeedSalesContractsAsync()
    {
        _logger.LogInformation("Seeding sales contracts...");

        // Get seeded data
        var brentProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "BRENT")
            ?? throw new InvalidOperationException("BRENT product not found");
        var wtiProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "WTI")
            ?? throw new InvalidOperationException("WTI product not found");
        var trader02 = await _context.Users.FirstOrDefaultAsync(u => u.Name == "trader02")
            ?? throw new InvalidOperationException("trader02 user not found");
        var vitol = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "VITOL")
            ?? throw new InvalidOperationException("VITOL partner not found");
        var trafigura = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "TRAFIGURA")
            ?? throw new InvalidOperationException("TRAFIGURA partner not found");

        var contracts = new List<SalesContract>();

        // Contract 1: Brent to Vitol
        var contract1 = new SalesContract(
            ContractNumber.Parse("SC-2025-001"),
            ContractType.CARGO,
            vitol.Id,
            brentProduct.Id,
            trader02.Id,
            new Quantity(50000, QuantityUnit.BBL),
            7.6m,
            null,
            null,
            "EXT-VITOL-001"
        );
        contract1.UpdateLaycan(new DateTime(2025, 12, 5), new DateTime(2025, 12, 20));
        contract1.UpdatePorts("Singapore", "Rotterdam, Netherlands");
        contracts.Add(contract1);

        // Contract 2: WTI to Trafigura
        var contract2 = new SalesContract(
            ContractNumber.Parse("SC-2025-002"),
            ContractType.CARGO,
            trafigura.Id,
            wtiProduct.Id,
            trader02.Id,
            new Quantity(30000, QuantityUnit.BBL),
            7.6m,
            null,
            null,
            "EXT-TRAFIGURA-001"
        );
        contract2.UpdateLaycan(new DateTime(2026, 1, 5), new DateTime(2026, 1, 25));
        contract2.UpdatePorts("Rotterdam, Netherlands", "Houston, USA");
        contracts.Add(contract2);

        // Contract 3: Brent to Vitol (second one)
        var contract3 = new SalesContract(
            ContractNumber.Parse("SC-2025-003"),
            ContractType.CARGO,
            vitol.Id,
            brentProduct.Id,
            trader02.Id,
            new Quantity(25000, QuantityUnit.BBL),
            7.6m,
            null,
            null,
            "EXT-VITOL-002"
        );
        contract3.UpdateLaycan(new DateTime(2026, 1, 15), new DateTime(2026, 1, 30));
        contract3.UpdatePorts("Singapore", "Bangkok, Thailand");
        contracts.Add(contract3);

        await _context.SalesContracts.AddRangeAsync(contracts);
        _logger.LogInformation("Added {Count} sales contracts", contracts.Count);
    }

    private async Task SeedShippingOperationsAsync()
    {
        _logger.LogInformation("Seeding shipping operations...");

        // Get seeded data
        var purchaseContracts = await _context.PurchaseContracts.ToListAsync();
        var salesContracts = await _context.SalesContracts.ToListAsync();

        if (!purchaseContracts.Any() || !salesContracts.Any())
        {
            _logger.LogWarning("No purchase or sales contracts found. Skipping shipping operations seeding.");
            return;
        }

        var shippingOps = new[]
        {
            new ShippingOperation(
                "SHIP-2025-001",
                purchaseContracts[0].Id,
                "MT Supertanker I",
                new Quantity(50000, QuantityUnit.BBL),
                new DateTime(2025, 12, 10),
                new DateTime(2025, 12, 25),
                "Ras Tanura, Saudi Arabia",
                "Singapore"
            ),
            new ShippingOperation(
                "SHIP-2025-002",
                salesContracts[0].Id,
                "MT Tanker Express",
                new Quantity(50000, QuantityUnit.BBL),
                new DateTime(2025, 12, 21),
                new DateTime(2026, 1, 20),
                "Singapore",
                "Rotterdam, Netherlands"
            ),
            new ShippingOperation(
                "SHIP-2025-003",
                purchaseContracts[1].Id,
                "MT Ocean Destiny",
                new Quantity(30000, QuantityUnit.BBL),
                new DateTime(2026, 1, 15),
                new DateTime(2026, 2, 2),
                "Kuantan, Malaysia",
                "Tokyo, Japan"
            )
        };

        // Add additional details through domain methods
        shippingOps[0].UpdateVesselDetails("MT Supertanker I", "1234567", 150000m);

        shippingOps[1].UpdateVesselDetails("MT Tanker Express", "7654321", 140000m);

        shippingOps[2].UpdateVesselDetails("MT Ocean Destiny", "5555555", 120000m);

        await _context.ShippingOperations.AddRangeAsync(shippingOps);
        _logger.LogInformation("Added {Count} shipping operations", shippingOps.Length);
    }
}
