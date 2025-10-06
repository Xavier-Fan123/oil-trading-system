using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SettlementAdjustmentConfiguration : IEntityTypeConfiguration<SettlementAdjustment>
{
    public void Configure(EntityTypeBuilder<SettlementAdjustment> builder)
    {
        builder.ToTable("SettlementAdjustments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>();

        // Configure Money value object
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 6)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired()
                .HasDefaultValue("USD");
        });

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.AdjustmentDate)
            .IsRequired();

        builder.Property(e => e.AdjustedBy)
            .IsRequired();

        builder.Property(e => e.Reference)
            .HasMaxLength(100);

        // Configure relationships
        builder.HasOne(e => e.AdjusterUser)
            .WithMany()
            .HasForeignKey(e => e.AdjustedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_SettlementAdjustments_Type");

        builder.HasIndex(e => e.AdjustmentDate)
            .HasDatabaseName("IX_SettlementAdjustments_AdjustmentDate");

        builder.HasIndex(e => e.AdjustedBy)
            .HasDatabaseName("IX_SettlementAdjustments_AdjustedBy");
    }
}