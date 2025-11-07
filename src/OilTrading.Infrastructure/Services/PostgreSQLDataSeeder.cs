using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Enums;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

public class PostgreSQLDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostgreSQLDataSeeder> _logger;

    public PostgreSQLDataSeeder(ApplicationDbContext context, ILogger<PostgreSQLDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            _logger.LogInformation("Starting PostgreSQL database seeding...");

            await SeedUsersAsync();
            await SeedProductsAsync();
            await SeedTradingPartnersAsync();
            await SeedSampleContractsAsync();
            await SeedMarketDataAsync();

            await _context.SaveChangesAsync();

            _logger.LogInformation("PostgreSQL database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding PostgreSQL database");
            throw;
        }
    }

    private async Task SeedUsersAsync()
    {
        var users = new[]
        {
            new User
            {
                Email = "trader@oiltrading.com",
                FirstName = "John",
                LastName = "Trader",
                PasswordHash = "hashed_password_123",
                Role = OilTrading.Core.Entities.UserRole.Trader,
                IsActive = true
            },
            new User
            {
                Email = "admin@oiltrading.com",
                FirstName = "Sarah",
                LastName = "Admin",
                PasswordHash = "hashed_password_456",
                Role = OilTrading.Core.Entities.UserRole.Administrator,
                IsActive = true
            },
            new User
            {
                Email = "riskmanager@oiltrading.com",
                FirstName = "Mike",
                LastName = "Risk",
                PasswordHash = "hashed_password_789",
                Role = OilTrading.Core.Entities.UserRole.RiskManager,
                IsActive = true
            }
        };

        await _context.Users.AddRangeAsync(users);
        _logger.LogInformation("Added {Count} users", users.Length);
    }

    private async Task SeedProductsAsync()
    {
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
                Grade = "Light Sweet",
                Specification = "API 38.0°, Sulfur 0.37%",
                UnitOfMeasure = "BBL",
                Density = 835.0m,
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
                Grade = "Light Sweet",
                Specification = "API 39.6°, Sulfur 0.24%",
                UnitOfMeasure = "BBL",
                Density = 827.0m,
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
                Grade = "0.5% Sulfur",
                Specification = "ISO 8217:2017, Max 0.5% Sulfur",
                UnitOfMeasure = "MT",
                Density = 890.0m,
                Origin = "Singapore",
                IsActive = true
            },
            new Product
            {
                Code = "HSFO",
                Name = "High Sulfur Fuel Oil",
                ProductName = "High Sulfur Fuel Oil",
                ProductCode = "HSFO",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Grade = "380 CST",
                Specification = "3.5% Sulfur Max, 380 CST @ 50°C",
                UnitOfMeasure = "MT",
                Density = 991.0m,
                Origin = "Singapore",
                IsActive = true
            },
            new Product
            {
                Code = "JET",
                Name = "Jet Fuel A-1",
                ProductName = "Jet Fuel A-1",
                ProductCode = "JET",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Grade = "Aviation",
                Specification = "ASTM D1655, DEF STAN 91-91",
                UnitOfMeasure = "BBL",
                Density = 775.0m,
                Origin = "Singapore",
                IsActive = true
            },
            new Product
            {
                Code = "GASOIL",
                Name = "Gas Oil",
                ProductName = "Gas Oil",
                ProductCode = "GASOIL",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Grade = "0.1% Sulfur",
                Specification = "EN 590, Max 0.1% Sulfur",
                UnitOfMeasure = "MT",
                Density = 845.0m,
                Origin = "Europe",
                IsActive = true
            }
        };

        await _context.Products.AddRangeAsync(products);
        _logger.LogInformation("Added {Count} products", products.Length);
    }

    private async Task SeedTradingPartnersAsync()
    {
        var partners = new[]
        {
            new TradingPartner
            {
                Code = "SHELL",
                Name = "Shell Trading",
                CompanyName = "Royal Dutch Shell plc",
                CompanyCode = "SHELL",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@shell.com",
                ContactPhone = "+65 6384 8000",
                Address = "12 Marina Boulevard, Marina Bay Financial Centre Tower 3, Singapore 018982",
                Country = "Singapore",
                TaxId = "199404942G",
                IsActive = true,
                CreditLimit = 50000000m,
                CreditRating = "AA"
            },
            new TradingPartner
            {
                Code = "BP",
                Name = "BP Trading",
                CompanyName = "BP p.l.c.",
                CompanyCode = "BP",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@bp.com",
                ContactPhone = "+65 6349 3888",
                Address = "1 HarbourFront Place, #18-01 HarbourFront Tower One, Singapore 098633",
                Country = "Singapore",
                TaxId = "200001234K",
                IsActive = true,
                CreditLimit = 45000000m,
                CreditRating = "AA"
            },
            new TradingPartner
            {
                Code = "EXXON",
                Name = "ExxonMobil Trading",
                CompanyName = "Exxon Mobil Corporation",
                CompanyCode = "EXXON",
                Type = TradingPartnerType.Supplier,
                ContactEmail = "trading@exxonmobil.com",
                ContactPhone = "+65 6885 0000",
                Address = "1 HarbourFront Avenue, #14-01 Keppel Bay Tower, Singapore 098632",
                Country = "Singapore",
                TaxId = "199901234H",
                IsActive = true,
                CreditLimit = 60000000m,
                CreditRating = "AAA"
            },
            new TradingPartner
            {
                Code = "VITOL",
                Name = "Vitol Asia",
                CompanyName = "Vitol Asia Pte Ltd",
                CompanyCode = "VITOL",
                Type = TradingPartnerType.Customer,
                ContactEmail = "trading@vitol.com",
                ContactPhone = "+65 6327 6666",
                Address = "50 Collyer Quay, #09-01 OUE Bayfront, Singapore 049321",
                Country = "Singapore",
                TaxId = "200212345M",
                IsActive = true,
                CreditLimit = 40000000m,
                CreditRating = "A+"
            },
            new TradingPartner
            {
                Code = "TRAFIG",
                Name = "Trafigura",
                CompanyName = "Trafigura Pte Ltd",
                CompanyCode = "TRAFIG",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@trafigura.com",
                ContactPhone = "+65 6319 2960",
                Address = "10 Collyer Quay, #40-01 Ocean Financial Centre, Singapore 049315",
                Country = "Singapore",
                TaxId = "200312346N",
                IsActive = true,
                CreditLimit = 35000000m,
                CreditRating = "A"
            },
            new TradingPartner
            {
                Code = "GUNVOR",
                Name = "Gunvor Singapore",
                CompanyName = "Gunvor Singapore Pte Ltd",
                CompanyCode = "GUNVOR",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@gunvor.com",
                ContactPhone = "+65 6416 0800",
                Address = "80 Raffles Place, #58-01 UOB Plaza 1, Singapore 048624",
                Country = "Singapore",
                TaxId = "200412347P",
                IsActive = true,
                CreditLimit = 30000000m,
                CreditRating = "BBB+"
            }
        };

        await _context.TradingPartners.AddRangeAsync(partners);
        _logger.LogInformation("Added {Count} trading partners", partners.Length);
    }

    private async Task SeedSampleContractsAsync()
    {
        // Wait for products and partners to be saved
        await _context.SaveChangesAsync();

        var brentProduct = await _context.Products.FirstAsync(p => p.Code == "BRENT");
        var wtiProduct = await _context.Products.FirstAsync(p => p.Code == "WTI");
        var shellPartner = await _context.TradingPartners.FirstAsync(tp => tp.Code == "SHELL");
        var bpPartner = await _context.TradingPartners.FirstAsync(tp => tp.Code == "BP");
        var exxonPartner = await _context.TradingPartners.FirstAsync(tp => tp.Code == "EXXON");
        var trader = await _context.Users.FirstAsync(u => u.Role == OilTrading.Core.Entities.UserRole.Trader);

        // Contract 1: Brent Crude 50000 BBL @ USD 85.50/BBL
        var contract1Number = ContractNumber.Create(DateTime.UtcNow.Year, ContractType.CARGO, 1);
        var contract1Quantity = new Quantity(50000, QuantityUnit.BBL);
        var contract1 = new PurchaseContract(
            contract1Number,
            ContractType.CARGO,
            shellPartner.Id,
            brentProduct.Id,
            trader.Id,
            contract1Quantity,
            7.6m);
        contract1.SetExternalContractNumber("EXT-SINOPEC-001", "system");

        var contract1Price = 85.50m;
        var contract1Value = new Money(50000 * contract1Price, "USD");
        var contract1Formula = PriceFormula.Fixed(contract1Price);
        contract1.UpdatePricing(contract1Formula, contract1Value);
        contract1.UpdateDeliveryTerms(DeliveryTerms.FOB);
        contract1.UpdateLaycan(DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(45));
        contract1.UpdatePorts("Ras Tanura, Saudi Arabia", "Singapore");
        contract1.UpdateSettlementType(SettlementType.ContractPayment);
        contract1.UpdatePaymentTerms("TT 30 days after B/L presentation", 30);
        contract1.UpdateQualitySpecifications("API 38.0° min, Sulfur 0.37% max");
        contract1.UpdateInspectionAgency("SGS");
        contract1.AddNotes("Sample Brent crude contract - PC-2025-001");
        await _context.PurchaseContracts.AddAsync(contract1);

        // Contract 2: WTI Crude 30000 BBL @ USD 78.25/BBL
        var contract2Number = ContractNumber.Create(DateTime.UtcNow.Year, ContractType.CARGO, 2);
        var contract2Quantity = new Quantity(30000, QuantityUnit.BBL);
        var contract2 = new PurchaseContract(
            contract2Number,
            ContractType.CARGO,
            bpPartner.Id,
            wtiProduct.Id,
            trader.Id,
            contract2Quantity,
            7.6m);
        contract2.SetExternalContractNumber("EXT-PETRONAS-001", "system");

        var contract2Price = 78.25m;
        var contract2Value = new Money(30000 * contract2Price, "USD");
        var contract2Formula = PriceFormula.Fixed(contract2Price);
        contract2.UpdatePricing(contract2Formula, contract2Value);
        contract2.UpdateDeliveryTerms(DeliveryTerms.CIF);
        contract2.UpdateLaycan(DateTime.UtcNow.AddDays(60), DateTime.UtcNow.AddDays(75));
        contract2.UpdatePorts("Corpus Christi, USA", "Rotterdam, Netherlands");
        contract2.UpdateSettlementType(SettlementType.ContractPayment);
        contract2.UpdatePaymentTerms("10% prepayment, balance TT 45 days after B/L", 45);
        contract2.SetPrepaymentPercentage(10);
        contract2.UpdateQualitySpecifications("API 39.6° min, Sulfur 0.24% max");
        contract2.UpdateInspectionAgency("SGS");
        contract2.AddNotes("Sample WTI crude contract - PC-2025-002");
        await _context.PurchaseContracts.AddAsync(contract2);

        // Contract 3: Brent Crude 25000 BBL @ USD 84.75/BBL
        var contract3Number = ContractNumber.Create(DateTime.UtcNow.Year, ContractType.CARGO, 3);
        var contract3Quantity = new Quantity(25000, QuantityUnit.BBL);
        var contract3 = new PurchaseContract(
            contract3Number,
            ContractType.CARGO,
            exxonPartner.Id,
            brentProduct.Id,
            trader.Id,
            contract3Quantity,
            7.6m);
        contract3.SetExternalContractNumber("EXT-SINOPEC-002", "system");

        var contract3Price = 84.75m;
        var contract3Value = new Money(25000 * contract3Price, "USD");
        var contract3Formula = PriceFormula.Fixed(contract3Price);
        contract3.UpdatePricing(contract3Formula, contract3Value);
        contract3.UpdateDeliveryTerms(DeliveryTerms.FOB);
        contract3.UpdateLaycan(DateTime.UtcNow.AddDays(45), DateTime.UtcNow.AddDays(60));
        contract3.UpdatePorts("Ras Tanura, Saudi Arabia", "Singapore");
        contract3.UpdateSettlementType(SettlementType.ContractPayment);
        contract3.UpdatePaymentTerms("LC at sight, 60 days tenor", 60);
        contract3.UpdateQualitySpecifications("API 38.0° min, Sulfur 0.37% max");
        contract3.UpdateInspectionAgency("SGS");
        contract3.AddNotes("Sample Brent crude contract - PC-2025-003");
        await _context.PurchaseContracts.AddAsync(contract3);

        _logger.LogInformation("Added 3 complete sample purchase contracts with all required fields");
    }

    private async Task SeedMarketDataAsync()
    {
        var currentDate = DateTime.UtcNow.Date;
        var marketPrices = new List<MarketPrice>();

        // Generate sample price data for the last 30 days
        for (int i = 30; i >= 0; i--)
        {
            var date = currentDate.AddDays(-i);
            
            marketPrices.AddRange(new[]
            {
                new MarketPrice
                {
                    PriceDate = date,
                    ProductCode = "BRENT",
                    ProductName = "Brent Crude Oil",
                    PriceType = MarketPriceType.Spot,
                    Price = 75.50m + (decimal)(new Random().NextDouble() * 10 - 5), // 70.50 - 80.50 range
                    Currency = "USD",
                    Source = "ICE",
                    DataSource = "ICE",
                    ImportedAt = DateTime.UtcNow,
                    ImportedBy = "system"
                },
                new MarketPrice
                {
                    PriceDate = date,
                    ProductCode = "WTI",
                    ProductName = "West Texas Intermediate",
                    PriceType = MarketPriceType.Spot,
                    Price = 72.25m + (decimal)(new Random().NextDouble() * 8 - 4), // 68.25 - 76.25 range
                    Currency = "USD",
                    Source = "NYMEX",
                    DataSource = "NYMEX",
                    ImportedAt = DateTime.UtcNow,
                    ImportedBy = "system"
                },
                new MarketPrice
                {
                    PriceDate = date,
                    ProductCode = "GASOIL",
                    ProductName = "Gas Oil",
                    PriceType = MarketPriceType.Spot,
                    Price = 650.00m + (decimal)(new Random().NextDouble() * 100 - 50), // 600 - 700 range
                    Currency = "USD",
                    Source = "ICE",
                    DataSource = "ICE",
                    ImportedAt = DateTime.UtcNow,
                    ImportedBy = "system"
                }
            });
        }

        await _context.MarketPrices.AddRangeAsync(marketPrices);
        _logger.LogInformation("Added {Count} market price records", marketPrices.Count);
    }
}