using Microsoft.Extensions.Logging;
using OilTrading.Application.TransactionOperations;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for orchestrating complex business workflows with integrated risk management
/// </summary>
public class BusinessWorkflowService : IBusinessWorkflowService
{
    private readonly ITransactionCoordinatorService _transactionCoordinator;
    private readonly ITransactionOperationFactory _operationFactory;
    private readonly IRealTimeRiskMonitoringService _riskMonitoringService;
    private readonly ILogger<BusinessWorkflowService> _logger;

    public BusinessWorkflowService(
        ITransactionCoordinatorService transactionCoordinator,
        ITransactionOperationFactory operationFactory,
        IRealTimeRiskMonitoringService riskMonitoringService,
        ILogger<BusinessWorkflowService> logger)
    {
        _transactionCoordinator = transactionCoordinator;
        _operationFactory = operationFactory;
        _riskMonitoringService = riskMonitoringService;
        _logger = logger;
    }

    public async Task<WorkflowResult> ExecutePurchaseContractWorkflowAsync(
        PurchaseContractCreationData contractData,
        string initiatedBy,
        WorkflowOptions? options = null)
    {
        _logger.LogInformation("Starting purchase contract workflow for contract {ContractNumber}", contractData.ContractNumber);

        options ??= new WorkflowOptions();

        var workflowResult = new WorkflowResult
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "PurchaseContractCreation",
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy
        };

        try
        {
            // Step 1: Pre-workflow risk assessment
            var preRiskCheck = await PerformPreWorkflowRiskAssessmentAsync(contractData, options);
            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "PreWorkflowRiskAssessment",
                Status = preRiskCheck.IsApproved ? WorkflowStepStatus.Completed : WorkflowStepStatus.Failed,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Data = preRiskCheck
            });

            if (!preRiskCheck.IsApproved && !options.AllowRiskOverride)
            {
                workflowResult.Status = WorkflowStatus.Failed;
                workflowResult.ErrorMessage = $"Pre-workflow risk check failed: {string.Join(", ", preRiskCheck.Violations)}";
                return workflowResult;
            }

            // Step 2: Execute transaction operations
            var context = _transactionCoordinator.CreateTransactionContext("PurchaseContractCreation", initiatedBy);
            context.Data["ContractType"] = "Purchase";
            context.Data["ContractData"] = contractData;
            context.Data["WorkflowId"] = workflowResult.WorkflowId;
            
            if (options.AllowRiskOverride)
            {
                context.Data["AllowRiskOverride"] = true;
                context.Data["BusinessJustification"] = options.BusinessJustification ?? "";
            }

            var operations = _operationFactory.CreateContractCreationOperations("Purchase");
            var transactionResult = await _transactionCoordinator.ExecuteTransactionAsync(operations, context);

            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "TransactionExecution",
                Status = transactionResult.IsSuccess ? WorkflowStepStatus.Completed : WorkflowStepStatus.Failed,
                StartedAt = transactionResult.CompletedAt - transactionResult.Duration,
                CompletedAt = transactionResult.CompletedAt,
                Data = transactionResult
            });

            if (!transactionResult.IsSuccess)
            {
                workflowResult.Status = WorkflowStatus.Failed;
                workflowResult.ErrorMessage = transactionResult.ErrorMessage;
                return workflowResult;
            }

            // Step 3: Post-workflow risk monitoring
            await PerformPostWorkflowRiskMonitoringAsync(transactionResult, workflowResult);

            // Step 4: Send notifications if configured
            if (options.SendNotifications)
            {
                await SendWorkflowNotificationsAsync(workflowResult, contractData);
            }

            workflowResult.Status = WorkflowStatus.Completed;
            workflowResult.ContractId = ExtractContractId(transactionResult);
            
            _logger.LogInformation("Purchase contract workflow completed successfully for contract {ContractNumber}. Contract ID: {ContractId}", 
                contractData.ContractNumber, workflowResult.ContractId);

            return workflowResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in purchase contract workflow for contract {ContractNumber}", contractData.ContractNumber);
            
            workflowResult.Status = WorkflowStatus.Failed;
            workflowResult.ErrorMessage = ex.Message;
            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "Exception",
                Status = WorkflowStepStatus.Failed,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            });

            return workflowResult;
        }
        finally
        {
            workflowResult.CompletedAt = DateTime.UtcNow;
            workflowResult.Duration = workflowResult.CompletedAt.Value - workflowResult.StartedAt;
        }
    }

    public async Task<WorkflowResult> ExecuteContractApprovalWorkflowAsync(
        Guid contractId,
        string contractType,
        string initiatedBy,
        string? approvalComments = null,
        bool forceApproval = false)
    {
        _logger.LogInformation("Starting contract approval workflow for contract {ContractId}", contractId);

        var workflowResult = new WorkflowResult
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "ContractApproval",
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy,
            ContractId = contractId
        };

        try
        {
            // Enhanced risk check for approval
            var approvalRiskCheck = await PerformApprovalRiskCheckAsync(contractId, contractType, forceApproval);
            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "ApprovalRiskCheck",
                Status = approvalRiskCheck.IsApproved ? WorkflowStepStatus.Completed : WorkflowStepStatus.Failed,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Data = approvalRiskCheck
            });

            if (!approvalRiskCheck.IsApproved && !forceApproval)
            {
                workflowResult.Status = WorkflowStatus.Failed;
                workflowResult.ErrorMessage = $"Approval risk check failed: {string.Join(", ", approvalRiskCheck.Violations)}";
                return workflowResult;
            }

            // Execute approval transaction
            var context = _transactionCoordinator.CreateTransactionContext("ContractApproval", initiatedBy);
            context.Data["ContractId"] = contractId;
            context.Data["ContractType"] = contractType;
            context.Data["ApprovalComments"] = approvalComments ?? "";
            context.Data["ForceApproval"] = forceApproval;

            var operations = _operationFactory.CreateContractApprovalOperations();
            var transactionResult = await _transactionCoordinator.ExecuteTransactionAsync(operations, context);

            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "ApprovalTransaction",
                Status = transactionResult.IsSuccess ? WorkflowStepStatus.Completed : WorkflowStepStatus.Failed,
                StartedAt = transactionResult.CompletedAt - transactionResult.Duration,
                CompletedAt = transactionResult.CompletedAt,
                Data = transactionResult
            });

            workflowResult.Status = transactionResult.IsSuccess ? WorkflowStatus.Completed : WorkflowStatus.Failed;
            workflowResult.ErrorMessage = transactionResult.ErrorMessage;

            return workflowResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in contract approval workflow for contract {ContractId}", contractId);
            
            workflowResult.Status = WorkflowStatus.Failed;
            workflowResult.ErrorMessage = ex.Message;
            
            return workflowResult;
        }
        finally
        {
            workflowResult.CompletedAt = DateTime.UtcNow;
            workflowResult.Duration = workflowResult.CompletedAt.Value - workflowResult.StartedAt;
        }
    }

    public async Task<WorkflowResult> ExecuteContractCancellationWorkflowAsync(
        Guid contractId,
        string contractType,
        string cancellationReason,
        string initiatedBy)
    {
        _logger.LogInformation("Starting contract cancellation workflow for contract {ContractId}", contractId);

        var workflowResult = new WorkflowResult
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "ContractCancellation",
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy,
            ContractId = contractId
        };

        try
        {
            // Execute cancellation transaction
            var context = _transactionCoordinator.CreateTransactionContext("ContractCancellation", initiatedBy);
            context.Data["ContractId"] = contractId;
            context.Data["ContractType"] = contractType;
            context.Data["CancellationReason"] = cancellationReason;

            var operations = _operationFactory.CreateContractCancellationOperations();
            var transactionResult = await _transactionCoordinator.ExecuteTransactionAsync(operations, context);

            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "CancellationTransaction",
                Status = transactionResult.IsSuccess ? WorkflowStepStatus.Completed : WorkflowStepStatus.Failed,
                StartedAt = transactionResult.CompletedAt - transactionResult.Duration,
                CompletedAt = transactionResult.CompletedAt,
                Data = transactionResult
            });

            workflowResult.Status = transactionResult.IsSuccess ? WorkflowStatus.Completed : WorkflowStatus.Failed;
            workflowResult.ErrorMessage = transactionResult.ErrorMessage;

            return workflowResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in contract cancellation workflow for contract {ContractId}", contractId);
            
            workflowResult.Status = WorkflowStatus.Failed;
            workflowResult.ErrorMessage = ex.Message;
            
            return workflowResult;
        }
        finally
        {
            workflowResult.CompletedAt = DateTime.UtcNow;
            workflowResult.Duration = workflowResult.CompletedAt.Value - workflowResult.StartedAt;
        }
    }

    private async Task<RiskAssessmentResult> PerformPreWorkflowRiskAssessmentAsync(
        PurchaseContractCreationData contractData,
        WorkflowOptions options)
    {
        try
        {
            // Get current portfolio risk
            var currentRisk = await _riskMonitoringService.GetRealTimeRiskSnapshotAsync();
            
            // Estimate contract risk impact
            var contractValue = EstimateContractValue(contractData);
            var estimatedVaRIncrease = contractValue * 0.02m; // 2% estimate
            
            var violations = new List<string>();
            
            // Check portfolio VaR impact
            if (currentRisk.VaR.VaR95 + estimatedVaRIncrease > 100000) // $100K limit
            {
                violations.Add($"Contract would increase portfolio VaR beyond limit: {currentRisk.VaR.VaR95 + estimatedVaRIncrease:C}");
            }
            
            // Check contract size
            if (contractValue > 10_000_000) // $10M single contract limit
            {
                violations.Add($"Contract value {contractValue:C} exceeds single contract limit");
            }
            
            // Check counterparty limits
            var counterpartyExposure = await GetCounterpartyExposureAsync(contractData.SupplierId);
            if (counterpartyExposure + contractValue > 50_000_000) // $50M counterparty limit
            {
                violations.Add($"Contract would exceed counterparty exposure limit");
            }

            return new RiskAssessmentResult
            {
                IsApproved = violations.Count == 0,
                RiskScore = violations.Count * 25m,
                Violations = violations,
                EstimatedVaRIncrease = estimatedVaRIncrease,
                ContractValue = contractValue,
                AssessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing pre-workflow risk assessment");
            
            return new RiskAssessmentResult
            {
                IsApproved = !options.RequireRiskApproval, // Allow if risk approval not required
                RiskScore = 100,
                Violations = new List<string> { $"Risk assessment error: {ex.Message}" },
                AssessedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<RiskAssessmentResult> PerformApprovalRiskCheckAsync(
        Guid contractId,
        string contractType,
        bool forceApproval)
    {
        try
        {
            // Enhanced risk check for approval
            var riskMetrics = await _riskMonitoringService.GetRealTimeRiskSnapshotAsync();
            var limitCheck = await _riskMonitoringService.CheckRiskLimitsAsync();
            
            var violations = limitCheck.Breaches.Select(b => b.Description).ToList();
            
            // Additional checks for approval
            if (riskMetrics.VaR.VaR95 > 80000) // 80% of limit
            {
                violations.Add("Portfolio approaching VaR limit");
            }

            return new RiskAssessmentResult
            {
                IsApproved = violations.Count == 0 || forceApproval,
                RiskScore = violations.Count * 30m,
                Violations = violations,
                AssessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing approval risk check");
            
            return new RiskAssessmentResult
            {
                IsApproved = forceApproval,
                RiskScore = 100,
                Violations = new List<string> { $"Risk check error: {ex.Message}" },
                AssessedAt = DateTime.UtcNow
            };
        }
    }

    private async Task PerformPostWorkflowRiskMonitoringAsync(TransactionResult transactionResult, WorkflowResult workflowResult)
    {
        try
        {
            // Get updated risk metrics
            var updatedRisk = await _riskMonitoringService.GetRealTimeRiskSnapshotAsync();
            var limitCheck = await _riskMonitoringService.CheckRiskLimitsAsync();

            workflowResult.Steps.Add(new WorkflowStep
            {
                StepName = "PostWorkflowRiskMonitoring",
                Status = !limitCheck.HasBreaches ? WorkflowStepStatus.Completed : WorkflowStepStatus.CompletedWithWarnings,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Data = new { RiskMetrics = updatedRisk, LimitCheck = limitCheck }
            });

            if (limitCheck.HasBreaches)
            {
                workflowResult.Warnings.AddRange(limitCheck.Breaches.Select(b => $"Risk limit warning: {b.Description}"));
                
                // Trigger risk alerts
                await _riskMonitoringService.CreateRiskAlertAsync(new RiskAlertRequest
                {
                    Type = RiskAlertType.LimitExceeded,
                    Severity = RiskAlertSeverity.Warning,
                    Title = "Risk Limits Breached",
                    Description = "Risk limits breached after workflow completion"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in post-workflow risk monitoring");
            workflowResult.Warnings.Add($"Post-workflow risk monitoring failed: {ex.Message}");
        }
    }

    private async Task SendWorkflowNotificationsAsync(WorkflowResult workflowResult, PurchaseContractCreationData contractData)
    {
        try
        {
            // Implementation would send notifications via email, Slack, etc.
            _logger.LogInformation("Sending workflow notifications for workflow {WorkflowId}", workflowResult.WorkflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending workflow notifications");
            workflowResult.Warnings.Add($"Notification error: {ex.Message}");
        }
    }

    private decimal EstimateContractValue(PurchaseContractCreationData contractData)
    {
        if (contractData.PriceFormula.IsFixedPrice && contractData.PriceFormula.BasePrice != null)
        {
            return contractData.PriceFormula.BasePrice.Amount * contractData.ContractQuantity.Value;
        }
        
        return 75m * contractData.ContractQuantity.Value; // $75 per unit estimate
    }

    private async Task<decimal> GetCounterpartyExposureAsync(Guid counterpartyId)
    {
        // Placeholder - would query actual exposure
        return Random.Shared.Next(5_000_000, 15_000_000);
    }

    private Guid? ExtractContractId(TransactionResult transactionResult)
    {
        var contractOperation = transactionResult.OperationResults
            .FirstOrDefault(r => r.Data?.ContainsKey("ContractId") == true);
        
        return contractOperation?.Data?["ContractId"] as Guid?;
    }
}

/// <summary>
/// Interface for business workflow service
/// </summary>
public interface IBusinessWorkflowService
{
    Task<WorkflowResult> ExecutePurchaseContractWorkflowAsync(
        PurchaseContractCreationData contractData,
        string initiatedBy,
        WorkflowOptions? options = null);

    Task<WorkflowResult> ExecuteContractApprovalWorkflowAsync(
        Guid contractId,
        string contractType,
        string initiatedBy,
        string? approvalComments = null,
        bool forceApproval = false);

    Task<WorkflowResult> ExecuteContractCancellationWorkflowAsync(
        Guid contractId,
        string contractType,
        string cancellationReason,
        string initiatedBy);
}

/// <summary>
/// Workflow execution result
/// </summary>
public class WorkflowResult
{
    public Guid WorkflowId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public Guid? ContractId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Individual workflow step
/// </summary>
public class WorkflowStep
{
    public string StepName { get; set; } = string.Empty;
    public WorkflowStepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>
/// Workflow configuration options
/// </summary>
public class WorkflowOptions
{
    public bool AllowRiskOverride { get; set; } = false;
    public bool RequireRiskApproval { get; set; } = true;
    public bool SendNotifications { get; set; } = true;
    public string? BusinessJustification { get; set; }
    public CompensationStrategy CompensationStrategy { get; set; } = CompensationStrategy.BestEffort;
}

/// <summary>
/// Risk assessment result
/// </summary>
public class RiskAssessmentResult
{
    public bool IsApproved { get; set; }
    public decimal RiskScore { get; set; }
    public List<string> Violations { get; set; } = new();
    public decimal EstimatedVaRIncrease { get; set; }
    public decimal ContractValue { get; set; }
    public DateTime AssessedAt { get; set; }
}

public enum WorkflowStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum WorkflowStepStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    CompletedWithWarnings = 5,
    Skipped = 6
}