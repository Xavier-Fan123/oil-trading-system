using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// 交易组标签实体配置 - Trade Group Tag Entity Configuration
/// </summary>
public class TradeGroupTagConfiguration : IEntityTypeConfiguration<TradeGroupTag>
{
    public void Configure(EntityTypeBuilder<TradeGroupTag> builder)
    {
        builder.ToTable("TradeGroupTags");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.TradeGroupId)
            .IsRequired();

        builder.Property(e => e.TagId)
            .IsRequired();

        builder.Property(e => e.AssignedAt)
            .IsRequired();

        builder.Property(e => e.AssignedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Audit fields (inherited from BaseEntity)
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => new { e.TradeGroupId, e.TagId })
            .IsUnique()
            .HasDatabaseName("IX_TradeGroupTags_TradeGroupId_TagId");

        builder.HasIndex(e => e.TradeGroupId)
            .HasDatabaseName("IX_TradeGroupTags_TradeGroupId");

        builder.HasIndex(e => e.TagId)
            .HasDatabaseName("IX_TradeGroupTags_TagId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_TradeGroupTags_IsActive");

        builder.HasIndex(e => e.AssignedAt)
            .HasDatabaseName("IX_TradeGroupTags_AssignedAt");

        // Foreign key relationships
        builder.HasOne(e => e.TradeGroup)
            .WithMany(e => e.TradeGroupTags)
            .HasForeignKey(e => e.TradeGroupId)
            .OnDelete(DeleteBehavior.Cascade); // When trade group is deleted, remove all tag associations

        builder.HasOne(e => e.Tag)
            .WithMany() // Tag doesn't need to know about TradeGroupTags
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade); // When tag is deleted, remove all associations
    }
}