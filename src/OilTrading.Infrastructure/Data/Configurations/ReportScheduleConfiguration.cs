using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("ReportSchedules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportConfigId)
            .IsRequired();

        builder.Property(e => e.Frequency)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Time)
            .HasMaxLength(10);

        builder.Property(e => e.Timezone)
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(e => e.ReportConfig)
            .WithMany(rc => rc.Schedules)
            .HasForeignKey(e => e.ReportConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.ReportConfigId);
        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => new { e.Frequency, e.IsEnabled });
        builder.HasIndex(e => e.NextRunDate)
            .HasFilter("[IsEnabled] = 1 AND [IsDeleted] = 0");
    }
}
