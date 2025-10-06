using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PriceBenchmarkConfiguration : IEntityTypeConfiguration<PriceBenchmark>
{
    public void Configure(EntityTypeBuilder<PriceBenchmark> builder)
    {
        builder.ToTable("price_benchmarks");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
            
        builder.Property(x => x.BenchmarkName)
            .HasColumnName("benchmark_name")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(x => x.BenchmarkType)
            .HasColumnName("benchmark_type")
            .IsRequired();
            
        builder.Property(x => x.ProductCategory)
            .HasColumnName("product_category")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();
            
        builder.Property(x => x.Unit)
            .HasColumnName("unit")
            .HasMaxLength(10)
            .IsRequired();
            
        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
            
        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);
            
        builder.Property(x => x.DataSource)
            .HasColumnName("data_source")
            .HasMaxLength(200);
            
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
            
        // Indexes
        builder.HasIndex(x => x.BenchmarkName)
            .HasDatabaseName("ix_price_benchmarks_benchmark_name");
            
        builder.HasIndex(x => new { x.BenchmarkType, x.ProductCategory })
            .HasDatabaseName("ix_price_benchmarks_type_category");
            
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_price_benchmarks_is_active");
    }
}