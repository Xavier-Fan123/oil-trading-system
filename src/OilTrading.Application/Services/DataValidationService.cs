using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Text.Json;
using OilTrading.Application.Common.Extensions;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Services;

/// <summary>
/// Data validation service implementation
/// </summary>
public class DataValidationService : IDataValidationService
{
    private readonly ILogger<DataValidationService> _logger;
    private readonly DataValidationOptions _options;
    
    // In-memory storage for demo (in production, use database)
    private static readonly ConcurrentDictionary<Guid, ValidationRule> _rules = new();
    private static readonly ConcurrentDictionary<Guid, DataAnomaly> _anomalies = new();
    private static readonly List<ValidationResult> _validationHistory = new();
    
    // Performance tracking
    private static readonly ConcurrentDictionary<Guid, ValidationRuleStatistics> _ruleStats = new();
    private static long _totalValidations = 0;

    public DataValidationService(
        ILogger<DataValidationService> logger, 
        DataValidationOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new DataValidationOptions();
        
        InitializeSampleRules();
    }

    public async Task<Guid> CreateValidationRuleAsync(ValidationRuleDefinition definition)
    {
        try
        {
            var rule = new ValidationRule
            {
                RuleName = definition.RuleName,
                Description = definition.Description,
                EntityType = definition.EntityType,
                FieldName = definition.FieldName,
                RuleType = definition.RuleType,
                Severity = definition.Severity,
                RuleExpression = definition.RuleExpression,
                Parameters = definition.Parameters,
                RuleGroup = definition.RuleGroup,
                IsEnabled = definition.IsEnabled,
                Priority = definition.Priority,
                ErrorMessage = definition.ErrorMessage,
                Category = definition.Category,
                Tags = definition.Tags,
                CreatedBy = "System" // In production, get from current user context
            };

            _rules[rule.Id] = rule;
            _ruleStats[rule.Id] = new ValidationRuleStatistics();

            _logger.LogInformation("Validation rule created: {RuleId} - {RuleName}", rule.Id, rule.RuleName);
            
            return rule.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating validation rule: {RuleName}", definition.RuleName);
            throw;
        }
    }

    public async Task<bool> UpdateValidationRuleAsync(Guid ruleId, ValidationRuleDefinition definition)
    {
        try
        {
            if (!_rules.TryGetValue(ruleId, out var existingRule))
            {
                return false;
            }

            // Update rule properties
            existingRule.RuleName = definition.RuleName;
            existingRule.Description = definition.Description;
            existingRule.EntityType = definition.EntityType;
            existingRule.FieldName = definition.FieldName;
            existingRule.RuleType = definition.RuleType;
            existingRule.Severity = definition.Severity;
            existingRule.RuleExpression = definition.RuleExpression;
            existingRule.Parameters = definition.Parameters;
            existingRule.RuleGroup = definition.RuleGroup;
            existingRule.IsEnabled = definition.IsEnabled;
            existingRule.Priority = definition.Priority;
            existingRule.ErrorMessage = definition.ErrorMessage;
            existingRule.Category = definition.Category;
            existingRule.Tags = definition.Tags;
            existingRule.LastModified = DateTime.UtcNow;
            existingRule.LastModifiedBy = "System";
            existingRule.Version++;

            _logger.LogInformation("Validation rule updated: {RuleId} - {RuleName}", ruleId, existingRule.RuleName);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validation rule: {RuleId}", ruleId);
            return false;
        }
    }

    public async Task<bool> DeleteValidationRuleAsync(Guid ruleId)
    {
        try
        {
            var removed = _rules.TryRemove(ruleId, out var rule);
            if (removed)
            {
                _ruleStats.TryRemove(ruleId, out _);
                _logger.LogInformation("Validation rule deleted: {RuleId} - {RuleName}", ruleId, rule?.RuleName);
            }
            
            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting validation rule: {RuleId}", ruleId);
            return false;
        }
    }

    public async Task<List<ValidationRule>> GetValidationRulesAsync(string? entityType = null)
    {
        var rules = _rules.Values.AsEnumerable();
        
        if (!string.IsNullOrEmpty(entityType))
        {
            rules = rules.Where(r => r.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));
        }
        
        return rules.OrderBy(r => r.Priority).ThenBy(r => r.RuleName).ToList();
    }

    public async Task<ValidationRule?> GetValidationRuleAsync(Guid ruleId)
    {
        return _rules.GetValueOrDefault(ruleId);
    }

    public async Task<ValidationResult> ValidateEntityAsync<T>(T entity) where T : class
    {
        return await ValidateEntityAsync(entity, null);
    }

    public async Task<ValidationResult> ValidateEntityAsync<T>(T entity, string? ruleGroup) where T : class
    {
        var startTime = DateTime.UtcNow;
        Interlocked.Increment(ref _totalValidations);

        var result = new ValidationResult
        {
            EntityType = typeof(T).Name,
            EntityId = GetEntityId(entity),
            IsValid = true
        };

        try
        {
            var entityType = typeof(T).Name;
            var applicableRules = _rules.Values
                .Where(r => r.IsEnabled && 
                           r.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase) &&
                           (ruleGroup == null || r.RuleGroup == ruleGroup))
                .OrderBy(r => r.Priority)
                .ToList();

            result.RulesExecuted = applicableRules.Count;

            foreach (var rule in applicableRules)
            {
                var ruleResult = await ExecuteValidationRuleAsync(entity, rule);
                
                if (!ruleResult.IsValid)
                {
                    result.IsValid = false;
                    
                    if (rule.Severity == ValidationSeverity.Error || rule.Severity == ValidationSeverity.Critical)
                    {
                        result.Errors.AddRange(ruleResult.Errors);
                    }
                    else
                    {
                        result.Warnings.AddRange(ruleResult.Warnings);
                    }
                }

                // Update rule statistics
                UpdateRuleStatistics(rule.Id, ruleResult.IsValid, DateTime.UtcNow - startTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating entity of type {EntityType}", typeof(T).Name);
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                ErrorMessage = $"Validation failed: {ex.Message}",
                Severity = ValidationSeverity.Critical
            });
        }
        finally
        {
            result.ValidationDuration = DateTime.UtcNow - startTime;
        }

        // Store validation result for history
        _validationHistory.Add(result);
        if (_validationHistory.Count > _options.MaxValidationHistorySize)
        {
            _validationHistory.RemoveRange(0, _validationHistory.Count - _options.MaxValidationHistorySize);
        }

        return result;
    }

    public async Task<List<ValidationResult>> ValidateBatchAsync<T>(IEnumerable<T> entities) where T : class
    {
        var results = new List<ValidationResult>();
        var entityList = entities.ToList();

        _logger.LogInformation("Starting batch validation for {Count} entities of type {EntityType}", 
            entityList.Count, typeof(T).Name);

        var tasks = entityList.Select(async entity => await ValidateEntityAsync(entity));
        results.AddRange(await Task.WhenAll(tasks));

        _logger.LogInformation("Batch validation completed for {Count} entities. Valid: {ValidCount}, Invalid: {InvalidCount}",
            entityList.Count, 
            results.Count(r => r.IsValid),
            results.Count(r => !r.IsValid));

        return results;
    }

    public async Task<ValidationResult> ValidateFieldAsync<T>(T entity, string fieldName, object value) where T : class
    {
        var result = new ValidationResult
        {
            EntityType = typeof(T).Name,
            EntityId = GetEntityId(entity),
            IsValid = true
        };

        var entityType = typeof(T).Name;
        var fieldRules = _rules.Values
            .Where(r => r.IsEnabled && 
                       r.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase) &&
                       r.FieldName?.Equals(fieldName, StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(r => r.Priority)
            .ToList();

        foreach (var rule in fieldRules)
        {
            var ruleResult = await ExecuteFieldValidationRuleAsync(entity, fieldName, value, rule);
            
            if (!ruleResult.IsValid)
            {
                result.IsValid = false;
                
                if (rule.Severity == ValidationSeverity.Error || rule.Severity == ValidationSeverity.Critical)
                {
                    result.Errors.AddRange(ruleResult.Errors);
                }
                else
                {
                    result.Warnings.AddRange(ruleResult.Warnings);
                }
            }
        }

        return result;
    }

    public async Task<DataQualityReport> AssessDataQualityAsync<T>(IEnumerable<T> entities) where T : class
    {
        var entityList = entities.ToList();
        var entityType = typeof(T).Name;
        
        var report = new DataQualityReport
        {
            EntityType = entityType,
            TotalRecords = entityList.Count
        };

        var validationResults = await ValidateBatchAsync(entityList);
        report.ValidRecords = validationResults.Count(r => r.IsValid);
        report.InvalidRecords = entityList.Count - report.ValidRecords;
        report.QualityScore = entityList.Count > 0 ? (double)report.ValidRecords / entityList.Count * 100 : 100;

        // Analyze field-level quality
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            var fieldMetric = AnalyzeFieldQuality(entityList, property);
            report.FieldMetrics[property.Name] = fieldMetric;
        }

        // Calculate quality dimensions
        report.Dimensions = CalculateQualityDimensions(report.FieldMetrics, validationResults);

        // Identify quality issues
        report.QualityIssues = IdentifyQualityIssues(report.FieldMetrics, validationResults);

        return report;
    }

    public async Task<DataQualityReport> AssessDataQualityAsync(string entityType, DateTime? since = null)
    {
        // In a real implementation, this would query the database
        // For demo, return sample data
        return new DataQualityReport
        {
            EntityType = entityType,
            TotalRecords = 1000,
            ValidRecords = 950,
            InvalidRecords = 50,
            QualityScore = 95.0,
            Dimensions = new DataQualityDimensions
            {
                Completeness = 98.5,
                Validity = 95.0,
                Uniqueness = 99.2,
                Consistency = 94.8,
                Accuracy = 96.5,
                Timeliness = 92.0
            }
        };
    }

    public async Task<DataQualityTrend> GetDataQualityTrendAsync(string entityType, TimeSpan period)
    {
        // Generate sample trend data
        var trend = new DataQualityTrend
        {
            EntityType = entityType,
            Period = period
        };

        var days = Math.Min((int)period.TotalDays, 30);
        for (int i = days; i >= 0; i--)
        {
            trend.Snapshots.Add(new DataQualitySnapshot
            {
                Timestamp = DateTime.UtcNow.AddDays(-i),
                QualityScore = 90 + Random.Shared.NextDouble() * 10,
                TotalRecords = 1000 + Random.Shared.Next(-100, 100),
                IssueCount = Random.Shared.Next(5, 50),
                DimensionScores = new Dictionary<string, double>
                {
                    ["Completeness"] = 95 + Random.Shared.NextDouble() * 5,
                    ["Validity"] = 90 + Random.Shared.NextDouble() * 10,
                    ["Uniqueness"] = 98 + Random.Shared.NextDouble() * 2
                }
            });
        }

        trend.Analysis = AnalyzeTrend(trend.Snapshots);
        
        return trend;
    }

    public async Task<DataRepairResult> RepairDataAsync<T>(T entity, DataRepairOptions? options = null) where T : class
    {
        options ??= new DataRepairOptions();
        
        var result = new DataRepairResult
        {
            OriginalEntity = entity
        };

        try
        {
            var validationResult = await ValidateEntityAsync(entity);
            if (validationResult.IsValid)
            {
                result.IsRepaired = true;
                result.RepairedEntity = entity;
                return result;
            }

            var repairedEntity = CloneEntity(entity);
            var repairActions = new List<DataRepairAction>();

            foreach (var error in validationResult.Errors)
            {
                if (options.ExcludeFields.Contains(error.FieldName))
                    continue;

                var repairAction = await TryRepairField(repairedEntity, error, options);
                if (repairAction != null && repairAction.Confidence >= options.MinConfidenceThreshold)
                {
                    repairActions.Add(repairAction);
                    result.FieldsRepaired.Add(error.FieldName);
                }
                else
                {
                    result.UnrepairableIssues.Add($"{error.FieldName}: {error.ErrorMessage}");
                }
            }

            result.RepairActions = repairActions;
            result.RepairedEntity = repairedEntity;
            result.IsRepaired = repairActions.Any() && !result.UnrepairableIssues.Any();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error repairing entity of type {EntityType}", typeof(T).Name);
            result.UnrepairableIssues.Add($"Repair failed: {ex.Message}");
        }

        return result;
    }

    public async Task<List<DataRepairResult>> RepairBatchAsync<T>(IEnumerable<T> entities, DataRepairOptions? options = null) where T : class
    {
        var tasks = entities.Select(async entity => await RepairDataAsync(entity, options));
        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<DataCleansingReport> PerformDataCleansingAsync(string entityType, DataCleansingOptions options)
    {
        var report = new DataCleansingReport
        {
            EntityType = entityType,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // This is a simplified implementation
            // In production, this would work with actual data repositories
            
            report.TotalRecords = 1000; // Mock data
            report.RecordsProcessed = 1000;

            var actions = new List<DataCleansingAction>();

            if (options.RemoveDuplicates)
            {
                actions.Add(new DataCleansingAction
                {
                    ActionType = "RemoveDuplicates",
                    Description = "Removed duplicate records",
                    AffectedRecords = 15
                });
                report.RecordsRemoved += 15;
            }

            if (options.StandardizeFormats)
            {
                actions.Add(new DataCleansingAction
                {
                    ActionType = "StandardizeFormats",
                    Description = "Standardized date and number formats",
                    AffectedRecords = 150
                });
                report.RecordsCleansed += 150;
            }

            if (options.FillMissingValues)
            {
                actions.Add(new DataCleansingAction
                {
                    ActionType = "FillMissingValues",
                    Description = "Filled missing values using default strategies",
                    AffectedRecords = 75
                });
                report.RecordsCleansed += 75;
            }

            report.Actions = actions;
            report.IssuesFixed = new Dictionary<string, int>
            {
                ["Duplicates"] = 15,
                ["Format Issues"] = 150,
                ["Missing Values"] = 75
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing data cleansing for {EntityType}", entityType);
            throw;
        }
        finally
        {
            report.CompletedAt = DateTime.UtcNow;
        }

        return report;
    }

    public async Task<List<DataAnomaly>> DetectAnomaliesAsync<T>(IEnumerable<T> entities) where T : class
    {
        var anomalies = new List<DataAnomaly>();
        var entityList = entities.ToList();
        var entityType = typeof(T).Name;

        // Statistical anomaly detection for numeric fields
        var numericProperties = typeof(T).GetProperties()
            .Where(p => p.PropertyType.IsNumericType())
            .ToList();

        foreach (var property in numericProperties)
        {
            var values = entityList
                .Select(e => property.GetValue(e))
                .Where(v => v != null)
                .Cast<double>()
                .ToList();

            if (values.Count < 3) continue;

            var mean = values.Average();
            var stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
            var threshold = _options.AnomalyDetectionThreshold * stdDev;

            for (int i = 0; i < entityList.Count; i++)
            {
                var value = property.GetValue(entityList[i]);
                if (value != null && Math.Abs((double)value - mean) > threshold)
                {
                    var anomaly = new DataAnomaly
                    {
                        EntityType = entityType,
                        EntityId = GetEntityId(entityList[i]),
                        AnomalyType = DataAnomalyType.Outlier,
                        Description = $"Statistical outlier detected in {property.Name}",
                        FieldName = property.Name,
                        AnomalousValue = value,
                        ExpectedValue = $"Around {mean:F2} (±{stdDev:F2})",
                        AnomalyScore = Math.Abs((double)value - mean) / stdDev
                    };

                    anomalies.Add(anomaly);
                    _anomalies[anomaly.Id] = anomaly;
                }
            }
        }

        return anomalies;
    }

    public async Task<DataAnomalyReport> GetAnomalyReportAsync(string entityType, DateTime? since = null)
    {
        since ??= DateTime.UtcNow.AddDays(-30);

        var relevantAnomalies = _anomalies.Values
            .Where(a => a.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase) &&
                       a.DetectedAt >= since)
            .ToList();

        return new DataAnomalyReport
        {
            EntityType = entityType,
            TotalAnomalies = relevantAnomalies.Count,
            NewAnomalies = relevantAnomalies.Count(a => a.Status == DataAnomalyStatus.Detected),
            ResolvedAnomalies = relevantAnomalies.Count(a => a.Status == DataAnomalyStatus.Resolved),
            AnomaliesByType = relevantAnomalies.GroupBy(a => a.AnomalyType)
                .ToDictionary(g => g.Key, g => g.Count()),
            AnomaliesByField = relevantAnomalies.GroupBy(a => a.FieldName)
                .ToDictionary(g => g.Key, g => g.Count()),
            HighSeverityAnomalies = relevantAnomalies
                .Where(a => a.AnomalyScore > 3.0)
                .OrderByDescending(a => a.AnomalyScore)
                .Take(10)
                .ToList()
        };
    }

    public async Task<bool> MarkAnomalyAsReviewedAsync(Guid anomalyId, string reviewedBy, string? comments = null)
    {
        if (_anomalies.TryGetValue(anomalyId, out var anomaly))
        {
            anomaly.Status = DataAnomalyStatus.UnderReview;
            anomaly.ReviewedBy = reviewedBy;
            anomaly.ReviewedAt = DateTime.UtcNow;
            anomaly.ReviewComments = comments;

            _logger.LogInformation("Anomaly {AnomalyId} marked as reviewed by {ReviewedBy}", anomalyId, reviewedBy);
            return true;
        }

        return false;
    }

    public async Task<bool> EnableValidationRuleAsync(Guid ruleId)
    {
        if (_rules.TryGetValue(ruleId, out var rule))
        {
            rule.IsEnabled = true;
            rule.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Validation rule enabled: {RuleId}", ruleId);
            return true;
        }

        return false;
    }

    public async Task<bool> DisableValidationRuleAsync(Guid ruleId)
    {
        if (_rules.TryGetValue(ruleId, out var rule))
        {
            rule.IsEnabled = false;
            rule.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Validation rule disabled: {RuleId}", ruleId);
            return true;
        }

        return false;
    }

    public async Task<ValidationStatistics> GetValidationStatisticsAsync(TimeSpan? period = null)
    {
        period ??= TimeSpan.FromDays(30);
        var since = DateTime.UtcNow - period.Value;

        var recentValidations = _validationHistory
            .Where(v => v.ValidationTimestamp >= since)
            .ToList();

        return new ValidationStatistics
        {
            Period = period.Value,
            TotalValidations = recentValidations.Count,
            SuccessfulValidations = recentValidations.Count(v => v.IsValid),
            FailedValidations = recentValidations.Count(v => !v.IsValid),
            AverageValidationTime = recentValidations.Any() 
                ? TimeSpan.FromTicks((long)recentValidations.Average(v => v.ValidationDuration.Ticks))
                : TimeSpan.Zero,
            ValidationsByEntityType = recentValidations
                .GroupBy(v => v.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsBySeverity = recentValidations
                .SelectMany(v => v.Errors)
                .GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopFailingRules = _ruleStats.Values
                .Where(s => s.ExecutionCount > 0)
                .OrderByDescending(s => s.FailureCount)
                .Take(10)
                .Select(s => new ValidationRulePerformance
                {
                    RuleId = _rules.FirstOrDefault(r => _ruleStats[r.Key] == s).Key,
                    RuleName = _rules.FirstOrDefault(r => _ruleStats[r.Key] == s).Value?.RuleName ?? "",
                    ExecutionCount = s.ExecutionCount,
                    FailureCount = s.FailureCount,
                    FailureRate = (double)s.FailureCount / s.ExecutionCount * 100,
                    AverageExecutionTime = s.AverageExecutionTime
                })
                .ToList()
        };
    }

    public async Task<List<ValidationPerformanceMetric>> GetValidationPerformanceAsync()
    {
        return new List<ValidationPerformanceMetric>
        {
            new()
            {
                MetricName = "TotalValidations",
                Value = _totalValidations,
                Unit = "count"
            },
            new()
            {
                MetricName = "ActiveRules",
                Value = _rules.Values.Count(r => r.IsEnabled),
                Unit = "count"
            },
            new()
            {
                MetricName = "AverageRuleExecutionTime",
                Value = _ruleStats.Values.Any() 
                    ? _ruleStats.Values.Average(s => s.AverageExecutionTime.TotalMilliseconds)
                    : 0,
                Unit = "milliseconds"
            }
        };
    }

    // Private helper methods
    private async Task<ValidationResult> ExecuteValidationRuleAsync<T>(T entity, ValidationRule rule) where T : class
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            var isValid = rule.RuleType switch
            {
                ValidationRuleType.Required => ValidateRequired(entity, rule),
                ValidationRuleType.Format => ValidateFormat(entity, rule),
                ValidationRuleType.Range => ValidateRange(entity, rule),
                ValidationRuleType.Length => ValidateLength(entity, rule),
                ValidationRuleType.Uniqueness => await ValidateUniqueness(entity, rule),
                ValidationRuleType.BusinessRule => ValidateBusinessRule(entity, rule),
                _ => true
            };

            if (!isValid)
            {
                result.IsValid = false;
                
                var error = new ValidationError
                {
                    RuleId = rule.Id.ToString(),
                    RuleName = rule.RuleName,
                    FieldName = rule.FieldName ?? "",
                    ErrorMessage = rule.ErrorMessage ?? rule.Description,
                    Severity = rule.Severity,
                    ActualValue = GetFieldValue(entity, rule.FieldName)
                };

                if (rule.Severity == ValidationSeverity.Error || rule.Severity == ValidationSeverity.Critical)
                {
                    result.Errors.Add(error);
                }
                else
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        RuleId = rule.Id.ToString(),
                        RuleName = rule.RuleName,
                        FieldName = rule.FieldName ?? "",
                        WarningMessage = rule.ErrorMessage ?? rule.Description,
                        Value = GetFieldValue(entity, rule.FieldName)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing validation rule {RuleId}: {RuleName}", rule.Id, rule.RuleName);
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                RuleId = rule.Id.ToString(),
                RuleName = rule.RuleName,
                ErrorMessage = $"Rule execution failed: {ex.Message}",
                Severity = ValidationSeverity.Critical
            });
        }

        return result;
    }

    private async Task<ValidationResult> ExecuteFieldValidationRuleAsync<T>(T entity, string fieldName, object value, ValidationRule rule) where T : class
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            var isValid = rule.RuleType switch
            {
                ValidationRuleType.Required => value != null && !string.IsNullOrWhiteSpace(value.ToString()),
                ValidationRuleType.Format => ValidateFieldFormat(value, rule.RuleExpression),
                ValidationRuleType.Range => ValidateFieldRange(value, rule.Parameters),
                ValidationRuleType.Length => ValidateFieldLength(value, rule.Parameters),
                _ => true
            };

            if (!isValid)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    RuleId = rule.Id.ToString(),
                    RuleName = rule.RuleName,
                    FieldName = fieldName,
                    ErrorMessage = rule.ErrorMessage ?? rule.Description,
                    Severity = rule.Severity,
                    ActualValue = value
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing field validation rule {RuleId} for field {FieldName}", rule.Id, fieldName);
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                RuleId = rule.Id.ToString(),
                RuleName = rule.RuleName,
                FieldName = fieldName,
                ErrorMessage = $"Field validation failed: {ex.Message}",
                Severity = ValidationSeverity.Critical
            });
        }

        return result;
    }

    private bool ValidateRequired<T>(T entity, ValidationRule rule) where T : class
    {
        if (string.IsNullOrEmpty(rule.FieldName))
            return true;

        var value = GetFieldValue(entity, rule.FieldName);
        return value != null && !string.IsNullOrWhiteSpace(value.ToString());
    }

    private bool ValidateFormat<T>(T entity, ValidationRule rule) where T : class
    {
        if (string.IsNullOrEmpty(rule.FieldName))
            return true;

        var value = GetFieldValue(entity, rule.FieldName);
        if (value == null)
            return true;

        return ValidateFieldFormat(value, rule.RuleExpression);
    }

    private bool ValidateFieldFormat(object value, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        try
        {
            return Regex.IsMatch(value.ToString() ?? "", pattern);
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateRange<T>(T entity, ValidationRule rule) where T : class
    {
        if (string.IsNullOrEmpty(rule.FieldName))
            return true;

        var value = GetFieldValue(entity, rule.FieldName);
        return ValidateFieldRange(value, rule.Parameters);
    }

    private bool ValidateFieldRange(object value, Dictionary<string, object> parameters)
    {
        if (value == null || !parameters.ContainsKey("min") || !parameters.ContainsKey("max"))
            return true;

        if (double.TryParse(value.ToString(), out var numericValue) &&
            double.TryParse(parameters["min"].ToString(), out var min) &&
            double.TryParse(parameters["max"].ToString(), out var max))
        {
            return numericValue >= min && numericValue <= max;
        }

        return true;
    }

    private bool ValidateLength<T>(T entity, ValidationRule rule) where T : class
    {
        if (string.IsNullOrEmpty(rule.FieldName))
            return true;

        var value = GetFieldValue(entity, rule.FieldName);
        return ValidateFieldLength(value, rule.Parameters);
    }

    private bool ValidateFieldLength(object value, Dictionary<string, object> parameters)
    {
        if (value == null)
            return true;

        var stringValue = value.ToString() ?? "";
        var length = stringValue.Length;

        if (parameters.ContainsKey("minLength") && 
            int.TryParse(parameters["minLength"].ToString(), out var minLength))
        {
            if (length < minLength)
                return false;
        }

        if (parameters.ContainsKey("maxLength") && 
            int.TryParse(parameters["maxLength"].ToString(), out var maxLength))
        {
            if (length > maxLength)
                return false;
        }

        return true;
    }

    private async Task<bool> ValidateUniqueness<T>(T entity, ValidationRule rule) where T : class
    {
        // Simplified uniqueness validation
        // In production, this would check against the database
        return true;
    }

    private bool ValidateBusinessRule<T>(T entity, ValidationRule rule) where T : class
    {
        // Simplified business rule validation
        // In production, this would use a proper rule engine
        try
        {
            // This is a placeholder - in reality, you'd parse and execute the rule expression
            return !rule.RuleExpression.Contains("FAIL");
        }
        catch
        {
            return false;
        }
    }

    private object? GetFieldValue<T>(T entity, string? fieldName) where T : class
    {
        if (string.IsNullOrEmpty(fieldName))
            return null;

        try
        {
            var property = typeof(T).GetProperty(fieldName);
            return property?.GetValue(entity);
        }
        catch
        {
            return null;
        }
    }

    private object? GetEntityId<T>(T entity) where T : class
    {
        try
        {
            var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("ID");
            return idProperty?.GetValue(entity);
        }
        catch
        {
            return null;
        }
    }

    private void UpdateRuleStatistics(Guid ruleId, bool isValid, TimeSpan executionTime)
    {
        if (_ruleStats.TryGetValue(ruleId, out var stats))
        {
            stats.ExecutionCount++;
            if (isValid)
                stats.SuccessCount++;
            else
                stats.FailureCount++;

            stats.AverageExecutionTime = TimeSpan.FromTicks(
                (stats.AverageExecutionTime.Ticks * (stats.ExecutionCount - 1) + executionTime.Ticks) / stats.ExecutionCount);
            
            stats.LastExecuted = DateTime.UtcNow;
        }
    }

    private DataQualityMetric AnalyzeFieldQuality<T>(List<T> entities, PropertyInfo property) where T : class
    {
        var metric = new DataQualityMetric
        {
            FieldName = property.Name,
            TotalValues = entities.Count
        };

        var values = entities.Select(e => property.GetValue(e)).ToList();
        
        metric.NullValues = values.Count(v => v == null);
        metric.EmptyValues = values.Count(v => v != null && string.IsNullOrWhiteSpace(v.ToString()));
        metric.ValidValues = metric.TotalValues - metric.NullValues - metric.EmptyValues;
        
        metric.Completeness = metric.TotalValues > 0 ? (double)metric.ValidValues / metric.TotalValues * 100 : 100;
        metric.Validity = 100; // Simplified - would need actual validation
        
        // Check for duplicates if applicable
        var nonNullValues = values.Where(v => v != null).ToList();
        metric.DuplicateValues = nonNullValues.Count - nonNullValues.Distinct().Count();
        metric.Uniqueness = nonNullValues.Any() ? (double)nonNullValues.Distinct().Count() / nonNullValues.Count * 100 : 100;

        return metric;
    }

    private DataQualityDimensions CalculateQualityDimensions(Dictionary<string, DataQualityMetric> fieldMetrics, List<ValidationResult> validationResults)
    {
        return new DataQualityDimensions
        {
            Completeness = fieldMetrics.Values.Any() ? fieldMetrics.Values.Average(m => m.Completeness) : 100,
            Validity = validationResults.Any() ? (double)validationResults.Count(r => r.IsValid) / validationResults.Count * 100 : 100,
            Uniqueness = fieldMetrics.Values.Any() ? fieldMetrics.Values.Average(m => m.Uniqueness) : 100,
            Consistency = 95, // Simplified calculation
            Accuracy = 96,    // Would need reference data
            Timeliness = 92   // Would need timestamp analysis
        };
    }

    private List<DataQualityIssue> IdentifyQualityIssues(Dictionary<string, DataQualityMetric> fieldMetrics, List<ValidationResult> validationResults)
    {
        var issues = new List<DataQualityIssue>();

        foreach (var metric in fieldMetrics.Values)
        {
            if (metric.Completeness < 90)
            {
                issues.Add(new DataQualityIssue
                {
                    IssueType = "Completeness",
                    Description = $"Field {metric.FieldName} has low completeness ({metric.Completeness:F1}%)",
                    FieldName = metric.FieldName,
                    Severity = metric.Completeness < 70 ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    AffectedRecords = metric.NullValues + metric.EmptyValues,
                    SuggestedFix = "Review data collection process and add validation rules"
                });
            }
        }

        return issues;
    }

    private DataQualityTrendAnalysis AnalyzeTrend(List<DataQualitySnapshot> snapshots)
    {
        if (snapshots.Count < 2)
        {
            return new DataQualityTrendAnalysis
            {
                Direction = DataQualityTrendDirection.Stable,
                KeyInsights = { "Insufficient data for trend analysis" }
            };
        }

        var scores = snapshots.Select(s => s.QualityScore).ToList();
        var slope = CalculateLinearRegression(scores);
        
        var direction = slope > 0.1 ? DataQualityTrendDirection.Improving :
                       slope < -0.1 ? DataQualityTrendDirection.Declining :
                       DataQualityTrendDirection.Stable;

        return new DataQualityTrendAnalysis
        {
            Direction = direction,
            TrendSlope = slope,
            VariationCoefficient = scores.Any() ? scores.StandardDeviation() / scores.Average() : 0,
            KeyInsights = GenerateKeyInsights(direction, slope),
            Recommendations = GenerateRecommendations(direction)
        };
    }

    private double CalculateLinearRegression(List<double> values)
    {
        if (values.Count < 2) return 0;

        var n = values.Count;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        var y = values;

        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
        var sumX2 = x.Sum(xi => xi * xi);

        return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    }

    private List<string> GenerateKeyInsights(DataQualityTrendDirection direction, double slope)
    {
        return direction switch
        {
            DataQualityTrendDirection.Improving => new List<string> { "Data quality is improving over time", $"Improvement rate: {slope:F3}/day" },
            DataQualityTrendDirection.Declining => new List<string> { "Data quality is declining", $"Decline rate: {slope:F3}/day" },
            _ => new List<string> { "Data quality remains stable" }
        };
    }

    private List<string> GenerateRecommendations(DataQualityTrendDirection direction)
    {
        return direction switch
        {
            DataQualityTrendDirection.Declining => new List<string> 
            { 
                "Review data validation rules",
                "Investigate data sources",
                "Implement additional quality checks"
            },
            DataQualityTrendDirection.Improving => new List<string> 
            { 
                "Continue current quality improvement initiatives",
                "Consider expanding successful practices to other areas"
            },
            _ => new List<string> 
            { 
                "Monitor for any changes in trend",
                "Consider proactive quality improvement measures"
            }
        };
    }

    private async Task<DataRepairAction?> TryRepairField<T>(T entity, ValidationError error, DataRepairOptions options) where T : class
    {
        // Simplified repair logic - in production, this would be much more sophisticated
        try
        {
            var property = typeof(T).GetProperty(error.FieldName);
            if (property == null || !property.CanWrite)
                return null;

            var currentValue = property.GetValue(entity);
            object? repairedValue = null;
            var actionType = DataRepairActionType.Fill;
            var confidence = 0.5;

            // Example repair strategies
            if (currentValue == null || string.IsNullOrWhiteSpace(currentValue.ToString()))
            {
                // Fill missing values
                repairedValue = GetDefaultValue(property.PropertyType);
                actionType = DataRepairActionType.Fill;
                confidence = 0.7;
            }
            else if (error.RuleName.Contains("Format"))
            {
                // Try to correct format
                repairedValue = TryCorrectFormat(currentValue, property.PropertyType);
                actionType = DataRepairActionType.Correct;
                confidence = 0.6;
            }

            if (repairedValue != null)
            {
                property.SetValue(entity, repairedValue);
                
                return new DataRepairAction
                {
                    FieldName = error.FieldName,
                    ActionType = actionType,
                    OriginalValue = currentValue,
                    RepairedValue = repairedValue,
                    Description = $"Repaired {error.FieldName} using {actionType}",
                    Confidence = confidence
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to repair field {FieldName}", error.FieldName);
        }

        return null;
    }

    private object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
            return "";
        if (type == typeof(int) || type == typeof(int?))
            return 0;
        if (type == typeof(decimal) || type == typeof(decimal?))
            return 0m;
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return DateTime.UtcNow;
        if (type == typeof(bool) || type == typeof(bool?))
            return false;
        
        return null;
    }

    private object? TryCorrectFormat(object value, Type targetType)
    {
        try
        {
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                if (DateTime.TryParse(value.ToString(), out var dateValue))
                    return dateValue;
            }
            else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                if (decimal.TryParse(value.ToString(), out var decimalValue))
                    return decimalValue;
            }
            else if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(value.ToString(), out var intValue))
                    return intValue;
            }
        }
        catch
        {
            // Ignore conversion errors
        }

        return null;
    }

    private T CloneEntity<T>(T entity) where T : class
    {
        // Simplified cloning - in production, use proper deep cloning
        var json = JsonSerializer.Serialize(entity);
        return JsonSerializer.Deserialize<T>(json) ?? entity;
    }

    private void InitializeSampleRules()
    {
        var sampleRules = new[]
        {
            new ValidationRule
            {
                RuleName = "Contract_Number_Required",
                Description = "Contract number is required",
                EntityType = "PurchaseContract",
                FieldName = "ContractNumber",
                RuleType = ValidationRuleType.Required,
                Severity = ValidationSeverity.Error,
                RuleExpression = "ContractNumber != null && ContractNumber != ''",
                ErrorMessage = "Contract number cannot be empty",
                Category = "Basic Validation",
                CreatedBy = "System"
            },
            new ValidationRule
            {
                RuleName = "Quantity_Range",
                Description = "Quantity must be positive and within reasonable limits",
                EntityType = "PurchaseContract",
                FieldName = "Quantity",
                RuleType = ValidationRuleType.Range,
                Severity = ValidationSeverity.Error,
                RuleExpression = "Quantity > 0 && Quantity <= 1000000",
                Parameters = new Dictionary<string, object> { ["min"] = 0, ["max"] = 1000000 },
                ErrorMessage = "Quantity must be between 0 and 1,000,000",
                Category = "Business Rules",
                CreatedBy = "System"
            },
            new ValidationRule
            {
                RuleName = "Email_Format",
                Description = "Email must be in valid format",
                EntityType = "User",
                FieldName = "Email",
                RuleType = ValidationRuleType.Format,
                Severity = ValidationSeverity.Error,
                RuleExpression = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                ErrorMessage = "Please enter a valid email address",
                Category = "Format Validation",
                CreatedBy = "System"
            }
        };

        foreach (var rule in sampleRules)
        {
            _rules[rule.Id] = rule;
            _ruleStats[rule.Id] = new ValidationRuleStatistics();
        }

        _logger.LogInformation("Data validation service initialized with {RuleCount} sample rules", sampleRules.Length);
    }
}

/// <summary>
/// Data validation options
/// </summary>
public class DataValidationOptions
{
    public int MaxValidationHistorySize { get; set; } = 10000;
    public double AnomalyDetectionThreshold { get; set; } = 2.0; // Standard deviations
    public bool EnablePerformanceTracking { get; set; } = true;
    public bool EnableAnomalyDetection { get; set; } = true;
    public TimeSpan ValidationCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Extension methods for statistical calculations
/// </summary>
public static class StatisticsExtensions
{
    public static double StandardDeviation(this IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count <= 1) return 0;

        var mean = valueList.Average();
        var variance = valueList.Average(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(variance);
    }

    private static ValidationSeverity ConvertToValidationSeverity(ValidationSeverity severity)
    {
        return severity; // No conversion needed since both are the same enum
    }
}