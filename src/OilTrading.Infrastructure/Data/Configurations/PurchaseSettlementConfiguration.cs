using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PurchaseSettlementConfiguration : IEntityTypeConfiguration<PurchaseSettlement>
{
    public void Configure(EntityTypeBuilder<PurchaseSettlement> builder)
    {
        builder.HasKey(e => e.Id);

        // Foreign key relationship to PurchaseContract (NOT NULL - required)
        builder.HasOne(e => e.PurchaseContract)
               .WithMany(pc => pc.PurchaseSettlements)
               .HasForeignKey(e => e.PurchaseContractId)
               .OnDelete(DeleteBehavior.Restrict) // Prevent accidental deletion of contract with settlements
               .HasConstraintName("FK_PurchaseSettlements_PurchaseContracts");

        // Contract reference properties
        builder.Property(e => e.PurchaseContractId)
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

        // Concurrency control - RowVersion is automatically managed by the database
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasColumnType("BLOB")
               .HasDefaultValueSql("X'00000000000000000000000000000001'");

        // Collection of settlement charges
        // Note: SettlementCharge.Settlement references ContractSettlement base type
        // We don't configure this relationship here to avoid conflicts with the base ContractSettlement
        builder.HasMany(e => e.Charges)
               .WithOne()
               .HasForeignKey(c => c.SettlementId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_SettlementCharges_PurchaseSettlements");

        // Indexes for performance
        builder.HasIndex(e => e.PurchaseContractId)
               .HasDatabaseName("IX_PurchaseSettlements_PurchaseContractId");

        builder.HasIndex(e => e.ExternalContractNumber)
               .HasDatabaseName("IX_PurchaseSettlements_ExternalContractNumber");

        builder.HasIndex(e => e.DocumentNumber)
               .HasDatabaseName("IX_PurchaseSettlements_DocumentNumber");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_PurchaseSettlements_Status");

        builder.HasIndex(e => e.IsFinalized)
               .HasDatabaseName("IX_PurchaseSettlements_IsFinalized");

        builder.HasIndex(e => e.CreatedDate)
               .HasDatabaseName("IX_PurchaseSettlements_CreatedDate");

        builder.HasIndex(e => e.DocumentDate)
               .HasDatabaseName("IX_PurchaseSettlements_DocumentDate");

        // Composite indexes for common query patterns
        builder.HasIndex(e => new { e.PurchaseContractId, e.Status })
               .HasDatabaseName("IX_PurchaseSettlements_PurchaseContractId_Status");

        builder.HasIndex(e => new { e.Status, e.CreatedDate })
               .HasDatabaseName("IX_PurchaseSettlements_Status_CreatedDate");

        builder.HasIndex(e => new { e.IsFinalized, e.CreatedDate })
               .HasDatabaseName("IX_PurchaseSettlements_IsFinalized_CreatedDate");

        // Table configuration
        builder.ToTable("PurchaseSettlements");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}
