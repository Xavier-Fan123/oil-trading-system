using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ContractSettlementConfiguration : IEntityTypeConfiguration<ContractSettlement>
{
    public void Configure(EntityTypeBuilder<ContractSettlement> builder)
    {
        builder.HasKey(e => e.Id);

        // Contract reference properties
        builder.Property(e => e.ContractId)
               .IsRequired();

        builder.Property(e => e.ContractNumber)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.ExternalContractNumber)
               .HasMaxLength(100)
               .IsRequired();

        // Document information
        builder.Property(e => e.DocumentNumber)
               .HasMaxLength(100);

        builder.Property(e => e.DocumentType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.DocumentDate)
               .IsRequired();

        // Actual quantities
        builder.Property(e => e.ActualQuantityMT)
               .HasPrecision(18, 6);

        builder.Property(e => e.ActualQuantityBBL)
               .HasPrecision(18, 6);

        // Calculation quantities
        builder.Property(e => e.CalculationQuantityMT)
               .HasPrecision(18, 6);

        builder.Property(e => e.CalculationQuantityBBL)
               .HasPrecision(18, 6);

        builder.Property(e => e.QuantityCalculationNote)
               .HasMaxLength(500);

        // Price information
        builder.Property(e => e.BenchmarkPrice)
               .HasPrecision(18, 4);

        builder.Property(e => e.BenchmarkPriceFormula)
               .HasMaxLength(500);

        builder.Property(e => e.PricingStartDate);

        builder.Property(e => e.PricingEndDate);

        builder.Property(e => e.BenchmarkPriceCurrency)
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        // Calculation results
        builder.Property(e => e.BenchmarkAmount)
               .HasPrecision(18, 2);

        builder.Property(e => e.AdjustmentAmount)
               .HasPrecision(18, 2);

        builder.Property(e => e.CargoValue)
               .HasPrecision(18, 2);

        builder.Property(e => e.TotalCharges)
               .HasPrecision(18, 2);

        builder.Property(e => e.TotalSettlementAmount)
               .HasPrecision(18, 2);

        builder.Property(e => e.SettlementCurrency)
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        // Exchange rate handling
        builder.Property(e => e.ExchangeRate)
               .HasPrecision(10, 6);

        builder.Property(e => e.ExchangeRateNote)
               .HasMaxLength(200);

        // Status management
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.IsFinalized)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(e => e.CreatedDate)
               .IsRequired();

        builder.Property(e => e.LastModifiedDate);

        builder.Property(e => e.CreatedBy)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.LastModifiedBy)
               .HasMaxLength(100);

        builder.Property(e => e.FinalizedDate);

        builder.Property(e => e.FinalizedBy)
               .HasMaxLength(100);

        // Navigation properties
        builder.HasOne(e => e.PurchaseContract)
               .WithMany()
               .HasForeignKey(e => e.ContractId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_ContractSettlements_PurchaseContracts");

        builder.HasOne(e => e.SalesContract)
               .WithMany()
               .HasForeignKey(e => e.ContractId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_ContractSettlements_SalesContracts");

        // Collection of settlement charges
        builder.HasMany(e => e.Charges)
               .WithOne(c => c.Settlement)
               .HasForeignKey(c => c.SettlementId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_SettlementCharges_ContractSettlements");

        // Indexes for performance
        builder.HasIndex(e => e.ContractId)
               .HasDatabaseName("IX_ContractSettlements_ContractId");

        builder.HasIndex(e => e.ExternalContractNumber)
               .HasDatabaseName("IX_ContractSettlements_ExternalContractNumber");

        builder.HasIndex(e => e.DocumentNumber)
               .HasDatabaseName("IX_ContractSettlements_DocumentNumber");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_ContractSettlements_Status");

        builder.HasIndex(e => e.IsFinalized)
               .HasDatabaseName("IX_ContractSettlements_IsFinalized");

        builder.HasIndex(e => e.CreatedDate)
               .HasDatabaseName("IX_ContractSettlements_CreatedDate");

        builder.HasIndex(e => e.DocumentDate)
               .HasDatabaseName("IX_ContractSettlements_DocumentDate");

        // Composite indexes for common query patterns
        builder.HasIndex(e => new { e.ContractId, e.Status })
               .HasDatabaseName("IX_ContractSettlements_ContractId_Status");

        builder.HasIndex(e => new { e.Status, e.CreatedDate })
               .HasDatabaseName("IX_ContractSettlements_Status_CreatedDate");

        builder.HasIndex(e => new { e.IsFinalized, e.CreatedDate })
               .HasDatabaseName("IX_ContractSettlements_IsFinalized_CreatedDate");

        // Table configuration
        builder.ToTable("ContractSettlements");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}