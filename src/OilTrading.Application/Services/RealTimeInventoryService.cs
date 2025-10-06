using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Repositories;
using OilTrading.Application.DTOs;
using DtoInventoryAllocation = OilTrading.Application.DTOs.InventoryAllocation;
using ServiceInventoryAllocation = OilTrading.Application.Services.InventoryAllocation;

namespace OilTrading.Application.Services;

public class RealTimeInventoryService : IRealTimeInventoryService
{
    private readonly ILogger<RealTimeInventoryService> _logger;
    private readonly IMultiLayerCacheService _cacheService;
    private readonly IPurchaseContractRepository _contractRepository;
    private readonly RealTimeInventoryOptions _options;
    
    // In-memory storage for demo purposes (in production, this would be in a database)
    private static readonly ConcurrentDictionary<string, InventoryPosition> _inventoryPositions = new();
    private static readonly ConcurrentDictionary<Guid, List<OilTrading.Application.DTOs.InventoryMovement>> _inventoryMovements = new();
    private static readonly ConcurrentDictionary<string, InventoryThresholds> _inventoryThresholds = new();
    private static readonly List<OilTrading.Application.DTOs.InventoryAlert> _activeAlerts = new();
    private static readonly object _alertLock = new();
    
    public RealTimeInventoryService(
        ILogger<RealTimeInventoryService> logger,
        IMultiLayerCacheService cacheService,
        IPurchaseContractRepository contractRepository,
        IOptions<RealTimeInventoryOptions> options)
    {
        _logger = logger;
        _cacheService = cacheService;
        _contractRepository = contractRepository;
        _options = options.Value;
        
        // Initialize with sample data
        InitializeSampleData();
    }

    public async Task<InventorySnapshot> GetRealTimeInventoryAsync(Guid? productId = null, Guid? locationId = null)
    {
        _logger.LogInformation("Getting real-time inventory snapshot for product {ProductId}, location {LocationId}", 
            productId, locationId);
        
        try
        {
            var cacheKey = $"inventory:snapshot:{productId}:{locationId}";
            var cachedSnapshot = await _cacheService.GetAsync<InventorySnapshot>(cacheKey);
            
            if (cachedSnapshot != null && DateTime.UtcNow - cachedSnapshot.Timestamp < TimeSpan.FromMinutes(1))
            {
                return cachedSnapshot;
            }
            
            var positions = _inventoryPositions.Values
                .Where(p => (productId == null || p.ProductId == productId) &&
                           (locationId == null || p.LocationId == locationId))
                .ToList();
            
            var snapshot = new InventorySnapshot
            {
                Positions = positions,
                TotalsByProduct = positions
                    .GroupBy(p => p.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalQuantity.Value)),
                TotalsByLocation = positions
                    .GroupBy(p => p.LocationId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalQuantity.Value)),
                TotalValuation = CalculateTotalValuation(positions)
            };
            
            await _cacheService.SetAsync(cacheKey, snapshot, TimeSpan.FromMinutes(2));
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time inventory snapshot");
            return new InventorySnapshot();
        }
    }

    public async Task<List<OilTrading.Application.DTOs.InventoryMovement>> GetInventoryMovementsAsync(DateTime startDate, DateTime endDate, Guid? productId = null)
    {
        _logger.LogInformation("Getting inventory movements from {StartDate} to {EndDate} for product {ProductId}", 
            startDate, endDate, productId);
        
        try
        {
            var allMovements = _inventoryMovements.Values.SelectMany(movements => movements);
            
            var filteredMovements = allMovements
                .Where(m => m.MovementDate >= startDate && 
                           m.MovementDate <= endDate &&
                           (productId == null || m.ProductId == productId))
                .OrderByDescending(m => m.MovementDate)
                .ToList();
            
            return filteredMovements.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory movements");
            return new List<DTOs.InventoryMovement>();
        }
    }

    public async Task<InventoryBalance> GetProductBalanceAsync(Guid productId, Guid? locationId = null)
    {
        _logger.LogInformation("Getting product balance for product {ProductId}, location {LocationId}", 
            productId, locationId);
        
        try
        {
            var positions = _inventoryPositions.Values
                .Where(p => p.ProductId == productId && 
                           (locationId == null || p.LocationId == locationId))
                .ToList();
            
            if (!positions.Any())
            {
                return new InventoryBalance
                {
                    ProductId = productId,
                    LocationId = locationId,
                    BalanceDate = DateTime.UtcNow,
                    OpeningBalance = new Quantity(0, QuantityUnit.BBL),
                    ClosingBalance = new Quantity(0, QuantityUnit.BBL),
                    TotalReceipts = new Quantity(0, QuantityUnit.BBL),
                    TotalDeliveries = new Quantity(0, QuantityUnit.BBL),
                    TotalTransfersIn = new Quantity(0, QuantityUnit.BBL),
                    TotalTransfersOut = new Quantity(0, QuantityUnit.BBL),
                    NetMovement = new Quantity(0, QuantityUnit.BBL)
                };
            }
            
            var totalQuantity = positions.Sum(p => p.TotalQuantity.Value);
            var today = DateTime.UtcNow.Date;
            
            // Calculate movements for today
            var todayMovements = await GetInventoryMovementsAsync(today, today.AddDays(1), productId);
            
            var receipts = todayMovements
                .Where(m => m.MovementType == OilTrading.Application.DTOs.InventoryMovementType.Receipt)
                .Sum(m => m.Quantity.Value);
            
            var deliveries = todayMovements
                .Where(m => m.MovementType == OilTrading.Application.DTOs.InventoryMovementType.Shipment)
                .Sum(m => m.Quantity.Value);
            
            var transfersIn = todayMovements
                .Where(m => m.MovementType == OilTrading.Application.DTOs.InventoryMovementType.Transfer && m.Quantity.Value > 0)
                .Sum(m => m.Quantity.Value);
            
            var transfersOut = todayMovements
                .Where(m => m.MovementType == OilTrading.Application.DTOs.InventoryMovementType.Transfer && m.Quantity.Value < 0)
                .Sum(m => Math.Abs(m.Quantity.Value));
            
            var balance = new InventoryBalance
            {
                ProductId = productId,
                LocationId = locationId,
                BalanceDate = DateTime.UtcNow,
                ClosingBalance = new Quantity(totalQuantity, QuantityUnit.BBL),
                TotalReceipts = new Quantity(receipts, QuantityUnit.BBL),
                TotalDeliveries = new Quantity(deliveries, QuantityUnit.BBL),
                TotalTransfersIn = new Quantity(transfersIn, QuantityUnit.BBL),
                TotalTransfersOut = new Quantity(transfersOut, QuantityUnit.BBL)
            };
            
            var netMovement = receipts + transfersIn - deliveries - transfersOut;
            balance.NetMovement = new Quantity(netMovement, QuantityUnit.BBL);
            balance.OpeningBalance = new Quantity(totalQuantity - netMovement, QuantityUnit.BBL);
            
            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get product balance for product {ProductId}", productId);
            return new InventoryBalance();
        }
    }

    public async Task<InventoryOperationResult> ReceiveInventoryAsync(InventoryReceiptRequest request)
    {
        _logger.LogInformation("Receiving inventory: {Quantity} of product {ProductId} at location {LocationId}", 
            request.ReceivedQuantity.Value, request.ProductId, request.LocationId);
        
        try
        {
            var positionKey = $"{request.ProductId}:{request.LocationId}";
            var position = _inventoryPositions.GetOrAdd(positionKey, _ => CreateNewPosition(request.ProductId, request.LocationId));
            
            // Update position
            var newAvailable = position.AvailableQuantity.Value + request.ReceivedQuantity.Value;
            var newTotal = position.TotalQuantity.Value + request.ReceivedQuantity.Value;
            
            position.AvailableQuantity = new Quantity(newAvailable, position.AvailableQuantity.Unit);
            position.TotalQuantity = new Quantity(newTotal, position.TotalQuantity.Unit);
            position.LastUpdated = DateTime.UtcNow;
            
            // Update average cost (weighted average)
            var currentValue = position.AverageCost * (newTotal - request.ReceivedQuantity.Value);
            var newValue = request.UnitCost * request.ReceivedQuantity.Value;
            position.AverageCost = (currentValue + newValue) / newTotal;
            
            // Create movement record
            var movement = new OilTrading.Application.DTOs.InventoryMovement
            {
                MovementDate = request.ReceiptDate,
                ProductId = request.ProductId,
                FromLocationId = request.LocationId,
                // ToLocationId = request.LocationId,
                MovementType = OilTrading.Application.DTOs.InventoryMovementType.Receipt,
                Quantity = request.ReceivedQuantity,
                // Note: UnitPrice property removed from InventoryMovement DTO
                // MovementReference removed from request,
                PurchaseContractId = request.ContractId,
                // Notes property removed from request"
            };
            
            var movementKey = request.ProductId;
            _inventoryMovements.AddOrUpdate(movementKey, 
                new List<OilTrading.Application.DTOs.InventoryMovement> { movement },
                (key, movements) => { movements.Add(movement); return movements; });
            
            // Check thresholds and generate alerts
            await CheckThresholdsAsync(request.ProductId, request.LocationId, position);
            
            // Invalidate cache
            await InvalidateInventoryCache(request.ProductId, request.LocationId);
            
            return new InventoryOperationResult
            {
                IsSuccessful = true,
                UpdatedPosition = position,
                GeneratedMovements = { movement }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive inventory");
            return new InventoryOperationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<InventoryOperationResult> DeliverInventoryAsync(InventoryDeliveryRequest request)
    {
        _logger.LogInformation("Delivering inventory: {Quantity} of product {ProductId} from location {LocationId}", 
            request.DeliveredQuantity.Value, request.ProductId, request.LocationId);
        
        try
        {
            var positionKey = $"{request.ProductId}:{request.LocationId}";
            if (!_inventoryPositions.TryGetValue(positionKey, out var position))
            {
                return new InventoryOperationResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Inventory position not found"
                };
            }
            
            // Check availability
            if (position.AvailableQuantity.Value < request.DeliveredQuantity.Value)
            {
                return new InventoryOperationResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Insufficient inventory. Available: {position.AvailableQuantity.Value}, Requested: {request.DeliveredQuantity.Value}"
                };
            }
            
            // Update position
            var newAvailable = position.AvailableQuantity.Value - request.DeliveredQuantity.Value;
            var newTotal = position.TotalQuantity.Value - request.DeliveredQuantity.Value;
            
            position.AvailableQuantity = new Quantity(newAvailable, position.AvailableQuantity.Unit);
            position.TotalQuantity = new Quantity(newTotal, position.TotalQuantity.Unit);
            position.LastUpdated = DateTime.UtcNow;
            
            // Create movement record
            var movement = new OilTrading.Application.DTOs.InventoryMovement
            {
                MovementDate = request.DeliveryDate,
                ProductId = request.ProductId,
                FromLocationId = request.LocationId,
                // ToLocationId = request.LocationId,
                MovementType = OilTrading.Application.DTOs.InventoryMovementType.Shipment,
                Quantity = new Quantity(-request.DeliveredQuantity.Value, request.DeliveredQuantity.Unit), // Negative for outbound
                // MovementReference removed from request,
                PurchaseContractId = request.ContractId,
                // Notes property removed from request"
            };
            
            var movementKey = request.ProductId;
            _inventoryMovements.AddOrUpdate(movementKey,
                new List<OilTrading.Application.DTOs.InventoryMovement> { movement },
                (key, movements) => { movements.Add(movement); return movements; });
            
            // Check thresholds and generate alerts
            await CheckThresholdsAsync(request.ProductId, request.LocationId, position);
            
            // Invalidate cache
            await InvalidateInventoryCache(request.ProductId, request.LocationId);
            
            return new InventoryOperationResult
            {
                IsSuccessful = true,
                UpdatedPosition = position,
                GeneratedMovements = { movement }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver inventory");
            return new InventoryOperationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<InventoryOperationResult> TransferInventoryAsync(InventoryTransferRequest request)
    {
        _logger.LogInformation("Transferring inventory: {Quantity} of product {ProductId} from {FromLocation} to {ToLocation}", 
            request.TransferQuantity.Value, request.ProductId, request.FromLocationId, request.ToLocationId);
        
        try
        {
            // Execute as two operations: delivery from source and receipt at destination
            var deliveryRequest = new InventoryDeliveryRequest
            {
                ProductId = request.ProductId,
                LocationId = request.FromLocationId,
                DeliveredQuantity = request.TransferQuantity,
                // MovementReference removed from request,
                DeliveryDate = request.TransferDate,
                // Notes property removed from request"
            };
            
            var deliveryResult = await DeliverInventoryAsync(deliveryRequest);
            if (!deliveryResult.IsSuccessful)
            {
                return deliveryResult;
            }
            
            var receiptRequest = new InventoryReceiptRequest
            {
                ProductId = request.ProductId,
                LocationId = request.ToLocationId,
                ReceivedQuantity = request.TransferQuantity,
                UnitCost = request.TransferCost ?? 0,
                // MovementReference removed from request,
                ReceiptDate = request.TransferDate,
                // Notes property removed from request"
            };
            
            var receiptResult = await ReceiveInventoryAsync(receiptRequest);
            if (!receiptResult.IsSuccessful)
            {
                // Compensate for the delivery (add back the inventory)
                var compensationRequest = new InventoryReceiptRequest
                {
                    ProductId = request.ProductId,
                    LocationId = request.FromLocationId,
                    ReceivedQuantity = request.TransferQuantity,
                    UnitCost = 0,
                    // MovementReference = $"COMPENSATION:{request.Reference}",
                    ReceiptDate = request.TransferDate
                };
                
                await ReceiveInventoryAsync(compensationRequest);
                return receiptResult;
            }
            
            return new InventoryOperationResult
            {
                IsSuccessful = true,
                GeneratedMovements = deliveryResult.GeneratedMovements.Concat(receiptResult.GeneratedMovements).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer inventory");
            return new InventoryOperationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<InventoryOperationResult> AdjustInventoryAsync(InventoryAdjustmentRequest request)
    {
        _logger.LogInformation("Adjusting inventory: {Quantity} of product {ProductId} at location {LocationId}, reason: {Reason}", 
            request.AdjustmentQuantity.Value, request.ProductId, request.LocationId, request.Reason);
        
        try
        {
            var positionKey = $"{request.ProductId}:{request.LocationId}";
            var position = _inventoryPositions.GetOrAdd(positionKey, _ => CreateNewPosition(request.ProductId, request.LocationId));
            
            // Update position
            var newAvailable = Math.Max(0, position.AvailableQuantity.Value + request.AdjustmentQuantity.Value);
            var newTotal = Math.Max(0, position.TotalQuantity.Value + request.AdjustmentQuantity.Value);
            
            position.AvailableQuantity = new Quantity(newAvailable, position.AvailableQuantity.Unit);
            position.TotalQuantity = new Quantity(newTotal, position.TotalQuantity.Unit);
            position.LastUpdated = DateTime.UtcNow;
            
            // Create movement record
            var movement = new OilTrading.Application.DTOs.InventoryMovement
            {
                MovementDate = request.AdjustmentDate,
                ProductId = request.ProductId,
                FromLocationId = request.LocationId,
                // ToLocationId = request.LocationId,
                MovementType = OilTrading.Application.DTOs.InventoryMovementType.Adjustment,
                Quantity = request.AdjustmentQuantity,
                MovementReference = $"ADJ:{request.Reason}",
                // Notes property removed from request"
            };
            
            var movementKey = request.ProductId;
            _inventoryMovements.AddOrUpdate(movementKey,
                new List<OilTrading.Application.DTOs.InventoryMovement> { movement },
                (key, movements) => { movements.Add(movement); return movements; });
            
            // Check thresholds and generate alerts
            await CheckThresholdsAsync(request.ProductId, request.LocationId, position);
            
            // Invalidate cache
            await InvalidateInventoryCache(request.ProductId, request.LocationId);
            
            return new InventoryOperationResult
            {
                IsSuccessful = true,
                UpdatedPosition = position,
                GeneratedMovements = { movement }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust inventory");
            return new InventoryOperationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<OilTrading.Application.DTOs.InventoryAlert>> GetActiveInventoryAlertsAsync()
    {
        lock (_alertLock)
        {
            return _activeAlerts.Where(a => a.IsActive).ToList();
        }
    }

    public async Task<InventoryMetrics> GetInventoryMetricsAsync()
    {
        try
        {
            var positions = _inventoryPositions.Values.ToList();
            var totalValue = CalculateTotalValuation(positions);
            
            return new InventoryMetrics
            {
                TotalProducts = positions.Select(p => p.ProductId).Distinct().Count(),
                TotalLocations = positions.Select(p => p.LocationId).Distinct().Count(),
                TotalValue = totalValue,
                ValueByProduct = positions
                    .GroupBy(p => p.ProductName)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalQuantity.Value * p.AverageCost)),
                ValueByLocation = positions
                    .GroupBy(p => p.LocationName)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalQuantity.Value * p.AverageCost)),
                Turnover = CalculateTurnoverMetrics(positions),
                ActiveAlerts = await GetActiveInventoryAlertsAsync()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory metrics");
            return new InventoryMetrics();
        }
    }

    public async Task ConfigureInventoryThresholdsAsync(Guid productId, Guid locationId, InventoryThresholds thresholds)
    {
        var key = $"{productId}:{locationId}";
        _inventoryThresholds[key] = thresholds;
        
        _logger.LogInformation("Configured inventory thresholds for product {ProductId} at location {LocationId}", 
            productId, locationId);
    }

    public async Task<InventoryForecast> ForecastInventoryAsync(Guid productId, Guid locationId, int forecastDays)
    {
        _logger.LogInformation("Forecasting inventory for product {ProductId} at location {LocationId} for {Days} days", 
            productId, locationId, forecastDays);
        
        try
        {
            var currentPosition = _inventoryPositions.GetValueOrDefault($"{productId}:{locationId}");
            if (currentPosition == null)
            {
                return new InventoryForecast
                {
                    ProductId = productId,
                    LocationId = locationId,
                    ForecastDate = DateTime.UtcNow
                };
            }
            
            // Simple forecast based on historical movement patterns
            var historicalMovements = await GetInventoryMovementsAsync(
                DateTime.UtcNow.AddDays(-30), 
                DateTime.UtcNow, 
                productId);
            
            var avgDailyDemand = historicalMovements
                .Where(m => m.MovementType == OilTrading.Application.DTOs.InventoryMovementType.Shipment)
                .Sum(m => Math.Abs(m.Quantity.Value)) / 30;
            
            var avgDailySupply = historicalMovements
                .Where(m => m.MovementType == OilTrading.Application.DTOs.InventoryMovementType.Receipt)
                .Sum(m => m.Quantity.Value) / 30;
            
            var forecast = new InventoryForecast
            {
                ProductId = productId,
                LocationId = locationId,
                ForecastDate = DateTime.UtcNow
            };
            
            var currentLevel = currentPosition.AvailableQuantity.Value;
            
            for (int day = 1; day <= forecastDays; day++)
            {
                var demandVariation = (decimal)(Random.Shared.NextDouble() * 0.3 - 0.15); // ±15% variation
                var supplyVariation = (decimal)(Random.Shared.NextDouble() * 0.3 - 0.15);
                
                var predictedDemand = avgDailyDemand * (1 + demandVariation);
                var predictedSupply = avgDailySupply * (1 + supplyVariation);
                
                currentLevel = Math.Max(0, currentLevel - predictedDemand + predictedSupply);
                
                forecast.Periods.Add(new InventoryForecastPeriod
                {
                    Date = DateTime.UtcNow.AddDays(day),
                    PredictedLevel = new Quantity(currentLevel, QuantityUnit.BBL),
                    PredictedDemand = new Quantity(predictedDemand, QuantityUnit.BBL),
                    PredictedSupply = new Quantity(predictedSupply, QuantityUnit.BBL),
                    ConfidenceLevel = (decimal)Math.Max(0.5, 1.0 - (day * 0.05)) // Decreasing confidence over time
                });
            }
            
            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forecast inventory");
            return new InventoryForecast
            {
                ProductId = productId,
                LocationId = locationId,
                ForecastDate = DateTime.UtcNow
            };
        }
    }

    public async Task<List<InventoryRecommendation>> GetInventoryRecommendationsAsync()
    {
        var recommendations = new List<InventoryRecommendation>();
        
        try
        {
            foreach (var position in _inventoryPositions.Values)
            {
                var thresholdKey = $"{position.ProductId}:{position.LocationId}";
                if (_inventoryThresholds.TryGetValue(thresholdKey, out var thresholds))
                {
                    // Low stock recommendation
                    if (position.AvailableQuantity.Value <= thresholds.ReorderLevel.Value)
                    {
                        var reorderQuantity = thresholds.MaximumLevel.Value - position.AvailableQuantity.Value;
                        recommendations.Add(new InventoryRecommendation
                        {
                            ProductId = position.ProductId,
                            // FromLocationId = position.LocationId,
                // ToLocationId = position.LocationId,
                            Type = InventoryRecommendationType.Reorder,
                            Description = $"Reorder {reorderQuantity} units - below reorder level",
                            RecommendedQuantity = new Quantity(reorderQuantity, position.AvailableQuantity.Unit),
                            RecommendedDate = DateTime.UtcNow.AddDays(1),
                            Priority = position.AvailableQuantity.Value <= thresholds.MinimumLevel.Value ? 
                                InventoryRecommendationPriority.Critical : InventoryRecommendationPriority.High
                        });
                    }
                    
                    // High stock recommendation
                    if (position.AvailableQuantity.Value > thresholds.MaximumLevel.Value)
                    {
                        var excessQuantity = position.AvailableQuantity.Value - thresholds.MaximumLevel.Value;
                        recommendations.Add(new InventoryRecommendation
                        {
                            ProductId = position.ProductId,
                            // FromLocationId = position.LocationId,
                // ToLocationId = position.LocationId,
                            Type = InventoryRecommendationType.Transfer,
                            Description = $"Transfer {excessQuantity} units - above maximum level",
                            RecommendedQuantity = new Quantity(excessQuantity, position.AvailableQuantity.Unit),
                            RecommendedDate = DateTime.UtcNow.AddDays(3),
                            Priority = InventoryRecommendationPriority.Medium
                        });
                    }
                }
            }
            
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory recommendations");
            return recommendations;
        }
    }

    public async Task<InventoryAvailabilityCheck> CheckAvailabilityForContractAsync(Guid contractId)
    {
        _logger.LogInformation("Checking inventory availability for contract {ContractId}", contractId);
        
        try
        {
            // In a real implementation, we would fetch contract details from the repository
            // For demo purposes, we'll create a sample requirement
            var requiredQuantity = new Quantity(10000, QuantityUnit.BBL);
            var productId = Guid.NewGuid(); // Sample product ID
            
            var availablePositions = _inventoryPositions.Values
                .Where(p => p.ProductId == productId && p.AvailableQuantity.Value > 0)
                .OrderByDescending(p => p.AvailableQuantity.Value)
                .ToList();
            
            var totalAvailable = availablePositions.Sum(p => p.AvailableQuantity.Value);
            var isAvailable = totalAvailable >= requiredQuantity.Value;
            var shortfall = Math.Max(0, requiredQuantity.Value - totalAvailable);
            
            var allocationOptions = availablePositions.Select(p => new InventoryAllocationOption
            {
                LocationId = p.LocationId,
                LocationName = p.LocationName,
                AvailableQuantity = p.AvailableQuantity,
                AllocationScore = CalculateAllocationScore(p)
            }).ToList();
            
            return new InventoryAvailabilityCheck
            {
                ContractId = contractId,
                IsAvailable = isAvailable,
                RequiredQuantity = requiredQuantity,
                AvailableQuantity = new Quantity(totalAvailable, QuantityUnit.BBL),
                Shortfall = new Quantity(shortfall, QuantityUnit.BBL),
                AllocationOptions = allocationOptions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check inventory availability for contract {ContractId}", contractId);
            return new InventoryAvailabilityCheck { ContractId = contractId };
        }
    }

    public async Task<InventoryAllocationResult> AllocateInventoryForContractAsync(Guid contractId, InventoryAllocationRequest request)
    {
        _logger.LogInformation("Allocating inventory for contract {ContractId}", contractId);
        
        try
        {
            var result = new InventoryAllocationResult();
            var remainingRequirement = request.RequestedQuantity.Value;
            
            // Get available positions for the product (simplified - would get from contract)
            var availablePositions = _inventoryPositions.Values
                .Where(p => p.AvailableQuantity.Value > 0)
                .ToList();
            
            // Apply allocation strategy
            switch (request.Strategy)
            {
                case InventoryAllocationStrategy.FirstAvailable:
                    availablePositions = availablePositions.OrderBy(p => p.LocationId).ToList();
                    break;
                case InventoryAllocationStrategy.LowestCost:
                    availablePositions = availablePositions.OrderBy(p => p.AverageCost).ToList();
                    break;
                default:
                    availablePositions = availablePositions.OrderByDescending(p => p.AvailableQuantity.Value).ToList();
                    break;
            }
            
            foreach (var position in availablePositions)
            {
                if (remainingRequirement <= 0) break;
                
                var allocationQuantity = Math.Min(remainingRequirement, position.AvailableQuantity.Value);
                
                // Create allocation
                var allocation = new DtoInventoryAllocation
                {
                    // FromLocationId = position.LocationId,
                // ToLocationId = position.LocationId,
                    AllocatedQuantity = new Quantity(allocationQuantity, position.AvailableQuantity.Unit)
                };
                
                result.Allocations.Add(allocation);
                
                // Update position (reserve the quantity)
                var newReserved = position.ReservedQuantity.Value + allocationQuantity;
                var newAvailable = position.AvailableQuantity.Value - allocationQuantity;
                
                position.ReservedQuantity = new Quantity(newReserved, position.ReservedQuantity.Unit);
                position.AvailableQuantity = new Quantity(newAvailable, position.AvailableQuantity.Unit);
                position.LastUpdated = DateTime.UtcNow;
                
                remainingRequirement -= allocationQuantity;
            }
            
            result.IsSuccessful = remainingRequirement <= 0;
            result.TotalAllocated = new Quantity(request.RequestedQuantity.Value - remainingRequirement, request.RequestedQuantity.Unit);
            result.RemainingRequirement = new Quantity(remainingRequirement, request.RequestedQuantity.Unit);
            
            if (!result.IsSuccessful)
            {
                result.ErrorMessage = $"Insufficient inventory. Short by {remainingRequirement} units.";
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to allocate inventory for contract {ContractId}", contractId);
            return new InventoryAllocationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<InventoryOptimizationResult> OptimizeInventoryDistributionAsync(InventoryOptimizationRequest request)
    {
        _logger.LogInformation("Optimizing inventory distribution for {ProductCount} products across {LocationCount} locations", 
            request.ProductIds.Count, request.LocationIds.Count);
        
        try
        {
            var result = new InventoryOptimizationResult();
            
            // Simple optimization: balance inventory across locations
            foreach (var productId in request.ProductIds)
            {
                var productPositions = _inventoryPositions.Values
                    .Where(p => p.ProductId == productId && 
                               (request.LocationIds.Count == 0 || request.LocationIds.Contains(p.LocationId)))
                    .ToList();
                
                if (productPositions.Count < 2) continue;
                
                var totalQuantity = productPositions.Sum(p => p.AvailableQuantity.Value);
                var targetQuantityPerLocation = totalQuantity / productPositions.Count;
                
                foreach (var position in productPositions)
                {
                    var currentQuantity = position.AvailableQuantity.Value;
                    var difference = currentQuantity - targetQuantityPerLocation;
                    
                    if (Math.Abs(difference) > targetQuantityPerLocation * 0.2m) // 20% threshold
                    {
                        if (difference > 0) // Excess inventory
                        {
                            var targetLocation = productPositions
                                .Where(p => p.LocationId != position.LocationId && 
                                           p.AvailableQuantity.Value < targetQuantityPerLocation)
                                .OrderBy(p => p.AvailableQuantity.Value)
                                .FirstOrDefault();
                            
                            if (targetLocation != null)
                            {
                                var transferQuantity = Math.Min(difference, targetQuantityPerLocation - targetLocation.AvailableQuantity.Value);
                                
                                result.Recommendations.Add(new InventoryOptimizationRecommendation
                                {
                                    ProductId = productId,
                                    // FromLocationId = position.LocationId,
                                    // ToLocationId = targetLocation.LocationId,
                                    RecommendedQuantity = new Quantity(transferQuantity, position.AvailableQuantity.Unit),
                                    Rationale = "Balance inventory distribution across locations",
                                    EstimatedCostSaving = transferQuantity * 0.1m, // Simplified calculation
                                    Priority = InventoryRecommendationPriority.Medium
                                });
                            }
                        }
                    }
                }
            }
            
            result.IsSuccessful = true;
            result.EstimatedSavings = result.Recommendations.Sum(r => r.EstimatedCostSaving);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize inventory distribution");
            return new InventoryOptimizationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<InventoryRebalanceRecommendation>> GetRebalanceRecommendationsAsync()
    {
        try
        {
            var recommendations = new List<InventoryRebalanceRecommendation>();
            
            var productGroups = _inventoryPositions.Values.GroupBy(p => p.ProductId);
            
            foreach (var productGroup in productGroups)
            {
                var positions = productGroup.ToList();
                if (positions.Count < 2) continue;
                
                var productRecommendation = new InventoryRebalanceRecommendation
                {
                    ProductId = productGroup.Key,
                    ProductName = positions.First().ProductName
                };
                
                // Identify imbalances
                var totalQuantity = positions.Sum(p => p.AvailableQuantity.Value);
                var averageQuantity = totalQuantity / positions.Count;
                
                foreach (var position in positions)
                {
                    var deviation = position.AvailableQuantity.Value - averageQuantity;
                    
                    if (Math.Abs(deviation) > averageQuantity * 0.3m) // 30% deviation threshold
                    {
                        if (deviation > 0)
                        {
                            productRecommendation.Actions.Add(new InventoryRebalanceAction
                            {
                                ActionType = InventoryRebalanceActionType.Transfer,
                                SourceLocationId = position.LocationId,
                                Quantity = new Quantity(deviation * 0.5m, position.AvailableQuantity.Unit),
                                Rationale = "Excess inventory at location"
                            });
                        }
                        else
                        {
                            productRecommendation.Actions.Add(new InventoryRebalanceAction
                            {
                                ActionType = InventoryRebalanceActionType.Reorder,
                                TargetLocationId = position.LocationId,
                                Quantity = new Quantity(Math.Abs(deviation) * 0.5m, position.AvailableQuantity.Unit),
                                Rationale = "Low inventory at location"
                            });
                        }
                    }
                }
                
                if (productRecommendation.Actions.Any())
                {
                    productRecommendation.EstimatedBenefit = productRecommendation.Actions.Sum(a => a.Quantity.Value * 0.05m);
                    productRecommendation.Priority = productRecommendation.EstimatedBenefit > 1000 ? 
                        InventoryRecommendationPriority.High : InventoryRecommendationPriority.Medium;
                    
                    recommendations.Add(productRecommendation);
                }
            }
            
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rebalance recommendations");
            return new List<InventoryRebalanceRecommendation>();
        }
    }

    // Implementation of missing methods
    public async Task<InventoryReservationResult> ReserveInventoryAsync(InventoryReservationRequest request)
    {
        _logger.LogInformation("Reserving inventory: {Quantity} of product {ProductCode} at location {LocationCode}", 
            request.Quantity.Value, request.ProductCode, request.LocationCode);

        try
        {
            // Find the inventory position
            var positionKey = $"{request.ProductId}:{request.LocationId}";
            if (!_inventoryPositions.TryGetValue(positionKey, out var position))
            {
                return new InventoryReservationResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"No inventory found for product {request.ProductCode} at location {request.LocationCode}",
                    ReservedQuantity = new Quantity(0, request.Quantity.Unit)
                };
            }

            // Check availability
            if (position.AvailableQuantity.Value < request.Quantity.Value)
            {
                return new InventoryReservationResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Insufficient inventory. Available: {position.AvailableQuantity.Value}, Requested: {request.Quantity.Value}",
                    ReservedQuantity = new Quantity(0, request.Quantity.Unit)
                };
            }

            // Update position to reserve the inventory
            var newReserved = position.ReservedQuantity.Value + request.Quantity.Value;
            var newAvailable = position.AvailableQuantity.Value - request.Quantity.Value;

            position.ReservedQuantity = new Quantity(newReserved, position.ReservedQuantity.Unit);
            position.AvailableQuantity = new Quantity(newAvailable, position.AvailableQuantity.Unit);
            position.LastUpdated = DateTime.UtcNow;

            // Create movement record for reservation
            var movement = new OilTrading.Application.DTOs.InventoryMovement
            {
                ProductId = request.ProductId,
                FromLocationId = request.LocationId,
                // ToLocationId = request.LocationId,
                MovementType = OilTrading.Application.DTOs.InventoryMovementType.Adjustment,
                Quantity = new Quantity(-request.Quantity.Value, request.Quantity.Unit), // Negative for reservation
                MovementReference = request.ReservationReference,
                // Notes property removed from request"
            };

            var movementKey = request.ProductId;
            _inventoryMovements.AddOrUpdate(movementKey,
                new List<OilTrading.Application.DTOs.InventoryMovement> { movement },
                (key, movements) => { movements.Add(movement); return movements; });

            // Invalidate cache
            await InvalidateInventoryCache(request.ProductId, request.LocationId);

            var reservationId = Guid.NewGuid();
            return new InventoryReservationResult
            {
                IsSuccessful = true,
                ReservationId = reservationId,
                ReservedQuantity = request.Quantity,
                ReservationDetails = new List<ReservationDetail>
                {
                    new ReservationDetail
                    {
                        LocationId = request.LocationId,
                        LocationName = position.LocationName,
                        Quantity = request.Quantity
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve inventory");
            return new InventoryReservationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ReservedQuantity = new Quantity(0, request.Quantity.Unit)
            };
        }
    }

    public async Task<InventoryOperationResult> ReleaseReservationAsync(InventoryReleaseRequest request)
    {
        _logger.LogInformation("Releasing inventory reservation: {Quantity} for reference {Reference}", 
            request.Quantity.Value, request.ReservationReference);

        try
        {
            // For simplicity, we'll find the position by reference
            // In a real implementation, we'd have a reservation tracking system
            var position = _inventoryPositions.Values
                .FirstOrDefault(p => p.Tags.Contains(request.ReservationReference ?? ""));

            if (position == null)
            {
                _logger.LogWarning("Could not find inventory position for reservation reference {Reference}", 
                    request.ReservationReference);
                return new InventoryOperationResult
                {
                    IsSuccessful = true, // Consider it successful to avoid blocking other operations
                    ErrorMessage = "Reservation not found, but operation considered successful"
                };
            }

            // Release the reservation
            var releaseQuantity = request.Quantity ?? request.PartialQuantity ?? new Quantity(0, QuantityUnit.BBL);
            var newReserved = Math.Max(0, position.ReservedQuantity.Value - releaseQuantity.Value);
            var newAvailable = position.AvailableQuantity.Value + releaseQuantity.Value;

            position.ReservedQuantity = new Quantity(newReserved, position.ReservedQuantity.Unit);
            position.AvailableQuantity = new Quantity(newAvailable, position.AvailableQuantity.Unit);
            position.LastUpdated = DateTime.UtcNow;

            // Create movement record for release
            var movement = new OilTrading.Application.DTOs.InventoryMovement
            {
                ProductId = position.ProductId,
                FromLocationId = position.LocationId,
                // ToLocationId = position.LocationId,
                MovementType = OilTrading.Application.DTOs.InventoryMovementType.Adjustment,
                Quantity = releaseQuantity, // Positive for release
                MovementReference = $"RELEASE:{request.ReservationReference}",
                // Notes property removed from request"
            };

            var movementKey = position.ProductId;
            _inventoryMovements.AddOrUpdate(movementKey,
                new List<OilTrading.Application.DTOs.InventoryMovement> { movement },
                (key, movements) => { movements.Add(movement); return movements; });

            // Invalidate cache
            await InvalidateInventoryCache(position.ProductId, position.LocationId);

            return new InventoryOperationResult
            {
                IsSuccessful = true,
                UpdatedPosition = position,
                GeneratedMovements = new List<OilTrading.Application.DTOs.InventoryMovement> { movement }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release inventory reservation");
            return new InventoryOperationResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<InventorySnapshot> GetRealTimeInventoryAsync(string productCode, string locationCode)
    {
        _logger.LogInformation("Getting real-time inventory for product {ProductCode} at location {LocationCode}", 
            productCode, locationCode);

        try
        {
            var positions = _inventoryPositions.Values
                .Where(p => (string.IsNullOrEmpty(productCode) || p.ProductName.Contains(productCode)) &&
                           (string.IsNullOrEmpty(locationCode) || p.LocationName.Contains(locationCode)))
                .ToList();

            return new InventorySnapshot
            {
                Positions = positions,
                TotalsByProduct = positions
                    .GroupBy(p => p.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalQuantity.Value)),
                TotalsByLocation = positions
                    .GroupBy(p => p.LocationId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalQuantity.Value)),
                TotalValuation = CalculateTotalValuation(positions)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time inventory for product {ProductCode} at location {LocationCode}", 
                productCode, locationCode);
            return new InventorySnapshot();
        }
    }


    // Helper methods
    private void InitializeSampleData()
    {
        // Create sample inventory positions
        var sampleProducts = new[]
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Brent Crude"),
            (Guid.Parse("22222222-2222-2222-2222-222222222222"), "WTI Crude"),
            (Guid.Parse("33333333-3333-3333-3333-333333333333"), "MOPS FO 380"),
            (Guid.Parse("44444444-4444-4444-4444-444444444444"), "MOPS MGO")
        };
        
        var sampleLocations = new[]
        {
            (Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Singapore Terminal"),
            (Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Rotterdam Terminal"),
            (Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Houston Terminal")
        };
        
        foreach (var (productId, productName) in sampleProducts)
        {
            foreach (var (locationId, locationName) in sampleLocations)
            {
                var quantity = Random.Shared.Next(5000, 50000);
                var reserved = Random.Shared.Next(0, quantity / 4);
                var cost = Random.Shared.Next(50, 100);
                
                var position = new InventoryPosition
                {
                    ProductId = productId,
                    ProductName = productName,
                    LocationId = locationId,
                    LocationName = locationName,
                    AvailableQuantity = new Quantity(quantity - reserved, QuantityUnit.BBL),
                    ReservedQuantity = new Quantity(reserved, QuantityUnit.BBL),
                    TotalQuantity = new Quantity(quantity, QuantityUnit.BBL),
                    AverageCost = cost,
                    Status = InventoryStatus.Available,
                    LastUpdated = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 24))
                };
                
                _inventoryPositions[$"{productId}:{locationId}"] = position;
                
                // Configure sample thresholds
                _inventoryThresholds[$"{productId}:{locationId}"] = new InventoryThresholds
                {
                    MinimumLevel = new Quantity(quantity * 0.1m, QuantityUnit.BBL),
                    MaximumLevel = new Quantity(quantity * 1.5m, QuantityUnit.BBL),
                    ReorderLevel = new Quantity(quantity * 0.2m, QuantityUnit.BBL),
                    SafetyStock = new Quantity(quantity * 0.05m, QuantityUnit.BBL)
                };
            }
        }
        
        _logger.LogInformation("Initialized sample inventory data with {PositionCount} positions", _inventoryPositions.Count);
    }

    private InventoryPosition CreateNewPosition(Guid productId, Guid locationId)
    {
        return new InventoryPosition
        {
            ProductId = productId,
            ProductName = "Unknown Product",
            LocationId = locationId,
            LocationName = "Unknown Location",
            AvailableQuantity = new Quantity(0, QuantityUnit.BBL),
            ReservedQuantity = new Quantity(0, QuantityUnit.BBL),
            TotalQuantity = new Quantity(0, QuantityUnit.BBL),
            AverageCost = 0,
            Status = InventoryStatus.Available,
            LastUpdated = DateTime.UtcNow
        };
    }

    private InventoryValuation CalculateTotalValuation(List<InventoryPosition> positions)
    {
        var totalValue = positions.Sum(p => p.TotalQuantity.Value * p.AverageCost);
        
        return new InventoryValuation
        {
            TotalValue = totalValue,
            Currency = "USD",
            Method = InventoryValuationMethod.WeightedAverage
        };
    }

    private InventoryTurnoverMetrics CalculateTurnoverMetrics(List<InventoryPosition> positions)
    {
        // Simplified calculation
        var avgInventoryValue = positions.Sum(p => p.TotalQuantity.Value * p.AverageCost);
        var annualCOGS = avgInventoryValue * 4; // Assumed 4x turnover
        var turnoverRatio = avgInventoryValue > 0 ? annualCOGS / avgInventoryValue : 0;
        var daysOnHand = turnoverRatio > 0 ? TimeSpan.FromDays((double)(365m / turnoverRatio)) : TimeSpan.Zero;
        
        return new InventoryTurnoverMetrics
        {
            InventoryTurnoverRatio = turnoverRatio,
            AverageDaysOnHand = daysOnHand
        };
    }

    private async Task CheckThresholdsAsync(Guid productId, Guid locationId, InventoryPosition position)
    {
        var thresholdKey = $"{productId}:{locationId}";
        if (!_inventoryThresholds.TryGetValue(thresholdKey, out var thresholds) || !thresholds.EnableAlerts)
        {
            return;
        }
        
        lock (_alertLock)
        {
            // Check for low stock
            if (position.AvailableQuantity.Value <= thresholds.MinimumLevel.Value)
            {
                var existingAlert = _activeAlerts.FirstOrDefault(a => 
                    a.ProductId == productId && 
                    a.LocationId == locationId && 
                    a.AlertType == OilTrading.Application.DTOs.InventoryAlertType.LowStock && 
                    a.IsActive);
                
                if (existingAlert == null)
                {
                    _activeAlerts.Add(new OilTrading.Application.DTOs.InventoryAlert
                    {
                        ProductId = productId,
                        LocationId = locationId,
                        AlertType = OilTrading.Application.DTOs.InventoryAlertType.LowStock,
                        Message = "Low Stock Alert",
                        Severity = position.AvailableQuantity.Value <= thresholds.SafetyStock.Value ? 
                            OilTrading.Application.DTOs.InventoryAlertSeverity.Critical : OilTrading.Application.DTOs.InventoryAlertSeverity.High,
                        CurrentQuantity = position.AvailableQuantity,
                        ThresholdQuantity = thresholds.MinimumLevel,
                        // Notes property removed from request"
                    });
                }
            }
            
            // Check for high stock
            if (position.AvailableQuantity.Value > thresholds.MaximumLevel.Value)
            {
                var existingAlert = _activeAlerts.FirstOrDefault(a => 
                    a.ProductId == productId && 
                    a.LocationId == locationId && 
                    a.AlertType == OilTrading.Application.DTOs.InventoryAlertType.HighStock && 
                    a.IsActive);
                
                if (existingAlert == null)
                {
                    _activeAlerts.Add(new OilTrading.Application.DTOs.InventoryAlert
                    {
                        ProductId = productId,
                        LocationId = locationId,
                        AlertType = OilTrading.Application.DTOs.InventoryAlertType.HighStock,
                        Message = "High Stock Alert",
                        Severity = OilTrading.Application.DTOs.InventoryAlertSeverity.Medium,
                        CurrentQuantity = position.AvailableQuantity,
                        ThresholdQuantity = thresholds.MaximumLevel,
                        // Notes property removed from request"
                    });
                }
            }
        }
    }

    private async Task InvalidateInventoryCache(Guid productId, Guid locationId)
    {
        var patterns = new[]
        {
            $"inventory:snapshot:{productId}:{locationId}",
            $"inventory:snapshot:{productId}:",
            $"inventory:snapshot::",
            "inventory:metrics"
        };
        
        foreach (var pattern in patterns)
        {
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }

    private decimal CalculateAllocationScore(InventoryPosition position)
    {
        // Simple scoring based on available quantity and cost
        var quantityScore = Math.Min(100, position.AvailableQuantity.Value / 1000m);
        var costScore = Math.Max(0, 100 - position.AverageCost);
        
        return (quantityScore + costScore) / 2;
    }
}

public class RealTimeInventoryOptions
{
    public bool EnableRealTimeUpdates { get; set; } = true;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableAlerts { get; set; } = true;
    public int MaxHistoryDays { get; set; } = 90;
    public bool EnableForecasting { get; set; } = true;
}

// Note: InventoryAlert, InventoryAlertType, and InventoryAlertSeverity are defined in InventoryDTOs.cs