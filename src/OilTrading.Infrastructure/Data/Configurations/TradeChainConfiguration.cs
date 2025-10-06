using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using System.Text.Json;

namespace OilTrading.Infrastructure.Data.Configurations;

public class TradeChainConfiguration : IEntityTypeConfiguration<TradeChain>
{
    public void Configure(EntityTypeBuilder<TradeChain> builder)
    {
        builder.ToTable("TradingChains");

        // Primary key
        builder.HasKey(tc => tc.Id);

        // Properties
        builder.Property(tc => tc.ChainId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(tc => tc.ChainName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tc => tc.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(tc => tc.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(tc => tc.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tc => tc.Notes)
            .HasMaxLength(2000);

        // Value objects - Purchase Value
        builder.OwnsOne(tc => tc.PurchaseValue, pv =>
        {
            pv.Property(m => m.Amount)
                .HasColumnName("PurchaseAmount")
                .HasPrecision(18, 4);
            
            pv.Property(m => m.Currency)
                .HasColumnName("PurchaseCurrency")
                .HasMaxLength(3);
        });

        // Value objects - Sales Value
        builder.OwnsOne(tc => tc.SalesValue, sv =>
        {
            sv.Property(m => m.Amount)
                .HasColumnName("SalesAmount")
                .HasPrecision(18, 4);
            
            sv.Property(m => m.Currency)
                .HasColumnName("SalesCurrency")
                .HasMaxLength(3);
        });

        // Value objects - Realized PnL
        builder.OwnsOne(tc => tc.RealizedPnL, rpnl =>
        {
            rpnl.Property(m => m.Amount)
                .HasColumnName("RealizedPnLAmount")
                .HasPrecision(18, 4);
            
            rpnl.Property(m => m.Currency)
                .HasColumnName("RealizedPnLCurrency")
                .HasMaxLength(3);
        });

        // Value objects - Unrealized PnL
        builder.OwnsOne(tc => tc.UnrealizedPnL, upnl =>
        {
            upnl.Property(m => m.Amount)
                .HasColumnName("UnrealizedPnLAmount")
                .HasPrecision(18, 4);
            
            upnl.Property(m => m.Currency)
                .HasColumnName("UnrealizedPnLCurrency")
                .HasMaxLength(3);
        });

        // Value objects - Purchase Quantity
        builder.OwnsOne(tc => tc.PurchaseQuantity, pq =>
        {
            pq.Property(q => q.Value)
                .HasColumnName("PurchaseQuantityValue")
                .HasPrecision(18, 4);
            
            pq.Property(q => q.Unit)
                .HasColumnName("PurchaseQuantityUnit")
                .HasMaxLength(10);
        });

        // Value objects - Sales Quantity
        builder.OwnsOne(tc => tc.SalesQuantity, sq =>
        {
            sq.Property(q => q.Value)
                .HasColumnName("SalesQuantityValue")
                .HasPrecision(18, 4);
            
            sq.Property(q => q.Unit)
                .HasColumnName("SalesQuantityUnit")
                .HasMaxLength(10);
        });

        // Value objects - Remaining Quantity
        builder.OwnsOne(tc => tc.RemainingQuantity, rq =>
        {
            rq.Property(q => q.Value)
                .HasColumnName("RemainingQuantityValue")
                .HasPrecision(18, 4);
            
            rq.Property(q => q.Unit)
                .HasColumnName("RemainingQuantityUnit")
                .HasMaxLength(10);
        });

        // Operations collection
        builder.OwnsMany(tc => tc.Operations, op =>
        {
            op.ToTable("TradeChainOperations");
            op.WithOwner().HasForeignKey("TradeChainId");
            op.HasKey("Id");
            
            op.Property(o => o.Id)
                .ValueGeneratedNever();
            
            op.Property(o => o.OperationType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            op.Property(o => o.Description)
                .IsRequired()
                .HasMaxLength(500);
            
            op.Property(o => o.PerformedBy)
                .IsRequired()
                .HasMaxLength(100);
            
            op.Property(o => o.PerformedAt)
                .IsRequired();
            
            // Store Data as JSON
            op.Property(o => o.Data)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<object>(v, (JsonSerializerOptions?)null))
                .HasColumnType("TEXT");
        });

        // Events collection
        builder.OwnsMany(tc => tc.Events, ev =>
        {
            ev.ToTable("TradeChainEvents");
            ev.WithOwner().HasForeignKey("TradeChainId");
            ev.HasKey("Id");
            
            ev.Property(e => e.Id)
                .ValueGeneratedNever();
            
            ev.Property(e => e.EventType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            ev.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);
            
            ev.Property(e => e.PerformedBy)
                .IsRequired()
                .HasMaxLength(100);
            
            ev.Property(e => e.PerformedAt)
                .IsRequired();
        });

        // Metadata as JSON
        builder.Property(tc => tc.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("TEXT");

        // Indexes
        builder.HasIndex(tc => tc.ChainId)
            .IsUnique()
            .HasDatabaseName("IX_TradingChains_ChainId");

        builder.HasIndex(tc => tc.Status)
            .HasDatabaseName("IX_TradingChains_Status");

        builder.HasIndex(tc => tc.Type)
            .HasDatabaseName("IX_TradingChains_Type");

        builder.HasIndex(tc => tc.PurchaseContractId)
            .HasDatabaseName("IX_TradingChains_PurchaseContractId");

        builder.HasIndex(tc => tc.SalesContractId)
            .HasDatabaseName("IX_TradingChains_SalesContractId");

        builder.HasIndex(tc => tc.SupplierId)
            .HasDatabaseName("IX_TradingChains_SupplierId");

        builder.HasIndex(tc => tc.CustomerId)
            .HasDatabaseName("IX_TradingChains_CustomerId");

        builder.HasIndex(tc => tc.ProductId)
            .HasDatabaseName("IX_TradingChains_ProductId");

        builder.HasIndex(tc => tc.TradeDate)
            .HasDatabaseName("IX_TradingChains_TradeDate");

        builder.HasIndex(tc => tc.ExpectedDeliveryStart)
            .HasDatabaseName("IX_TradingChains_ExpectedDeliveryStart");

        builder.HasIndex(tc => tc.ExpectedDeliveryEnd)
            .HasDatabaseName("IX_TradingChains_ExpectedDeliveryEnd");

        builder.HasIndex(tc => tc.CreatedBy)
            .HasDatabaseName("IX_TradingChains_CreatedBy");

        // Composite indexes for common queries
        builder.HasIndex(tc => new { tc.Status, tc.Type })
            .HasDatabaseName("IX_TradingChains_Status_Type");

        builder.HasIndex(tc => new { tc.CreatedAt, tc.Status })
            .HasDatabaseName("IX_TradingChains_CreatedAt_Status");

        builder.HasIndex(tc => new { tc.SupplierId, tc.Status })
            .HasDatabaseName("IX_TradingChains_SupplierId_Status");

        builder.HasIndex(tc => new { tc.CustomerId, tc.Status })
            .HasDatabaseName("IX_TradingChains_CustomerId_Status");

        builder.HasIndex(tc => new { tc.ProductId, tc.TradeDate })
            .HasDatabaseName("IX_TradingChains_ProductId_TradeDate");
    }
}