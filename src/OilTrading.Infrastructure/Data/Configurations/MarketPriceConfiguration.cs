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
        
        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        
        // Indexes for performance
        builder.HasIndex(e => e.PriceDate)
               .HasDatabaseName("IX_MarketPrices_PriceDate");
               
        builder.HasIndex(e => e.ProductCode)
               .HasDatabaseName("IX_MarketPrices_ProductCode");
               
        builder.HasIndex(e => new { e.ProductCode, e.PriceDate })
               .IsUnique()
               .HasDatabaseName("IX_MarketPrices_ProductCode_PriceDate");
               
        builder.HasIndex(e => e.ContractMonth)
               .HasDatabaseName("IX_MarketPrices_ContractMonth");
               
        builder.HasIndex(e => e.PriceType)
               .HasDatabaseName("IX_MarketPrices_PriceType");
               
        builder.HasIndex(e => e.DataSource)
               .HasDatabaseName("IX_MarketPrices_DataSource");
               
        builder.ToTable("MarketPrices");
        
        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}