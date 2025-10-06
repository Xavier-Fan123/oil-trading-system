using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using System.Text.Json;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContractId)
            .IsRequired();

        builder.Property(e => e.SettlementNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>();

        // Configure Money value object
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 6)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired()
                .HasDefaultValue("USD");
        });

        builder.Property(e => e.DueDate)
            .IsRequired();

        builder.Property(e => e.CreatedDate)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.PayerPartyId)
            .IsRequired();

        builder.Property(e => e.PayeePartyId)
            .IsRequired();

        // Configure SettlementTerms as owned entity - ignore the complex Dictionary property
        builder.OwnsOne(e => e.Terms, terms =>
        {
            terms.Property(t => t.PaymentTerms)
                .HasColumnName("PaymentTerms")
                .HasConversion<int>();

            terms.Property(t => t.Method)
                .HasColumnName("SettlementMethod")
                .HasConversion<int>();

            terms.Property(t => t.Currency)
                .HasColumnName("TermsCurrency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            terms.Property(t => t.DiscountRate)
                .HasColumnName("DiscountRate")
                .HasPrecision(5, 4);

            terms.Property(t => t.EarlyPaymentDays)
                .HasColumnName("EarlyPaymentDays");

            terms.Property(t => t.LateFeeRate)
                .HasColumnName("LateFeeRate")
                .HasPrecision(5, 4);

            terms.Property(t => t.EnableAutomaticProcessing)
                .HasColumnName("EnableAutomaticProcessing")
                .HasDefaultValue(false);

            // Ignore the complex CustomTerms Dictionary for now
            terms.Ignore(t => t.CustomTerms);
        });

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ProcessedDate);
        builder.Property(e => e.CompletedDate);

        builder.Property(e => e.ProcessedBy)
            .HasMaxLength(200);

        builder.Property(e => e.CompletedBy)
            .HasMaxLength(200);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        // Configure relationships
        builder.HasOne(e => e.PayerParty)
            .WithMany()
            .HasForeignKey(e => e.PayerPartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PayeeParty)
            .WithMany()
            .HasForeignKey(e => e.PayeePartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Payments)
            .WithOne()
            .HasForeignKey("SettlementId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Adjustments)
            .WithOne()
            .HasForeignKey("SettlementId")
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => e.SettlementNumber)
            .IsUnique()
            .HasDatabaseName("IX_Settlements_SettlementNumber");

        builder.HasIndex(e => e.ContractId)
            .HasDatabaseName("IX_Settlements_ContractId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Settlements_Status");

        builder.HasIndex(e => e.DueDate)
            .HasDatabaseName("IX_Settlements_DueDate");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_Settlements_Type");

        builder.HasIndex(e => new { e.Status, e.DueDate })
            .HasDatabaseName("IX_Settlements_Status_DueDate");
    }
}