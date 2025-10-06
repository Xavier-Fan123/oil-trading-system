using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using System.Linq;

namespace OilTrading.Infrastructure.Data.Configurations;

public class InventoryLocationConfiguration : IEntityTypeConfiguration<InventoryLocation>
{
    public void Configure(EntityTypeBuilder<InventoryLocation> builder)
    {
        builder.ToTable("InventoryLocations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LocationCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.LocationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.LocationType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Region)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.Coordinates)
            .HasMaxLength(100);

        builder.Property(e => e.OperatorName)
            .HasMaxLength(200);

        builder.Property(e => e.ContactInfo)
            .HasMaxLength(500);

        // Configure value objects
        builder.OwnsOne(e => e.TotalCapacity, quantity =>
        {
            quantity.Property(q => q.Value)
                .HasColumnName("TotalCapacity")
                .HasPrecision(18, 2)
                .IsRequired();
            
            quantity.Property(q => q.Unit)
                .HasColumnName("TotalCapacityUnit")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.OwnsOne(e => e.AvailableCapacity, quantity =>
        {
            quantity.Property(q => q.Value)
                .HasColumnName("AvailableCapacity")
                .HasPrecision(18, 2)
                .IsRequired();
            
            quantity.Property(q => q.Unit)
                .HasColumnName("AvailableCapacityUnit")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.OwnsOne(e => e.UsedCapacity, quantity =>
        {
            quantity.Property(q => q.Value)
                .HasColumnName("UsedCapacity")
                .HasPrecision(18, 2)
                .IsRequired();
            
            quantity.Property(q => q.Unit)
                .HasColumnName("UsedCapacityUnit")
                .HasConversion<int>()
                .IsRequired();
        });

        // Configure arrays as JSON with value comparers
        builder.Property(e => e.SupportedProducts)
            .HasConversion(
                v => v == null ? null : string.Join(';', v),
                v => v == null ? null : v.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .HasMaxLength(1000)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<string[]>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new string[0] : c.ToArray()));

        builder.Property(e => e.HandlingServices)
            .HasConversion(
                v => v == null ? null : string.Join(';', v),
                v => v == null ? null : v.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .HasMaxLength(1000)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<string[]>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new string[0] : c.ToArray()));

        // Indexes
        builder.HasIndex(e => e.LocationCode)
            .IsUnique()
            .HasDatabaseName("IX_InventoryLocations_LocationCode");

        builder.HasIndex(e => e.LocationType)
            .HasDatabaseName("IX_InventoryLocations_LocationType");

        builder.HasIndex(e => e.Country)
            .HasDatabaseName("IX_InventoryLocations_Country");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_InventoryLocations_IsActive");

        // Navigation properties
        builder.HasMany(e => e.Inventories)
            .WithOne(e => e.Location)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Movements)
            .WithOne(e => e.FromLocation)
            .HasForeignKey(e => e.FromLocationId)
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