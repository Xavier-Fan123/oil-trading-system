using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Infrastructure.Data.Configurations;
using OilTrading.Core.Entities.TimeSeries;
using OilTrading.Infrastructure.Data.Extensions;

namespace OilTrading.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Main entities
    public DbSet<User> Users { get; set; }
    public DbSet<TradingPartner> TradingPartners { get; set; }
    public DbSet<FinancialReport> FinancialReports { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<PurchaseContract> PurchaseContracts { get; set; }
    public DbSet<SalesContract> SalesContracts { get; set; }
    public DbSet<ContractMatching> ContractMatchings { get; set; }
    public DbSet<ShippingOperation> ShippingOperations { get; set; }
    public DbSet<PricingEvent> PricingEvents { get; set; }
    
    // Time-series data
    public DbSet<MarketData> MarketData { get; set; }
    public DbSet<PriceIndex> PriceIndices { get; set; }
    public DbSet<ContractEvent> ContractEvents { get; set; }
    
    // New oil trading entities
    public DbSet<PriceBenchmark> PriceBenchmarks { get; set; }
    public DbSet<DailyPrice> DailyPrices { get; set; }
    public DbSet<ContractPricingEvent> ContractPricingEvents { get; set; }
    
    // Market data and paper trading entities
    public DbSet<MarketPrice> MarketPrices { get; set; }
    public DbSet<PaperContract> PaperContracts { get; set; }
    public DbSet<FuturesDeal> FuturesDeals { get; set; }
    
    // Physical contracts
    public DbSet<PhysicalContract> PhysicalContracts { get; set; }
    
    // Inventory management
    public DbSet<InventoryLocation> InventoryLocations { get; set; }
    public DbSet<InventoryPosition> InventoryPositions { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    
    // Trade groups for multi-leg strategies
    public DbSet<TradeGroup> TradeGroups { get; set; }
    public DbSet<TradeGroupTag> TradeGroupTags { get; set; }
    
    // Trade chain tracking
    public DbSet<TradeChain> TradingChains { get; set; }
    
    // Tag management
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ContractTag> ContractTags { get; set; }
    
    // Settlement and payment management
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<SettlementAdjustment> SettlementAdjustments { get; set; }
    
    // Contract settlement system with mixed-unit pricing support
    public DbSet<ContractSettlement> ContractSettlements { get; set; }
    public DbSet<SettlementCharge> SettlementCharges { get; set; }
    
    // Inventory reservations
    public DbSet<InventoryReservation> InventoryReservations { get; set; }
    
    // Audit logging
    public DbSet<OperationAuditLog> OperationAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TradingPartnerConfiguration());
        modelBuilder.ApplyConfiguration(new FinancialReportConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseContractConfiguration());
        modelBuilder.ApplyConfiguration(new SalesContractConfiguration());
        modelBuilder.ApplyConfiguration(new ShippingOperationConfiguration());
        modelBuilder.ApplyConfiguration(new PricingEventConfiguration());
        
        // Apply time-series configurations
        modelBuilder.ApplyConfiguration(new MarketDataConfiguration());
        modelBuilder.ApplyConfiguration(new PriceIndexConfiguration());
        modelBuilder.ApplyConfiguration(new ContractEventConfiguration());
        
        // Apply new oil trading configurations
        modelBuilder.ApplyConfiguration(new PriceBenchmarkConfiguration());
        modelBuilder.ApplyConfiguration(new DailyPriceConfiguration());
        modelBuilder.ApplyConfiguration(new ContractPricingEventConfiguration());
        
        // Apply market data and paper trading configurations
        modelBuilder.ApplyConfiguration(new MarketPriceConfiguration());
        modelBuilder.ApplyConfiguration(new PaperContractConfiguration());
        modelBuilder.ApplyConfiguration(new FuturesDealConfiguration());
        
        // Apply physical contract configuration
        modelBuilder.ApplyConfiguration(new PhysicalContractConfiguration());
        
        // Apply inventory configurations
        modelBuilder.ApplyConfiguration(new InventoryLocationConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryPositionConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryMovementConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryReservationConfiguration());
        
        // Apply trade group configuration
        modelBuilder.ApplyConfiguration(new TradeGroupConfiguration());
        modelBuilder.ApplyConfiguration(new TradeGroupTagConfiguration());
        
        // Apply trade chain configuration
        modelBuilder.ApplyConfiguration(new TradeChainConfiguration());
        
        // Apply payment configuration
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        
        // Apply settlement configurations
        modelBuilder.ApplyConfiguration(new SettlementConfiguration());
        modelBuilder.ApplyConfiguration(new SettlementAdjustmentConfiguration());
        
        // Apply contract settlement configurations (mixed-unit pricing support)
        modelBuilder.ApplyConfiguration(new ContractSettlementConfiguration());
        modelBuilder.ApplyConfiguration(new SettlementChargeConfiguration());
        
        // Apply tag configurations
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new ContractTagConfiguration());
        
        // Apply audit log configuration
        modelBuilder.ApplyConfiguration(new OperationAuditLogConfiguration());

        // Mark shared owned types globally
        modelBuilder.Owned<Money>();
        modelBuilder.Owned<Quantity>();
        modelBuilder.Owned<PriceFormula>();
        modelBuilder.Owned<ContractNumber>();
        
        // Configure enum conversions
        ConfigureEnumConversions(modelBuilder);

        // Apply database optimizations for performance
        modelBuilder.ApplyDatabaseOptimizations();

        // Apply global query filters for soft delete and configure optimistic concurrency
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityClrType = entityType.ClrType;
            if (typeof(BaseEntity).IsAssignableFrom(entityClrType))
            {
                // Apply soft delete query filter
                var filterMethod = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityClrType);
                filterMethod.Invoke(null, new object[] { modelBuilder });

                // Configure RowVersion for optimistic concurrency control
                var concurrencyMethod = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ConfigureOptimisticConcurrency), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityClrType);
                concurrencyMethod.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetGlobalQueryFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureOptimisticConcurrency<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        // Configure RowVersion as a concurrency token (Timestamp in SQL Server, auto-updated)
        modelBuilder.Entity<T>()
            .Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
    }


    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // User enums
        modelBuilder.Entity<User>()
            .Property(e => e.Role)
            .HasConversion<int>();

        // TradingPartner enums
        modelBuilder.Entity<TradingPartner>()
            .Property(e => e.Type)
            .HasConversion<int>();

        // Product enums
        modelBuilder.Entity<Product>()
            .Property(e => e.Type)
            .HasConversion<int>();

        // Contract enums
        modelBuilder.Entity<PurchaseContract>()
            .Property(e => e.Status)
            .HasConversion<int>();

        modelBuilder.Entity<PurchaseContract>()
            .Property(e => e.ContractType)
            .HasConversion<int>();

        modelBuilder.Entity<SalesContract>()
            .Property(e => e.Status)
            .HasConversion<int>();

        modelBuilder.Entity<SalesContract>()
            .Property(e => e.ContractType)
            .HasConversion<int>();

        // New enum conversions
        modelBuilder.Entity<PurchaseContract>()
            .Property(e => e.DeliveryTerms)
            .HasConversion<int>();

        modelBuilder.Entity<PurchaseContract>()
            .Property(e => e.SettlementType)
            .HasConversion<int>();

        modelBuilder.Entity<SalesContract>()
            .Property(e => e.DeliveryTerms)
            .HasConversion<int>();

        modelBuilder.Entity<SalesContract>()
            .Property(e => e.SettlementType)
            .HasConversion<int>();

        // Shipping Operation enums
        modelBuilder.Entity<ShippingOperation>()
            .Property(e => e.Status)
            .HasConversion<int>();

        // Pricing Event enums
        modelBuilder.Entity<PricingEvent>()
            .Property(e => e.EventType)
            .HasConversion<int>();

        // New entity enum conversions
        modelBuilder.Entity<PriceBenchmark>()
            .Property(e => e.BenchmarkType)
            .HasConversion<int>();

        modelBuilder.Entity<ContractPricingEvent>()
            .Property(e => e.EventType)
            .HasConversion<int>();

        modelBuilder.Entity<ContractPricingEvent>()
            .Property(e => e.Status)
            .HasConversion<int>();
        
        // Physical Contract enums  
        modelBuilder.Entity<PhysicalContract>()
            .Property(e => e.ContractType)
            .HasConversion<int>();
        
        modelBuilder.Entity<PhysicalContract>()
            .Property(e => e.PricingType)
            .HasConversion<int>();
        
        modelBuilder.Entity<PhysicalContract>()
            .Property(e => e.Status)
            .HasConversion<int>();

        // Contract Settlement enums
        modelBuilder.Entity<ContractSettlement>()
            .Property(e => e.DocumentType)
            .HasConversion<int>();

        modelBuilder.Entity<ContractSettlement>()
            .Property(e => e.Status)
            .HasConversion<int>();

        modelBuilder.Entity<SettlementCharge>()
            .Property(e => e.ChargeType)
            .HasConversion<int>();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        // This would be handled by a domain event dispatcher in a real implementation
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}