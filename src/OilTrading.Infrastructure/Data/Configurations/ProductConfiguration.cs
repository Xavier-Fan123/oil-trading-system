using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Code - Unique index
        builder.HasIndex(e => e.Code)
               .IsUnique()
               .HasDatabaseName("IX_Products_Code");
        
        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Code)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.Type)
               .IsRequired();
               
        builder.Property(e => e.ProductType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Grade)
               .HasMaxLength(100);

        builder.Property(e => e.Specification)
               .HasMaxLength(1000);

        builder.Property(e => e.UnitOfMeasure)
               .HasMaxLength(50);

        builder.Property(e => e.Density)
               .HasPrecision(10, 4);

        builder.Property(e => e.Origin)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Indexes for performance
        builder.HasIndex(e => e.Type)
               .HasDatabaseName("IX_Products_Type");
               
        builder.HasIndex(e => e.ProductType)
               .HasDatabaseName("IX_Products_ProductType");

        builder.HasIndex(e => e.IsActive)
               .HasDatabaseName("IX_Products_IsActive");

        builder.HasIndex(e => e.Origin)
               .HasDatabaseName("IX_Products_Origin");

        builder.HasIndex(e => e.Name)
               .HasDatabaseName("IX_Products_Name");

        // Configure RowVersion for optimistic concurrency control
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasColumnType("BLOB")
               .HasDefaultValueSql("X'00000000000000000000000000000001'");

        // Table configuration
        builder.ToTable("Products");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}