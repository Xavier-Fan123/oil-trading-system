using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data.Configurations;

namespace OilTrading.Infrastructure.Data;

public class ApplicationReadDbContext : DbContext
{
    public ApplicationReadDbContext(DbContextOptions<ApplicationReadDbContext> options) : base(options)
    {
    }

    public DbSet<PurchaseContract> PurchaseContracts { get; set; }
    public DbSet<SalesContract> SalesContracts { get; set; }
    public DbSet<TradingPartner> TradingPartners { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ShippingOperation> ShippingOperations { get; set; }
    public DbSet<PricingEvent> PricingEvents { get; set; }
    public DbSet<DailyPrice> DailyPrices { get; set; }
    public DbSet<PaperContract> PaperContracts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityClrType = entityType.ClrType;
            if (typeof(BaseEntity).IsAssignableFrom(entityClrType))
            {
                var method = typeof(ApplicationReadDbContext)
                    .GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private static void SetGlobalQueryFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    // Override SaveChanges to make it read-only
    public override int SaveChanges()
    {
        throw new InvalidOperationException("This context is read-only. Use ApplicationDbContext for write operations.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only. Use ApplicationDbContext for write operations.");
    }
}