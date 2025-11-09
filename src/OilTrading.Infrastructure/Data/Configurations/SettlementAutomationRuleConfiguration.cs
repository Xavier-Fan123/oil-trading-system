using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OilTrading.Core.Entities;
using System.Text.Json;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// Custom value converter for List&lt;Guid&gt; to/from JSON string
/// </summary>
internal class GuidListConverter : ValueConverter<List<Guid>, string?>
{
    public GuidListConverter() : base(
        v => ConvertToDb(v),
        v => ConvertFromDb(v))
    {
    }

    private static string? ConvertToDb(List<Guid>? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value);
    }

    private static List<Guid> ConvertFromDb(string? value)
    {
        return string.IsNullOrEmpty(value) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(value) ?? new List<Guid>();
    }
}

/// <summary>
/// EF Core configuration for SettlementAutomationRule aggregate
/// Configures: SettlementAutomationRule (root), SettlementRuleCondition, SettlementRuleAction, RuleExecutionRecord
/// </summary>
public class SettlementAutomationRuleConfiguration : IEntityTypeConfiguration<SettlementAutomationRule>
{
    public void Configure(EntityTypeBuilder<SettlementAutomationRule> builder)
    {
        builder.ToTable("SettlementAutomationRules");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.RuleType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Normal");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(RuleStatus.Active);

        builder.Property(e => e.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedDate)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.LastModifiedDate);
        builder.Property(e => e.LastModifiedBy).HasMaxLength(250);

        builder.Property(e => e.RuleVersion)
            .IsRequired()
            .HasDefaultValue(1);

        // Scope configuration
        builder.Property(e => e.Scope)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SettlementRuleScope.All);

        builder.Property(e => e.ScopeFilter)
            .HasMaxLength(500);

        // Trigger configuration
        builder.Property(e => e.Trigger)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SettlementRuleTrigger.OnContractCompletion);

        builder.Property(e => e.ScheduleExpression)
            .HasMaxLength(100);

        // Orchestration settings
        builder.Property(e => e.OrchestrationStrategy)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SettlementOrchestrationStrategy.Sequential);

        builder.Property(e => e.MaxSettlementsPerExecution);

        builder.Property(e => e.GroupingDimension)
            .HasMaxLength(100);

        // Execution tracking
        builder.Property(e => e.ExecutionCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.SuccessCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.FailureCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LastExecutedDate);
        builder.Property(e => e.LastExecutionSettlementCount);

        builder.Property(e => e.LastExecutionError)
            .HasMaxLength(1000);

        // Audit
        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.DisabledDate);

        builder.Property(e => e.DisabledReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.RuleType, e.IsEnabled }).HasDatabaseName("IX_RuleTypeEnabled");
        builder.HasIndex(e => e.CreatedDate).HasDatabaseName("IX_CreatedDate");
        builder.HasIndex(e => e.LastExecutedDate).HasDatabaseName("IX_LastExecuted");

        // Navigation properties
        builder.HasMany(e => e.Conditions)
            .WithOne(c => c.Rule)
            .HasForeignKey(c => c.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Actions)
            .WithOne(a => a.Rule)
            .HasForeignKey(a => a.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ExecutionHistory)
            .WithOne()
            .HasForeignKey(eh => eh.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration for SettlementRuleCondition
/// </summary>
public class SettlementRuleConditionConfiguration : IEntityTypeConfiguration<SettlementRuleCondition>
{
    public void Configure(EntityTypeBuilder<SettlementRuleCondition> builder)
    {
        builder.ToTable("SettlementRuleConditions");

        // Primary key
        builder.HasKey(e => e.Id);

        // Foreign key
        builder.Property(e => e.RuleId).IsRequired();

        // Properties
        builder.Property(e => e.Field)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.OperatorType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Value)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.SequenceNumber)
            .IsRequired();

        builder.Property(e => e.LogicalOperator)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("AND");

        builder.Property(e => e.GroupReference)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => new { e.RuleId, e.SequenceNumber })
            .HasDatabaseName("IX_RuleConditionSequence");
    }
}

/// <summary>
/// EF Core configuration for SettlementRuleAction
/// </summary>
public class SettlementRuleActionConfiguration : IEntityTypeConfiguration<SettlementRuleAction>
{
    public void Configure(EntityTypeBuilder<SettlementRuleAction> builder)
    {
        builder.ToTable("SettlementRuleActions");

        // Primary key
        builder.HasKey(e => e.Id);

        // Foreign key
        builder.Property(e => e.RuleId).IsRequired();

        // Properties
        builder.Property(e => e.ActionType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.SequenceNumber)
            .IsRequired();

        builder.Property(e => e.Parameters)
            .HasMaxLength(2000)
            .HasColumnType("TEXT");

        builder.Property(e => e.StopOnFailure)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotificationTemplateId)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => new { e.RuleId, e.SequenceNumber })
            .HasDatabaseName("IX_RuleActionSequence");

        builder.HasIndex(e => e.ActionType)
            .HasDatabaseName("IX_ActionType");
    }
}

/// <summary>
/// EF Core configuration for RuleExecutionRecord
/// </summary>
public class RuleExecutionRecordConfiguration : IEntityTypeConfiguration<RuleExecutionRecord>
{
    public void Configure(EntityTypeBuilder<RuleExecutionRecord> builder)
    {
        builder.ToTable("RuleExecutionRecords");

        // Primary key
        builder.HasKey(e => e.Id);

        // Foreign key
        builder.Property(e => e.RuleId).IsRequired();

        // Properties
        builder.Property(e => e.TriggerSource)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExecutionStartTime)
            .IsRequired();

        builder.Property(e => e.ExecutionEndTime);

        builder.Property(e => e.ExecutionDurationMs);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ExecutionStatus.Running);

        builder.Property(e => e.SettlementCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ConditionsEvaluated)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ActionsExecuted)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.DetailedLog)
            .HasMaxLength(5000)
            .HasColumnType("TEXT");

        // For storing affected settlement IDs as JSON
        builder.Property(e => e.AffectedSettlementIds)
            .HasColumnType("TEXT")
            .HasConversion(new GuidListConverter());

        // Indexes
        builder.HasIndex(e => e.RuleId).HasDatabaseName("IX_ExecutionRuleId");
        builder.HasIndex(e => e.ExecutionStartTime).HasDatabaseName("IX_ExecutionStartTime");
        builder.HasIndex(e => new { e.RuleId, e.ExecutionStartTime })
            .HasDatabaseName("IX_RuleExecutionTime");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_ExecutionStatus");
    }
}
