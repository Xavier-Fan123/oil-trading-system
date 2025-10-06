using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class DailyPriceConfiguration : IEntityTypeConfiguration<DailyPrice>
{
    public void Configure(EntityTypeBuilder<DailyPrice> builder)
    {
        builder.ToTable("daily_prices");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
            
        builder.Property(x => x.BenchmarkId)
            .HasColumnName("benchmark_id")
            .IsRequired();
            
        builder.Property(x => x.PriceDate)
            .HasColumnName("price_date")
            .HasColumnType("date")
            .IsRequired();
            
        builder.Property(x => x.OpenPrice)
            .HasColumnName("open_price")
            .HasPrecision(18, 6);
            
        builder.Property(x => x.HighPrice)
            .HasColumnName("high_price")
            .HasPrecision(18, 6);
            
        builder.Property(x => x.LowPrice)
            .HasColumnName("low_price")
            .HasPrecision(18, 6);
            
        builder.Property(x => x.ClosePrice)
            .HasColumnName("close_price")
            .HasPrecision(18, 6)
            .IsRequired();
            
        builder.Property(x => x.Volume)
            .HasColumnName("volume")
            .HasPrecision(18, 2);
            
        builder.Property(x => x.Premium)
            .HasColumnName("premium")
            .HasPrecision(18, 6);
            
        builder.Property(x => x.Discount)
            .HasColumnName("discount")
            .HasPrecision(18, 6);
            
        builder.Property(x => x.IsPublished)
            .HasColumnName("is_published")
            .IsRequired();
            
        builder.Property(x => x.DataQuality)
            .HasColumnName("data_quality")
            .HasMaxLength(20);
            
        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);
            
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);
            
        // Foreign key relationship
        builder.HasOne(x => x.Benchmark)
            .WithMany(x => x.DailyPrices)
            .HasForeignKey(x => x.BenchmarkId)
            .HasConstraintName("fk_daily_prices_benchmark")
            .OnDelete(DeleteBehavior.Cascade);
            
        // Indexes for time-series queries
        builder.HasIndex(x => new { x.BenchmarkId, x.PriceDate })
            .HasDatabaseName("ix_daily_prices_benchmark_date")
            .IsUnique();
            
        builder.HasIndex(x => x.PriceDate)
            .HasDatabaseName("ix_daily_prices_price_date");
            
        builder.HasIndex(x => x.IsPublished)
            .HasDatabaseName("ix_daily_prices_is_published");
    }
}