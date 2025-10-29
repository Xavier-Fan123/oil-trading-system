using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class TradingPartnerConfiguration : IEntityTypeConfiguration<TradingPartner>
{
    public void Configure(EntityTypeBuilder<TradingPartner> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Code - Unique index
        builder.HasIndex(e => e.Code)
               .IsUnique()
               .HasDatabaseName("IX_TradingPartners_Code");
        
        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Code)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.Type)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.ContactEmail)
               .HasMaxLength(255);

        builder.Property(e => e.ContactPhone)
               .HasMaxLength(50);

        builder.Property(e => e.Address)
               .HasMaxLength(500);

        builder.Property(e => e.Country)
               .HasMaxLength(100);

        builder.Property(e => e.TaxId)
               .HasMaxLength(100);

        builder.Property(e => e.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(e => e.CreditLimit)
               .HasPrecision(18, 2)
               .HasDefaultValue(0);

        builder.Property(e => e.CreditRating)
               .HasMaxLength(10);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Indexes for performance
        builder.HasIndex(e => e.Type)
               .HasDatabaseName("IX_TradingPartners_Type");

        builder.HasIndex(e => e.IsActive)
               .HasDatabaseName("IX_TradingPartners_IsActive");

        builder.HasIndex(e => e.Country)
               .HasDatabaseName("IX_TradingPartners_Country");

        builder.HasIndex(e => e.Name)
               .HasDatabaseName("IX_TradingPartners_Name");

        // Configure RowVersion for optimistic concurrency control
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasColumnType("BLOB")
               .HasDefaultValueSql("X'00000000000000000000000000000001'");

        // Table configuration
        builder.ToTable("TradingPartners");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}