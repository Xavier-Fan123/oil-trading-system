using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using System.Diagnostics;

namespace OilTrading.Application.Commands.Positions;

public class RecalculatePositionsCommandHandler : IRequestHandler<RecalculatePositionsCommand, PositionRecalculationResultDto>
{
    private readonly INetPositionService _netPositionService;
    private readonly ILogger<RecalculatePositionsCommandHandler> _logger;

    public RecalculatePositionsCommandHandler(
        INetPositionService netPositionService,
        ILogger<RecalculatePositionsCommandHandler> logger)
    {
        _netPositionService = netPositionService;
        _logger = logger;
    }

    public async Task<PositionRecalculationResultDto> Handle(RecalculatePositionsCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new PositionRecalculationResultDto
        {
            RecalculationTime = DateTime.UtcNow,
            RequestedBy = request.RequestedBy
        };

        try
        {
            _logger.LogInformation("Starting position recalculation requested by {RequestedBy}", request.RequestedBy);

            // Recalculate real-time positions
            var positions = await _netPositionService.CalculateRealTimePositionsAsync(cancellationToken);
            
            // Filter by product types if specified
            if (request.ProductTypes?.Length > 0)
            {
                positions = positions.Where(p => request.ProductTypes.Contains(p.ProductType, StringComparer.OrdinalIgnoreCase));
            }

            result.TotalPositionsRecalculated = positions.Count();
            result.Status = "Success";

            // Check for potential issues
            var largeExposures = positions.Where(p => p.ExposureValue > 5_000_000m).ToList();
            if (largeExposures.Any())
            {
                result.Warnings.Add($"Found {largeExposures.Count} positions with exposure > $5M");
            }

            var flatPositions = positions.Where(p => p.PositionStatus == "Flat").ToList();
            if (flatPositions.Any())
            {
                result.Warnings.Add($"Found {flatPositions.Count} flat positions");
            }

            // Check position limits
            var limitsOk = await _netPositionService.CheckPositionLimitsAsync(cancellationToken);
            if (!limitsOk)
            {
                result.Warnings.Add("Position limits exceeded");
            }

            _logger.LogInformation("Position recalculation completed successfully. Processed {Count} positions in {ElapsedMs}ms", 
                result.TotalPositionsRecalculated, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.Errors.Add($"Recalculation failed: {ex.Message}");
            _logger.LogError(ex, "Position recalculation failed");
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }
}