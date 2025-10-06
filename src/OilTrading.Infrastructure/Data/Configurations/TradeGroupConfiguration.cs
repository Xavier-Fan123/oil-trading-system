using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// 交易组实体配置 - Trade Group Entity Configuration
/// </summary>
public class TradeGroupConfiguration : IEntityTypeConfiguration<TradeGroup>
{
    public void Configure(EntityTypeBuilder<TradeGroup> builder)
    {
        builder.ToTable("TradeGroups");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.GroupName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.StrategyType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ExpectedRiskLevel)
            .HasConversion<int>();

        builder.Property(e => e.MaxAllowedLoss)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TargetProfit)
            .HasColumnType("decimal(18,2)");

        // Audit fields (inherited from BaseEntity)
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => e.GroupName)
            .IsUnique()
            .HasDatabaseName("IX_TradeGroups_GroupName");

        builder.HasIndex(e => e.StrategyType)
            .HasDatabaseName("IX_TradeGroups_StrategyType");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_TradeGroups_Status");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_TradeGroups_CreatedAt");

        // Navigation properties - These are already configured in the other entity configurations
        // TradeGroup is the principal entity, and the contracts reference it via TradeGroupId foreign keys
        
        builder.HasMany(e => e.PaperContracts)
            .WithOne(e => e.TradeGroup)
            .HasForeignKey(e => e.TradeGroupId)
            .OnDelete(DeleteBehavior.SetNull); // Allow removing contracts from groups

        builder.HasMany(e => e.PurchaseContracts)
            .WithOne(e => e.TradeGroup)
            .HasForeignKey(e => e.TradeGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.SalesContracts)
            .WithOne(e => e.TradeGroup)
            .HasForeignKey(e => e.TradeGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}