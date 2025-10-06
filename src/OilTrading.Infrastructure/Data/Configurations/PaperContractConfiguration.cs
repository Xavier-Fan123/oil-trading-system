using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PaperContractConfiguration : IEntityTypeConfiguration<PaperContract>
{
    public void Configure(EntityTypeBuilder<PaperContract> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ContractMonth)
               .IsRequired()
               .HasMaxLength(20);
               
        builder.Property(e => e.ProductType)
               .IsRequired()
               .HasMaxLength(50);
               
        builder.Property(e => e.Position)
               .IsRequired()
               .HasConversion<int>();
               
        builder.Property(e => e.Quantity)
               .HasPrecision(18, 4)
               .IsRequired();
               
        builder.Property(e => e.LotSize)
               .HasPrecision(18, 4)
               .IsRequired();
               
        builder.Property(e => e.EntryPrice)
               .HasPrecision(18, 4)
               .IsRequired();
               
        builder.Property(e => e.CurrentPrice)
               .HasPrecision(18, 4);
               
        builder.Property(e => e.TradeDate)
               .IsRequired();
               
        builder.Property(e => e.SettlementDate);
        
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();
               
        builder.Property(e => e.RealizedPnL)
               .HasPrecision(18, 2);
               
        builder.Property(e => e.UnrealizedPnL)
               .HasPrecision(18, 2);
               
        builder.Property(e => e.DailyPnL)
               .HasPrecision(18, 2);
               
        builder.Property(e => e.LastMTMDate);
        
        builder.Property(e => e.IsSpread)
               .IsRequired()
               .HasDefaultValue(false);
               
        builder.Property(e => e.Leg1Product)
               .HasMaxLength(100);
               
        builder.Property(e => e.Leg2Product)
               .HasMaxLength(100);
               
        builder.Property(e => e.SpreadValue)
               .HasPrecision(18, 4);
               
        builder.Property(e => e.VaRValue)
               .HasPrecision(18, 2);
               
        builder.Property(e => e.Volatility)
               .HasPrecision(8, 4);
               
        builder.Property(e => e.TradeReference)
               .HasMaxLength(100);
               
        builder.Property(e => e.CounterpartyName)
               .HasMaxLength(200);
               
        builder.Property(e => e.Notes)
               .HasMaxLength(1000);
        
        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        
        // Indexes
        builder.HasIndex(e => e.ContractMonth)
               .HasDatabaseName("IX_PaperContracts_ContractMonth");
               
        builder.HasIndex(e => e.ProductType)
               .HasDatabaseName("IX_PaperContracts_ProductType");
               
        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_PaperContracts_Status");
               
        builder.HasIndex(e => e.TradeDate)
               .HasDatabaseName("IX_PaperContracts_TradeDate");
               
        builder.HasIndex(e => e.Position)
               .HasDatabaseName("IX_PaperContracts_Position");
               
        builder.HasIndex(e => new { e.ProductType, e.ContractMonth })
               .HasDatabaseName("IX_PaperContracts_ProductType_ContractMonth");
               
        builder.ToTable("PaperContracts");
        
        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}