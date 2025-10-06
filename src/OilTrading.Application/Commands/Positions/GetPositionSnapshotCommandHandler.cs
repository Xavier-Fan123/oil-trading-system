using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using System.Diagnostics;

namespace OilTrading.Application.Commands.Positions;

public class GetPositionSnapshotCommandHandler : IRequestHandler<GetPositionSnapshotCommand, PositionSnapshotDto>
{
    private readonly INetPositionService _netPositionService;
    private readonly ILogger<GetPositionSnapshotCommandHandler> _logger;

    public GetPositionSnapshotCommandHandler(
        INetPositionService netPositionService,
        ILogger<GetPositionSnapshotCommandHandler> logger)
    {
        _netPositionService = netPositionService;
        _logger = logger;
    }

    public async Task<PositionSnapshotDto> Handle(GetPositionSnapshotCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var snapshotDate = request.AsOfDate ?? DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Generating position snapshot for {SnapshotDate}", snapshotDate);

            var snapshot = new PositionSnapshotDto
            {
                SnapshotDate = snapshotDate,
                GeneratedBy = "System"
            };

            // Get position summary
            snapshot.Summary = await _netPositionService.GetPositionSummaryAsync(cancellationToken);

            // Get detailed positions
            var positions = await _netPositionService.CalculateRealTimePositionsAsync(cancellationToken);
            
            // Apply filters
            if (request.ProductTypes?.Length > 0)
            {
                positions = positions.Where(p => request.ProductTypes.Contains(p.ProductType, StringComparer.OrdinalIgnoreCase));
            }

            snapshot.Positions = positions;

            // Include P&L details if requested
            if (request.IncludePnL)
            {
                var pnlDetails = await _netPositionService.CalculatePnLAsync(request.AsOfDate, cancellationToken);
                
                // Filter P&L by product types
                if (request.ProductTypes?.Length > 0)
                {
                    pnlDetails = pnlDetails.Where(p => request.ProductTypes.Contains(p.ProductType, StringComparer.OrdinalIgnoreCase));
                }

                snapshot.PnLDetails = pnlDetails;
            }

            // Include exposure breakdown if requested
            if (request.IncludeExposure)
            {
                var productExposure = await _netPositionService.CalculateExposureByProductAsync(cancellationToken);
                var counterpartyExposure = await _netPositionService.CalculateExposureByCounterpartyAsync(cancellationToken);

                // Apply product filter to exposures
                if (request.ProductTypes?.Length > 0)
                {
                    productExposure = productExposure.Where(e => request.ProductTypes.Contains(e.Category, StringComparer.OrdinalIgnoreCase));
                }

                // Apply counterparty filter
                if (request.Counterparties?.Length > 0)
                {
                    counterpartyExposure = counterpartyExposure.Where(e => request.Counterparties.Contains(e.Category, StringComparer.OrdinalIgnoreCase));
                }

                snapshot.ProductExposure = productExposure;
                snapshot.CounterpartyExposure = counterpartyExposure;
            }

            // Check position limits
            await BuildLimitsAnalysis(snapshot, cancellationToken);

            stopwatch.Stop();
            snapshot.GenerationTime = stopwatch.Elapsed;

            _logger.LogInformation("Position snapshot generated successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate position snapshot");
            throw;
        }
    }

    private async Task BuildLimitsAnalysis(PositionSnapshotDto snapshot, CancellationToken cancellationToken)
    {
        var limits = new PositionLimitsDto();

        // Calculate current exposures
        limits.CurrentTotalExposure = snapshot.Summary.TotalExposure;
        limits.CurrentMaxProductExposure = snapshot.Summary.LargestExposure;

        // Check for breaches
        var breaches = new List<LimitBreachDto>();

        // Total exposure limit check
        if (limits.CurrentTotalExposure > limits.TotalExposureLimit)
        {
            breaches.Add(new LimitBreachDto
            {
                LimitType = "Total Exposure",
                Category = "Portfolio",
                Limit = limits.TotalExposureLimit,
                Current = limits.CurrentTotalExposure,
                Excess = limits.CurrentTotalExposure - limits.TotalExposureLimit,
                Severity = limits.CurrentTotalExposure > limits.TotalExposureLimit * 1.2m ? "Critical" : "Warning"
            });
        }

        // Product exposure limit check
        if (limits.CurrentMaxProductExposure > limits.MaxProductExposureLimit)
        {
            breaches.Add(new LimitBreachDto
            {
                LimitType = "Product Exposure",
                Category = "Single Product",
                Limit = limits.MaxProductExposureLimit,
                Current = limits.CurrentMaxProductExposure,
                Excess = limits.CurrentMaxProductExposure - limits.MaxProductExposureLimit,
                Severity = limits.CurrentMaxProductExposure > limits.MaxProductExposureLimit * 1.5m ? "Critical" : "Warning"
            });
        }

        // Check individual product limits
        foreach (var exposure in snapshot.ProductExposure.Where(e => e.TotalExposure > limits.MaxProductExposureLimit))
        {
            breaches.Add(new LimitBreachDto
            {
                LimitType = "Product Exposure",
                Category = exposure.Category,
                Limit = limits.MaxProductExposureLimit,
                Current = exposure.TotalExposure,
                Excess = exposure.TotalExposure - limits.MaxProductExposureLimit,
                Severity = exposure.TotalExposure > limits.MaxProductExposureLimit * 1.5m ? "Critical" : "Warning"
            });
        }

        limits.Breaches = breaches;
        limits.WithinLimits = !breaches.Any();

        snapshot.Limits = limits;
    }
}