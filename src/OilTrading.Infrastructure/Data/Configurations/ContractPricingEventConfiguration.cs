using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class ContractPricingEventConfiguration : IEntityTypeConfiguration<ContractPricingEvent>
{
    public void Configure(EntityTypeBuilder<ContractPricingEvent> builder)
    {
        builder.ToTable("contract_pricing_events");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
            
        builder.Property(x => x.ContractId)
            .HasColumnName("contract_id")
            .IsRequired();
            
        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .IsRequired();
            
        builder.Property(x => x.EventDate)
            .HasColumnName("event_date")
            .HasColumnType("date")
            .IsRequired();
            
        builder.Property(x => x.PricingStartDate)
            .HasColumnName("pricing_start_date")
            .HasColumnType("date")
            .IsRequired();
            
        builder.Property(x => x.PricingEndDate)
            .HasColumnName("pricing_end_date")
            .HasColumnType("date")
            .IsRequired();
            
        builder.Property(x => x.AveragePrice)
            .HasColumnName("average_price")
            .HasPrecision(18, 6);
            
        builder.Property(x => x.IsFinalized)
            .HasColumnName("is_finalized")
            .IsRequired();
            
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();
            
        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);
            
        builder.Property(x => x.PricingBenchmark)
            .HasColumnName("pricing_benchmark")
            .HasMaxLength(100);
            
        builder.Property(x => x.PricingDaysCount)
            .HasColumnName("pricing_days_count");
            
        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
            
        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);
            
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
            
        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);
            
        // Foreign key relationships
        builder.HasOne(x => x.PurchaseContract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .HasConstraintName("fk_contract_pricing_events_purchase_contract")
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.SalesContract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .HasConstraintName("fk_contract_pricing_events_sales_contract")
            .OnDelete(DeleteBehavior.Cascade);
            
        // Indexes
        builder.HasIndex(x => x.ContractId)
            .HasDatabaseName("ix_contract_pricing_events_contract_id");
            
        builder.HasIndex(x => new { x.ContractId, x.EventType, x.EventDate })
            .HasDatabaseName("ix_contract_pricing_events_contract_type_date")
            .IsUnique();
            
        builder.HasIndex(x => x.EventDate)
            .HasDatabaseName("ix_contract_pricing_events_event_date");
            
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_contract_pricing_events_status");
            
        builder.HasIndex(x => x.IsFinalized)
            .HasDatabaseName("ix_contract_pricing_events_is_finalized");
    }
}