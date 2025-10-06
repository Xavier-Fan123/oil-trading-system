using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// 标签实体配置 - Tag Entity Configuration
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Name");

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .HasColumnName("Description");

        builder.Property(t => t.Color)
            .IsRequired()
            .HasMaxLength(7) // #RRGGBB
            .HasColumnName("Color")
            .HasDefaultValue("#6B7280");

        builder.Property(t => t.Category)
            .IsRequired()
            .HasConversion<int>()
            .HasColumnName("Category");

        builder.Property(t => t.Priority)
            .HasColumnName("Priority")
            .HasDefaultValue(0);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        builder.Property(t => t.UsageCount)
            .IsRequired()
            .HasColumnName("UsageCount")
            .HasDefaultValue(0);

        builder.Property(t => t.LastUsedAt)
            .HasColumnName("LastUsedAt");

        builder.Property(t => t.MutuallyExclusiveTags)
            .HasMaxLength(1000)
            .HasColumnName("MutuallyExclusiveTags");

        builder.Property(t => t.MaxUsagePerEntity)
            .HasColumnName("MaxUsagePerEntity");

        builder.Property(t => t.AllowedContractStatuses)
            .HasMaxLength(500)
            .HasColumnName("AllowedContractStatuses");

        // Audit Fields
        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt");

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100)
            .HasColumnName("CreatedBy");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(100)
            .HasColumnName("UpdatedBy");

        // Indexes
        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("IX_Tags_Name");

        builder.HasIndex(t => t.Category)
            .HasDatabaseName("IX_Tags_Category");

        builder.HasIndex(t => new { t.Category, t.IsActive })
            .HasDatabaseName("IX_Tags_Category_IsActive");

        builder.HasIndex(t => t.UsageCount)
            .HasDatabaseName("IX_Tags_UsageCount");

        builder.HasIndex(t => t.LastUsedAt)
            .HasDatabaseName("IX_Tags_LastUsedAt");

        // Navigation Properties
        builder.HasMany(t => t.ContractTags)
            .WithOne(ct => ct.Tag)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// 合同标签关联实体配置 - Contract Tag Entity Configuration
/// </summary>
public class ContractTagConfiguration : IEntityTypeConfiguration<ContractTag>
{
    public void Configure(EntityTypeBuilder<ContractTag> builder)
    {
        builder.ToTable("ContractTags");

        // Primary Key
        builder.HasKey(ct => ct.Id);

        // Properties
        builder.Property(ct => ct.ContractId)
            .IsRequired()
            .HasColumnName("ContractId");

        builder.Property(ct => ct.ContractType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("ContractType");

        builder.Property(ct => ct.TagId)
            .IsRequired()
            .HasColumnName("TagId");

        builder.Property(ct => ct.Notes)
            .HasMaxLength(1000)
            .HasColumnName("Notes");

        builder.Property(ct => ct.AssignedBy)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("AssignedBy");

        builder.Property(ct => ct.AssignedAt)
            .IsRequired()
            .HasColumnName("AssignedAt");

        // Audit Fields
        builder.Property(ct => ct.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt");

        builder.Property(ct => ct.CreatedBy)
            .HasMaxLength(100)
            .HasColumnName("CreatedBy");

        builder.Property(ct => ct.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.Property(ct => ct.UpdatedBy)
            .HasMaxLength(100)
            .HasColumnName("UpdatedBy");

        // Indexes
        builder.HasIndex(ct => new { ct.ContractId, ct.ContractType })
            .HasDatabaseName("IX_ContractTags_Contract");

        builder.HasIndex(ct => ct.TagId)
            .HasDatabaseName("IX_ContractTags_TagId");

        builder.HasIndex(ct => new { ct.ContractId, ct.ContractType, ct.TagId })
            .IsUnique()
            .HasDatabaseName("IX_ContractTags_Contract_Tag_Unique");

        builder.HasIndex(ct => ct.AssignedAt)
            .HasDatabaseName("IX_ContractTags_AssignedAt");

        // Foreign Keys
        builder.HasOne(ct => ct.Tag)
            .WithMany(t => t.ContractTags)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: We don't create explicit foreign keys to contract tables 
        // because ContractTag can reference different types of contracts
        // This is managed at the application level
    }
}