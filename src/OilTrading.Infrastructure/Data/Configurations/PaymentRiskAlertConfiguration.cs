using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PaymentRiskAlertConfiguration : IEntityTypeConfiguration<PaymentRiskAlert>
{
    public void Configure(EntityTypeBuilder<PaymentRiskAlert> builder)
    {
        builder.HasKey(e => e.Id);

        // Foreign key relationship to TradingPartner
        builder.HasOne(e => e.TradingPartner)
            .WithMany(tp => tp.PaymentRiskAlerts)
            .HasForeignKey(e => e.TradingPartnerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(e => e.TradingPartnerId)
            .HasDatabaseName("IX_PaymentRiskAlerts_TradingPartnerId");

        builder.HasIndex(e => new { e.TradingPartnerId, e.IsResolved })
            .HasDatabaseName("IX_PaymentRiskAlerts_TradingPartnerId_IsResolved");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_PaymentRiskAlerts_Severity");

        builder.HasIndex(e => e.AlertType)
            .HasDatabaseName("IX_PaymentRiskAlerts_AlertType");

        builder.HasIndex(e => e.CreatedDate)
            .HasDatabaseName("IX_PaymentRiskAlerts_CreatedDate");

        // Property configurations
        builder.Property(e => e.AlertType)
            .IsRequired();

        builder.Property(e => e.Severity)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.IsResolved)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.DaysOverdue);

        builder.Property(e => e.DaysUntilDue);
    }
}
