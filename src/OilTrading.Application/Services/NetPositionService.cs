using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public interface INetPositionService
{
    Task<IEnumerable<NetPositionDto>> CalculateNetPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NetPositionDto>> CalculateMonthlyNetPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NetPositionDto>> CalculateRealTimePositionsAsync(CancellationToken cancellationToken = default);
    Task<PositionSummaryDto> GetPositionSummaryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PnLDto>> CalculatePnLAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExposureDto>> CalculateExposureByProductAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ExposureDto>> CalculateExposureByCounterpartyAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckPositionLimitsAsync(CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════════
    // HEDGE LINKING METHODS (Data Lineage Enhancement)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculate positions with explicit hedge linkage information.
    /// Returns positions showing which paper contracts hedge which physical contracts.
    /// </summary>
    Task<IEnumerable<HedgedPositionDto>> CalculateHedgedPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get hedge effectiveness metrics for a specific physical contract.
    /// Shows all paper hedges linked to the physical contract and their effectiveness.
    /// </summary>
    Task<HedgeEffectivenessDto?> GetHedgeEffectivenessAsync(Guid physicalContractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all unhedged physical positions (positions without linked paper hedges).
    /// </summary>
    Task<IEnumerable<UnhedgedPositionDto>> GetUnhedgedPositionsAsync(CancellationToken cancellationToken = default);
}

public class NetPositionService : INetPositionService
{
    private readonly IPhysicalContractRepository _physicalContractRepository;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IPositionCacheService _cacheService;

    public NetPositionService(
        IPhysicalContractRepository physicalContractRepository,
        IPaperContractRepository paperContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IShippingOperationRepository shippingOperationRepository,
        IMarketDataRepository marketDataRepository,
        IPositionCacheService cacheService)
    {
        _physicalContractRepository = physicalContractRepository;
        _paperContractRepository = paperContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _shippingOperationRepository = shippingOperationRepository;
        _marketDataRepository = marketDataRepository;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<NetPositionDto>> CalculateNetPositionsAsync(CancellationToken cancellationToken = default)
    {
        var positions = new List<NetPositionDto>();
        
        // Get all active contracts (optimized for position calculations)
        var physicalContracts = await _physicalContractRepository.GetActiveContractsForPositionAsync(cancellationToken);
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        
        // Group by product type and month
        var groupedData = new Dictionary<(string product, string month), NetPositionDto>();
        
        // Process physical contracts
        foreach (var contract in physicalContracts)
        {
            var month = contract.LaycanStart.ToString("MMMyy").ToUpper();
            var key = (contract.ProductType, month);
            
            if (!groupedData.ContainsKey(key))
            {
                groupedData[key] = new NetPositionDto
                {
                    ProductType = contract.ProductType,
                    Month = month
                };
            }
            
            if (contract.ContractType == Core.Entities.PhysicalContractType.Purchase)
            {
                groupedData[key].PhysicalPurchases += contract.Quantity;
            }
            else
            {
                groupedData[key].PhysicalSales += contract.Quantity;
            }
        }
        
        // Process paper contracts
        foreach (var contract in paperContracts)
        {
            var key = (contract.ProductType, contract.ContractMonth);
            
            if (!groupedData.ContainsKey(key))
            {
                groupedData[key] = new NetPositionDto
                {
                    ProductType = contract.ProductType,
                    Month = contract.ContractMonth
                };
            }
            
            if (contract.Position == Core.Entities.PositionType.Long)
            {
                groupedData[key].PaperLongPosition += contract.Quantity;
            }
            else
            {
                groupedData[key].PaperShortPosition += contract.Quantity;
            }
        }
        
        // Calculate net positions and exposure
        foreach (var position in groupedData.Values)
        {
            position.PhysicalNetPosition = position.PhysicalPurchases - position.PhysicalSales;
            position.PaperNetPosition = position.PaperLongPosition - position.PaperShortPosition;
            position.TotalNetPosition = position.PhysicalNetPosition + position.PaperNetPosition;
            
            // Determine position status
            if (Math.Abs(position.TotalNetPosition) < 100)
            {
                position.PositionStatus = "Flat";
            }
            else if (position.TotalNetPosition > 0)
            {
                position.PositionStatus = "Long";
            }
            else
            {
                position.PositionStatus = "Short";
            }
            
            // Calculate exposure value (simplified - should use market prices)
            var estimatedPrice = GetEstimatedPrice(position.ProductType);
            position.ExposureValue = Math.Abs(position.TotalNetPosition) * estimatedPrice;
            
            positions.Add(position);
        }
        
        return positions.OrderBy(p => p.ProductType).ThenBy(p => p.Month);
    }
    
    public async Task<IEnumerable<NetPositionDto>> CalculateMonthlyNetPositionsAsync(CancellationToken cancellationToken = default)
    {
        // Similar to above but with monthly aggregation
        return await CalculateNetPositionsAsync(cancellationToken);
    }

    public async Task<IEnumerable<NetPositionDto>> CalculateRealTimePositionsAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cachedPositions = await _cacheService.GetCachedPositionsAsync(cancellationToken);
        if (cachedPositions != null && await _cacheService.IsCacheValidAsync(cancellationToken))
        {
            return cachedPositions;
        }

        var positions = new List<NetPositionDto>();
        
        // Get all active contracts with real-time data
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        var physicalContracts = await _physicalContractRepository.GetActiveContractsForPositionAsync(cancellationToken);
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        
        // Group by product type and laycan month
        var groupedData = new Dictionary<(string product, string month), NetPositionDto>();
        
        // Process purchase contracts
        // Include both Draft and Active contracts for real-time position calculation
        // Draft contracts represent ongoing negotiations and are critical for traders to see
        // This follows commodity trading best practice: traders need visibility into pending positions
        foreach (var contract in purchaseContracts)
        {
            // Include Draft and Active contracts - only skip Cancelled and Completed
            if (contract.Status == Core.Entities.ContractStatus.Cancelled ||
                contract.Status == Core.Entities.ContractStatus.Completed) continue;

            var productType = contract.Product?.Type.ToString() ?? "Unknown";

            // Safely get Laycan month - use current month if not set (important for new contracts)
            var monthDate = contract.LaycanStart ?? DateTime.UtcNow;
            var month = monthDate.ToString("MMMyy").ToUpper();
            var key = (productType, month);

            if (!groupedData.ContainsKey(key))
            {
                groupedData[key] = new NetPositionDto
                {
                    ProductType = productType,
                    Month = month
                };
            }

            groupedData[key].PurchaseContractQuantity += contract.ContractQuantity.Value;
        }

        // Process sales contracts
        // Include both Draft and Active contracts for real-time position calculation
        // Draft contracts represent ongoing negotiations and are critical for traders to see
        foreach (var contract in salesContracts)
        {
            // Include Draft and Active contracts - only skip Cancelled and Completed
            if (contract.Status == Core.Entities.ContractStatus.Cancelled ||
                contract.Status == Core.Entities.ContractStatus.Completed) continue;

            var productType = contract.Product?.Type.ToString() ?? "Unknown";

            // Safely get Laycan month - use current month if not set (important for new contracts)
            var monthDate = contract.LaycanStart ?? DateTime.UtcNow;
            var month = monthDate.ToString("MMMyy").ToUpper();
            var key = (productType, month);

            if (!groupedData.ContainsKey(key))
            {
                groupedData[key] = new NetPositionDto
                {
                    ProductType = productType,
                    Month = month
                };
            }

            groupedData[key].SalesContractQuantity += contract.ContractQuantity.Value;
        }
        
        // Process physical contracts
        foreach (var contract in physicalContracts)
        {
            var month = contract.LaycanStart.ToString("MMMyy").ToUpper();
            var key = (contract.ProductType, month);
            
            if (!groupedData.ContainsKey(key))
            {
                groupedData[key] = new NetPositionDto
                {
                    ProductType = contract.ProductType,
                    Month = month
                };
            }
            
            if (contract.ContractType == Core.Entities.PhysicalContractType.Purchase)
            {
                groupedData[key].PhysicalPurchases += contract.Quantity;
            }
            else
            {
                groupedData[key].PhysicalSales += contract.Quantity;
            }
        }
        
        // Process paper contracts
        foreach (var contract in paperContracts)
        {
            var key = (contract.ProductType, contract.ContractMonth);
            
            if (!groupedData.ContainsKey(key))
            {
                groupedData[key] = new NetPositionDto
                {
                    ProductType = contract.ProductType,
                    Month = contract.ContractMonth
                };
            }
            
            if (contract.Position == Core.Entities.PositionType.Long)
            {
                groupedData[key].PaperLongPosition += contract.Quantity;
            }
            else
            {
                groupedData[key].PaperShortPosition += contract.Quantity;
            }
        }
        
        // Calculate net positions and exposure with real market data
        foreach (var position in groupedData.Values)
        {
            // Physical net position
            position.PhysicalNetPosition = position.PhysicalPurchases - position.PhysicalSales;
            
            // Contract net position
            position.ContractNetPosition = position.PurchaseContractQuantity - position.SalesContractQuantity;
            
            // Paper net position
            position.PaperNetPosition = position.PaperLongPosition - position.PaperShortPosition;
            
            // Total net position
            position.TotalNetPosition = position.PhysicalNetPosition + position.ContractNetPosition + position.PaperNetPosition;
            
            // Determine position status
            if (Math.Abs(position.TotalNetPosition) < 100)
            {
                position.PositionStatus = "Flat";
            }
            else if (position.TotalNetPosition > 0)
            {
                position.PositionStatus = "Long";
            }
            else
            {
                position.PositionStatus = "Short";
            }
            
            // Calculate exposure value using real market prices
            var marketPrice = await GetCurrentMarketPriceAsync(position.ProductType, cancellationToken);
            position.ExposureValue = Math.Abs(position.TotalNetPosition) * marketPrice;
            position.MarketPrice = marketPrice;
            
            positions.Add(position);
        }
        
        var sortedPositions = positions.OrderBy(p => p.ProductType).ThenBy(p => p.Month).ToList();
        
        // Cache the calculated positions
        await _cacheService.SetCachedPositionsAsync(sortedPositions, cancellationToken: cancellationToken);
        
        return sortedPositions;
    }

    public async Task<PositionSummaryDto> GetPositionSummaryAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cachedSummary = await _cacheService.GetCachedSummaryAsync(cancellationToken);
        if (cachedSummary != null && await _cacheService.IsCacheValidAsync(cancellationToken))
        {
            return cachedSummary;
        }

        var positions = await CalculateRealTimePositionsAsync(cancellationToken);
        
        var summary = new PositionSummaryDto
        {
            TotalContracts = positions.Count(),
            TotalExposure = positions.Sum(p => p.ExposureValue),
            LongPositions = positions.Count(p => p.PositionStatus == "Long"),
            ShortPositions = positions.Count(p => p.PositionStatus == "Short"),
            FlatPositions = positions.Count(p => p.PositionStatus == "Flat"),
            LargestExposure = positions.Any() ? positions.Max(p => p.ExposureValue) : 0,
            SmallestExposure = positions.Any() ? positions.Min(p => p.ExposureValue) : 0,
            CalculatedAt = DateTime.UtcNow
        };

        // Cache the summary
        await _cacheService.SetCachedSummaryAsync(summary, cancellationToken: cancellationToken);

        return summary;
    }

    public async Task<IEnumerable<PnLDto>> CalculatePnLAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var valueDate = asOfDate ?? DateTime.UtcNow.Date;

        // Try to get from cache first
        var cachedPnL = await _cacheService.GetCachedPnLAsync(valueDate, cancellationToken);
        if (cachedPnL != null)
        {
            return cachedPnL;
        }

        var pnlResults = new List<PnLDto>();
        
        // Get all active contracts
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        
        // Calculate P&L for purchase contracts
        foreach (var contract in purchaseContracts)
        {
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var currentPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);
            var contractPrice = contract.PriceFormula?.FixedPrice ?? 0;
            
            var unrealizedPnL = (currentPrice - contractPrice) * contract.ContractQuantity.Value;
            
            pnlResults.Add(new PnLDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber.Value,
                ContractType = "Purchase",
                ProductType = productType,
                Quantity = contract.ContractQuantity.Value,
                ContractPrice = contractPrice,
                MarketPrice = currentPrice,
                UnrealizedPnL = unrealizedPnL,
                Currency = contract.PriceFormula?.FixedPrice != null ? "USD" : "USD",
                AsOfDate = valueDate
            });
        }
        
        // Calculate P&L for sales contracts
        foreach (var contract in salesContracts)
        {
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var currentPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);
            var contractPrice = contract.PriceFormula?.FixedPrice ?? 0;
            
            var unrealizedPnL = (contractPrice - currentPrice) * contract.ContractQuantity.Value;
            
            pnlResults.Add(new PnLDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber.Value,
                ContractType = "Sales",
                ProductType = productType,
                Quantity = contract.ContractQuantity.Value,
                ContractPrice = contractPrice,
                MarketPrice = currentPrice,
                UnrealizedPnL = unrealizedPnL,
                Currency = contract.PriceFormula?.FixedPrice != null ? "USD" : "USD",
                AsOfDate = valueDate
            });
        }
        
        var sortedPnL = pnlResults.OrderBy(p => p.ProductType).ThenBy(p => p.ContractNumber).ToList();
        
        // Cache the P&L results
        await _cacheService.SetCachedPnLAsync(sortedPnL, valueDate, cancellationToken: cancellationToken);
        
        return sortedPnL;
    }

    public async Task<IEnumerable<ExposureDto>> CalculateExposureByProductAsync(CancellationToken cancellationToken = default)
    {
        var positions = await CalculateRealTimePositionsAsync(cancellationToken);
        
        return positions
            .GroupBy(p => p.ProductType)
            .Select(g => new ExposureDto
            {
                Category = g.Key,
                Type = "Product",
                TotalQuantity = g.Sum(p => Math.Abs(p.TotalNetPosition)),
                TotalExposure = g.Sum(p => p.ExposureValue),
                LongQuantity = g.Where(p => p.TotalNetPosition > 0).Sum(p => p.TotalNetPosition),
                ShortQuantity = Math.Abs(g.Where(p => p.TotalNetPosition < 0).Sum(p => p.TotalNetPosition)),
                ContractCount = g.Count()
            })
            .OrderByDescending(e => e.TotalExposure);
    }

    public async Task<IEnumerable<ExposureDto>> CalculateExposureByCounterpartyAsync(CancellationToken cancellationToken = default)
    {
        var exposures = new List<ExposureDto>();
        
        // Get contracts with trading partner information
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        
        var counterpartyExposures = new Dictionary<string, ExposureDto>();
        
        // Process purchase contracts
        foreach (var contract in purchaseContracts)
        {
            var counterparty = contract.TradingPartner?.Name ?? "Unknown";
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var marketPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);
            var exposure = contract.ContractQuantity.Value * marketPrice;

            if (!counterpartyExposures.ContainsKey(counterparty))
            {
                counterpartyExposures[counterparty] = new ExposureDto
                {
                    Category = counterparty,
                    Type = "Counterparty"
                };
            }

            counterpartyExposures[counterparty].TotalQuantity += contract.ContractQuantity.Value;
            counterpartyExposures[counterparty].TotalExposure += exposure;
            counterpartyExposures[counterparty].LongQuantity += contract.ContractQuantity.Value;
            counterpartyExposures[counterparty].ContractCount++;
        }

        // Process sales contracts
        foreach (var contract in salesContracts)
        {
            var counterparty = contract.TradingPartner?.Name ?? "Unknown";
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var marketPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);
            var exposure = contract.ContractQuantity.Value * marketPrice;
            
            if (!counterpartyExposures.ContainsKey(counterparty))
            {
                counterpartyExposures[counterparty] = new ExposureDto
                {
                    Category = counterparty,
                    Type = "Counterparty"
                };
            }
            
            counterpartyExposures[counterparty].TotalQuantity += contract.ContractQuantity.Value;
            counterpartyExposures[counterparty].TotalExposure += exposure;
            counterpartyExposures[counterparty].ShortQuantity += contract.ContractQuantity.Value;
            counterpartyExposures[counterparty].ContractCount++;
        }
        
        return counterpartyExposures.Values.OrderByDescending(e => e.TotalExposure);
    }

    public async Task<bool> CheckPositionLimitsAsync(CancellationToken cancellationToken = default)
    {
        var positions = await CalculateRealTimePositionsAsync(cancellationToken);
        
        // Simple position limit check - in production this would be configurable
        const decimal maxExposurePerProduct = 10_000_000m; // $10M per product
        const decimal maxTotalExposure = 50_000_000m; // $50M total
        
        var totalExposure = positions.Sum(p => p.ExposureValue);
        var maxProductExposure = positions.Any() ? positions.Max(p => p.ExposureValue) : 0;
        
        return totalExposure <= maxTotalExposure && maxProductExposure <= maxExposurePerProduct;
    }
    
    private decimal GetEstimatedPrice(string productType)
    {
        // Simplified price estimation - in production, would fetch from market data
        return productType.ToUpper() switch
        {
            "BRENT" => 85m,
            "WTI" => 80m,
            "380CST" => 450m,
            "0.5%" => 550m,
            "MGO" => 650m,
            "GASOIL" => 600m,
            _ => 500m
        };
    }

    private async Task<decimal> GetCurrentMarketPriceAsync(string productType, CancellationToken cancellationToken)
    {
        try
        {
            // Try to get real market price from market data repository
            var marketPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
            var marketPrice = marketPrices.FirstOrDefault(p => p.ProductName.Equals(productType, StringComparison.OrdinalIgnoreCase));
            
            if (marketPrice != null)
            {
                return marketPrice.Price;
            }
        }
        catch
        {
            // Fall back to estimated price if market data is not available
        }
        
        // Fallback to estimated prices
        return GetEstimatedPrice(productType);
    }

    #region Hedge Linking Methods (Data Lineage Enhancement)

    /// <inheritdoc />
    public async Task<IEnumerable<HedgedPositionDto>> CalculateHedgedPositionsAsync(CancellationToken cancellationToken = default)
    {
        var hedgedPositions = new List<HedgedPositionDto>();

        // Get all active purchase and sales contracts
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);

        // Get all paper contracts with hedge designations
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        var hedgingPaperContracts = paperContracts.Where(p => p.HedgedContractId.HasValue).ToList();

        // Build a lookup of paper hedges by physical contract ID
        var hedgesByPhysical = hedgingPaperContracts
            .GroupBy(p => p.HedgedContractId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process purchase contracts
        foreach (var contract in purchaseContracts.Where(c =>
            c.Status == Core.Entities.ContractStatus.Active ||
            c.Status == Core.Entities.ContractStatus.Draft))
        {
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var monthDate = contract.LaycanStart ?? DateTime.UtcNow;
            var month = monthDate.ToString("MMMyy").ToUpper();
            var quantity = contract.ContractQuantity.Value;
            var marketPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);

            var hedgedPosition = new HedgedPositionDto
            {
                ProductType = productType,
                Month = month,
                PhysicalQuantity = quantity,
                PhysicalPositionType = "Long", // Purchase = Long physical position
                MarketPrice = marketPrice,
                GrossExposure = quantity * marketPrice
            };

            // Find linked hedges
            if (hedgesByPhysical.TryGetValue(contract.Id, out var linkedPaperContracts))
            {
                foreach (var paper in linkedPaperContracts)
                {
                    hedgedPosition.LinkedHedges.Add(new LinkedHedgeDto
                    {
                        PaperContractId = paper.Id,
                        ContractNumber = paper.ContractNumber,
                        Quantity = paper.Quantity,
                        Position = paper.Position.ToString(),
                        HedgeRatio = paper.HedgeRatio,
                        HedgeEffectiveness = paper.HedgeEffectiveness ?? 0,
                        DealReferenceId = paper.TradeReference, // Paper contracts use TradeReference instead of DealReferenceId
                        DesignationDate = paper.HedgeDesignationDate ?? DateTime.UtcNow
                    });

                    hedgedPosition.HedgedQuantity += paper.Quantity * paper.HedgeRatio;
                }
            }

            hedgedPosition.UnhedgedQuantity = Math.Max(0, quantity - hedgedPosition.HedgedQuantity);
            hedgedPosition.HedgeRatio = quantity > 0 ? hedgedPosition.HedgedQuantity / quantity : 0;
            hedgedPosition.NetExposure = hedgedPosition.UnhedgedQuantity * marketPrice;

            hedgedPositions.Add(hedgedPosition);
        }

        // Process sales contracts
        foreach (var contract in salesContracts.Where(c =>
            c.Status == Core.Entities.ContractStatus.Active ||
            c.Status == Core.Entities.ContractStatus.Draft))
        {
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var monthDate = contract.LaycanStart ?? DateTime.UtcNow;
            var month = monthDate.ToString("MMMyy").ToUpper();
            var quantity = contract.ContractQuantity.Value;
            var marketPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);

            var hedgedPosition = new HedgedPositionDto
            {
                ProductType = productType,
                Month = month,
                PhysicalQuantity = quantity,
                PhysicalPositionType = "Short", // Sales = Short physical position
                MarketPrice = marketPrice,
                GrossExposure = quantity * marketPrice
            };

            // Find linked hedges
            if (hedgesByPhysical.TryGetValue(contract.Id, out var linkedPaperContracts))
            {
                foreach (var paper in linkedPaperContracts)
                {
                    hedgedPosition.LinkedHedges.Add(new LinkedHedgeDto
                    {
                        PaperContractId = paper.Id,
                        ContractNumber = paper.ContractNumber,
                        Quantity = paper.Quantity,
                        Position = paper.Position.ToString(),
                        HedgeRatio = paper.HedgeRatio,
                        HedgeEffectiveness = paper.HedgeEffectiveness ?? 0,
                        DealReferenceId = paper.TradeReference, // Paper contracts use TradeReference instead of DealReferenceId
                        DesignationDate = paper.HedgeDesignationDate ?? DateTime.UtcNow
                    });

                    hedgedPosition.HedgedQuantity += paper.Quantity * paper.HedgeRatio;
                }
            }

            hedgedPosition.UnhedgedQuantity = Math.Max(0, quantity - hedgedPosition.HedgedQuantity);
            hedgedPosition.HedgeRatio = quantity > 0 ? hedgedPosition.HedgedQuantity / quantity : 0;
            hedgedPosition.NetExposure = hedgedPosition.UnhedgedQuantity * marketPrice;

            hedgedPositions.Add(hedgedPosition);
        }

        return hedgedPositions.OrderBy(p => p.ProductType).ThenBy(p => p.Month);
    }

    /// <inheritdoc />
    public async Task<HedgeEffectivenessDto?> GetHedgeEffectivenessAsync(Guid physicalContractId, CancellationToken cancellationToken = default)
    {
        // Try to find the contract in purchase contracts first
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(physicalContractId, cancellationToken);
        var salesContract = purchaseContract == null
            ? await _salesContractRepository.GetByIdAsync(physicalContractId, cancellationToken)
            : null;

        if (purchaseContract == null && salesContract == null)
        {
            return null;
        }

        // Get paper contracts linked to this physical contract
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        var linkedHedges = paperContracts
            .Where(p => p.HedgedContractId == physicalContractId)
            .ToList();

        string contractNumber;
        string productType;
        decimal physicalQuantity;
        string physicalPosition;

        if (purchaseContract != null)
        {
            contractNumber = purchaseContract.ContractNumber.Value;
            productType = purchaseContract.Product?.Type.ToString() ?? "Unknown";
            physicalQuantity = purchaseContract.ContractQuantity.Value;
            physicalPosition = "Long";
        }
        else
        {
            contractNumber = salesContract!.ContractNumber.Value;
            productType = salesContract.Product?.Type.ToString() ?? "Unknown";
            physicalQuantity = salesContract.ContractQuantity.Value;
            physicalPosition = "Short";
        }

        var effectiveness = new HedgeEffectivenessDto
        {
            PhysicalContractId = physicalContractId,
            ContractNumber = contractNumber,
            ProductType = productType,
            PhysicalQuantity = physicalQuantity,
            PhysicalPosition = physicalPosition,
            CalculatedAt = DateTime.UtcNow
        };

        decimal totalWeightedEffectiveness = 0;

        foreach (var paper in linkedHedges)
        {
            var linkedHedge = new LinkedHedgeDto
            {
                PaperContractId = paper.Id,
                ContractNumber = paper.ContractNumber,
                Quantity = paper.Quantity,
                Position = paper.Position.ToString(),
                HedgeRatio = paper.HedgeRatio,
                HedgeEffectiveness = paper.HedgeEffectiveness ?? 0,
                DealReferenceId = paper.TradeReference, // Paper contracts use TradeReference instead of DealReferenceId
                DesignationDate = paper.HedgeDesignationDate ?? DateTime.UtcNow
            };

            effectiveness.Hedges.Add(linkedHedge);
            effectiveness.TotalHedgedQuantity += paper.Quantity * paper.HedgeRatio;
            totalWeightedEffectiveness += paper.Quantity * (paper.HedgeEffectiveness ?? 0);
        }

        if (physicalQuantity > 0)
        {
            effectiveness.OverallHedgeRatio = effectiveness.TotalHedgedQuantity / physicalQuantity;
        }

        if (effectiveness.TotalHedgedQuantity > 0)
        {
            effectiveness.WeightedAverageEffectiveness = totalWeightedEffectiveness / effectiveness.TotalHedgedQuantity;
        }

        // Assess effectiveness per IFRS 9 / ASC 815 guidelines (80-125% effectiveness)
        if (effectiveness.WeightedAverageEffectiveness >= 0.8m && effectiveness.WeightedAverageEffectiveness <= 1.25m)
        {
            effectiveness.EffectivenessStatus = "Highly Effective";
            effectiveness.MeetsAccountingThreshold = true;
        }
        else if (effectiveness.WeightedAverageEffectiveness >= 0.7m)
        {
            effectiveness.EffectivenessStatus = "Effective";
            effectiveness.MeetsAccountingThreshold = false;
        }
        else
        {
            effectiveness.EffectivenessStatus = "Ineffective";
            effectiveness.MeetsAccountingThreshold = false;
        }

        return effectiveness;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UnhedgedPositionDto>> GetUnhedgedPositionsAsync(CancellationToken cancellationToken = default)
    {
        var unhedgedPositions = new List<UnhedgedPositionDto>();

        // Get all paper contracts with hedge designations
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        var hedgedPhysicalIds = paperContracts
            .Where(p => p.HedgedContractId.HasValue)
            .Select(p => p.HedgedContractId!.Value)
            .ToHashSet();

        // Get active purchase contracts
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);

        foreach (var contract in purchaseContracts.Where(c =>
            (c.Status == Core.Entities.ContractStatus.Active || c.Status == Core.Entities.ContractStatus.Draft) &&
            !hedgedPhysicalIds.Contains(c.Id)))
        {
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var monthDate = contract.LaycanStart ?? DateTime.UtcNow;
            var marketPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);
            var exposure = contract.ContractQuantity.Value * marketPrice;

            unhedgedPositions.Add(new UnhedgedPositionDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber.Value,
                ContractType = "Purchase",
                ProductType = productType,
                Month = monthDate.ToString("MMMyy").ToUpper(),
                Quantity = contract.ContractQuantity.Value,
                MarketPrice = marketPrice,
                Exposure = exposure,
                DealReferenceId = contract.DealReferenceId,
                ContractDate = contract.CreatedAt, // Use CreatedAt from BaseEntity
                PotentialLoss = exposure * 0.05m, // Simplified 5% VaR assumption
                RiskLevel = exposure > 5_000_000 ? "High" : exposure > 1_000_000 ? "Medium" : "Low"
            });
        }

        // Get active sales contracts
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);

        foreach (var contract in salesContracts.Where(c =>
            (c.Status == Core.Entities.ContractStatus.Active || c.Status == Core.Entities.ContractStatus.Draft) &&
            !hedgedPhysicalIds.Contains(c.Id)))
        {
            var productType = contract.Product?.Type.ToString() ?? "Unknown";
            var monthDate = contract.LaycanStart ?? DateTime.UtcNow;
            var marketPrice = await GetCurrentMarketPriceAsync(productType, cancellationToken);
            var exposure = contract.ContractQuantity.Value * marketPrice;

            unhedgedPositions.Add(new UnhedgedPositionDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber.Value,
                ContractType = "Sales",
                ProductType = productType,
                Month = monthDate.ToString("MMMyy").ToUpper(),
                Quantity = contract.ContractQuantity.Value,
                MarketPrice = marketPrice,
                Exposure = exposure,
                DealReferenceId = contract.DealReferenceId,
                ContractDate = contract.CreatedAt, // Use CreatedAt from BaseEntity
                PotentialLoss = exposure * 0.05m, // Simplified 5% VaR assumption
                RiskLevel = exposure > 5_000_000 ? "High" : exposure > 1_000_000 ? "Medium" : "Low"
            });
        }

        return unhedgedPositions.OrderByDescending(p => p.Exposure);
    }

    #endregion
}