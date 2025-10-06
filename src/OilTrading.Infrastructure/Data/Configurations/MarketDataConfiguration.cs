using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities.TimeSeries;

namespace OilTrading.Infrastructure.Data.Configurations;

public class MarketDataConfiguration : IEntityTypeConfiguration<MarketData>
{
    public void Configure(EntityTypeBuilder<MarketData> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Timestamp)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.Symbol)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.Exchange)
               .HasMaxLength(50);

        builder.Property(e => e.Price)
               .HasPrecision(18, 6);

        builder.Property(e => e.Volume)
               .HasPrecision(18, 6);

        builder.Property(e => e.High)
               .HasPrecision(18, 6);

        builder.Property(e => e.Low)
               .HasPrecision(18, 6);

        builder.Property(e => e.Open)
               .HasPrecision(18, 6);

        builder.Property(e => e.Close)
               .HasPrecision(18, 6);

        builder.Property(e => e.Currency)
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        builder.Property(e => e.DataSource)
               .HasMaxLength(100);

        // Indexes for TimescaleDB hypertable
        builder.HasIndex(e => new { e.Timestamp, e.Symbol })
               .HasDatabaseName("IX_MarketData_Timestamp_Symbol");

        builder.HasIndex(e => e.Symbol)
               .HasDatabaseName("IX_MarketData_Symbol");

        builder.HasIndex(e => e.Exchange)
               .HasDatabaseName("IX_MarketData_Exchange");

        // Table configuration
        builder.ToTable("MarketData");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}

public class PriceIndexConfiguration : IEntityTypeConfiguration<PriceIndex>
{
    public void Configure(EntityTypeBuilder<PriceIndex> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Timestamp)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.IndexName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.Price)
               .HasPrecision(18, 6);

        builder.Property(e => e.Currency)
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        builder.Property(e => e.Region)
               .HasMaxLength(100);

        builder.Property(e => e.Grade)
               .HasMaxLength(100);

        builder.Property(e => e.Change)
               .HasPrecision(18, 6);

        builder.Property(e => e.ChangePercent)
               .HasPrecision(8, 4);

        // Indexes for TimescaleDB hypertable
        builder.HasIndex(e => new { e.Timestamp, e.IndexName })
               .HasDatabaseName("IX_PriceIndex_Timestamp_IndexName");

        builder.HasIndex(e => e.IndexName)
               .HasDatabaseName("IX_PriceIndex_IndexName");

        builder.HasIndex(e => e.Region)
               .HasDatabaseName("IX_PriceIndex_Region");

        // Table configuration
        builder.ToTable("PriceIndices");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}

public class ContractEventConfiguration : IEntityTypeConfiguration<ContractEvent>
{
    public void Configure(EntityTypeBuilder<ContractEvent> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Timestamp)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.ContractId)
               .IsRequired();

        builder.Property(e => e.ContractNumber)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.EventType)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.EventDescription)
               .HasMaxLength(500);

        builder.Property(e => e.UserId)
               .HasMaxLength(100);

        builder.Property(e => e.UserName)
               .HasMaxLength(200);

        builder.Property(e => e.OldValue)
               .HasPrecision(18, 6);

        builder.Property(e => e.NewValue)
               .HasPrecision(18, 6);

        builder.Property(e => e.OldStatus)
               .HasMaxLength(50);

        builder.Property(e => e.NewStatus)
               .HasMaxLength(50);

        builder.Property(e => e.AdditionalData)
               .HasColumnType("jsonb");

        // Indexes for TimescaleDB hypertable
        builder.HasIndex(e => new { e.Timestamp, e.ContractId })
               .HasDatabaseName("IX_ContractEvents_Timestamp_ContractId");

        builder.HasIndex(e => e.ContractId)
               .HasDatabaseName("IX_ContractEvents_ContractId");

        builder.HasIndex(e => e.EventType)
               .HasDatabaseName("IX_ContractEvents_EventType");

        builder.HasIndex(e => e.UserId)
               .HasDatabaseName("IX_ContractEvents_UserId");

        // Table configuration
        builder.ToTable("ContractEvents");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}