using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ReportExecutionConfiguration : IEntityTypeConfiguration<ReportExecution>
{
    public void Configure(EntityTypeBuilder<ReportExecution> builder)
    {
        builder.ToTable("ReportExecutions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportConfigId)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("TEXT");

        builder.Property(e => e.OutputFilePath)
            .HasMaxLength(512);

        builder.Property(e => e.OutputFileName)
            .HasMaxLength(256);

        builder.Property(e => e.OutputFileFormat)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("CSV");

        builder.Property(e => e.DurationSeconds)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(e => e.ReportConfig)
            .WithMany(rc => rc.Executions)
            .HasForeignKey(e => e.ReportConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ExecutedByUser)
            .WithMany()
            .HasForeignKey(e => e.ExecutedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.ReportConfigId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ExecutionStartTime);
        builder.HasIndex(e => e.IsScheduled);
        builder.HasIndex(e => new { e.ReportConfigId, e.Status });
        builder.HasIndex(e => e.ExecutionStartTime)
            .HasFilter("[IsDeleted] = 0")
            .HasName("IX_ReportExecutions_ExecutionStartTime_Active");
    }
}
