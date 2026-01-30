using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

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

        // ═══════════════════════════════════════════════════════════════════════════
        // DATA LINEAGE ENHANCEMENT - Hedge Linking
        // Purpose: Establish direct FK relationship between paper contracts and physical contracts
        // Solves: String-based matching leading to wrong hedges applied to physical positions
        // ═══════════════════════════════════════════════════════════════════════════

        // Hedged Contract ID - Direct FK to the physical contract being hedged
        // Can reference either PurchaseContract or SalesContract based on HedgedContractType
        builder.Property(e => e.HedgedContractId)
               .IsRequired(false);

        // Hedged Contract Type - Whether this hedges a Purchase or Sales contract
        builder.Property(e => e.HedgedContractType)
               .HasConversion<int>()
               .IsRequired(false);

        // Hedge Ratio - The ratio of hedge quantity to physical quantity (e.g., 1.0 for 1:1)
        builder.Property(e => e.HedgeRatio)
               .HasPrecision(8, 4)
               .HasDefaultValue(1.0m);

        // Hedge Effectiveness - Accounting metric for hedge effectiveness (0-100%)
        builder.Property(e => e.HedgeEffectiveness)
               .HasPrecision(5, 2)
               .IsRequired(false);

        // Hedge Designation Date - When this paper contract was formally designated as a hedge
        builder.Property(e => e.HedgeDesignationDate)
               .IsRequired(false);

        // Is Designated Hedge - Quick filter flag indicating formal hedge designation
        builder.Property(e => e.IsDesignatedHedge)
               .IsRequired()
               .HasDefaultValue(false);

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

        // Data Lineage Enhancement - Hedge Linking Indexes
        builder.HasIndex(e => e.HedgedContractId)
               .HasDatabaseName("IX_PaperContracts_HedgedContractId");

        builder.HasIndex(e => e.IsDesignatedHedge)
               .HasDatabaseName("IX_PaperContracts_IsDesignatedHedge");

        // Composite index for hedge queries
        builder.HasIndex(e => new { e.HedgedContractId, e.HedgedContractType })
               .HasDatabaseName("IX_PaperContracts_HedgedContractId_Type");

        // Composite index for designated hedge queries
        builder.HasIndex(e => new { e.IsDesignatedHedge, e.Status })
               .HasDatabaseName("IX_PaperContracts_IsDesignatedHedge_Status");

        builder.ToTable("PaperContracts");
        
        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}