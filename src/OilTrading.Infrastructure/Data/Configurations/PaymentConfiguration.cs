using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.PaymentReference)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.HasIndex(e => e.PaymentReference)
            .IsUnique()
            .HasDatabaseName("IX_Payments_PaymentReference");
        
        // Configure Money properties
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("Amount")
                 .HasPrecision(18, 4)
                 .IsRequired();
            
            money.Property(m => m.Currency)
                 .HasColumnName("Currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });
        
        // Configure BankAccount properties with proper handling of Dictionary
        builder.OwnsOne(e => e.PayerAccount, bankAccount =>
        {
            bankAccount.Property(ba => ba.AccountNumber)
                      .HasColumnName("PayerAccountNumber")
                      .HasMaxLength(50);
            
            bankAccount.Property(ba => ba.BankName)
                      .HasColumnName("PayerBankName")
                      .HasMaxLength(200);
            
            bankAccount.Property(ba => ba.SwiftCode)
                      .HasColumnName("PayerSwiftCode")
                      .HasMaxLength(20);
            
            bankAccount.Property(ba => ba.IBAN)
                      .HasColumnName("PayerIBAN")
                      .HasMaxLength(50);
            
            bankAccount.Property(ba => ba.AccountHolderName)
                      .HasColumnName("PayerAccountHolderName")
                      .HasMaxLength(200);
            
            bankAccount.Property(ba => ba.Currency)
                      .HasColumnName("PayerCurrency")
                      .HasMaxLength(3)
                      .HasDefaultValue("USD");
            
            bankAccount.Property(ba => ba.RoutingNumber)
                      .HasColumnName("PayerRoutingNumber")
                      .HasMaxLength(20);
            
            bankAccount.Property(ba => ba.BranchCode)
                      .HasColumnName("PayerBranchCode")
                      .HasMaxLength(20);
            
            // Ignore the Dictionary property since EF Core can't map it directly
            bankAccount.Ignore(ba => ba.AdditionalDetails);
        });
        
        builder.OwnsOne(e => e.PayeeAccount, bankAccount =>
        {
            bankAccount.Property(ba => ba.AccountNumber)
                      .HasColumnName("PayeeAccountNumber")
                      .HasMaxLength(50);
            
            bankAccount.Property(ba => ba.BankName)
                      .HasColumnName("PayeeBankName")
                      .HasMaxLength(200);
            
            bankAccount.Property(ba => ba.SwiftCode)
                      .HasColumnName("PayeeSwiftCode")
                      .HasMaxLength(20);
            
            bankAccount.Property(ba => ba.IBAN)
                      .HasColumnName("PayeeIBAN")
                      .HasMaxLength(50);
            
            bankAccount.Property(ba => ba.AccountHolderName)
                      .HasColumnName("PayeeAccountHolderName")
                      .HasMaxLength(200);
            
            bankAccount.Property(ba => ba.Currency)
                      .HasColumnName("PayeeCurrency")
                      .HasMaxLength(3)
                      .HasDefaultValue("USD");
            
            bankAccount.Property(ba => ba.RoutingNumber)
                      .HasColumnName("PayeeRoutingNumber")
                      .HasMaxLength(20);
            
            bankAccount.Property(ba => ba.BranchCode)
                      .HasColumnName("PayeeBranchCode")
                      .HasMaxLength(20);
            
            // Ignore the Dictionary property since EF Core can't map it directly
            bankAccount.Ignore(ba => ba.AdditionalDetails);
        });
        
        // Other properties
        builder.Property(e => e.Method)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();
        
        builder.Property(e => e.PaymentDate)
            .IsRequired();
        
        builder.Property(e => e.CreatedDate)
            .IsRequired();
        
        builder.Property(e => e.Instructions)
            .HasMaxLength(1000);
        
        builder.Property(e => e.BankReference)
            .HasMaxLength(100);
        
        builder.Property(e => e.FailureReason)
            .HasMaxLength(500);
        
        builder.Property(e => e.InitiatedDate);
        
        builder.Property(e => e.CompletedDate);
        
        // Ignore complex collections that are difficult to map
        builder.Ignore(e => e.StatusHistory);
        
        // Relationships
        builder.HasOne(e => e.Settlement)
            .WithMany()
            .HasForeignKey(e => e.SettlementId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(e => e.PaymentDate)
            .HasDatabaseName("IX_Payments_PaymentDate");
        
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Payments_Status");
        
        builder.HasIndex(e => e.SettlementId)
            .HasDatabaseName("IX_Payments_SettlementId");
    }
}