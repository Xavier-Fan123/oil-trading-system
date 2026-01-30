using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class MarketPriceConfiguration : IEntityTypeConfiguration<MarketPrice>
{
    public void Configure(EntityTypeBuilder<MarketPrice> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.PriceDate)
               .IsRequired();
               
        builder.Property(e => e.ProductCode)
               .IsRequired()
               .HasMaxLength(50);
               
        builder.Property(e => e.ProductName)
               .IsRequired()
               .HasMaxLength(200);
               
        builder.Property(e => e.PriceType)
               .IsRequired()
               .HasConversion<int>();
               
        builder.Property(e => e.Price)
               .HasPrecision(18, 4)
               .IsRequired();
               
        builder.Property(e => e.Currency)
               .HasMaxLength(3)
               .IsRequired();
               
        builder.Property(e => e.ContractMonth)
               .HasMaxLength(20);
               
        builder.Property(e => e.Source)
               .HasMaxLength(100);
               
        builder.Property(e => e.DataSource)
               .HasMaxLength(100);
               
        builder.Property(e => e.IsSettlement)
               .IsRequired()
               .HasDefaultValue(false);
               
        builder.Property(e => e.ImportedAt)
               .IsRequired();
               
        builder.Property(e => e.ImportedBy)
               .HasMaxLength(100);

        // New fields for enhanced market data support
        // CRITICAL FIX: Unit column doesn't exist in SQLite database
        // Ignore this property to prevent "no such column: m0.Unit" errors
        // When database schema is updated to include Unit, change this to:
        // builder.Property(e => e.Unit).HasConversion<int?>();
        builder.Ignore(e => e.Unit);

        // CRITICAL FIX: ExchangeName column doesn't exist in SQLite database
        // Ignore this property to prevent "no such column: m0.ExchangeName" errors
        // When database schema is updated to include ExchangeName, change this to:
        // builder.Property(e => e.ExchangeName).HasMaxLength(50);
        builder.Ignore(e => e.ExchangeName);

        // Region field for spot price regional differentiation
        builder.Property(e => e.Region)
               .HasMaxLength(50);  // "Singapore", "Dubai", etc.

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Concurrency control - RowVersion for optimistic concurrency
        // CRITICAL FIX FOR SQLITE: Use HasDefaultValueSql() with CURRENT_TIMESTAMP
        // SQLite requires explicit configuration to auto-increment version on updates
        // EF Core change tracker still needs explicit initialization in factory methods
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasDefaultValueSql("X'00'");  // SQLite binary literal for initial value

        // Soft delete
        // CRITICAL: Do NOT use HasDefaultValue() with soft delete flag
        // EF Core's change tracker ignores properties set to their inline-initialized defaults
        // By removing the database default, we force the application to always provide the value
        builder.Property(e => e.IsDeleted)
               .IsRequired();
               // .HasDefaultValue(false);  // REMOVED: This caused change tracking optimization
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy).HasMaxLength(100);
        
        // Indexes for performance
        builder.HasIndex(e => e.PriceDate)
               .HasDatabaseName("IX_MarketPrices_PriceDate");
               
        builder.HasIndex(e => e.ProductCode)
               .HasDatabaseName("IX_MarketPrices_ProductCode");
               
        builder.HasIndex(e => new { e.ProductCode, e.ContractMonth, e.PriceDate, e.PriceType })
               .IsUnique()
               .HasDatabaseName("IX_MarketPrices_ProductCode_ContractMonth_PriceDate_PriceType");

        // Index for Region-based queries (spot prices)
        builder.HasIndex(e => new { e.ProductCode, e.Region, e.PriceDate, e.PriceType })
               .HasDatabaseName("IX_MarketPrices_ProductCode_Region_PriceDate_PriceType");
               
        builder.HasIndex(e => e.ContractMonth)
               .HasDatabaseName("IX_MarketPrices_ContractMonth");
               
        builder.HasIndex(e => e.PriceType)
               .HasDatabaseName("IX_MarketPrices_PriceType");
               
        builder.HasIndex(e => e.DataSource)
               .HasDatabaseName("IX_MarketPrices_DataSource");
               
        builder.ToTable("MarketPrices");

        // ===== REMOVED: Foreign Key Relationship with Product =====
        // Removed ProductId and Product navigation to fix schema mismatch errors
        // Using ProductCode as natural key instead
        // ❌ builder.HasOne(e => e.Product)
        // ❌        .WithMany(p => p.MarketPrices)
        // ❌        .HasForeignKey(e => e.ProductId)
        // ❌        .OnDelete(DeleteBehavior.SetNull)
        // ❌        .HasConstraintName("FK_MarketPrices_Products_ProductId");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}