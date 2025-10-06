using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SettlementChargeConfiguration : IEntityTypeConfiguration<SettlementCharge>
{
    public void Configure(EntityTypeBuilder<SettlementCharge> builder)
    {
        builder.HasKey(e => e.Id);

        // Foreign key to ContractSettlement
        builder.Property(e => e.SettlementId)
               .IsRequired();

        // Charge properties
        builder.Property(e => e.ChargeType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Description)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(e => e.Amount)
               .HasPrecision(18, 2)
               .IsRequired();

        builder.Property(e => e.Currency)
               .HasMaxLength(3)
               .IsRequired()
               .HasDefaultValue("USD");

        builder.Property(e => e.IncurredDate);

        builder.Property(e => e.ReferenceDocument)
               .HasMaxLength(200);

        builder.Property(e => e.Notes)
               .HasMaxLength(1000);

        // Audit fields
        builder.Property(e => e.CreatedDate)
               .IsRequired();

        builder.Property(e => e.CreatedBy)
               .HasMaxLength(100)
               .IsRequired();

        // Navigation property
        builder.HasOne(e => e.Settlement)
               .WithMany(s => s.Charges)
               .HasForeignKey(e => e.SettlementId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_SettlementCharges_ContractSettlements");

        // Indexes for performance
        builder.HasIndex(e => e.SettlementId)
               .HasDatabaseName("IX_SettlementCharges_SettlementId");

        builder.HasIndex(e => e.ChargeType)
               .HasDatabaseName("IX_SettlementCharges_ChargeType");

        builder.HasIndex(e => e.CreatedDate)
               .HasDatabaseName("IX_SettlementCharges_CreatedDate");

        builder.HasIndex(e => e.IncurredDate)
               .HasDatabaseName("IX_SettlementCharges_IncurredDate");

        // Composite indexes for common query patterns
        builder.HasIndex(e => new { e.SettlementId, e.ChargeType })
               .HasDatabaseName("IX_SettlementCharges_SettlementId_ChargeType");

        builder.HasIndex(e => new { e.ChargeType, e.CreatedDate })
               .HasDatabaseName("IX_SettlementCharges_ChargeType_CreatedDate");

        // Table configuration
        builder.ToTable("SettlementCharges");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}