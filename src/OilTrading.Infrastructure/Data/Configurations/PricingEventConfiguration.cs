using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PricingEventConfiguration : IEntityTypeConfiguration<PricingEvent>
{
    public void Configure(EntityTypeBuilder<PricingEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContractId).IsRequired();

        builder.Property(e => e.EventType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.EventDate).IsRequired();

        builder.Property(e => e.BeforeDays)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.AfterDays)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.HasIndexOnEventDay)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(e => e.PricingPeriodStart).IsRequired();
        builder.Property(e => e.PricingPeriodEnd).IsRequired();

        builder.Property(e => e.TotalPricingDays)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.Property(e => e.ActualEventDate);

        builder.Property(e => e.IsEventConfirmed)
               .IsRequired()
               .HasDefaultValue(false);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Relationships - will be configured in contract configurations

        // Indexes for performance
        builder.HasIndex(e => e.ContractId)
               .HasDatabaseName("IX_PricingEvents_ContractId");

        builder.HasIndex(e => e.EventType)
               .HasDatabaseName("IX_PricingEvents_EventType");

        builder.HasIndex(e => e.EventDate)
               .HasDatabaseName("IX_PricingEvents_EventDate");

        builder.HasIndex(e => new { e.PricingPeriodStart, e.PricingPeriodEnd })
               .HasDatabaseName("IX_PricingEvents_PricingPeriod");

        builder.HasIndex(e => e.IsEventConfirmed)
               .HasDatabaseName("IX_PricingEvents_IsEventConfirmed");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_PricingEvents_CreatedAt");

        // Table configuration
        builder.ToTable("PricingEvents");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}