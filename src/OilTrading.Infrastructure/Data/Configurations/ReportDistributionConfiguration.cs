using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ReportDistributionConfiguration : IEntityTypeConfiguration<ReportDistribution>
{
    public void Configure(EntityTypeBuilder<ReportDistribution> builder)
    {
        builder.ToTable("ReportDistributions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportConfigId)
            .IsRequired();

        builder.Property(e => e.ChannelName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ChannelType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ChannelConfiguration)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        builder.Property(e => e.LastTestStatus)
            .HasMaxLength(50);

        builder.Property(e => e.LastTestMessage)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(e => e.ReportConfig)
            .WithMany(rc => rc.Distributions)
            .HasForeignKey(e => e.ReportConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.ReportConfigId);
        builder.HasIndex(e => e.ChannelType);
        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => new { e.ReportConfigId, e.ChannelType });
    }
}
