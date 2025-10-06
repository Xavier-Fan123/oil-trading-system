using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace OilTrading.Application.Commands.MarketData;

public class DeleteMarketDataCommandHandler : IRequestHandler<DeleteMarketDataCommand, DeleteMarketDataResultDto>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMarketDataCommandHandler> _logger;

    public DeleteMarketDataCommandHandler(
        IMarketDataRepository marketDataRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteMarketDataCommandHandler> logger)
    {
        _marketDataRepository = marketDataRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DeleteMarketDataResultDto> Handle(
        DeleteMarketDataCommand request,
        CancellationToken cancellationToken)
    {
        var result = new DeleteMarketDataResultDto();

        try
        {
            _logger.LogInformation(
                "Starting market data deletion. Type: {DeleteType}, User: {User}, Reason: {Reason}",
                request.DeleteType,
                request.DeletedBy,
                request.Reason ?? "Not specified");

            // Get count before deletion for logging
            var initialCount = await _marketDataRepository.CountAllAsync(cancellationToken);
            _logger.LogInformation("Total market data records before deletion: {Count}", initialCount);

            int deletedCount = 0;

            switch (request.DeleteType)
            {
                case DeleteMarketDataType.All:
                    await _marketDataRepository.DeleteAllAsync(cancellationToken);
                    deletedCount = initialCount;
                    result.Message = "All market data records deleted successfully";
                    break;

                case DeleteMarketDataType.ByDateRange:
                    if (!request.StartDate.HasValue || !request.EndDate.HasValue)
                    {
                        result.Errors.Add("Start date and end date are required for date range deletion");
                        return result;
                    }

                    deletedCount = await _marketDataRepository.DeleteByDateRangeAsync(
                        request.StartDate.Value,
                        request.EndDate.Value,
                        cancellationToken);

                    result.Message = $"Market data records deleted for date range {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}";
                    break;

                case DeleteMarketDataType.ByProduct:
                    if (string.IsNullOrEmpty(request.ProductCode))
                    {
                        result.Errors.Add("Product code is required for product-specific deletion");
                        return result;
                    }

                    deletedCount = await _marketDataRepository.DeleteByProductCodeAsync(
                        request.ProductCode,
                        cancellationToken);

                    result.Message = $"Market data records deleted for product: {request.ProductCode}";
                    break;

                default:
                    result.Errors.Add("Invalid delete type specified");
                    return result;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.RecordsDeleted = deletedCount;

            _logger.LogInformation(
                "Market data deletion completed successfully. Type: {DeleteType}, Records deleted: {RecordsDeleted}, User: {User}",
                request.DeleteType,
                deletedCount,
                request.DeletedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred during market data deletion. Type: {DeleteType}, User: {User}",
                request.DeleteType,
                request.DeletedBy);

            result.Errors.Add($"Deletion failed: {ex.Message}");
            result.Message = "Market data deletion failed";
        }

        return result;
    }
}