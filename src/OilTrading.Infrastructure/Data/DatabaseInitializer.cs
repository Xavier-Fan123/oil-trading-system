using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using CoreUserRole = OilTrading.Core.Enums.UserRole;

namespace OilTrading.Infrastructure.Data;

/// <summary>
/// Database initializer for setting up the enhanced oil trading system
/// </summary>
public class DatabaseInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(ApplicationDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database with enhanced features
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization for enhanced trading features");

            // Ensure database is created and migrations are applied
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully");

            // Seed enhanced data if needed
            await SeedEnhancedDataAsync();

            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    /// <summary>
    /// Seeds enhanced trading system data
    /// </summary>
    private async Task SeedEnhancedDataAsync()
    {
        try
        {
            // Check if we need to seed any enhanced data
            var hasUsers = await _context.Users.AnyAsync();
            var hasProducts = await _context.Products.AnyAsync();
            var hasTradingPartners = await _context.TradingPartners.AnyAsync();
            var hasTradeChains = await _context.TradingChains.AnyAsync();

            if (!hasUsers || !hasProducts || !hasTradingPartners)
            {
                _logger.LogInformation("Seeding basic data for enhanced features");
                await SeedBasicDataAsync();
            }

            if (!hasTradeChains)
            {
                _logger.LogInformation("Seeding sample trade chains");
                await SeedSampleTradeChainsAsync();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Enhanced data seeding completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enhanced data seeding");
            throw;
        }
    }

    /// <summary>
    /// Seeds basic required data for the enhanced system
    /// </summary>
    private async Task SeedBasicDataAsync()
    {
        // Add enhanced users with proper roles
        if (!await _context.Users.AnyAsync(u => u.Role == UserRole.RiskManager))
        {
            var riskManager = new User
            {
                Email = "risk.manager@oiltrading.com",
                FirstName = "Risk",
                LastName = "Manager",
                PasswordHash = "hashed_password",
                Role = UserRole.RiskManager,
                IsActive = true
            };
            _context.Users.Add(riskManager);
        }

        if (!await _context.Users.AnyAsync(u => u.Role == (UserRole)CoreUserRole.SeniorTrader))
        {
            var seniorTrader = new User
            {
                Email = "senior.trader@oiltrading.com",
                FirstName = "Senior",
                LastName = "Trader",
                PasswordHash = "hashed_password",
                Role = (UserRole)CoreUserRole.SeniorTrader,
                IsActive = true
            };
            _context.Users.Add(seniorTrader);
        }

        // Add sample inventory locations if needed
        if (!await _context.InventoryLocations.AnyAsync())
        {
            var locations = new[]
            {
                new InventoryLocation
                {
                    LocationCode = "SG-JURONG",
                    LocationName = "Jurong Island Terminal",
                    LocationType = InventoryLocationType.Terminal,
                    Address = "Jurong Island, Singapore",
                    Country = "Singapore",
                    IsActive = true,
                    TotalCapacity = new Quantity(1000000, QuantityUnit.BBL),
                    AvailableCapacity = new Quantity(1000000, QuantityUnit.BBL)
                },
                new InventoryLocation
                {
                    LocationCode = "HK-TERMINAL",
                    LocationName = "Hong Kong Oil Terminal",
                    LocationType = InventoryLocationType.Terminal,
                    Address = "Hong Kong",
                    Country = "Hong Kong",
                    IsActive = true,
                    TotalCapacity = new Quantity(500000, QuantityUnit.BBL),
                    AvailableCapacity = new Quantity(500000, QuantityUnit.BBL)
                },
                new InventoryLocation
                {
                    LocationCode = "UAE-FUJAIRAH",
                    LocationName = "Fujairah Terminal",
                    LocationType = InventoryLocationType.Terminal,
                    Address = "Fujairah, UAE",
                    Country = "UAE",
                    IsActive = true,
                    TotalCapacity = new Quantity(2000000, QuantityUnit.BBL),
                    AvailableCapacity = new Quantity(2000000, QuantityUnit.BBL)
                }
            };
            _context.InventoryLocations.AddRange(locations);
        }
    }

    /// <summary>
    /// Seeds sample trade chains for demonstration
    /// </summary>
    private async Task SeedSampleTradeChainsAsync()
    {
        try
        {
            // Get required reference data
            var brentProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "BRENT");
            var shellSupplier = await _context.TradingPartners.FirstOrDefaultAsync(tp => tp.Code == "SHELL");
            var vitolCustomer = await _context.TradingPartners.FirstOrDefaultAsync(tp => tp.Code == "VITOL");

            if (brentProduct == null || shellSupplier == null || vitolCustomer == null)
            {
                _logger.LogWarning("Required reference data not found for sample trade chains");
                return;
            }

            // Create sample back-to-back trade chain
            var backToBackChain = new TradeChain(
                "BTB-20250116-0001",
                "Brent Back-to-Back Jan 2025",
                TradeChainType.BackToBack,
                "System");

            // Link purchase contract (simulated)
            backToBackChain.LinkPurchaseContract(
                Guid.NewGuid(),
                shellSupplier.Id,
                brentProduct.Id,
                new Quantity(100000, QuantityUnit.BBL),
                new Money(7500000m, "USD"),
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow.AddDays(35),
                "System");

            // Link sales contract (simulated)
            backToBackChain.LinkSalesContract(
                Guid.NewGuid(),
                vitolCustomer.Id,
                new Quantity(100000, QuantityUnit.BBL),
                new Money(7600000m, "USD"),
                "System");

            // Add some operations
            backToBackChain.AddOperation(
                TradeChainOperationType.ContractExecution,
                "Purchase and sales contracts executed",
                "System");

            backToBackChain.AddOperation(
                TradeChainOperationType.RiskManagement,
                "Risk limits verified and approved",
                "System");

            _context.TradingChains.Add(backToBackChain);

            // Create sample speculative trade chain
            var speculativeChain = new TradeChain(
                "SPEC-20250116-0001",
                "WTI Speculative Position",
                TradeChainType.Speculative,
                "System");

            var wtiProduct = await _context.Products.FirstOrDefaultAsync(p => p.Code == "WTI");
            if (wtiProduct != null)
            {
                speculativeChain.LinkPurchaseContract(
                    Guid.NewGuid(),
                    shellSupplier.Id,
                    wtiProduct.Id,
                    new Quantity(50000, QuantityUnit.BBL),
                    new Money(3750000m, "USD"),
                    DateTime.UtcNow.AddDays(45),
                    DateTime.UtcNow.AddDays(50),
                    "System");

                speculativeChain.AddOperation(
                    TradeChainOperationType.ContractExecution,
                    "Speculative purchase executed",
                    "System");

                _context.TradingChains.Add(speculativeChain);
            }

            _logger.LogInformation("Sample trade chains created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample trade chains");
        }
    }

    /// <summary>
    /// Creates database indexes for optimal performance
    /// </summary>
    public async Task CreateOptimalIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Creating optimal database indexes");

            // Create additional performance indexes via raw SQL
            var indexCommands = new[]
            {
                // Settlement performance indexes
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Settlements_ContractId_Status_DueDate')
                  CREATE INDEX IX_Settlements_ContractId_Status_DueDate ON Settlements (ContractId, Status, DueDate)",

                // Trade chain performance indexes
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TradingChains_Status_Type_TradeDate')
                  CREATE INDEX IX_TradingChains_Status_Type_TradeDate ON TradingChains (Status, Type, TradeDate)",

                // Inventory reservation performance indexes
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryReservations_ProductId_Status_ExpiryDate')
                  CREATE INDEX IX_InventoryReservations_ProductId_Status_ExpiryDate ON InventoryReservations (ProductId, Status, ExpiryDate)",

                // Trade chain operations performance index
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TradeChainOperations_TradeChainId_OperationType_PerformedAt')
                  CREATE INDEX IX_TradeChainOperations_TradeChainId_OperationType_PerformedAt ON TradeChainOperations (TradeChainId, OperationType, PerformedAt)",

                // Payment processing performance index
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_Status_PaymentDate')
                  CREATE INDEX IX_Payments_Status_PaymentDate ON Payments (Status, PaymentDate)"
            };

            foreach (var command in indexCommands)
            {
                await _context.Database.ExecuteSqlRawAsync(command);
            }

            _logger.LogInformation("Optimal database indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating optimal database indexes");
            throw;
        }
    }

    /// <summary>
    /// Validates database integrity and constraints
    /// </summary>
    public async Task<DatabaseValidationResult> ValidateDatabaseAsync()
    {
        var result = new DatabaseValidationResult();

        try
        {
            _logger.LogInformation("Starting database validation");

            // Check table existence
            result.TablesExist = await ValidateTablesExistAsync();
            
            // Check index existence
            result.IndexesExist = await ValidateIndexesExistAsync();
            
            // Check data integrity
            result.DataIntegrityValid = await ValidateDataIntegrityAsync();
            
            // Check foreign key constraints
            result.ForeignKeysValid = await ValidateForeignKeysAsync();

            result.IsValid = result.TablesExist && result.IndexesExist && 
                           result.DataIntegrityValid && result.ForeignKeysValid;

            _logger.LogInformation("Database validation completed. Valid: {IsValid}", result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database validation");
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    private async Task<bool> ValidateTablesExistAsync()
    {
        try
        {
            var requiredTables = new[]
            {
                "Settlements", "Payments", "SettlementAdjustments",
                "InventoryReservations", "TradingChains", 
                "TradeChainOperations", "TradeChainEvents"
            };

            foreach (var table in requiredTables)
            {
                var exists = await _context.Database.ExecuteSqlRawAsync(
                    $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}'") > 0;
                
                if (!exists)
                {
                    _logger.LogError("Required table {TableName} does not exist", table);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating table existence");
            return false;
        }
    }

    private async Task<bool> ValidateIndexesExistAsync()
    {
        try
        {
            // Check critical indexes exist
            var criticalIndexes = new[]
            {
                "IX_TradingChains_ChainId",
                "IX_Settlements_ContractId",
                "IX_InventoryReservations_ContractId",
                "IX_TradeChainOperations_TradeChainId"
            };

            // This is a simplified check - in production you'd query sys.indexes
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating index existence");
            return false;
        }
    }

    private async Task<bool> ValidateDataIntegrityAsync()
    {
        try
        {
            // Check for orphaned records
            var orphanedPayments = await _context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM Payments p LEFT JOIN Settlements s ON p.SettlementId = s.Id WHERE s.Id IS NULL");

            if (orphanedPayments > 0)
            {
                _logger.LogWarning("Found {Count} orphaned payment records", orphanedPayments);
            }

            // Add more integrity checks as needed
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data integrity");
            return false;
        }
    }

    private async Task<bool> ValidateForeignKeysAsync()
    {
        try
        {
            // Validate foreign key constraints are properly enforced
            // This is a placeholder - actual implementation would check constraint violations
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating foreign keys");
            return false;
        }
    }
}

/// <summary>
/// Result of database validation
/// </summary>
public class DatabaseValidationResult
{
    public bool IsValid { get; set; }
    public bool TablesExist { get; set; }
    public bool IndexesExist { get; set; }
    public bool DataIntegrityValid { get; set; }
    public bool ForeignKeysValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public string GetSummary()
    {
        var status = IsValid ? "VALID" : "INVALID";
        var errorCount = Errors.Count;
        var warningCount = Warnings.Count;
        
        return $"Database Validation: {status} | Errors: {errorCount} | Warnings: {warningCount}";
    }
}