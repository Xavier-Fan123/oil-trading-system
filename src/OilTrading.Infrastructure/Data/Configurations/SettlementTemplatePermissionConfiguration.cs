using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SettlementTemplatePermissionConfiguration : IEntityTypeConfiguration<SettlementTemplatePermission>
{
    public void Configure(EntityTypeBuilder<SettlementTemplatePermission> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TemplateId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.PermissionLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.GrantedAt)
            .IsRequired();

        builder.Property(e => e.GrantedBy)
            .IsRequired()
            .HasMaxLength(255);

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(255);

        // Unique constraint: one permission per template per user
        builder.HasIndex(e => new { e.TemplateId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_SettlementTemplatePermissions_TemplateId_UserId");

        // Additional indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SettlementTemplatePermissions_UserId");

        builder.HasIndex(e => e.PermissionLevel)
            .HasDatabaseName("IX_SettlementTemplatePermissions_PermissionLevel");

        // Foreign key constraint
        builder.HasOne(e => e.Template)
            .WithMany(t => t.Permissions)
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SettlementTemplatePermissions");
    }
}
