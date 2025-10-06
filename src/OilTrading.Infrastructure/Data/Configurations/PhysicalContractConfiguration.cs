using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PhysicalContractConfiguration : IEntityTypeConfiguration<PhysicalContract>
{
    public void Configure(EntityTypeBuilder<PhysicalContract> builder)
    {
        builder.ToTable("PhysicalContracts");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ContractNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.HasIndex(e => e.ContractNumber)
            .IsUnique()
            .HasDatabaseName("IX_PhysicalContracts_ContractNumber");
        
        builder.Property(e => e.ContractType)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(e => e.ContractDate)
            .IsRequired();
        
        // Product Details
        builder.Property(e => e.ProductType)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.Quantity)
            .IsRequired()
            .HasPrecision(18, 3);
        
        builder.Property(e => e.QuantityUnit)
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(e => e.ProductSpec)
            .HasMaxLength(500);
        
        // Pricing
        builder.Property(e => e.PricingType)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(e => e.FixedPrice)
            .HasPrecision(18, 4);
        
        builder.Property(e => e.PricingFormula)
            .HasMaxLength(200);
        
        builder.Property(e => e.PricingBasis)
            .HasMaxLength(100);
        
        builder.Property(e => e.Premium)
            .HasPrecision(18, 4);
        
        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");
        
        builder.Property(e => e.ContractValue)
            .HasPrecision(18, 2);
        
        // Delivery Terms
        builder.Property(e => e.DeliveryTerms)
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(e => e.LoadPort)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.DischargePort)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.LaycanStart)
            .IsRequired();
        
        builder.Property(e => e.LaycanEnd)
            .IsRequired();
        
        // Payment Terms
        builder.Property(e => e.PaymentTerms)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(e => e.PrepaymentPercentage)
            .HasPrecision(5, 2);
        
        builder.Property(e => e.CreditDays)
            .HasDefaultValue(30);
        
        // Agency Trade
        builder.Property(e => e.IsAgencyTrade)
            .HasDefaultValue(false);
        
        builder.Property(e => e.PrincipalName)
            .HasMaxLength(200);
        
        builder.Property(e => e.AgencyFee)
            .HasPrecision(18, 2);
        
        // Status and Settlement
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(e => e.DeliveredQuantity)
            .HasPrecision(18, 3);
        
        builder.Property(e => e.InvoicedAmount)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.PaidAmount)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.OutstandingAmount)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.IsFullySettled)
            .HasDefaultValue(false);
        
        // Invoice
        builder.Property(e => e.ProformaInvoiceNumber)
            .HasMaxLength(50);
        
        builder.Property(e => e.CommercialInvoiceNumber)
            .HasMaxLength(50);
        
        // Notes
        builder.Property(e => e.Notes)
            .HasMaxLength(1000);
        
        builder.Property(e => e.InternalNotes)
            .HasMaxLength(1000);
        
        // Relationships
        builder.HasOne(e => e.TradingPartner)
            .WithMany(tp => tp.PhysicalContracts)
            .HasForeignKey(e => e.TradingPartnerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(e => e.ContractDate)
            .HasDatabaseName("IX_PhysicalContracts_ContractDate");
        
        builder.HasIndex(e => e.TradingPartnerId)
            .HasDatabaseName("IX_PhysicalContracts_TradingPartnerId");
        
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_PhysicalContracts_Status");
        
        builder.HasIndex(e => new { e.LaycanStart, e.LaycanEnd })
            .HasDatabaseName("IX_PhysicalContracts_Laycan");
    }
}