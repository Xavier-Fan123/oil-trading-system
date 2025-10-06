using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class FinancialReportConfiguration : IEntityTypeConfiguration<FinancialReport>
{
    public void Configure(EntityTypeBuilder<FinancialReport> builder)
    {
        builder.HasKey(e => e.Id);

        // Foreign Key to TradingPartner
        builder.Property(e => e.TradingPartnerId)
               .IsRequired();

        // Date Properties
        builder.Property(e => e.ReportStartDate)
               .IsRequired()
               .HasColumnType("date");

        builder.Property(e => e.ReportEndDate)
               .IsRequired()
               .HasColumnType("date");

        // Financial Position Properties with proper precision
        builder.Property(e => e.TotalAssets)
               .HasPrecision(18, 2);

        builder.Property(e => e.TotalLiabilities)
               .HasPrecision(18, 2);

        builder.Property(e => e.NetAssets)
               .HasPrecision(18, 2);

        builder.Property(e => e.CurrentAssets)
               .HasPrecision(18, 2);

        builder.Property(e => e.CurrentLiabilities)
               .HasPrecision(18, 2);

        // Performance Properties with proper precision
        builder.Property(e => e.Revenue)
               .HasPrecision(18, 2);

        builder.Property(e => e.NetProfit)
               .HasPrecision(18, 2);

        builder.Property(e => e.OperatingCashFlow)
               .HasPrecision(18, 2);

        // Relationship Configuration
        builder.HasOne(e => e.TradingPartner)
               .WithMany(tp => tp.FinancialReports)
               .HasForeignKey(e => e.TradingPartnerId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for Performance
        builder.HasIndex(e => e.TradingPartnerId)
               .HasDatabaseName("IX_FinancialReports_TradingPartnerId");

        builder.HasIndex(e => e.ReportStartDate)
               .HasDatabaseName("IX_FinancialReports_ReportStartDate");

        builder.HasIndex(e => new { e.TradingPartnerId, e.ReportStartDate })
               .IsUnique()
               .HasDatabaseName("IX_FinancialReports_TradingPartnerId_ReportStartDate");

        // Audit fields (inherited from BaseEntity)
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Table configuration
        builder.ToTable("FinancialReports");

        // Ignore computed properties (these are calculated, not stored)
        builder.Ignore(e => e.CurrentRatio);
        builder.Ignore(e => e.DebtToAssetRatio);
        builder.Ignore(e => e.ROE);
        builder.Ignore(e => e.ROA);
        builder.Ignore(e => e.ReportYear);
        builder.Ignore(e => e.IsAnnualReport);

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}