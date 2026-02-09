using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Enums;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PurchaseContractConfiguration : IEntityTypeConfiguration<PurchaseContract>
{
    public void Configure(EntityTypeBuilder<PurchaseContract> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Configure ContractNumber as owned entity with index
        builder.OwnsOne(e => e.ContractNumber, cn =>
        {
            cn.Property(c => c.Value)
              .HasColumnName("ContractNumber")
              .IsRequired()
              .HasMaxLength(50);
            
            // Create index on the Value property inside the owned configuration
            cn.HasIndex(c => c.Value)
              .IsUnique()
              .HasDatabaseName("IX_PurchaseContracts_ContractNumber");
        });

        // Configure External Contract Number
        builder.Property(e => e.ExternalContractNumber)
               .HasMaxLength(100)
               .IsRequired(false); // Optional field

        // Create index on External Contract Number for fast lookups
        builder.HasIndex(e => e.ExternalContractNumber)
               .IsUnique(false) // Allow nulls, but unique when not null
               .HasDatabaseName("IX_PurchaseContracts_ExternalContractNumber");

        // Contract Type
        builder.Property(e => e.ContractType)
               .IsRequired()
               .HasConversion<int>();

        // Foreign Keys
        builder.Property(e => e.TradingPartnerId).IsRequired();
        builder.Property(e => e.ProductId).IsRequired();
        builder.Property(e => e.TraderId).IsRequired();

        // Configure Quantity as owned entity
        builder.OwnsOne(e => e.ContractQuantity, quantity =>
        {
            quantity.Property(q => q.Value)
                    .HasColumnName("ContractQuantity")
                    .HasPrecision(18, 6)
                    .IsRequired();
            
            quantity.Property(q => q.Unit)
                    .HasColumnName("ContractQuantityUnit")
                    .IsRequired();
        });

        builder.Property(e => e.TonBarrelRatio)
               .HasPrecision(8, 4)
               .HasDefaultValue(7.6m);

        // Configure ContractValue as owned entity
        builder.OwnsOne(e => e.ContractValue, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("ContractValue")
                 .HasPrecision(18, 4);
            
            money.Property(m => m.Currency)
                 .HasColumnName("ContractValueCurrency")
                 .HasMaxLength(3);
        });

        // Pricing Period
        builder.Property(e => e.PricingPeriodStart);
        builder.Property(e => e.PricingPeriodEnd);
        builder.Property(e => e.IsPriceFinalized)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(e => e.BenchmarkContractId);
        
        // Price Benchmark Configuration - 价格基准物配置
        // Purpose: 配置与PriceBenchmark实体的外键关系，用于价格计算
        builder.Property(e => e.PriceBenchmarkId);

        // Configure Premium as owned entity
        builder.OwnsOne(e => e.Premium, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("Premium")
                 .HasPrecision(18, 4);
            
            money.Property(m => m.Currency)
                 .HasColumnName("PremiumCurrency")
                 .HasMaxLength(3);
        });

        // Configure Discount as owned entity
        builder.OwnsOne(e => e.Discount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("Discount")
                 .HasPrecision(18, 4);
            
            money.Property(m => m.Currency)
                 .HasColumnName("DiscountCurrency")
                 .HasMaxLength(3);
        });

        // Configure PriceFormula as owned entity
        builder.OwnsOne(e => e.PriceFormula, formula =>
        {
            formula.Property(f => f.Formula)
                   .HasColumnName("PriceFormula")
                   .HasMaxLength(500);
            
            formula.Property(f => f.Method)
                   .HasColumnName("PricingMethod")
                   .HasConversion<int>();
            
            formula.Property(f => f.IndexName)
                   .HasColumnName("PriceIndexName")
                   .HasMaxLength(100);
            
            formula.Property(f => f.FixedPrice)
                   .HasColumnName("FixedPrice")
                   .HasPrecision(18, 4);
            
            formula.Property(f => f.Currency)
                   .HasColumnName("PriceCurrency")
                   .HasMaxLength(3);
            
            formula.Property(f => f.Unit)
                   .HasColumnName("PriceUnit")
                   .HasMaxLength(20);
            
            formula.Property(f => f.PricingDays)
                   .HasColumnName("PricingDays");
            
            formula.Property(f => f.PricingPeriodStart)
                   .HasColumnName("FormulaPricingStart");
            
            formula.Property(f => f.PricingPeriodEnd)
                   .HasColumnName("FormulaPricingEnd");
            
            // Ignore computed properties - they're calculated, not stored
            formula.Ignore(f => f.IsFixedPrice);
            formula.Ignore(f => f.BasePrice);
            
            // Mixed-unit pricing support fields
            formula.Property(f => f.BenchmarkUnit)
                   .HasColumnName("BenchmarkUnit")
                   .HasConversion<int>();
            
            formula.Property(f => f.AdjustmentUnit)
                   .HasColumnName("AdjustmentUnit")
                   .HasConversion<int>();
            
            formula.Property(f => f.CalculationMode)
                   .HasColumnName("CalculationMode")
                   .HasConversion<int>();
            
            formula.Property(f => f.ContractualConversionRatio)
                   .HasColumnName("ContractualConversionRatio")
                   .HasPrecision(10, 4);

            // Configure nested Money value objects within PriceFormula
            formula.OwnsOne(f => f.Premium, premium =>
            {
                premium.Property(m => m.Amount)
                       .HasColumnName("FormulaPremium")
                       .HasPrecision(18, 4);
                
                premium.Property(m => m.Currency)
                       .HasColumnName("FormulaPremiumCurrency")
                       .HasMaxLength(3);
            });
            
            formula.OwnsOne(f => f.Discount, discount =>
            {
                discount.Property(m => m.Amount)
                        .HasColumnName("FormulaDiscount")
                        .HasPrecision(18, 4);
                
                discount.Property(m => m.Currency)
                        .HasColumnName("FormulaDiscountCurrency")
                        .HasMaxLength(3);
            });

            // Configure nested Adjustment Money value object within PriceFormula
            formula.OwnsOne(f => f.Adjustment, adjustment =>
            {
                adjustment.Property(m => m.Amount)
                          .HasColumnName("FormulaAdjustment")
                          .HasPrecision(18, 4);
                
                adjustment.Property(m => m.Currency)
                          .HasColumnName("FormulaAdjustmentCurrency")
                          .HasMaxLength(3);
            });
        });

        // Status
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        // Dates
        builder.Property(e => e.LaycanStart);
        builder.Property(e => e.LaycanEnd);

        // Ports
        builder.Property(e => e.LoadPort).HasMaxLength(100);
        builder.Property(e => e.DischargePort).HasMaxLength(100);

        // Delivery Terms
        builder.Property(e => e.DeliveryTerms)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(DeliveryTerms.FOB)
               .HasSentinel(0);

        // Payment Terms
        builder.Property(e => e.PaymentTerms).HasMaxLength(500);
        builder.Property(e => e.CreditPeriodDays);

        builder.Property(e => e.SettlementType)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(SettlementType.ContractPayment)
               .HasSentinel(0);

        builder.Property(e => e.PrepaymentPercentage)
               .HasPrecision(5, 2);

        // Payment Date - Ignore for now (column doesn't exist in database yet, migration pending)
        builder.Ignore(e => e.EstimatedPaymentDate);

        // Professional Trading Fields (v2.19) - Ignore for now (columns don't exist in database yet)
        builder.Ignore(e => e.QuantityTolerancePercent);
        builder.Ignore(e => e.QuantityToleranceOption);
        builder.Ignore(e => e.BrokerName);
        builder.Ignore(e => e.BrokerCommission);
        builder.Ignore(e => e.BrokerCommissionType);
        builder.Ignore(e => e.LaytimeHours);
        builder.Ignore(e => e.DemurrageRate);
        builder.Ignore(e => e.DespatchRate);

        // ═══════════════════════════════════════════════════════════════════════════
        // DATA LINEAGE ENHANCEMENT - Deal Reference ID & Pricing Status
        // Purpose: Enable full lifecycle traceability and explicit pricing state tracking
        // ═══════════════════════════════════════════════════════════════════════════

        // Deal Reference ID - Business-meaningful identifier that flows through entire transaction lifecycle
        builder.Property(e => e.DealReferenceId)
               .HasMaxLength(50)
               .IsRequired(false); // Nullable for backward compatibility with existing data

        // Pricing Status - Explicit state tracking (Unpriced, PartiallyPriced, FullyPriced)
        builder.Property(e => e.PricingStatus)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(ContractPricingStatus.Unpriced);

        // Fixed Quantity - Amount of contract quantity that has been price-fixed
        builder.Property(e => e.FixedQuantity)
               .HasPrecision(18, 6)
               .HasDefaultValue(0m);

        // Unfixed Quantity - Amount of contract quantity pending pricing
        builder.Property(e => e.UnfixedQuantity)
               .HasPrecision(18, 6)
               .HasDefaultValue(0m);

        // Fixed Percentage - Calculated percentage of total quantity that is priced (0-100)
        builder.Property(e => e.FixedPercentage)
               .HasPrecision(5, 2)
               .HasDefaultValue(0m);

        // Last Pricing Date - When pricing was last updated
        builder.Property(e => e.LastPricingDate)
               .IsRequired(false);

        // Price Source - How the price was determined (Manual, MarketData, Formula, etc.)
        builder.Property(e => e.PriceSource)
               .HasConversion<int>()
               .IsRequired(false);

        // Additional Fields
        builder.Property(e => e.ExternalContractNumber)
               .HasMaxLength(100)
               .IsUnicode(true);

        builder.Property(e => e.Incoterms).HasMaxLength(50);
        builder.Property(e => e.QualitySpecifications).HasMaxLength(2000);
        builder.Property(e => e.InspectionAgency).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Relationships
        builder.HasOne(e => e.TradingPartner)
               .WithMany(tp => tp.PurchaseContracts)
               .HasForeignKey(e => e.TradingPartnerId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PurchaseContracts_TradingPartners");

        builder.HasOne(e => e.Product)
               .WithMany(p => p.PurchaseContracts)
               .HasForeignKey(e => e.ProductId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PurchaseContracts_Products");

        builder.HasOne(e => e.Trader)
               .WithMany(u => u.PurchaseContracts)
               .HasForeignKey(e => e.TraderId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_PurchaseContracts_Users");

        builder.HasOne(e => e.BenchmarkContract)
               .WithMany()
               .HasForeignKey(e => e.BenchmarkContractId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("FK_PurchaseContracts_BenchmarkContract");

        // Price Benchmark Relationship - 价格基准物关系配置
        // Purpose: 建立与PriceBenchmark实体的外键关系，支持基准物查询和导航
        builder.HasOne(e => e.PriceBenchmark)
               .WithMany()
               .HasForeignKey(e => e.PriceBenchmarkId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("FK_PurchaseContracts_PriceBenchmark");

        // Indexes for performance
        builder.HasIndex(e => e.TradingPartnerId)
               .HasDatabaseName("IX_PurchaseContracts_TradingPartnerId");

        builder.HasIndex(e => e.ProductId)
               .HasDatabaseName("IX_PurchaseContracts_ProductId");

        builder.HasIndex(e => e.TraderId)
               .HasDatabaseName("IX_PurchaseContracts_TraderId");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_PurchaseContracts_Status");

        builder.HasIndex(e => e.PriceBenchmarkId)
               .HasDatabaseName("IX_PurchaseContracts_PriceBenchmarkId");

        builder.HasIndex(e => new { e.LaycanStart, e.LaycanEnd })
               .HasDatabaseName("IX_PurchaseContracts_LaycanDates");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_PurchaseContracts_CreatedAt");

        // Data Lineage Enhancement - Deal Reference ID Index
        builder.HasIndex(e => e.DealReferenceId)
               .HasDatabaseName("IX_PurchaseContracts_DealReferenceId");

        // Data Lineage Enhancement - Pricing Status Index for filtering
        builder.HasIndex(e => e.PricingStatus)
               .HasDatabaseName("IX_PurchaseContracts_PricingStatus");

        // Composite index for Deal Reference ID + Status (common query pattern)
        builder.HasIndex(e => new { e.DealReferenceId, e.Status })
               .HasDatabaseName("IX_PurchaseContracts_DealReferenceId_Status");

        // Contract Tags Relationship
        builder.HasMany(e => e.ContractTags)
               .WithOne()
               .HasForeignKey(ct => ct.ContractId)
               .HasPrincipalKey(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade);

        // Settlement Relationship (one-to-many)
        // One PurchaseContract can have multiple PurchaseSettlements
        builder.HasMany(e => e.PurchaseSettlements)
               .WithOne(ps => ps.PurchaseContract)
               .HasForeignKey(ps => ps.PurchaseContractId)
               .OnDelete(DeleteBehavior.Restrict) // Prevent accidental deletion of contract with settlements
               .HasConstraintName("FK_PurchaseSettlements_PurchaseContracts");

        // Configure RowVersion for optimistic concurrency control
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasColumnType("BLOB")
               .HasDefaultValueSql("X'00000000000000000000000000000001'");

        // Table configuration
        builder.ToTable("PurchaseContracts");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}