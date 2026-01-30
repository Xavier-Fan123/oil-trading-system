using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

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

        // ═══════════════════════════════════════════════════════════════════════════
        // DATA LINEAGE ENHANCEMENT - Deal Reference ID & Amendment Chain
        // Purpose: Enable full lifecycle traceability and audit trail for settlements
        // ═══════════════════════════════════════════════════════════════════════════

        // Deal Reference ID - Business-meaningful identifier inherited from contract
        builder.Property(e => e.DealReferenceId)
               .HasMaxLength(50)
               .IsRequired(false);

        // Previous Settlement ID - Self-referencing FK for amendment chain
        builder.Property(e => e.PreviousSettlementId)
               .IsRequired(false);

        // Original Settlement ID - Self-referencing FK to root of amendment chain
        builder.Property(e => e.OriginalSettlementId)
               .IsRequired(false);

        // Settlement Sequence - Version number in the amendment chain
        builder.Property(e => e.SettlementSequence)
               .IsRequired()
               .HasDefaultValue(1);

        // Amendment Type - Why this settlement was created
        builder.Property(e => e.AmendmentType)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(SettlementAmendmentType.Initial);

        // Amendment Reason - Business justification for non-initial settlements
        builder.Property(e => e.AmendmentReason)
               .HasMaxLength(500)
               .IsRequired(false);

        // Is Latest Version - Quick filter flag for current active version
        builder.Property(e => e.IsLatestVersion)
               .IsRequired()
               .HasDefaultValue(true);

        // Superseded Date - When this settlement was replaced by a newer version
        builder.Property(e => e.SupersededDate)
               .IsRequired(false);

        // Self-referencing relationship for amendment chain - Previous Settlement
        builder.HasOne(e => e.PreviousSettlement)
               .WithMany()
               .HasForeignKey(e => e.PreviousSettlementId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        // Self-referencing relationship for amendment chain - Original Settlement
        builder.HasOne(e => e.OriginalSettlement)
               .WithMany()
               .HasForeignKey(e => e.OriginalSettlementId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

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

        // Navigation properties
        // Note: ContractSettlement doesn't have explicit foreign keys to PurchaseContract or SalesContract
        // because a settlement can reference either type and EF Core cannot have two HasOne relationships
        // pointing to the same foreign key column. The ContractId is a Guid that references one of them.
        // Navigation properties are populated manually in the service layer based on contract type.

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

        // Data Lineage Enhancement - Deal Reference ID Index
        builder.HasIndex(e => e.DealReferenceId)
               .HasDatabaseName("IX_ContractSettlements_DealReferenceId");

        // Data Lineage Enhancement - Amendment Chain Indexes
        builder.HasIndex(e => e.OriginalSettlementId)
               .HasDatabaseName("IX_ContractSettlements_OriginalSettlementId");

        builder.HasIndex(e => e.PreviousSettlementId)
               .HasDatabaseName("IX_ContractSettlements_PreviousSettlementId");

        builder.HasIndex(e => e.IsLatestVersion)
               .HasDatabaseName("IX_ContractSettlements_IsLatestVersion");

        // Composite index for amendment chain queries
        builder.HasIndex(e => new { e.OriginalSettlementId, e.SettlementSequence })
               .HasDatabaseName("IX_ContractSettlements_OriginalSettlementId_Sequence");

        // Table configuration
        builder.ToTable("ContractSettlements");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);

        // Ignore unmapped properties that don't have database columns yet
        // These are business logic properties that will be added in future migrations
        builder.Ignore(e => e.ActualPayableDueDate);
        builder.Ignore(e => e.ActualPaymentDate);
    }
}