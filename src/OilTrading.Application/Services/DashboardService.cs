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
        
        var dailyHistory = await GetDailyPnLHistory(start, end, cancellationToken);

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
            MaxDrawdown = CalculateMaxDrawdownFromHistory(dailyHistory),

            WinRate = await CalculateWinRate(pnlData),
            ProfitFactor = await CalculateProfitFactor(pnlData),

            VaRUtilization = await CalculateVaRUtilization(cancellationToken),
            VolatilityAdjustedReturn = await CalculateVolatilityAdjustedReturn(pnlData),

            DailyPnLHistory = dailyHistory,
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
        var purchaseContracts = await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        var salesContracts = await _salesContractRepository.GetActiveContractsAsync(cancellationToken);

        var totalTrades = purchaseContracts.Count() + salesContracts.Count();
        var days = Math.Max(1, (end - start).Days);
        var monthlyRate = (decimal)totalTrades / days * 30;
        return Math.Round(monthlyRate, 1);
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

    private decimal CalculateMaxDrawdownFromHistory(List<DailyPnLDto> history)
    {
        if (!history.Any()) return 0;
        decimal peak = 0;
        decimal maxDrawdown = 0;
        foreach (var day in history)
        {
            if (day.CumulativePnL > peak) peak = day.CumulativePnL;
            var dd = day.CumulativePnL - peak;
            if (dd < maxDrawdown) maxDrawdown = dd;
        }
        return maxDrawdown;
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
        var totalExposure = exposures.Sum(e => e.TotalExposure);
        return exposures.Select(e => new ProductPerformanceDto
        {
            Product = e.Category,
            Exposure = e.TotalExposure,
            PnL = 0, // Needs per-product P&L tracking
            Return = totalExposure != 0 ? Math.Round((e.TotalExposure / totalExposure) * 100, 2) : 0
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
        var result = new Dictionary<string, decimal>();
        var grouped = marketPrices.GroupBy(p => p.ProductName);

        foreach (var group in grouped)
        {
            var prices = group.OrderBy(p => p.PriceDate).Select(p => p.Price).ToList();
            if (prices.Count >= 2)
            {
                var returns = new List<double>();
                for (int i = 1; i < prices.Count; i++)
                {
                    if (prices[i - 1] != 0)
                        returns.Add((double)((prices[i] - prices[i - 1]) / prices[i - 1]));
                }
                if (returns.Count > 0)
                {
                    var avg = returns.Average();
                    var variance = returns.Select(r => Math.Pow(r - avg, 2)).Average();
                    var dailyVol = Math.Sqrt(variance);
                    var annualizedVol = dailyVol * Math.Sqrt(252) * 100;
                    result[group.Key] = (decimal)annualizedVol;
                }
            }
        }

        if (!result.Any())
        {
            result["NoData"] = 0;
        }

        return result;
    }

    private async Task<Dictionary<string, Dictionary<string, decimal>>> CalculateCorrelationMatrix(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Dictionary<string, decimal>>();
        var products = new[] { "BRENT", "WTI", "GASOIL", "MGO" };
        var productReturns = new Dictionary<string, List<double>>();

        foreach (var product in products)
        {
            var history = await _marketDataRepository.GetHistoricalPricesAsync(
                product, DateTime.UtcNow.AddDays(-60), DateTime.UtcNow, cancellationToken);
            var prices = history.OrderBy(p => p.PriceDate).Select(p => p.Price).ToList();

            if (prices.Count >= 2)
            {
                var returns = new List<double>();
                for (int i = 1; i < prices.Count; i++)
                {
                    if (prices[i - 1] != 0)
                        returns.Add((double)((prices[i] - prices[i - 1]) / prices[i - 1]));
                }
                if (returns.Count > 0)
                    productReturns[product] = returns;
            }
        }

        foreach (var p1 in productReturns.Keys)
        {
            result[p1] = new Dictionary<string, decimal>();
            foreach (var p2 in productReturns.Keys)
            {
                if (p1 == p2) { result[p1][p2] = 1.0m; continue; }
                var r1 = productReturns[p1];
                var r2 = productReturns[p2];
                var minLen = Math.Min(r1.Count, r2.Count);
                if (minLen < 2) { result[p1][p2] = 0; continue; }

                var list1 = r1.TakeLast(minLen).ToList();
                var list2 = r2.TakeLast(minLen).ToList();
                var avg1 = list1.Average();
                var avg2 = list2.Average();
                var cov = list1.Zip(list2, (a, b) => (a - avg1) * (b - avg2)).Average();
                var std1 = Math.Sqrt(list1.Select(x => Math.Pow(x - avg1, 2)).Average());
                var std2 = Math.Sqrt(list2.Select(x => Math.Pow(x - avg2, 2)).Average());
                var corr = (std1 > 0 && std2 > 0) ? cov / (std1 * std2) : 0;
                result[p1][p2] = (decimal)Math.Round(corr, 4);
            }
        }

        return result;
    }

    private async Task<Dictionary<string, decimal>> CalculateTechnicalIndicators(IEnumerable<MarketPrice> priceHistory)
    {
        var result = new Dictionary<string, decimal>();
        var prices = priceHistory.OrderBy(p => p.PriceDate).Select(p => p.Price).ToList();

        if (prices.Count == 0) return result;

        // Simple Moving Average (20-day)
        if (prices.Count >= 20)
            result["SMA20"] = prices.TakeLast(20).Average();

        // Simple Moving Average (50-day)
        if (prices.Count >= 50)
            result["SMA50"] = prices.TakeLast(50).Average();
        else if (prices.Count >= 5)
            result["SMA50"] = prices.Average();

        // RSI (14-period)
        if (prices.Count >= 15)
        {
            var gains = new List<decimal>();
            var losses = new List<decimal>();
            var lookback = Math.Min(14, prices.Count - 1);
            for (int i = prices.Count - lookback; i < prices.Count; i++)
            {
                var change = prices[i] - prices[i - 1];
                if (change > 0) { gains.Add(change); losses.Add(0); }
                else { gains.Add(0); losses.Add(Math.Abs(change)); }
            }
            var avgGain = gains.Average();
            var avgLoss = losses.Average();
            var rs = avgLoss > 0 ? avgGain / avgLoss : 100;
            result["RSI"] = Math.Round(100 - (100 / (1 + rs)), 2);
        }

        // MACD (simplified: 12-day EMA - 26-day EMA approximation)
        if (prices.Count >= 12)
        {
            var short12 = prices.TakeLast(12).Average();
            var long26 = prices.TakeLast(Math.Min(26, prices.Count)).Average();
            result["MACD"] = Math.Round(short12 - long26, 4);
        }

        // Latest price
        result["LatestPrice"] = prices.Last();

        return result;
    }

    private async Task<List<MarketTrendDto>> AnalyzeMarketTrends(IEnumerable<MarketPrice> priceHistory)
    {
        var trends = new List<MarketTrendDto>();
        var grouped = priceHistory.GroupBy(p => p.ProductName);

        foreach (var group in grouped)
        {
            var prices = group.OrderBy(p => p.PriceDate).Select(p => p.Price).ToList();
            if (prices.Count < 2) continue;

            var recentAvg = prices.TakeLast(Math.Min(5, prices.Count)).Average();
            var olderAvg = prices.Take(Math.Max(1, prices.Count / 2)).Average();

            var changePct = olderAvg > 0 ? ((recentAvg - olderAvg) / olderAvg) * 100 : 0;

            string trend;
            decimal strength;
            if (changePct > 2)
            {
                trend = "Bullish";
                strength = Math.Min(10, Math.Abs(changePct));
            }
            else if (changePct < -2)
            {
                trend = "Bearish";
                strength = Math.Min(10, Math.Abs(changePct));
            }
            else
            {
                trend = "Neutral";
                strength = Math.Abs(changePct);
            }

            trends.Add(new MarketTrendDto { Product = group.Key, Trend = trend, Strength = strength });
        }

        return trends;
    }

    private async Task<Dictionary<string, decimal>> CalculateSentimentIndicators()
    {
        var marketPrices = await _marketDataRepository.GetLatestPricesAsync(default);
        var priceHistory = await _marketDataRepository.GetHistoricalPricesAsync("BRENT", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, default);

        var bullish = 0;
        var bearish = 0;
        var total = 0;

        var grouped = priceHistory.GroupBy(p => p.ProductName);
        foreach (var group in grouped)
        {
            var prices = group.OrderBy(p => p.PriceDate).Select(p => p.Price).ToList();
            if (prices.Count >= 2)
            {
                total++;
                if (prices.Last() > prices.First()) bullish++;
                else bearish++;
            }
        }

        var totalSentiment = Math.Max(1, total);
        return new Dictionary<string, decimal>
        {
            ["BullishSentiment"] = Math.Round((decimal)bullish / totalSentiment * 100, 1),
            ["BearishSentiment"] = Math.Round((decimal)bearish / totalSentiment * 100, 1),
            ["NeutralSentiment"] = Math.Round((decimal)(totalSentiment - bullish - bearish) / totalSentiment * 100, 1)
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
        var dbStatus = "Healthy";
        var cacheStatus = "Healthy";
        var marketStatus = "Healthy";

        try
        {
            await _purchaseContractRepository.GetActiveContractsAsync(cancellationToken);
        }
        catch
        {
            dbStatus = "Unhealthy";
        }

        try
        {
            var cacheRatio = await _cacheService.GetCacheHitRatioAsync(cancellationToken);
            if (cacheRatio < 0) cacheStatus = "Degraded";
        }
        catch
        {
            cacheStatus = "Unavailable";
        }

        try
        {
            var prices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
            if (!prices.Any())
                marketStatus = "NoData";
            else if (prices.Max(p => p.PriceDate) < DateTime.UtcNow.AddHours(-24))
                marketStatus = "Stale";
        }
        catch
        {
            marketStatus = "Unhealthy";
        }

        var overallStatus = (dbStatus == "Healthy" && cacheStatus == "Healthy" && marketStatus == "Healthy")
            ? "Healthy"
            : (dbStatus == "Unhealthy" ? "Critical" : "Degraded");

        return new SystemHealthDto
        {
            DatabaseStatus = dbStatus,
            CacheStatus = cacheStatus,
            MarketDataStatus = marketStatus,
            OverallStatus = overallStatus
        };
    }
}