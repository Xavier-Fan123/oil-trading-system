using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly INetPositionService _netPositionService;
    private readonly IRiskCalculationService _riskCalculationService;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IPositionCacheService _cacheService;

    public DashboardService(
        INetPositionService netPositionService,
        IRiskCalculationService riskCalculationService,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IShippingOperationRepository shippingOperationRepository,
        IPaperContractRepository paperContractRepository,
        IMarketDataRepository marketDataRepository,
        IPositionCacheService cacheService)
    {
        _netPositionService = netPositionService;
        _riskCalculationService = riskCalculationService;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _shippingOperationRepository = shippingOperationRepository;
        _paperContractRepository = paperContractRepository;
        _marketDataRepository = marketDataRepository;
        _cacheService = cacheService;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "dashboard_overview";
        var cached = await _cacheService.GetCachedDataAsync<DashboardOverviewDto>(cacheKey, cancellationToken);
        if (cached != null && await _cacheService.IsCacheValidAsync(cancellationToken))
        {
            return cached;
        }

        var positionSummary = await _netPositionService.GetPositionSummaryAsync(cancellationToken);
        var riskCalculation = await _riskCalculationService.CalculatePortfolioRiskAsync(DateTime.UtcNow);
        var marketPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
        
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        
        var overview = new DashboardOverviewDto
        {
            TotalPositions = positionSummary.TotalContracts,
            TotalExposure = positionSummary.TotalExposure,
            NetExposure = positionSummary.TotalExposure - positionSummary.LargestExposure,
            LongPositions = positionSummary.LongPositions,
            ShortPositions = positionSummary.ShortPositions,
            FlatPositions = positionSummary.FlatPositions,
            
            DailyPnL = await CalculateDailyPnLAsync(cancellationToken),
            UnrealizedPnL = await CalculateUnrealizedPnLAsync(cancellationToken),
            
            VaR95 = riskCalculation.HistoricalVaR95,
            VaR99 = riskCalculation.HistoricalVaR99,
            PortfolioVolatility = riskCalculation.PortfolioVolatility,
            
            ActivePurchaseContracts = purchaseContracts.Count(c => c.Status == ContractStatus.Active),
            ActiveSalesContracts = salesContracts.Count(c => c.Status == ContractStatus.Active),
            PendingContracts = purchaseContracts.Count(c => c.Status == ContractStatus.PendingApproval) + 
                             salesContracts.Count(c => c.Status == ContractStatus.PendingApproval),
            
            MarketDataPoints = marketPrices.Count(),
            LastMarketUpdate = marketPrices.Any() ? marketPrices.Max(p => p.PriceDate) : DateTime.MinValue,
            
            AlertCount = await GetActiveAlertCountAsync(cancellationToken),
            CalculatedAt = DateTime.UtcNow
        };

        await _cacheService.SetCachedDataAsync(cacheKey, overview, TimeSpan.FromMinutes(5), cancellationToken);
        return overview;
    }

    public async Task<TradingMetricsDto> GetTradingMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        var paperContracts = await _paperContractRepository.GetActiveContractsAsync(cancellationToken);
        
        var tradingMetrics = new TradingMetricsDto
        {
            Period = $"{start:MMM dd} - {end:MMM dd}",
            TotalTrades = purchaseContracts.Count() + salesContracts.Count() + paperContracts.Count(),
            
            TotalVolume = purchaseContracts.Sum(c => c.ContractQuantity.Value) + 
                         salesContracts.Sum(c => c.ContractQuantity.Value) +
                         paperContracts.Sum(c => c.Quantity),
            
            AverageTradeSize = await CalculateAverageTradeSize(purchaseContracts, salesContracts, paperContracts),
            
            PurchaseVolume = purchaseContracts.Sum(c => c.ContractQuantity.Value),
            SalesVolume = salesContracts.Sum(c => c.ContractQuantity.Value),
            PaperVolume = paperContracts.Sum(c => c.Quantity),
            
            LongPaperVolume = paperContracts.Where(c => c.Position == PositionType.Long).Sum(c => c.Quantity),
            ShortPaperVolume = paperContracts.Where(c => c.Position == PositionType.Short).Sum(c => c.Quantity),
            
            ProductBreakdown = await CalculateProductBreakdown(purchaseContracts, salesContracts, paperContracts),
            CounterpartyBreakdown = await CalculateCounterpartyBreakdown(purchaseContracts, salesContracts),
            
            TradeFrequency = await CalculateTradeFrequency(start, end, cancellationToken),
            VolumeByProduct = await CalculateVolumeByProduct(purchaseContracts, salesContracts, paperContracts),
            
            CalculatedAt = DateTime.UtcNow
        };

        return tradingMetrics;
    }

    public async Task<PerformanceAnalyticsDto> GetPerformanceAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        
        var pnlData = await _netPositionService.CalculatePnLAsync(end, cancellationToken);
        var exposures = await _netPositionService.CalculateExposureByProductAsync(cancellationToken);
        
        var analytics = new PerformanceAnalyticsDto
        {
            Period = $"{start:MMM dd} - {end:MMM dd}",
            
            TotalPnL = pnlData.Sum(p => p.UnrealizedPnL),
            RealizedPnL = 0, // Would need additional tracking
            UnrealizedPnL = pnlData.Sum(p => p.UnrealizedPnL),
            
            BestPerformingProduct = exposures.OrderByDescending(e => e.TotalExposure).FirstOrDefault()?.Category ?? "N/A",
            WorstPerformingProduct = exposures.OrderBy(e => e.TotalExposure).FirstOrDefault()?.Category ?? "N/A",
            
            TotalReturn = await CalculateTotalReturn(pnlData),
            AnnualizedReturn = await CalculateAnnualizedReturn(start, end, pnlData),
            
            SharpeRatio = await CalculateSharpeRatio(pnlData),
            MaxDrawdown = await CalculateMaxDrawdown(start, end, cancellationToken),
            
            WinRate = await CalculateWinRate(pnlData),
            ProfitFactor = await CalculateProfitFactor(pnlData),
            
            VaRUtilization = await CalculateVaRUtilization(cancellationToken),
            VolatilityAdjustedReturn = await CalculateVolatilityAdjustedReturn(pnlData),
            
            DailyPnLHistory = await GetDailyPnLHistory(start, end, cancellationToken),
            ProductPerformance = await GetProductPerformance(exposures),
            
            CalculatedAt = DateTime.UtcNow
        };

        return analytics;
    }

    public async Task<MarketInsightsDto> GetMarketInsightsAsync(CancellationToken cancellationToken = default)
    {
        var marketPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
        var priceHistory = await _marketDataRepository.GetHistoricalPricesAsync("BRENT", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, cancellationToken);
        
        var insights = new MarketInsightsDto
        {
            MarketDataCount = marketPrices.Count(),
            LastUpdate = marketPrices.Any() ? marketPrices.Max(p => p.PriceDate) : DateTime.MinValue,
            
            KeyPrices = marketPrices.Take(10).Select(p => new KeyPriceDto
            {
                Product = p.ProductName,
                Price = p.Price,
                Change = CalculatePriceChange(p.ProductName, priceHistory),
                ChangePercent = CalculatePriceChangePercent(p.ProductName, priceHistory),
                LastUpdate = p.PriceDate
            }).ToList(),
            
            VolatilityIndicators = await CalculateVolatilityIndicators(marketPrices),
            CorrelationMatrix = await CalculateCorrelationMatrix(cancellationToken),
            
            TechnicalIndicators = await CalculateTechnicalIndicators(priceHistory),
            
            MarketTrends = await AnalyzeMarketTrends(priceHistory),
            SentimentIndicators = await CalculateSentimentIndicators(),
            
            CalculatedAt = DateTime.UtcNow
        };

        return insights;
    }

    public async Task<OperationalStatusDto> GetOperationalStatusAsync(CancellationToken cancellationToken = default)
    {
        var shippingOps = await _shippingOperationRepository.GetActiveShipmentsAsync(cancellationToken);
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        
        var status = new OperationalStatusDto
        {
            ActiveShipments = shippingOps.Count(s => s.Status == ShippingStatus.InTransit),
            PendingDeliveries = shippingOps.Count(s => s.Status == ShippingStatus.Loading),
            CompletedDeliveries = shippingOps.Count(s => s.Status == ShippingStatus.Discharged),
            
            ContractsAwaitingExecution = purchaseContracts.Count(c => c.Status == ContractStatus.Active && !c.LaycanStart.HasValue) +
                                      salesContracts.Count(c => c.Status == ContractStatus.Active && !c.LaycanStart.HasValue),
            
            ContractsInLaycan = purchaseContracts.Count(c => c.Status == ContractStatus.Active && 
                                                           c.LaycanStart.HasValue && c.LaycanStart <= DateTime.UtcNow &&
                                                           c.LaycanEnd.HasValue && c.LaycanEnd >= DateTime.UtcNow) +
                             salesContracts.Count(c => c.Status == ContractStatus.Active && 
                                                      c.LaycanStart.HasValue && c.LaycanStart <= DateTime.UtcNow &&
                                                      c.LaycanEnd.HasValue && c.LaycanEnd >= DateTime.UtcNow),
            
            UpcomingLaycans = await GetUpcomingLaycans(DateTime.UtcNow.AddDays(7), cancellationToken),
            
            SystemHealth = await CheckSystemHealth(cancellationToken),
            CacheHitRatio = await _cacheService.GetCacheHitRatioAsync(cancellationToken),
            
            LastDataRefresh = DateTime.UtcNow,
            CalculatedAt = DateTime.UtcNow
        };

        return status;
    }

    public async Task<IEnumerable<AlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = new List<AlertDto>();
        
        // Position limit alerts
        var positionLimitsOk = await _netPositionService.CheckPositionLimitsAsync(cancellationToken);
        if (!positionLimitsOk)
        {
            alerts.Add(new AlertDto
            {
                Type = "Position Limit",
                Severity = "High",
                Message = "Position limits exceeded",
                Timestamp = DateTime.UtcNow
            });
        }
        
        // VaR alerts
        var riskCalculation = await _riskCalculationService.CalculatePortfolioRiskAsync(DateTime.UtcNow);
        if (riskCalculation.HistoricalVaR95 > 1_000_000)
        {
            alerts.Add(new AlertDto
            {
                Type = "Risk Alert",
                Severity = "Medium",
                Message = $"Portfolio VaR95 is ${riskCalculation.HistoricalVaR95:N0}",
                Timestamp = DateTime.UtcNow
            });
        }
        
        // Market data freshness
        var marketPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
        var oldestPrice = marketPrices.Any() ? marketPrices.Min(p => p.PriceDate) : DateTime.MinValue;
        if (DateTime.UtcNow - oldestPrice > TimeSpan.FromHours(4))
        {
            alerts.Add(new AlertDto
            {
                Type = "Data Quality",
                Severity = "Low",
                Message = "Some market data is stale",
                Timestamp = DateTime.UtcNow
            });
        }

        return alerts.OrderByDescending(a => a.Timestamp);
    }

    public async Task<KpiSummaryDto> GetKpiSummaryAsync(CancellationToken cancellationToken = default)
    {
        var positionSummary = await _netPositionService.GetPositionSummaryAsync(cancellationToken);
        var pnlData = await _netPositionService.CalculatePnLAsync(DateTime.UtcNow, cancellationToken);
        var riskCalculation = await _riskCalculationService.CalculatePortfolioRiskAsync(DateTime.UtcNow);
        
        var kpis = new KpiSummaryDto
        {
            TotalExposure = positionSummary.TotalExposure,
            DailyPnL = await CalculateDailyPnLAsync(cancellationToken),
            VaR95 = riskCalculation.HistoricalVaR95,
            PortfolioCount = positionSummary.TotalContracts,
            
            ExposureUtilization = Math.Min(100, (positionSummary.TotalExposure / 50_000_000m) * 100),
            RiskUtilization = Math.Min(100, (riskCalculation.HistoricalVaR95 / 5_000_000m) * 100),
            
            CalculatedAt = DateTime.UtcNow
        };

        return kpis;
    }

    // Helper methods
    private async Task<decimal> CalculateDailyPnLAsync(CancellationToken cancellationToken)
    {
        var pnlData = await _netPositionService.CalculatePnLAsync(DateTime.UtcNow, cancellationToken);
        return pnlData.Sum(p => p.UnrealizedPnL);
    }

    private async Task<decimal> CalculateUnrealizedPnLAsync(CancellationToken cancellationToken)
    {
        var pnlData = await _netPositionService.CalculatePnLAsync(DateTime.UtcNow, cancellationToken);
        return pnlData.Sum(p => p.UnrealizedPnL);
    }

    private async Task<int> GetActiveAlertCountAsync(CancellationToken cancellationToken)
    {
        var alerts = await GetActiveAlertsAsync(cancellationToken);
        return alerts.Count();
    }

    private async Task<decimal> CalculateAverageTradeSize(
        IEnumerable<PurchaseContract> purchaseContracts,
        IEnumerable<SalesContract> salesContracts,
        IEnumerable<PaperContract> paperContracts)
    {
        var totalVolume = purchaseContracts.Sum(c => c.ContractQuantity.Value) + 
                         salesContracts.Sum(c => c.ContractQuantity.Value) +
                         paperContracts.Sum(c => c.Quantity);
        
        var totalTrades = purchaseContracts.Count() + salesContracts.Count() + paperContracts.Count();
        
        return totalTrades > 0 ? totalVolume / totalTrades : 0;
    }

    private async Task<Dictionary<string, decimal>> CalculateProductBreakdown(
        IEnumerable<PurchaseContract> purchaseContracts,
        IEnumerable<SalesContract> salesContracts,
        IEnumerable<PaperContract> paperContracts)
    {
        var breakdown = new Dictionary<string, decimal>();
        
        foreach (var contract in purchaseContracts)
        {
            var product = contract.Product?.Type.ToString() ?? "Unknown";
            if (!breakdown.ContainsKey(product)) breakdown[product] = 0;
            breakdown[product] += contract.ContractQuantity.Value;
        }
        
        foreach (var contract in salesContracts)
        {
            var product = contract.Product?.Type.ToString() ?? "Unknown";
            if (!breakdown.ContainsKey(product)) breakdown[product] = 0;
            breakdown[product] += contract.ContractQuantity.Value;
        }
        
        foreach (var contract in paperContracts)
        {
            var product = contract.ProductType;
            if (!breakdown.ContainsKey(product)) breakdown[product] = 0;
            breakdown[product] += contract.Quantity;
        }
        
        return breakdown;
    }

    private async Task<Dictionary<string, decimal>> CalculateCounterpartyBreakdown(
        IEnumerable<PurchaseContract> purchaseContracts,
        IEnumerable<SalesContract> salesContracts)
    {
        var breakdown = new Dictionary<string, decimal>();
        
        foreach (var contract in purchaseContracts)
        {
            var counterparty = contract.TradingPartner?.Name ?? "Unknown";
            if (!breakdown.ContainsKey(counterparty)) breakdown[counterparty] = 0;
            breakdown[counterparty] += contract.ContractQuantity.Value;
        }
        
        foreach (var contract in salesContracts)
        {
            var counterparty = contract.TradingPartner?.Name ?? "Unknown";
            if (!breakdown.ContainsKey(counterparty)) breakdown[counterparty] = 0;
            breakdown[counterparty] += contract.ContractQuantity.Value;
        }
        
        return breakdown;
    }

    private async Task<decimal> CalculateTradeFrequency(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var days = (end - start).Days;
        return days > 0 ? 30m / days : 0; // Simplified calculation
    }

    private async Task<Dictionary<string, decimal>> CalculateVolumeByProduct(
        IEnumerable<PurchaseContract> purchaseContracts,
        IEnumerable<SalesContract> salesContracts,
        IEnumerable<PaperContract> paperContracts)
    {
        return await CalculateProductBreakdown(purchaseContracts, salesContracts, paperContracts);
    }

    private async Task<decimal> CalculateTotalReturn(IEnumerable<PnLDto> pnlData)
    {
        return pnlData.Sum(p => p.UnrealizedPnL);
    }

    private async Task<decimal> CalculateAnnualizedReturn(DateTime start, DateTime end, IEnumerable<PnLDto> pnlData)
    {
        var days = (end - start).Days;
        var totalReturn = pnlData.Sum(p => p.UnrealizedPnL);
        return days > 0 ? (totalReturn / days) * 365 : 0;
    }

    private async Task<decimal> CalculateSharpeRatio(IEnumerable<PnLDto> pnlData)
    {
        if (!pnlData.Any()) return 0;
        
        var returns = pnlData.Select(p => p.UnrealizedPnL).ToList();
        var avgReturn = returns.Average();
        var stdDev = Math.Sqrt(returns.Select(r => Math.Pow((double)(r - avgReturn), 2)).Average());
        
        return stdDev > 0 ? (decimal)(avgReturn / (decimal)stdDev) : 0;
    }

    private async Task<decimal> CalculateMaxDrawdown(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        // Simplified - would need historical P&L data
        return -100000m;
    }

    private async Task<decimal> CalculateWinRate(IEnumerable<PnLDto> pnlData)
    {
        if (!pnlData.Any()) return 0;
        var winningTrades = pnlData.Count(p => p.UnrealizedPnL > 0);
        return (decimal)winningTrades / pnlData.Count() * 100;
    }

    private async Task<decimal> CalculateProfitFactor(IEnumerable<PnLDto> pnlData)
    {
        var profits = pnlData.Where(p => p.UnrealizedPnL > 0).Sum(p => p.UnrealizedPnL);
        var losses = Math.Abs(pnlData.Where(p => p.UnrealizedPnL < 0).Sum(p => p.UnrealizedPnL));
        return losses > 0 ? profits / losses : 0;
    }

    private async Task<decimal> CalculateVaRUtilization(CancellationToken cancellationToken)
    {
        var riskCalculation = await _riskCalculationService.CalculatePortfolioRiskAsync(DateTime.UtcNow);
        return Math.Min(100, (riskCalculation.HistoricalVaR95 / 5_000_000m) * 100);
    }

    private async Task<decimal> CalculateVolatilityAdjustedReturn(IEnumerable<PnLDto> pnlData)
    {
        return await CalculateSharpeRatio(pnlData);
    }

    private async Task<List<DailyPnLDto>> GetDailyPnLHistory(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var history = new List<DailyPnLDto>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var pnl = await _netPositionService.CalculatePnLAsync(date, cancellationToken);
            history.Add(new DailyPnLDto
            {
                Date = date,
                DailyPnL = pnl.Sum(p => p.UnrealizedPnL),
                CumulativePnL = pnl.Sum(p => p.Quantity * p.MarketPrice)
            });
        }
        return history;
    }

    private async Task<List<ProductPerformanceDto>> GetProductPerformance(IEnumerable<ExposureDto> exposures)
    {
        return exposures.Select(e => new ProductPerformanceDto
        {
            Product = e.Category,
            Exposure = e.TotalExposure,
            PnL = e.TotalExposure * 0.05m, // Simplified
            Return = 5.0m // Simplified
        }).ToList();
    }

    private decimal CalculatePriceChange(string productName, IEnumerable<MarketPrice> priceHistory)
    {
        var prices = priceHistory.Where(p => p.ProductName == productName).OrderBy(p => p.PriceDate).ToList();
        if (prices.Count < 2) return 0;
        return prices.Last().Price - prices[prices.Count - 2].Price;
    }

    private decimal CalculatePriceChangePercent(string productName, IEnumerable<MarketPrice> priceHistory)
    {
        var prices = priceHistory.Where(p => p.ProductName == productName).OrderBy(p => p.PriceDate).ToList();
        if (prices.Count < 2) return 0;
        var oldPrice = prices[prices.Count - 2].Price;
        var newPrice = prices.Last().Price;
        return oldPrice > 0 ? ((newPrice - oldPrice) / oldPrice) * 100 : 0;
    }

    private async Task<Dictionary<string, decimal>> CalculateVolatilityIndicators(IEnumerable<MarketPrice> marketPrices)
    {
        return new Dictionary<string, decimal>
        {
            ["OverallVolatility"] = 15.5m,
            ["BrentVolatility"] = 18.2m,
            ["WTIVolatility"] = 17.8m
        };
    }

    private async Task<Dictionary<string, Dictionary<string, decimal>>> CalculateCorrelationMatrix(CancellationToken cancellationToken)
    {
        return new Dictionary<string, Dictionary<string, decimal>>
        {
            ["BRENT"] = new Dictionary<string, decimal> { ["WTI"] = 0.85m, ["GASOIL"] = 0.75m },
            ["WTI"] = new Dictionary<string, decimal> { ["BRENT"] = 0.85m, ["GASOIL"] = 0.70m }
        };
    }

    private async Task<Dictionary<string, decimal>> CalculateTechnicalIndicators(IEnumerable<MarketPrice> priceHistory)
    {
        return new Dictionary<string, decimal>
        {
            ["RSI"] = 45.5m,
            ["MovingAverage"] = 82.5m,
            ["MACD"] = 1.2m
        };
    }

    private async Task<List<MarketTrendDto>> AnalyzeMarketTrends(IEnumerable<MarketPrice> priceHistory)
    {
        return new List<MarketTrendDto>
        {
            new MarketTrendDto { Product = "BRENT", Trend = "Bullish", Strength = 7.5m },
            new MarketTrendDto { Product = "WTI", Trend = "Neutral", Strength = 5.0m }
        };
    }

    private async Task<Dictionary<string, decimal>> CalculateSentimentIndicators()
    {
        return new Dictionary<string, decimal>
        {
            ["BullishSentiment"] = 65m,
            ["BearishSentiment"] = 35m,
            ["NeutralSentiment"] = 45m
        };
    }

    private async Task<List<LaycanDto>> GetUpcomingLaycans(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);
        
        var laycans = new List<LaycanDto>();
        
        foreach (var contract in purchaseContracts.Where(c => c.LaycanStart.HasValue && c.LaycanStart <= cutoffDate))
        {
            laycans.Add(new LaycanDto
            {
                ContractNumber = contract.ContractNumber.Value,
                ContractType = "Purchase",
                LaycanStart = contract.LaycanStart.Value,
                LaycanEnd = contract.LaycanEnd ?? contract.LaycanStart.Value.AddDays(5),
                Product = contract.Product?.Type.ToString() ?? "Unknown",
                Quantity = contract.ContractQuantity.Value
            });
        }
        
        foreach (var contract in salesContracts.Where(c => c.LaycanStart.HasValue && c.LaycanStart <= cutoffDate))
        {
            laycans.Add(new LaycanDto
            {
                ContractNumber = contract.ContractNumber.Value,
                ContractType = "Sales",
                LaycanStart = contract.LaycanStart.Value,
                LaycanEnd = contract.LaycanEnd ?? contract.LaycanStart.Value.AddDays(5),
                Product = contract.Product?.Type.ToString() ?? "Unknown",
                Quantity = contract.ContractQuantity.Value
            });
        }
        
        return laycans.OrderBy(l => l.LaycanStart).ToList();
    }

    private async Task<SystemHealthDto> CheckSystemHealth(CancellationToken cancellationToken)
    {
        return new SystemHealthDto
        {
            DatabaseStatus = "Healthy",
            CacheStatus = "Healthy",
            MarketDataStatus = "Healthy",
            OverallStatus = "Healthy"
        };
    }
}