using Microsoft.Extensions.Logging;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for managing trade chains and tracking complete trade lifecycles
/// </summary>
public class TradeChainService : ITradeChainService
{
    private readonly ITradeChainRepository _tradeChainRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TradeChainService> _logger;

    public TradeChainService(
        ITradeChainRepository tradeChainRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork,
        ILogger<TradeChainService> logger)
    {
        _tradeChainRepository = tradeChainRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TradeChain> CreateTradeChainAsync(CreateTradeChainRequest request)
    {
        _logger.LogInformation("Creating trade chain {ChainName} of type {Type}", request.ChainName, request.Type);

        // Generate unique chain ID
        var chainId = await GenerateChainIdAsync(request.Type);

        var tradeChain = new TradeChain(
            chainId,
            request.ChainName,
            request.Type,
            request.CreatedBy);

        if (!string.IsNullOrEmpty(request.Notes))
        {
            tradeChain.AddMetadata("initial_notes", request.Notes, request.CreatedBy);
        }

        await _tradeChainRepository.AddAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Trade chain {ChainId} created successfully", chainId);
        return tradeChain;
    }

    public async Task<TradeChain> LinkPurchaseContractAsync(LinkPurchaseContractRequest request)
    {
        _logger.LogInformation("Linking purchase contract {ContractId} to trade chain {ChainId}", 
            request.PurchaseContractId, request.ChainId);

        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(request.ChainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {request.ChainId} not found");

        // Get purchase contract details
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(request.PurchaseContractId);
        if (purchaseContract == null)
            throw new InvalidOperationException($"Purchase contract {request.PurchaseContractId} not found");

        // Link the contract
        tradeChain.LinkPurchaseContract(
            request.PurchaseContractId,
            purchaseContract.SupplierId,
            purchaseContract.ProductId,
            purchaseContract.ContractQuantity,
            CalculateContractValue(purchaseContract),
            request.ExpectedDeliveryStart,
            request.ExpectedDeliveryEnd,
            request.LinkedBy);

        await _tradeChainRepository.UpdateAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Purchase contract {ContractId} linked to trade chain {ChainId}", 
            request.PurchaseContractId, request.ChainId);

        return tradeChain;
    }

    public async Task<TradeChain> LinkSalesContractAsync(LinkSalesContractRequest request)
    {
        _logger.LogInformation("Linking sales contract {ContractId} to trade chain {ChainId}", 
            request.SalesContractId, request.ChainId);

        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(request.ChainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {request.ChainId} not found");

        // Get sales contract details
        var salesContract = await _salesContractRepository.GetByIdAsync(request.SalesContractId);
        if (salesContract == null)
            throw new InvalidOperationException($"Sales contract {request.SalesContractId} not found");

        // Link the contract
        tradeChain.LinkSalesContract(
            request.SalesContractId,
            salesContract.CustomerId,
            salesContract.ContractQuantity,
            CalculateContractValue(salesContract),
            request.LinkedBy);

        await _tradeChainRepository.UpdateAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Sales contract {ContractId} linked to trade chain {ChainId}", 
            request.SalesContractId, request.ChainId);

        return tradeChain;
    }

    public async Task<TradeChain> AddOperationAsync(AddOperationRequest request)
    {
        _logger.LogInformation("Adding operation {OperationType} to trade chain {ChainId}", 
            request.OperationType, request.ChainId);

        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(request.ChainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {request.ChainId} not found");

        tradeChain.AddOperation(request.OperationType, request.Description, request.PerformedBy, request.Data);

        await _tradeChainRepository.UpdateAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Operation {OperationType} added to trade chain {ChainId}", 
            request.OperationType, request.ChainId);

        return tradeChain;
    }

    public async Task<TradeChain> UpdateDeliveryActualsAsync(UpdateDeliveryActualsRequest request)
    {
        _logger.LogInformation("Updating delivery actuals for trade chain {ChainId}", request.ChainId);

        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(request.ChainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {request.ChainId} not found");

        tradeChain.UpdateDeliveryActuals(request.ActualStart, request.ActualEnd, request.UpdatedBy);

        await _tradeChainRepository.UpdateAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Delivery actuals updated for trade chain {ChainId}", request.ChainId);

        return tradeChain;
    }

    public async Task<TradeChain> CompleteTradeChainAsync(CompleteTradeChainRequest request)
    {
        _logger.LogInformation("Completing trade chain {ChainId}", request.ChainId);

        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(request.ChainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {request.ChainId} not found");

        tradeChain.MarkCompleted(request.CompletedBy, request.CompletionNotes);

        await _tradeChainRepository.UpdateAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Trade chain {ChainId} marked as completed", request.ChainId);

        return tradeChain;
    }

    public async Task<TradeChain> CancelTradeChainAsync(CancelTradeChainRequest request)
    {
        _logger.LogInformation("Cancelling trade chain {ChainId}", request.ChainId);

        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(request.ChainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {request.ChainId} not found");

        tradeChain.Cancel(request.Reason, request.CancelledBy);

        await _tradeChainRepository.UpdateAsync(tradeChain);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Trade chain {ChainId} cancelled", request.ChainId);

        return tradeChain;
    }

    public async Task<TradeChain?> GetTradeChainAsync(string chainId)
    {
        return await _tradeChainRepository.GetByChainIdAsync(chainId);
    }

    public async Task<List<TradeChain>> GetTradeChainsByContractAsync(Guid contractId, bool isPurchase)
    {
        if (isPurchase)
            return await _tradeChainRepository.GetByPurchaseContractIdAsync(contractId);
        else
            return await _tradeChainRepository.GetBySalesContractIdAsync(contractId);
    }

    public async Task<(List<TradeChainSummary> Items, int TotalCount)> SearchTradeChainsAsync(TradeChainSearchCriteria criteria)
    {
        return await _tradeChainRepository.GetSummariesAsync(
            criteria.Page,
            criteria.PageSize,
            criteria.Statuses?.FirstOrDefault(),
            criteria.Types?.FirstOrDefault(),
            criteria.StartDate,
            criteria.EndDate);
    }

    public async Task<TradeChainAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        return await _tradeChainRepository.GetAnalyticsAsync(startDate, endDate);
    }

    public async Task<List<TradeChain>> GetActiveTradeChainsAsync()
    {
        return await _tradeChainRepository.GetActiveAsync();
    }

    public async Task<List<TradeChain>> GetTradeChainsRequiringAttentionAsync()
    {
        var overdue = await _tradeChainRepository.GetOverdueAsync();
        var incomplete = await _tradeChainRepository.GetWithIncompleteOperationsAsync();
        var requiresAttention = await _tradeChainRepository.GetRequiringAttentionAsync();

        // Combine and deduplicate
        var allChains = overdue.Concat(incomplete).Concat(requiresAttention)
            .GroupBy(tc => tc.Id)
            .Select(g => g.First())
            .ToList();

        return allChains;
    }

    public async Task<TradeChainPerformanceReport> GeneratePerformanceReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating trade chain performance report from {StartDate} to {EndDate}", 
            startDate, endDate);

        var chains = await _tradeChainRepository.GetWithPerformanceMetricsAsync(startDate, endDate);
        var analytics = await _tradeChainRepository.GetAnalyticsAsync(startDate, endDate);

        var report = new TradeChainPerformanceReport
        {
            ReportPeriodStart = startDate,
            ReportPeriodEnd = endDate,
            GeneratedAt = DateTime.UtcNow,
            TotalChains = chains.Count,
            Analytics = analytics
        };

        // Calculate aggregated performance metrics
        report.OverallMetrics = CalculateOverallMetrics(chains);
        report.PerformanceByType = CalculatePerformanceByType(chains);
        report.TopPerformingChains = GetTopPerformingChains(chains, 10);
        report.UnderPerformingChains = GetUnderPerformingChains(chains, 10);

        return report;
    }

    public async Task<TradeChainVisualization> GetTradeChainVisualizationAsync(string chainId)
    {
        var tradeChain = await _tradeChainRepository.GetByChainIdAsync(chainId);
        if (tradeChain == null)
            throw new InvalidOperationException($"Trade chain {chainId} not found");

        return new TradeChainVisualization
        {
            ChainId = chainId,
            ChainName = tradeChain.ChainName,
            Status = tradeChain.Status,
            Timeline = CreateTimeline(tradeChain),
            Operations = tradeChain.Operations.OrderBy(op => op.PerformedAt).ToList(),
            Events = tradeChain.Events.OrderBy(e => e.PerformedAt).ToList(),
            PerformanceMetrics = tradeChain.CalculatePerformanceMetrics(),
            Summary = tradeChain.GetSummary()
        };
    }

    private async Task<string> GenerateChainIdAsync(TradeChainType type)
    {
        var prefix = type switch
        {
            TradeChainType.BackToBack => "BTB",
            TradeChainType.Speculative => "SPEC",
            TradeChainType.Storage => "STOR",
            TradeChainType.Processing => "PROC",
            TradeChainType.Transit => "TRAN",
            _ => "TC"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var sequence = await GetNextSequenceAsync(prefix, timestamp);

        return $"{prefix}-{timestamp}-{sequence:D4}";
    }

    private async Task<int> GetNextSequenceAsync(string prefix, string timestamp)
    {
        // Simple implementation - in production, this should use a proper sequence generator
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var todayChains = await _tradeChainRepository.GetByDateRangeAsync(today, tomorrow);
        var count = todayChains.Count(tc => tc.ChainId.StartsWith($"{prefix}-{timestamp}"));
        
        return count + 1;
    }

    private Money CalculateContractValue(PurchaseContract contract)
    {
        // Simplified calculation - in reality would be more complex
        var basePrice = contract.PriceFormula.BasePrice ?? new Money(75m, "USD");
        var totalValue = basePrice.Amount * contract.ContractQuantity.Value;
        
        return new Money(totalValue, basePrice.Currency);
    }

    private Money CalculateContractValue(SalesContract contract)
    {
        // Simplified calculation - in reality would be more complex
        var basePrice = contract.PriceFormula.BasePrice ?? new Money(75m, "USD");
        var totalValue = basePrice.Amount * contract.ContractQuantity.Value;
        
        return new Money(totalValue, basePrice.Currency);
    }

    private TradeChainPerformanceMetrics CalculateOverallMetrics(List<TradeChain> chains)
    {
        if (!chains.Any())
            return new TradeChainPerformanceMetrics();

        var metrics = chains.Select(tc => tc.CalculatePerformanceMetrics()).ToList();

        return new TradeChainPerformanceMetrics
        {
            TotalDurationDays = (int)metrics.Average(m => m.TotalDurationDays),
            DeliveryDurationDays = metrics.Where(m => m.DeliveryDurationDays.HasValue)
                .Average(m => m.DeliveryDurationDays!.Value),
            PlannedDurationDays = metrics.Where(m => m.PlannedDurationDays.HasValue)
                .Average(m => m.PlannedDurationDays!.Value),
            DeliveryOnTime = metrics.Where(m => m.DeliveryOnTime.HasValue)
                .All(m => m.DeliveryOnTime!.Value),
            ProfitMargin = metrics.Where(m => m.ProfitMargin.HasValue)
                .Average(m => m.ProfitMargin!.Value),
            OperationEfficiency = metrics.Average(m => m.OperationEfficiency),
            RiskAdjustedReturn = metrics.Where(m => m.RiskAdjustedReturn.HasValue)
                .Average(m => m.RiskAdjustedReturn!.Value)
        };
    }

    private List<TradeChainPerformanceByType> CalculatePerformanceByType(List<TradeChain> chains)
    {
        return chains.GroupBy(tc => tc.Type)
            .Select(g => new TradeChainPerformanceByType
            {
                Type = g.Key,
                Count = g.Count(),
                AverageProfitMargin = g.Where(tc => tc.RealizedPnL != null && tc.SalesValue != null)
                    .Average(tc => (tc.RealizedPnL!.Amount / tc.SalesValue!.Amount) * 100),
                AverageCompletionTime = (decimal)g.Average(tc => (DateTime.UtcNow - tc.CreatedAt).TotalDays),
                OnTimeDeliveryRate = g.Where(tc => tc.ActualDeliveryEnd.HasValue && tc.ExpectedDeliveryEnd.HasValue)
                    .Count(tc => tc.ActualDeliveryEnd <= tc.ExpectedDeliveryEnd) / (decimal)g.Count() * 100,
                TotalValue = g.Where(tc => tc.PurchaseValue != null).Sum(tc => tc.PurchaseValue!.Amount)
            })
            .OrderByDescending(p => p.TotalValue)
            .ToList();
    }

    private List<TradeChainSummary> GetTopPerformingChains(List<TradeChain> chains, int count)
    {
        return chains.Where(tc => tc.RealizedPnL != null && tc.SalesValue != null)
            .OrderByDescending(tc => (tc.RealizedPnL!.Amount / tc.SalesValue!.Amount))
            .Take(count)
            .Select(tc => tc.GetSummary())
            .ToList();
    }

    private List<TradeChainSummary> GetUnderPerformingChains(List<TradeChain> chains, int count)
    {
        return chains.Where(tc => tc.RealizedPnL != null && tc.SalesValue != null)
            .OrderBy(tc => (tc.RealizedPnL!.Amount / tc.SalesValue!.Amount))
            .Take(count)
            .Select(tc => tc.GetSummary())
            .ToList();
    }

    private List<TradeChainTimelineItem> CreateTimeline(TradeChain tradeChain)
    {
        var timeline = new List<TradeChainTimelineItem>();

        // Add key events to timeline
        timeline.Add(new TradeChainTimelineItem
        {
            Date = tradeChain.CreatedAt,
            EventType = "Chain Created",
            Description = $"Trade chain {tradeChain.ChainName} initiated",
            Status = "completed"
        });

        if (tradeChain.PurchaseContractId.HasValue)
        {
            var purchaseEvent = tradeChain.Events.FirstOrDefault(e => e.EventType == TradeChainEventType.PurchaseLinked);
            if (purchaseEvent != null)
            {
                timeline.Add(new TradeChainTimelineItem
                {
                    Date = purchaseEvent.PerformedAt,
                    EventType = "Purchase Linked",
                    Description = $"Purchase contract linked",
                    Status = "completed"
                });
            }
        }

        if (tradeChain.SalesContractId.HasValue)
        {
            var salesEvent = tradeChain.Events.FirstOrDefault(e => e.EventType == TradeChainEventType.SalesLinked);
            if (salesEvent != null)
            {
                timeline.Add(new TradeChainTimelineItem
                {
                    Date = salesEvent.PerformedAt,
                    EventType = "Sales Linked",
                    Description = $"Sales contract linked",
                    Status = "completed"
                });
            }
        }

        // Add delivery milestones
        if (tradeChain.ExpectedDeliveryStart.HasValue)
        {
            var status = tradeChain.ActualDeliveryStart.HasValue ? "completed" :
                         DateTime.UtcNow > tradeChain.ExpectedDeliveryStart.Value ? "overdue" : "pending";
            
            timeline.Add(new TradeChainTimelineItem
            {
                Date = tradeChain.ExpectedDeliveryStart.Value,
                EventType = "Delivery Start",
                Description = "Expected delivery start",
                Status = status
            });
        }

        if (tradeChain.ExpectedDeliveryEnd.HasValue)
        {
            var status = tradeChain.ActualDeliveryEnd.HasValue ? "completed" :
                         DateTime.UtcNow > tradeChain.ExpectedDeliveryEnd.Value ? "overdue" : "pending";
            
            timeline.Add(new TradeChainTimelineItem
            {
                Date = tradeChain.ExpectedDeliveryEnd.Value,
                EventType = "Delivery End",
                Description = "Expected delivery completion",
                Status = status
            });
        }

        return timeline.OrderBy(t => t.Date).ToList();
    }
}

/// <summary>
/// Interface for trade chain service
/// </summary>
public interface ITradeChainService
{
    Task<TradeChain> CreateTradeChainAsync(CreateTradeChainRequest request);
    Task<TradeChain> LinkPurchaseContractAsync(LinkPurchaseContractRequest request);
    Task<TradeChain> LinkSalesContractAsync(LinkSalesContractRequest request);
    Task<TradeChain> AddOperationAsync(AddOperationRequest request);
    Task<TradeChain> UpdateDeliveryActualsAsync(UpdateDeliveryActualsRequest request);
    Task<TradeChain> CompleteTradeChainAsync(CompleteTradeChainRequest request);
    Task<TradeChain> CancelTradeChainAsync(CancelTradeChainRequest request);
    Task<TradeChain?> GetTradeChainAsync(string chainId);
    Task<List<TradeChain>> GetTradeChainsByContractAsync(Guid contractId, bool isPurchase);
    Task<(List<TradeChainSummary> Items, int TotalCount)> SearchTradeChainsAsync(TradeChainSearchCriteria criteria);
    Task<TradeChainAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);
    Task<List<TradeChain>> GetActiveTradeChainsAsync();
    Task<List<TradeChain>> GetTradeChainsRequiringAttentionAsync();
    Task<TradeChainPerformanceReport> GeneratePerformanceReportAsync(DateTime startDate, DateTime endDate);
    Task<TradeChainVisualization> GetTradeChainVisualizationAsync(string chainId);
}

// Request DTOs
public class CreateTradeChainRequest
{
    public string ChainName { get; set; } = string.Empty;
    public TradeChainType Type { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class LinkPurchaseContractRequest
{
    public string ChainId { get; set; } = string.Empty;
    public Guid PurchaseContractId { get; set; }
    public DateTime ExpectedDeliveryStart { get; set; }
    public DateTime ExpectedDeliveryEnd { get; set; }
    public string LinkedBy { get; set; } = string.Empty;
}

public class LinkSalesContractRequest
{
    public string ChainId { get; set; } = string.Empty;
    public Guid SalesContractId { get; set; }
    public string LinkedBy { get; set; } = string.Empty;
}

public class AddOperationRequest
{
    public string ChainId { get; set; } = string.Empty;
    public TradeChainOperationType OperationType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class UpdateDeliveryActualsRequest
{
    public string ChainId { get; set; } = string.Empty;
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class CompleteTradeChainRequest
{
    public string ChainId { get; set; } = string.Empty;
    public string CompletedBy { get; set; } = string.Empty;
    public string? CompletionNotes { get; set; }
}

public class CancelTradeChainRequest
{
    public string ChainId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CancelledBy { get; set; } = string.Empty;
}

// Response DTOs
public class TradeChainPerformanceReport
{
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalChains { get; set; }
    public TradeChainAnalytics Analytics { get; set; } = null!;
    public TradeChainPerformanceMetrics OverallMetrics { get; set; } = null!;
    public List<TradeChainPerformanceByType> PerformanceByType { get; set; } = new();
    public List<TradeChainSummary> TopPerformingChains { get; set; } = new();
    public List<TradeChainSummary> UnderPerformingChains { get; set; } = new();
}

public class TradeChainVisualization
{
    public string ChainId { get; set; } = string.Empty;
    public string ChainName { get; set; } = string.Empty;
    public TradeChainStatus Status { get; set; }
    public List<TradeChainTimelineItem> Timeline { get; set; } = new();
    public List<TradeChainOperation> Operations { get; set; } = new();
    public List<TradeChainEvent> Events { get; set; } = new();
    public TradeChainPerformanceMetrics PerformanceMetrics { get; set; } = null!;
    public TradeChainSummary Summary { get; set; } = null!;
}

public class TradeChainTimelineItem
{
    public DateTime Date { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // completed, pending, overdue
}