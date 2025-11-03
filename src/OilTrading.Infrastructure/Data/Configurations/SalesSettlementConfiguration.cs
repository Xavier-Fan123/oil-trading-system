using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SalesSettlementConfiguration : IEntityTypeConfiguration<SalesSettlement>
{
    public void Configure(EntityTypeBuilder<SalesSettlement> builder)
    {
        builder.HasKey(e => e.Id);

        // Foreign key relationship to SalesContract (NOT NULL - required)
        builder.HasOne(e => e.SalesContract)
               .WithMany(sc => sc.SalesSettlements)
               .HasForeignKey(e => e.SalesContractId)
               .OnDelete(DeleteBehavior.Restrict) // Prevent accidental deletion of contract with settlements
               .HasConstraintName("FK_SalesSettlements_SalesContracts");

        // Contract reference properties
        builder.Property(e => e.SalesContractId)
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
               .HasConstraintName("FK_SettlementCharges_SalesSettlements");

        // Indexes for performance
        builder.HasIndex(e => e.SalesContractId)
               .HasDatabaseName("IX_SalesSettlements_SalesContractId");

        builder.HasIndex(e => e.ExternalContractNumber)
               .HasDatabaseName("IX_SalesSettlements_ExternalContractNumber");

        builder.HasIndex(e => e.DocumentNumber)
               .HasDatabaseName("IX_SalesSettlements_DocumentNumber");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_SalesSettlements_Status");

        builder.HasIndex(e => e.IsFinalized)
               .HasDatabaseName("IX_SalesSettlements_IsFinalized");

        builder.HasIndex(e => e.CreatedDate)
               .HasDatabaseName("IX_SalesSettlements_CreatedDate");

        builder.HasIndex(e => e.DocumentDate)
               .HasDatabaseName("IX_SalesSettlements_DocumentDate");

        // Composite indexes for common query patterns
        builder.HasIndex(e => new { e.SalesContractId, e.Status })
               .HasDatabaseName("IX_SalesSettlements_SalesContractId_Status");

        builder.HasIndex(e => new { e.Status, e.CreatedDate })
               .HasDatabaseName("IX_SalesSettlements_Status_CreatedDate");

        builder.HasIndex(e => new { e.IsFinalized, e.CreatedDate })
               .HasDatabaseName("IX_SalesSettlements_IsFinalized_CreatedDate");

        // Table configuration
        builder.ToTable("SalesSettlements");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}
