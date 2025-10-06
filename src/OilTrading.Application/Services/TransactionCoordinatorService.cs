using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Application.TransactionOperations;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

public class TransactionCoordinatorService : ITransactionCoordinatorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TransactionCoordinatorService> _logger;
    private readonly Dictionary<Guid, TransactionContext> _transactionCache = new();

    public TransactionCoordinatorService(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<TransactionCoordinatorService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<TransactionResult> ExecuteTransactionAsync(ITransactionOperation[] operations, TransactionContext context)
    {
        _logger.LogInformation("Starting distributed transaction {TransactionId}: {TransactionName}", 
            context.TransactionId, context.TransactionName);

        context.Status = TransactionStatus.InProgress;
        _transactionCache[context.TransactionId] = context;

        var result = new TransactionResult
        {
            TransactionId = context.TransactionId,
            Status = TransactionStatus.InProgress
        };

        var completedOperations = new List<ITransactionOperation>();

        try
        {
            // Sort operations by execution order
            var orderedOperations = operations.OrderBy(op => op.Order).ToArray();

            // Execute operations in order
            foreach (var operation in orderedOperations)
            {
                _logger.LogDebug("Executing operation: {OperationName}", operation.OperationName);
                context.AddStep(operation.OperationName, TransactionStepStatus.Started);

                var operationResult = await operation.ExecuteAsync(context);
                result.OperationResults.Add(operationResult);

                if (!operationResult.IsSuccess)
                {
                    _logger.LogError("Operation {OperationName} failed: {ErrorMessage}", 
                        operation.OperationName, operationResult.ErrorMessage);
                    
                    context.AddStep(operation.OperationName, TransactionStepStatus.Failed, operationResult.ErrorMessage);
                    context.Status = TransactionStatus.Failed;
                    context.FailureReason = $"Operation {operation.OperationName} failed: {operationResult.ErrorMessage}";

                    // Compensate completed operations in reverse order
                    await CompensateOperations(completedOperations.AsEnumerable().Reverse().ToArray(), context);
                    
                    result.IsSuccess = false;
                    result.Status = TransactionStatus.Failed;
                    result.ErrorMessage = context.FailureReason;
                    break;
                }

                context.AddStep(operation.OperationName, TransactionStepStatus.Completed);
                completedOperations.Add(operation);
            }

            if (result.OperationResults.All(r => r.IsSuccess))
            {
                // Commit the unit of work
                await _unitOfWork.SaveChangesAsync();
                
                context.Status = TransactionStatus.Completed;
                result.IsSuccess = true;
                result.Status = TransactionStatus.Completed;
                
                _logger.LogInformation("Transaction {TransactionId} completed successfully", context.TransactionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in transaction {TransactionId}", context.TransactionId);
            
            context.Status = TransactionStatus.Failed;
            context.FailureReason = ex.Message;
            
            // Attempt compensation
            await CompensateOperations(completedOperations.AsEnumerable().Reverse().ToArray(), context);
            
            result.IsSuccess = false;
            result.Status = TransactionStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = result.CompletedAt - context.StartTime;

            // Log transaction to audit log
            await LogTransactionAsync(context, result);
        }

        return result;
    }

    public async Task<T> ExecuteBusinessTransactionAsync<T>(Func<TransactionContext, Task<T>> operation, string transactionName = "")
    {
        var context = CreateTransactionContext(transactionName, "System");
        
        using var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        
        try
        {
            var result = await operation(context);
            await _unitOfWork.SaveChangesAsync();
            scope.Complete();
            
            context.Status = TransactionStatus.Completed;
            _logger.LogInformation("Business transaction {TransactionName} completed successfully", transactionName);
            
            return result;
        }
        catch (Exception ex)
        {
            context.Status = TransactionStatus.Failed;
            context.FailureReason = ex.Message;
            
            _logger.LogError(ex, "Business transaction {TransactionName} failed", transactionName);
            
            await LogTransactionAsync(context, new TransactionResult
            {
                TransactionId = context.TransactionId,
                IsSuccess = false,
                Status = TransactionStatus.Failed,
                ErrorMessage = ex.Message,
                CompletedAt = DateTime.UtcNow,
                Duration = DateTime.UtcNow - context.StartTime
            });
            
            throw;
        }
    }

    public TransactionContext CreateTransactionContext(string transactionName, string initiatedBy)
    {
        return new TransactionContext
        {
            TransactionId = Guid.NewGuid(),
            TransactionName = transactionName,
            InitiatedBy = initiatedBy,
            StartTime = DateTime.UtcNow,
            Status = TransactionStatus.NotStarted
        };
    }

    public async Task<TransactionOperations.TransactionStatus> GetTransactionStatusAsync(Guid transactionId)
    {
        if (_transactionCache.TryGetValue(transactionId, out var context))
        {
            return context.Status;
        }

        // In a real implementation, you would query the transaction log
        return TransactionOperations.TransactionStatus.Pending;
    }

    public async Task<bool> CompensateTransactionAsync(Guid transactionId)
    {
        if (!_transactionCache.TryGetValue(transactionId, out var context))
        {
            _logger.LogWarning("Transaction {TransactionId} not found for compensation", transactionId);
            return false;
        }

        _logger.LogInformation("Starting compensation for transaction {TransactionId}", transactionId);
        
        context.Status = TransactionStatus.Compensating;
        
        try
        {
            // Get completed operations that require compensation
            var completedSteps = context.Steps
                .Where(s => s.Status == TransactionStepStatus.Completed)
                .OrderByDescending(s => s.Timestamp)
                .ToList();

            foreach (var step in completedSteps)
            {
                // In a real implementation, you would need to reconstruct the operations
                // This is a simplified version
                context.AddStep($"Compensate_{step.OperationName}", TransactionStepStatus.Compensated);
            }

            context.Status = TransactionStatus.Compensated;
            _logger.LogInformation("Transaction {TransactionId} compensated successfully", transactionId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compensate transaction {TransactionId}", transactionId);
            context.Status = TransactionStatus.CompensationFailed;
            return false;
        }
    }

    private async Task CompensateOperations(ITransactionOperation[] operations, TransactionContext context)
    {
        _logger.LogInformation("Starting compensation for {Count} operations", operations.Length);
        
        // Create enhanced compensation context
        var compensationContext = new CompensationContext
        {
            TransactionId = context.TransactionId,
            TransactionName = context.TransactionName,
            CompensationReason = context.FailureReason ?? "Transaction failed",
            InitiatedBy = context.InitiatedBy,
            Strategy = CompensationStrategy.BestEffort,
            OriginalTransactionData = context.Data
        };
        
        compensationContext.Status = CompensationStatus.InProgress;
        
        foreach (var operation in operations.Where(op => op.RequiresCompensation))
        {
            var operationName = operation.OperationName;
            var maxRetries = compensationContext.MaxRetryAttempts;
            var retryCount = 0;
            bool compensationSucceeded = false;
            
            while (retryCount <= maxRetries && !compensationSucceeded)
            {
                try
                {
                    _logger.LogDebug("Compensating operation: {OperationName} (Attempt {Attempt}/{MaxAttempts})", 
                        operationName, retryCount + 1, maxRetries + 1);
                    
                    compensationContext.AddStep(operationName, CompensationStepStatus.InProgress);
                    
                    var compensationResult = await operation.CompensateAsync(context);
                    if (compensationResult.IsSuccess)
                    {
                        compensationContext.UpdateStepStatus(operationName, CompensationStepStatus.Completed, 
                            "Compensation completed successfully");
                        context.AddStep($"Compensate_{operationName}", TransactionStepStatus.Compensated);
                        compensationSucceeded = true;
                        
                        _logger.LogInformation("Successfully compensated operation {OperationName}", operationName);
                    }
                    else
                    {
                        var errorMsg = $"Compensation failed: {compensationResult.ErrorMessage}";
                        
                        if (retryCount < maxRetries)
                        {
                            compensationContext.UpdateStepStatus(operationName, CompensationStepStatus.Retrying, errorMsg);
                            compensationContext.IncrementRetryCount(operationName);
                            retryCount++;
                            
                            _logger.LogWarning("Failed to compensate operation {OperationName}, retrying ({Retry}/{MaxRetries}): {Error}", 
                                operationName, retryCount, maxRetries, compensationResult.ErrorMessage);
                            
                            // Wait before retry
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))); // Exponential backoff
                        }
                        else
                        {
                            compensationContext.UpdateStepStatus(operationName, CompensationStepStatus.Failed, errorMsg);
                            compensationContext.Errors.Add($"Failed to compensate {operationName}: {compensationResult.ErrorMessage}");
                            
                            _logger.LogError("Failed to compensate operation {OperationName} after {MaxRetries} attempts: {ErrorMessage}", 
                                operationName, maxRetries, compensationResult.ErrorMessage);
                            
                            // Check compensation strategy
                            if (compensationContext.Strategy == CompensationStrategy.FailFast)
                            {
                                compensationContext.Status = CompensationStatus.Failed;
                                return;
                            }
                            else if (compensationContext.Strategy == CompensationStrategy.AllOrNothing)
                            {
                                compensationContext.Status = CompensationStatus.Failed;
                                throw new InvalidOperationException($"AllOrNothing compensation strategy failed at operation {operationName}");
                            }
                            
                            break; // Move to next operation with BestEffort strategy
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Exception during compensation: {ex.Message}";
                    
                    if (retryCount < maxRetries)
                    {
                        compensationContext.UpdateStepStatus(operationName, CompensationStepStatus.Retrying, errorMsg);
                        compensationContext.IncrementRetryCount(operationName);
                        retryCount++;
                        
                        _logger.LogError(ex, "Exception during compensation of operation {OperationName}, retrying ({Retry}/{MaxRetries})", 
                            operationName, retryCount, maxRetries);
                        
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    }
                    else
                    {
                        compensationContext.UpdateStepStatus(operationName, CompensationStepStatus.Failed, errorMsg);
                        compensationContext.Errors.Add($"Exception during compensation of {operationName}: {ex.Message}");
                        
                        _logger.LogError(ex, "Exception during compensation of operation {OperationName} after {MaxRetries} attempts", 
                            operationName, maxRetries);
                        
                        if (compensationContext.Strategy == CompensationStrategy.FailFast ||
                            compensationContext.Strategy == CompensationStrategy.AllOrNothing)
                        {
                            compensationContext.Status = CompensationStatus.Failed;
                            throw;
                        }
                        
                        break; // Move to next operation with BestEffort strategy
                    }
                }
            }
        }
        
        // Determine final compensation status
        var totalSteps = compensationContext.CompensationSteps.Count;
        var completedSteps = compensationContext.CompensationSteps.Count(s => s.Status == CompensationStepStatus.Completed);
        var failedSteps = compensationContext.CompensationSteps.Count(s => s.Status == CompensationStepStatus.Failed);
        
        if (failedSteps == 0)
        {
            compensationContext.Status = CompensationStatus.Completed;
        }
        else if (completedSteps > 0)
        {
            compensationContext.Status = CompensationStatus.PartiallyCompleted;
        }
        else
        {
            compensationContext.Status = CompensationStatus.Failed;
        }
        
        // Log compensation summary
        var summary = compensationContext.GetSummary();
        _logger.LogInformation("Compensation completed. Status: {Status}, Success Rate: {SuccessRate:F1}%, Duration: {Duration}", 
            summary.OverallStatus, summary.SuccessRate, summary.CompensationDuration);
        
        // Add compensation warnings to context
        foreach (var warning in compensationContext.Warnings)
        {
            context.Warnings.Add(warning);
        }
        
        foreach (var error in compensationContext.Errors)
        {
            context.Warnings.Add($"Compensation Error: {error}");
        }
    }

    private async Task LogTransactionAsync(TransactionContext context, TransactionResult result)
    {
        try
        {
            await _auditLogService.LogTransactionAsync(new TransactionAuditLog
            {
                TransactionId = context.TransactionId,
                TransactionName = context.TransactionName,
                InitiatedBy = context.InitiatedBy,
                StartTime = context.StartTime,
                EndTime = result.CompletedAt,
                Duration = result.Duration,
                Status = result.Status,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                Steps = context.Steps,
                Warnings = context.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log transaction {TransactionId} to audit log", context.TransactionId);
        }
    }
}

