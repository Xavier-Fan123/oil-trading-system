using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ReportArchive entity
/// </summary>
public class ReportArchiveConfiguration : IEntityTypeConfiguration<ReportArchive>
{
    public void Configure(EntityTypeBuilder<ReportArchive> builder)
    {
        builder.ToTable("ReportArchives");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StorageLocation)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.RetentionDays)
            .IsRequired()
            .HasDefaultValue(90);

        builder.Property(e => e.FileSize)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.IsCompressed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ArchiveDate)
            .IsRequired();

        builder.Property(e => e.ExpiryDate)
            .IsRequired();

        builder.Property(e => e.CreatedDate)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(e => e.ReportExecution)
            .WithMany()
            .HasForeignKey(e => e.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => e.ExecutionId);
        builder.HasIndex(e => e.ArchiveDate);
        builder.HasIndex(e => e.ExpiryDate);

        // Filtered index to find expired archives
        builder.HasIndex(e => new { e.ExpiryDate, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0");
    }
}
