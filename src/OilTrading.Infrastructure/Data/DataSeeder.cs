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

            // Check if using InMemory database - ExecuteDeleteAsync is NOT supported by InMemory provider
            var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

            if (isInMemory)
            {
                // For InMemory database, use traditional RemoveRange approach
                _logger.LogInformation("Using InMemory database - using RemoveRange for data clearing");
                _context.PurchaseContracts.RemoveRange(_context.PurchaseContracts);
                _context.SalesContracts.RemoveRange(_context.SalesContracts);
                _context.ShippingOperations.RemoveRange(_context.ShippingOperations);
                _context.Users.RemoveRange(_context.Users);
                _context.TradingPartners.RemoveRange(_context.TradingPartners);
                _context.Products.RemoveRange(_context.Products);
            }
            else
            {
                // For SQL databases, use efficient bulk delete
                await _context.PurchaseContracts.ExecuteDeleteAsync();
                await _context.SalesContracts.ExecuteDeleteAsync();
                await _context.ShippingOperations.ExecuteDeleteAsync();
                await _context.Users.ExecuteDeleteAsync();
                await _context.TradingPartners.ExecuteDeleteAsync();
                await _context.Products.ExecuteDeleteAsync();
            }
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

            // Seed Market Data (Spot Prices)
            await SeedMarketPricesAsync();
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
            },
            // DAXIN MARINE products - Gas Oil and Gasoline
            new Product
            {
                Code = "GASOIL",
                Name = "Gas Oil (Diesel)",
                ProductName = "Gas Oil (Diesel)",
                ProductCode = "GASOIL",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Description = "Gas Oil / Diesel for DAXIN MARINE contracts",
                Grade = "Standard",
                Specification = "ISO 8217",
                UnitOfMeasure = "BBL",
                Density = 0.85m,
                Origin = "Singapore",
                IsActive = true
            },
            new Product
            {
                Code = "GASOLINE",
                Name = "Gasoline",
                ProductName = "Gasoline",
                ProductCode = "GASOLINE",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Description = "Gasoline for DAXIN MARINE contracts",
                Grade = "Standard",
                Specification = "Standard",
                UnitOfMeasure = "BBL",
                Density = 0.75m,
                Origin = "Singapore",
                IsActive = true
            }
        };

        // Initialize RowVersion for InMemory database (required property)
        foreach (var product in products)
        {
            product.SetRowVersion(new byte[] { 0 });
        }

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
            },
            // DAXIN MARINE - Myanmar customer for oil sales
            new TradingPartner
            {
                Code = "DAXIN",
                Name = "DAXIN MARINE PTE LTD",
                CompanyCode = "DAXIN",
                CompanyName = "DAXIN MARINE PTE LTD",
                Type = TradingPartnerType.Customer,
                PartnerType = TradingPartnerType.Customer,
                CreditLimit = 100000000m,
                Country = "Singapore",
                ContactEmail = "trading@daxinmarine.com",
                ContactPhone = "+65-6000-0000",
                Address = "Singapore",
                PaymentTermDays = 45,
                CreditLimitValidUntil = DateTime.UtcNow.AddYears(2),
                IsActive = true
            }
        };

        // Initialize RowVersion for InMemory database (required property)
        foreach (var partner in partners)
        {
            partner.SetRowVersion(new byte[] { 0 });
        }

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

        // Initialize RowVersion for InMemory database (required property)
        foreach (var user in users)
        {
            user.SetRowVersion(new byte[] { 0 });
        }

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

        // Initialize RowVersion for InMemory database (required property)
        foreach (var contract in contracts)
        {
            contract.SetRowVersion(new byte[] { 0 });
        }

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
        var gasoilProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "GASOIL")
            ?? throw new InvalidOperationException("GASOIL product not found");
        var gasolineProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "GASOLINE")
            ?? throw new InvalidOperationException("GASOLINE product not found");
        var trader02 = await _context.Users.FirstOrDefaultAsync(u => u.Name == "trader02")
            ?? throw new InvalidOperationException("trader02 user not found");
        var vitol = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "VITOL")
            ?? throw new InvalidOperationException("VITOL partner not found");
        var trafigura = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "TRAFIGURA")
            ?? throw new InvalidOperationException("TRAFIGURA partner not found");
        var daxin = await _context.TradingPartners.FirstOrDefaultAsync(p => p.Code == "DAXIN")
            ?? throw new InvalidOperationException("DAXIN partner not found");

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

        // ============================================================
        // DAXIN MARINE PTE LTD - 18 Sales Contracts from EXPORT.XLSX
        // Payment Terms: NET 45, Delivery: DES, Settlement: TT
        // Route: Singapore -> Yangon
        // ============================================================
        var daxinContracts = new[]
        {
            new { Ext = "ITGR-2025-CAG-S0267", Prod = "GASOIL", Qty = 103500m, Price = 90.4m, Date = new DateTime(2025, 10, 10) },
            new { Ext = "ITGR-2025-CAG-S0271", Prod = "GASOIL", Qty = 22500m, Price = 85.2m, Date = new DateTime(2025, 10, 20) },
            new { Ext = "ITGR-2025-CAG-S0280", Prod = "GASOLINE", Qty = 30000m, Price = 80.3m, Date = new DateTime(2025, 10, 27) },
            new { Ext = "ITGR-2025-CAG-S0276", Prod = "GASOLINE", Qty = 100000m, Price = 80.3m, Date = new DateTime(2025, 10, 28) },
            new { Ext = "ITGR-2025-CAG-S0274", Prod = "GASOLINE", Qty = 150000m, Price = 80.3m, Date = new DateTime(2025, 10, 28) },
            new { Ext = "ITGR-2025-CAG-S0281", Prod = "GASOIL", Qty = 22500m, Price = 85.4m, Date = new DateTime(2025, 10, 28) },
            new { Ext = "ITGR-2025-CAG-S0282", Prod = "GASOIL", Qty = 36000m, Price = 85.4m, Date = new DateTime(2025, 10, 28) },
            new { Ext = "ITGR-2025-CAG-S0283", Prod = "BRENT", Qty = 85066m, Price = 45.4m, Date = new DateTime(2025, 10, 29) },
            new { Ext = "ITGR-2025-CAG-S0286", Prod = "GASOLINE", Qty = 67500m, Price = 80.4m, Date = new DateTime(2025, 11, 5) },
            new { Ext = "ITGR-2025-CAG-S0287", Prod = "GASOIL", Qty = 30000m, Price = 95.4m, Date = new DateTime(2025, 11, 5) },
            new { Ext = "ITGR-2025-CAG-S0293", Prod = "GASOIL", Qty = 30000m, Price = 95.4m, Date = new DateTime(2025, 11, 25) },
            new { Ext = "ITGR-2025-CAG-S0295", Prod = "GASOIL", Qty = 21000m, Price = 95.4m, Date = new DateTime(2025, 11, 27) },
            new { Ext = "ITGR-2025-CAG-S0296", Prod = "GASOIL", Qty = 54750m, Price = 95.4m, Date = new DateTime(2025, 12, 1) },
            new { Ext = "ITGR-2025-CAG-S0301", Prod = "GASOIL", Qty = 93750m, Price = 85.4m, Date = new DateTime(2025, 12, 9) },
            new { Ext = "ITGR-2025-CAG-S0302", Prod = "GASOIL", Qty = 48750m, Price = 85.4m, Date = new DateTime(2025, 12, 9) },
            new { Ext = "ITGR-2025-CAG-S0297", Prod = "GASOLINE", Qty = 180000m, Price = 80.3m, Date = new DateTime(2025, 12, 12) },
            new { Ext = "ITGR-2025-CAG-S0298", Prod = "GASOLINE", Qty = 100000m, Price = 80.3m, Date = new DateTime(2025, 12, 12) },
            new { Ext = "ITGR-2025-CAG-S0303", Prod = "GASOIL", Qty = 60000m, Price = 85.4m, Date = new DateTime(2025, 12, 16) }
        };

        int daxinContractNum = 100;
        foreach (var dc in daxinContracts)
        {
            var productId = dc.Prod switch
            {
                "GASOIL" => gasoilProduct.Id,
                "GASOLINE" => gasolineProduct.Id,
                "BRENT" => brentProduct.Id,
                _ => throw new InvalidOperationException($"Unknown product: {dc.Prod}")
            };

            var contract = new SalesContract(
                ContractNumber.Parse($"SC-2025-{daxinContractNum:D3}"),
                ContractType.CARGO,
                daxin.Id,
                productId,
                trader02.Id,
                new Quantity(dc.Qty, QuantityUnit.BBL),
                7.6m,
                null,
                null,
                dc.Ext
            );

            // Set laycan dates (contract date + 1 day for end)
            contract.UpdateLaycan(dc.Date, dc.Date.AddDays(1));
            contract.UpdatePorts("Singapore", "Yangon");

            // Set fixed price and contract value
            var contractValue = new Money(dc.Qty * dc.Price, "USD");
            contract.UpdatePricing(PriceFormula.Fixed(dc.Price), contractValue);

            contracts.Add(contract);
            daxinContractNum++;
        }

        // Initialize RowVersion for InMemory database (required property)
        foreach (var contract in contracts)
        {
            contract.SetRowVersion(new byte[] { 0 });
        }

        await _context.SalesContracts.AddRangeAsync(contracts);
        _logger.LogInformation("Added {Count} sales contracts (including {DaxinCount} DAXIN MARINE contracts)",
            contracts.Count, daxinContracts.Length);
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

        // Set RowVersion for InMemory database compatibility
        foreach (var op in shippingOps)
        {
            op.SetRowVersion(new byte[] { 0 });
        }

        await _context.ShippingOperations.AddRangeAsync(shippingOps);
        _logger.LogInformation("Added {Count} shipping operations", shippingOps.Length);
    }

    private async Task SeedMarketPricesAsync()
    {
        _logger.LogInformation("Seeding market prices (spot and futures)...");

        // Only seed if no prices exist
        var existingPrices = await _context.Set<MarketPrice>().AnyAsync();

        if (existingPrices)
        {
            _logger.LogInformation("Market prices already exist, skipping seeding");
            return;
        }

        var now = DateTime.UtcNow;
        var prices = new List<MarketPrice>();

        // Create spot prices for last 30 days - using professional API product codes
        // Following international oil trading standards (Vitol, Trafigura, Glencore)
        var spotProducts = new[]
        {
            new { ApiCode = "BRENT_CRUDE", DisplayName = "Brent Crude", BasePrice = 82.50m, Unit = MarketPriceUnit.BBL, Region = "North Sea", Source = "Platts" },
            new { ApiCode = "MOPS_GASOIL", DisplayName = "Gasoil 0.1% S", BasePrice = 95.50m, Unit = MarketPriceUnit.MT, Region = "Singapore", Source = "MOPS" },
            new { ApiCode = "MGO", DisplayName = "Marine Gas Oil", BasePrice = 650.00m, Unit = MarketPriceUnit.MT, Region = "Singapore", Source = "MOPS" },
            new { ApiCode = "JET_FUEL", DisplayName = "Jet Fuel (Kerosene)", BasePrice = 98.75m, Unit = MarketPriceUnit.BBL, Region = "Singapore", Source = "MOPS" },
            new { ApiCode = "BUNKER_SPORE", DisplayName = "HSFO 380 CST", BasePrice = 520.00m, Unit = MarketPriceUnit.MT, Region = "Singapore", Source = "MOPS" },
            new { ApiCode = "BUNKER_HK", DisplayName = "HSFO 380 CST", BasePrice = 525.00m, Unit = MarketPriceUnit.MT, Region = "Hong Kong", Source = "MOPS" },
            new { ApiCode = "FUEL_OIL_35_RTDM", DisplayName = "HSFO 380 CST", BasePrice = 515.00m, Unit = MarketPriceUnit.MT, Region = "Rotterdam", Source = "Platts" },
            new { ApiCode = "GASOLINE_92", DisplayName = "Gasoline 92 RON", BasePrice = 92.00m, Unit = MarketPriceUnit.BBL, Region = "Singapore", Source = "MOPS" },
            new { ApiCode = "GASOLINE_95", DisplayName = "Gasoline 95 RON", BasePrice = 95.50m, Unit = MarketPriceUnit.BBL, Region = "Singapore", Source = "MOPS" },
            new { ApiCode = "GASOLINE_97", DisplayName = "Gasoline 97 RON", BasePrice = 98.00m, Unit = MarketPriceUnit.BBL, Region = "Singapore", Source = "MOPS" }
        };

        for (int daysAgo = 30; daysAgo >= 0; daysAgo--)
        {
            var priceDate = now.AddDays(-daysAgo).Date;

            foreach (var product in spotProducts)
            {
                // Add slight daily variation
                var variation = (decimal)(new Random(product.ApiCode.GetHashCode() ^ daysAgo).NextDouble() * 4 - 2);
                var price = product.BasePrice + variation;

                var marketPrice = MarketPrice.Create(
                    priceDate: priceDate,
                    productCode: product.ApiCode,
                    productName: product.DisplayName,
                    priceType: MarketPriceType.Spot,
                    price: Math.Max(price, 10m), // Ensure price is positive
                    currency: "USD",
                    source: product.Source,
                    dataSource: "DataSeeder",
                    isSettlement: false,
                    importedAt: now,
                    importedBy: "System",
                    region: product.Region
                );

                marketPrice.Unit = product.Unit;
                marketPrice.ExchangeName = null; // Spot prices have no exchange

                prices.Add(marketPrice);
            }
        }

        // Create futures prices for next 6 contract months - using professional futures product codes
        // Following exchange standards (ICE, NYMEX)
        var futuresProducts = new[]
        {
            new { FuturesCode = "BRENT", DisplayName = "Brent Crude", BasePrice = 83.00m, Unit = MarketPriceUnit.BBL, Exchange = "ICE" },
            new { FuturesCode = "WTI", DisplayName = "WTI Crude", BasePrice = 79.00m, Unit = MarketPriceUnit.BBL, Exchange = "NYMEX" },
            new { FuturesCode = "GASOIL_FUTURES", DisplayName = "Gasoil 0.1% S", BasePrice = 96.00m, Unit = MarketPriceUnit.MT, Exchange = "ICE" }
        };

        // Contract months: current month + next 5 months in ISO format (YYYY-MM)
        var contractMonths = new List<string>();
        for (int monthOffset = 0; monthOffset < 6; monthOffset++)
        {
            var futureMonth = now.AddMonths(monthOffset);
            contractMonths.Add(futureMonth.ToString("yyyy-MM"));
        }

        for (int daysAgo = 30; daysAgo >= 0; daysAgo--)
        {
            var priceDate = now.AddDays(-daysAgo).Date;

            foreach (var product in futuresProducts)
            {
                // Add futures prices for each contract month
                for (int monthIndex = 0; monthIndex < contractMonths.Count; monthIndex++)
                {
                    var contractMonth = contractMonths[monthIndex];

                    // Futures typically have contango structure (forward months slightly higher)
                    var monthPremium = (decimal)monthIndex * 0.5m;
                    var variation = (decimal)(new Random((product.FuturesCode.GetHashCode() ^ daysAgo ^ monthIndex)).NextDouble() * 3 - 1.5);
                    var price = product.BasePrice + monthPremium + variation;

                    var futuresPrice = MarketPrice.Create(
                        priceDate: priceDate,
                        productCode: product.FuturesCode,  // Professional futures code (BRENT, WTI, GASOIL_FUTURES)
                        productName: product.DisplayName,
                        priceType: MarketPriceType.FuturesClose,
                        price: Math.Max(price, 10m), // Ensure price is positive
                        currency: "USD",
                        source: product.Exchange,
                        dataSource: "DataSeeder",
                        isSettlement: false,
                        importedAt: now,
                        importedBy: "System",
                        contractMonth: contractMonth // Separate field for contract month in ISO format
                    );

                    futuresPrice.Unit = product.Unit;
                    futuresPrice.ExchangeName = product.Exchange;

                    prices.Add(futuresPrice);
                }
            }
        }

        // Set RowVersion for InMemory database compatibility
        foreach (var price in prices)
        {
            price.SetRowVersion(new byte[] { 0 });
        }

        await _context.Set<MarketPrice>().AddRangeAsync(prices);
        _logger.LogInformation("Added {Count} market price records ({SpotCount} spot + {FuturesCount} futures)",
            prices.Count,
            prices.Count(p => p.PriceType == MarketPriceType.Spot),
            prices.Count(p => p.PriceType == MarketPriceType.FuturesClose));
    }
}
