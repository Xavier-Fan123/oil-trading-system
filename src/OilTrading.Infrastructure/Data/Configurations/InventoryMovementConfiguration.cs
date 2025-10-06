using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromLocationId)
            .IsRequired();

        builder.Property(e => e.ToLocationId)
            .IsRequired();

        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.MovementReference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.MovementType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.MovementDate)
            .IsRequired();

        builder.Property(e => e.TransportMode)
            .HasMaxLength(100);

        builder.Property(e => e.VesselName)
            .HasMaxLength(200);

        builder.Property(e => e.TransportReference)
            .HasMaxLength(100);

        builder.Property(e => e.InitiatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.ApprovedBy)
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Configure value objects
        builder.OwnsOne(e => e.Quantity, quantity =>
        {
            quantity.Property(q => q.Value)
                .HasColumnName("Quantity")
                .HasPrecision(18, 2)
                .IsRequired();
            
            quantity.Property(q => q.Unit)
                .HasColumnName("QuantityUnit")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.OwnsOne(e => e.TransportCost, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TransportCost")
                .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                .HasColumnName("TransportCostCurrency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(e => e.HandlingCost, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("HandlingCost")
                .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                .HasColumnName("HandlingCostCurrency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(e => e.TotalCost, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalCost")
                .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                .HasColumnName("TotalCostCurrency")
                .HasMaxLength(3);
        });

        // Indexes
        builder.HasIndex(e => e.MovementReference)
            .IsUnique()
            .HasDatabaseName("IX_InventoryMovements_MovementReference");

        builder.HasIndex(e => e.FromLocationId)
            .HasDatabaseName("IX_InventoryMovements_FromLocationId");

        builder.HasIndex(e => e.ToLocationId)
            .HasDatabaseName("IX_InventoryMovements_ToLocationId");

        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_InventoryMovements_ProductId");

        builder.HasIndex(e => e.MovementType)
            .HasDatabaseName("IX_InventoryMovements_MovementType");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_InventoryMovements_Status");

        builder.HasIndex(e => e.MovementDate)
            .HasDatabaseName("IX_InventoryMovements_MovementDate");

        builder.HasIndex(e => e.PlannedDate)
            .HasDatabaseName("IX_InventoryMovements_PlannedDate");

        builder.HasIndex(e => e.PurchaseContractId)
            .HasDatabaseName("IX_InventoryMovements_PurchaseContractId");

        builder.HasIndex(e => e.SalesContractId)
            .HasDatabaseName("IX_InventoryMovements_SalesContractId");

        builder.HasIndex(e => e.ShippingOperationId)
            .HasDatabaseName("IX_InventoryMovements_ShippingOperationId");

        // Foreign key relationships
        builder.HasOne(e => e.FromLocation)
            .WithMany(l => l.Movements)
            .HasForeignKey(e => e.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToLocation)
            .WithMany()
            .HasForeignKey(e => e.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PurchaseContract)
            .WithMany()
            .HasForeignKey(e => e.PurchaseContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.SalesContract)
            .WithMany()
            .HasForeignKey(e => e.SalesContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ShippingOperation)
            .WithMany()
            .HasForeignKey(e => e.ShippingOperationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Base entity configuration
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.DeletedAt);

        builder.Property(e => e.DeletedBy)
            .HasMaxLength(100);

        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}