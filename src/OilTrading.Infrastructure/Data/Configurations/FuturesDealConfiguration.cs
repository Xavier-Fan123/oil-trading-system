using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Infrastructure.Data.Configurations;

public class FuturesDealConfiguration : IEntityTypeConfiguration<FuturesDeal>
{
    public void Configure(EntityTypeBuilder<FuturesDeal> builder)
    {
        builder.ToTable("FuturesDeals");
        
        builder.HasKey(e => e.Id);
        
        // Deal identification
        builder.Property(e => e.DealNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.HasIndex(e => e.DealNumber)
            .HasDatabaseName("IX_FuturesDeals_DealNumber");
            
        builder.HasIndex(e => new { e.DealNumber, e.TradeDate })
            .IsUnique()
            .HasDatabaseName("IX_FuturesDeals_DealNumber_TradeDate");
        
        // Trading details
        builder.Property(e => e.ProductCode)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.ProductName)
            .HasMaxLength(200);
            
        builder.Property(e => e.ContractMonth)
            .IsRequired()
            .HasMaxLength(10);
            
        builder.HasIndex(e => e.ContractMonth)
            .HasDatabaseName("IX_FuturesDeals_ContractMonth");
            
        builder.Property(e => e.Direction)
            .IsRequired();
            
        // Quantity and pricing
        builder.Property(e => e.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(e => e.QuantityUnit)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(QuantityUnit.MT);
            
        builder.Property(e => e.Price)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");
            
        builder.Property(e => e.PriceUnit)
            .HasMaxLength(20);
            
        builder.Property(e => e.TotalValue)
            .HasPrecision(18, 2);
        
        // Trading parties
        builder.Property(e => e.Trader)
            .HasMaxLength(100);
            
        builder.Property(e => e.Broker)
            .HasMaxLength(100);
            
        builder.Property(e => e.Exchange)
            .HasMaxLength(50);
            
        builder.Property(e => e.ClearingHouse)
            .HasMaxLength(100);
        
        // Status
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(e => e.ClearingReference)
            .HasMaxLength(100);
        
        // P&L fields
        builder.Property(e => e.MarketPrice)
            .HasPrecision(18, 4);
            
        builder.Property(e => e.UnrealizedPnL)
            .HasPrecision(18, 2);
            
        builder.Property(e => e.RealizedPnL)
            .HasPrecision(18, 2);
        
        // Import tracking
        builder.Property(e => e.DataSource)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(e => e.ImportedBy)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(e => e.OriginalFileName)
            .HasMaxLength(255);
        
        // Indexes for performance
        builder.HasIndex(e => e.TradeDate)
            .HasDatabaseName("IX_FuturesDeals_TradeDate");
            
        builder.HasIndex(e => e.ProductCode)
            .HasDatabaseName("IX_FuturesDeals_ProductCode");
            
        builder.HasIndex(e => new { e.ProductCode, e.ContractMonth })
            .HasDatabaseName("IX_FuturesDeals_Product_Contract");
            
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_FuturesDeals_Status");
            
        builder.HasIndex(e => e.ImportedAt)
            .HasDatabaseName("IX_FuturesDeals_ImportedAt");
        
        // Relationship with PaperContract
        builder.HasOne(e => e.PaperContract)
            .WithMany()
            .HasForeignKey(e => e.PaperContractId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}