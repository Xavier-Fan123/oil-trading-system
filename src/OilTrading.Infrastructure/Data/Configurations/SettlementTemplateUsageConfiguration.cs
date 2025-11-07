using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SettlementTemplateUsageConfiguration : IEntityTypeConfiguration<SettlementTemplateUsage>
{
    public void Configure(EntityTypeBuilder<SettlementTemplateUsage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TemplateId)
            .IsRequired();

        builder.Property(e => e.SettlementId)
            .IsRequired();

        builder.Property(e => e.AppliedBy)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.AppliedAt)
            .IsRequired();

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => e.TemplateId)
            .HasDatabaseName("IX_SettlementTemplateUsages_TemplateId");

        builder.HasIndex(e => e.SettlementId)
            .HasDatabaseName("IX_SettlementTemplateUsages_SettlementId");

        builder.HasIndex(e => e.AppliedAt)
            .HasDatabaseName("IX_SettlementTemplateUsages_AppliedAt");

        // Foreign key constraint
        builder.HasOne(e => e.Template)
            .WithMany(t => t.Usages)
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("SettlementTemplateUsages");
    }
}
