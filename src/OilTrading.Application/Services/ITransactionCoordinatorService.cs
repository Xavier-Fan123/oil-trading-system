using OilTrading.Application.TransactionOperations;

namespace OilTrading.Application.Services;

public interface ITransactionCoordinatorService
{
    /// <summary>
    /// Execute a distributed transaction with multiple operations
    /// </summary>
    Task<TransactionResult> ExecuteTransactionAsync(ITransactionOperation[] operations, TransactionContext context);
    
    /// <summary>
    /// Execute a business transaction with automatic rollback on failure
    /// </summary>
    Task<T> ExecuteBusinessTransactionAsync<T>(Func<TransactionContext, Task<T>> operation, string transactionName = "");
    
    /// <summary>
    /// Create a new transaction context
    /// </summary>
    TransactionContext CreateTransactionContext(string transactionName, string initiatedBy);
    
    /// <summary>
    /// Get transaction status by ID
    /// </summary>
    Task<TransactionOperations.TransactionStatus> GetTransactionStatusAsync(Guid transactionId);
    
    /// <summary>
    /// Compensate failed transactions
    /// </summary>
    Task<bool> CompensateTransactionAsync(Guid transactionId);
}


