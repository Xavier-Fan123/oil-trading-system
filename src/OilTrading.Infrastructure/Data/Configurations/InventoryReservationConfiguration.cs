using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ContractId)
            .IsRequired();
        
        builder.Property(e => e.ContractType)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(e => e.ProductCode)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.LocationCode)
            .IsRequired()
            .HasMaxLength(50);
        
        // Configure Quantity value object
        builder.OwnsOne(e => e.Quantity, quantity =>
        {
            quantity.Property(q => q.Value)
                    .HasColumnName("Quantity")
                    .HasPrecision(18, 6)
                    .IsRequired();
            
            quantity.Property(q => q.Unit)
                    .HasColumnName("QuantityUnit")
                    .IsRequired();
        });
        
        // Configure ReleasedQuantity value object
        builder.OwnsOne(e => e.ReleasedQuantity, quantity =>
        {
            quantity.Property(q => q.Value)
                    .HasColumnName("ReleasedQuantity")
                    .HasPrecision(18, 6)
                    .IsRequired();
            
            quantity.Property(q => q.Unit)
                    .HasColumnName("ReleasedQuantityUnit")
                    .IsRequired();
        });
        
        builder.Property(e => e.ReservationDate)
            .IsRequired();
        
        builder.Property(e => e.ExpiryDate);
        
        builder.Property(e => e.ReleasedDate);
        
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(e => e.Notes)
            .HasMaxLength(1000);
        
        builder.Property(e => e.ReservedBy)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.ReleasedBy)
            .HasMaxLength(100);
        
        builder.Property(e => e.ReleaseReason)
            .HasMaxLength(500);
        
        // Relationships - Optional navigation properties
        builder.HasOne(e => e.PurchaseContract)
            .WithMany()
            .HasForeignKey(e => e.ContractId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
        
        builder.HasOne(e => e.SalesContract)
            .WithMany()
            .HasForeignKey(e => e.ContractId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(e => e.ContractId)
            .HasDatabaseName("IX_InventoryReservations_ContractId");
        
        builder.HasIndex(e => e.ProductCode)
            .HasDatabaseName("IX_InventoryReservations_ProductCode");
        
        builder.HasIndex(e => e.LocationCode)
            .HasDatabaseName("IX_InventoryReservations_LocationCode");
        
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_InventoryReservations_Status");
        
        builder.HasIndex(e => e.ReservationDate)
            .HasDatabaseName("IX_InventoryReservations_ReservationDate");
        
        builder.HasIndex(e => new { e.ProductCode, e.LocationCode, e.Status })
            .HasDatabaseName("IX_InventoryReservations_Product_Location_Status");
    }
}