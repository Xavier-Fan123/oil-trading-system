using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Email - Unique index
        builder.HasIndex(e => e.Email)
               .IsUnique()
               .HasDatabaseName("IX_Users_Email");
        
        builder.Property(e => e.Email)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(e => e.FirstName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.LastName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.PasswordHash)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.Role)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(e => e.LastLoginAt);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Computed property
        builder.Ignore(e => e.FullName);

        // Indexes for performance
        builder.HasIndex(e => e.Role)
               .HasDatabaseName("IX_Users_Role");

        builder.HasIndex(e => e.IsActive)
               .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(e => new { e.FirstName, e.LastName })
               .HasDatabaseName("IX_Users_Name");

        // Configure RowVersion for optimistic concurrency control
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasColumnType("BLOB")
               .HasDefaultValueSql("X'00000000000000000000000000000001'");

        // Table configuration
        builder.ToTable("Users");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}