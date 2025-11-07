using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SettlementTemplateConfiguration : IEntityTypeConfiguration<SettlementTemplate>
{
    public void Configure(EntityTypeBuilder<SettlementTemplate> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.TemplateConfiguration)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(e => e.TimesUsed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LastUsedAt)
            .IsRequired(false);

        // Concurrency control
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(255);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_SettlementTemplates_Name");

        builder.HasIndex(e => e.CreatedByUserId)
            .HasDatabaseName("IX_SettlementTemplates_CreatedByUserId");

        builder.HasIndex(e => e.IsPublic)
            .HasDatabaseName("IX_SettlementTemplates_IsPublic");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_SettlementTemplates_IsActive");

        // Navigation relationships
        builder.HasMany(e => e.Usages)
            .WithOne(u => u.Template)
            .HasForeignKey(u => u.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Permissions)
            .WithOne(p => p.Template)
            .HasForeignKey(p => p.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SettlementTemplates");
    }
}
