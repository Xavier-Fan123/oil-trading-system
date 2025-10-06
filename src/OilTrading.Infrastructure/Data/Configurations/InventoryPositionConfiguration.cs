using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class InventoryPositionConfiguration : IEntityTypeConfiguration<InventoryPosition>
{
    public void Configure(EntityTypeBuilder<InventoryPosition> builder)
    {
        builder.ToTable("InventoryPositions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LocationId)
            .IsRequired();

        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.LastUpdated)
            .IsRequired();

        builder.Property(e => e.Grade)
            .HasMaxLength(100);

        builder.Property(e => e.BatchReference)
            .HasMaxLength(100);

        builder.Property(e => e.QualityNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.StatusNotes)
            .HasMaxLength(500);

        // Quality specifications
        builder.Property(e => e.Sulfur)
            .HasPrecision(10, 4);

        builder.Property(e => e.API)
            .HasPrecision(10, 4);

        builder.Property(e => e.Viscosity)
            .HasPrecision(10, 4);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

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

        builder.OwnsOne(e => e.AverageCost, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("AverageCost")
                .HasPrecision(18, 2)
                .IsRequired();
            
            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Computed property - ignore TotalValue as it's calculated
        builder.Ignore(e => e.TotalValue);

        // Indexes
        builder.HasIndex(e => e.LocationId)
            .HasDatabaseName("IX_InventoryPositions_LocationId");

        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_InventoryPositions_ProductId");

        builder.HasIndex(e => new { e.LocationId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_InventoryPositions_Location_Product");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_InventoryPositions_Status");

        builder.HasIndex(e => e.ReceivedDate)
            .HasDatabaseName("IX_InventoryPositions_ReceivedDate");

        // Foreign key relationships
        builder.HasOne(e => e.Location)
            .WithMany(l => l.Inventories)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation properties
        builder.HasMany(e => e.IncomingMovements)
            .WithOne()
            .HasForeignKey("ToInventoryPositionId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.OutgoingMovements)
            .WithOne()
            .HasForeignKey("FromInventoryPositionId")
            .OnDelete(DeleteBehavior.Restrict);

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