using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OilTrading.Core.Entities;
using System.Text.Json;

namespace OilTrading.Infrastructure.Data.Configurations;

public class OperationAuditLogConfiguration : IEntityTypeConfiguration<OperationAuditLog>
{
    public void Configure(EntityTypeBuilder<OperationAuditLog> builder)
    {
        builder.ToTable("OperationAuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OperationId)
            .IsRequired();

        builder.Property(e => e.TransactionId)
            .IsRequired(false);

        builder.Property(e => e.OperationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.OperationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.IsSuccess)
            .IsRequired();

        // Convert Dictionary to JSON string for database storage
        builder.Property(e => e.Data)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
            )
            .HasColumnType("text")
            .HasColumnName("Data");

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(e => e.InitiatedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(e => e.OperationId)
            .HasDatabaseName("IX_OperationAuditLogs_OperationId");

        builder.HasIndex(e => e.TransactionId)
            .HasDatabaseName("IX_OperationAuditLogs_TransactionId");

        builder.HasIndex(e => e.OperationType)
            .HasDatabaseName("IX_OperationAuditLogs_OperationType");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_OperationAuditLogs_Timestamp");

        builder.HasIndex(e => e.IsSuccess)
            .HasDatabaseName("IX_OperationAuditLogs_IsSuccess");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_OperationAuditLogs_CreatedAt");

        // Composite index for common queries
        builder.HasIndex(e => new { e.OperationType, e.IsSuccess, e.Timestamp })
            .HasDatabaseName("IX_OperationAuditLogs_Type_Success_Timestamp");
    }
}