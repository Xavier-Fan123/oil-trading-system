using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ShippingOperationConfiguration : IEntityTypeConfiguration<ShippingOperation>
{
    public void Configure(EntityTypeBuilder<ShippingOperation> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Shipping Number - Unique index
        builder.HasIndex(e => e.ShippingNumber)
               .IsUnique()
               .HasDatabaseName("IX_ShippingOperations_ShippingNumber");
        
        builder.Property(e => e.ShippingNumber)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.ContractId).IsRequired();

        builder.Property(e => e.VesselName)
               .IsRequired()
               .HasMaxLength(200);

        // Configure PlannedQuantity as owned entity
        builder.OwnsOne(e => e.PlannedQuantity, quantity =>
        {
            quantity.Property(q => q.Value)
                    .HasColumnName("PlannedQuantity")
                    .HasPrecision(18, 6)
                    .IsRequired();
            
            quantity.Property(q => q.Unit)
                    .HasColumnName("PlannedQuantityUnit")
                    .IsRequired();
        });

        // Configure ActualQuantity as owned entity
        builder.OwnsOne(e => e.ActualQuantity, quantity =>
        {
            quantity.Property(q => q.Value)
                    .HasColumnName("ActualQuantity")
                    .HasPrecision(18, 6);
            
            quantity.Property(q => q.Unit)
                    .HasColumnName("ActualQuantityUnit");
        });

        // Dates
        builder.Property(e => e.LoadPortETA).IsRequired();
        builder.Property(e => e.DischargePortETA).IsRequired();
        builder.Property(e => e.LoadPortATA);
        builder.Property(e => e.DischargePortATA);
        builder.Property(e => e.BillOfLadingDate);
        builder.Property(e => e.NoticeOfReadinessDate);
        builder.Property(e => e.CertificateOfDischargeDate);

        // Ports
        builder.Property(e => e.LoadPort).HasMaxLength(100);
        builder.Property(e => e.DischargePort).HasMaxLength(100);

        // Status
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        // Additional Details
        builder.Property(e => e.ChartererName).HasMaxLength(200);
        builder.Property(e => e.IMONumber).HasMaxLength(20);
        builder.Property(e => e.VesselCapacity).HasPrecision(18, 2);
        builder.Property(e => e.ShippingAgent).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        // ═══════════════════════════════════════════════════════════════════════════
        // DATA LINEAGE ENHANCEMENT - Deal Reference ID & Split Tracking
        // Purpose: Enable full lifecycle traceability and parent-child tracking for splits
        // ═══════════════════════════════════════════════════════════════════════════

        // Deal Reference ID - Business-meaningful identifier inherited from contract
        builder.Property(e => e.DealReferenceId)
               .HasMaxLength(50)
               .IsRequired(false);

        // Parent Shipping Operation ID - Self-referencing FK for split tracking
        builder.Property(e => e.ParentShippingOperationId)
               .IsRequired(false);

        // Split Sequence - Order in split (0 = not a split or parent, 1+ = split children)
        builder.Property(e => e.SplitSequence)
               .IsRequired()
               .HasDefaultValue(0);

        // Split Reason Type - Why this shipping operation was split
        builder.Property(e => e.SplitReasonType)
               .HasConversion<int>()
               .IsRequired(false);

        // Split Reason Notes - Additional context for the split
        builder.Property(e => e.SplitReasonNotes)
               .HasMaxLength(500)
               .IsRequired(false);

        // Is Split - Quick filter flag indicating this is a split shipment
        builder.Property(e => e.IsSplit)
               .IsRequired()
               .HasDefaultValue(false);

        // Configure OriginalPlannedQuantity as owned entity (from parent before split)
        builder.OwnsOne(e => e.OriginalPlannedQuantity, quantity =>
        {
            quantity.Property(q => q.Value)
                    .HasColumnName("OriginalPlannedQuantity")
                    .HasPrecision(18, 6);

            quantity.Property(q => q.Unit)
                    .HasColumnName("OriginalPlannedQuantityUnit");
        });

        // Self-referencing relationship for split tracking - Parent Shipping Operation
        builder.HasOne(e => e.ParentShippingOperation)
               .WithMany(e => e.SplitShipments)
               .HasForeignKey(e => e.ParentShippingOperationId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Optimistic concurrency control
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasDefaultValue(new byte[] { 0 });

        // Relationships - will be configured in contract configurations
        // to avoid circular references

        // Indexes for performance
        builder.HasIndex(e => e.ContractId)
               .HasDatabaseName("IX_ShippingOperations_ContractId");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_ShippingOperations_Status");

        builder.HasIndex(e => e.VesselName)
               .HasDatabaseName("IX_ShippingOperations_VesselName");

        builder.HasIndex(e => new { e.LoadPortETA, e.DischargePortETA })
               .HasDatabaseName("IX_ShippingOperations_Schedule");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_ShippingOperations_CreatedAt");

        // Data Lineage Enhancement - Deal Reference ID Index
        builder.HasIndex(e => e.DealReferenceId)
               .HasDatabaseName("IX_ShippingOperations_DealReferenceId");

        // Data Lineage Enhancement - Split Tracking Indexes
        builder.HasIndex(e => e.ParentShippingOperationId)
               .HasDatabaseName("IX_ShippingOperations_ParentShippingOperationId");

        builder.HasIndex(e => e.IsSplit)
               .HasDatabaseName("IX_ShippingOperations_IsSplit");

        // Composite index for split queries
        builder.HasIndex(e => new { e.ParentShippingOperationId, e.SplitSequence })
               .HasDatabaseName("IX_ShippingOperations_ParentId_SplitSequence");

        // Table configuration
        builder.ToTable("ShippingOperations");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}