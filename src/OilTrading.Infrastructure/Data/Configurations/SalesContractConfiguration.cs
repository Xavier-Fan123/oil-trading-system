using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Infrastructure.Data.Configurations;

public class SalesContractConfiguration : IEntityTypeConfiguration<SalesContract>
{
    public void Configure(EntityTypeBuilder<SalesContract> builder)
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
              .HasDatabaseName("IX_SalesContracts_ContractNumber");
        });

        // Contract Type
        builder.Property(e => e.ContractType)
               .IsRequired()
               .HasConversion<int>();

        // Foreign Keys
        builder.Property(e => e.TradingPartnerId).IsRequired();
        builder.Property(e => e.ProductId).IsRequired();
        builder.Property(e => e.TraderId).IsRequired();
        builder.Property(e => e.LinkedPurchaseContractId);

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

        // Configure ProfitMargin as owned entity
        builder.OwnsOne(e => e.ProfitMargin, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("ProfitMargin")
                 .HasPrecision(18, 4);
            
            money.Property(m => m.Currency)
                 .HasColumnName("ProfitMarginCurrency")
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
               .WithMany(tp => tp.SalesContracts)
               .HasForeignKey(e => e.TradingPartnerId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_SalesContracts_TradingPartners");

        builder.HasOne(e => e.Product)
               .WithMany(p => p.SalesContracts)
               .HasForeignKey(e => e.ProductId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_SalesContracts_Products");

        builder.HasOne(e => e.Trader)
               .WithMany(u => u.SalesContracts)
               .HasForeignKey(e => e.TraderId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_SalesContracts_Users");

        builder.HasOne(e => e.LinkedPurchaseContract)
               .WithMany(pc => pc.LinkedSalesContracts)
               .HasForeignKey(e => e.LinkedPurchaseContractId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("FK_SalesContracts_PurchaseContracts");

        // Indexes for performance
        builder.HasIndex(e => e.TradingPartnerId)
               .HasDatabaseName("IX_SalesContracts_TradingPartnerId");

        builder.HasIndex(e => e.ProductId)
               .HasDatabaseName("IX_SalesContracts_ProductId");

        builder.HasIndex(e => e.TraderId)
               .HasDatabaseName("IX_SalesContracts_TraderId");

        builder.HasIndex(e => e.LinkedPurchaseContractId)
               .HasDatabaseName("IX_SalesContracts_LinkedPurchaseContractId");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_SalesContracts_Status");

        builder.HasIndex(e => new { e.LaycanStart, e.LaycanEnd })
               .HasDatabaseName("IX_SalesContracts_LaycanDates");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_SalesContracts_CreatedAt");

        // Index for ExternalContractNumber - CRITICAL for external contract lookups
        builder.HasIndex(e => e.ExternalContractNumber)
               .HasDatabaseName("IX_SalesContracts_ExternalContractNumber");

        // Contract Tags Relationship
        builder.HasMany(e => e.ContractTags)
               .WithOne()
               .HasForeignKey(ct => ct.ContractId)
               .HasPrincipalKey(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade);

        // Settlement Relationship (one-to-many)
        // One SalesContract can have multiple SalesSettlements
        builder.HasMany(e => e.SalesSettlements)
               .WithOne(ss => ss.SalesContract)
               .HasForeignKey(ss => ss.SalesContractId)
               .OnDelete(DeleteBehavior.Restrict) // Prevent accidental deletion of contract with settlements
               .HasConstraintName("FK_SalesSettlements_SalesContracts");

        // Configure RowVersion for optimistic concurrency control
        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .HasColumnType("BLOB")
               .HasDefaultValueSql("X'00000000000000000000000000000001'");

        // Table configuration
        builder.ToTable("SalesContracts");

        // Ignore domain events collection for EF
        builder.Ignore(e => e.DomainEvents);
    }
}