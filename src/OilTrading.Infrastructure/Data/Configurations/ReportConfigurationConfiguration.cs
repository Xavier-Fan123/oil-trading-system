using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ReportConfigurationConfiguration : IEntityTypeConfiguration<ReportConfiguration>
{
    public void Configure(EntityTypeBuilder<ReportConfiguration> builder)
    {
        builder.ToTable("ReportConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.ReportType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.FilterJson)
            .HasColumnType("TEXT");

        builder.Property(e => e.ColumnsJson)
            .HasColumnType("TEXT");

        builder.Property(e => e.ExportFormat)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("CSV");

        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Relationships
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation properties
        builder.HasMany(e => e.Schedules)
            .WithOne(s => s.ReportConfig)
            .HasForeignKey(s => s.ReportConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Distributions)
            .WithOne(d => d.ReportConfig)
            .HasForeignKey(d => d.ReportConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.ReportConfig)
            .HasForeignKey(ex => ex.ReportConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.ReportType);
        builder.HasIndex(e => e.CreatedBy);
        builder.HasIndex(e => e.CreatedDate);
        builder.HasIndex(e => new { e.IsActive, e.IsDeleted });
    }
}
