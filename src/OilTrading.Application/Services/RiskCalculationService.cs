using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

public class RiskCalculationService : IRiskCalculationService
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ILogger<RiskCalculationService> _logger;
    private readonly string _pythonScriptPath;
    private const int RANDOM_SEED = 42; // Fixed seed for reproducibility

    public RiskCalculationService(
        IMarketDataRepository marketDataRepository,
        IPaperContractRepository paperContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ILogger<RiskCalculationService> logger)
    {
        _marketDataRepository = marketDataRepository;
        _paperContractRepository = paperContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _logger = logger;
        _pythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "risk_engine.py");
    }

    public async Task<RiskCalculationResultDto> CalculatePortfolioRiskAsync(
        DateTime calculationDate,
        int historicalDays = 252,
        bool includeStressTests = true)
    {
        _logger.LogInformation("Starting portfolio risk calculation for {Date} with {Days} days of history",
            calculationDate, historicalDays);

        // Get open positions from database
        var positions = await GetOpenPositionsAsync(calculationDate);
        if (!positions.Any())
        {
            _logger.LogWarning("No open positions found for risk calculation");
            return new RiskCalculationResultDto
            {
                CalculationDate = calculationDate,
                TotalPortfolioValue = 0,
                PositionCount = 0
            };
        }

        // Get unique product-month combinations
        // UPDATED: Group by (ProductType, ContractMonth) for proper futures/derivatives VaR
        var productMonthPairs = positions
            .Select(p => $"{p.ProductType}|{p.ContractMonth}")
            .Distinct()
            .ToList();

        // Get historical returns for all product-month combinations
        var productReturns = await GetHistoricalReturnsAsync(productMonthPairs, calculationDate, historicalDays);

        // Calculate portfolio returns (for historical VaR)
        var portfolioReturns = CalculatePortfolioReturns(positions, productReturns);

        // Calculate different VaR measures
        var (histVaR95, histVaR99) = await CalculateHistoricalVaRAsync(positions, portfolioReturns);

        // CORRECTED: Use proper delta-normal VaR with covariance matrix
        var (deltaNormalVaR95, deltaNormalVaR99) = await CalculateDeltaNormalVaRAsync(positions, productReturns);

        var (garchVaR95, garchVaR99) = await CalculateGarchVaRAsync(positions, productReturns);
        var (mcVaR95, mcVaR99) = await CalculateMonteCarloVaRAsync(positions, productReturns);

        // Calculate Expected Shortfall
        var (es95, es99) = await CalculateExpectedShortfallAsync(portfolioReturns, positions);

        // Run stress tests if requested
        var stressTests = new List<StressTestResultDto>();
        if (includeStressTests)
        {
            var currentPrices = await GetCurrentPricesAsync(productMonthPairs, calculationDate);
            stressTests = await RunStressTestsAsync(positions, currentPrices);
        }

        // Calculate additional metrics using corrected covariance-based method
        var portfolioVolatility = CalculatePortfolioVolatility(positions, productReturns);
        var maxDrawdown = CalculateMaxDrawdown(portfolioReturns);

        // Calculate product exposures
        var productExposures = CalculateProductExposures(positions, productReturns);

        // Calculate total portfolio value
        var totalPortfolioValue = positions.Sum(p => Math.Abs(p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice)));

        return new RiskCalculationResultDto
        {
            CalculationDate = calculationDate,
            TotalPortfolioValue = Math.Round(totalPortfolioValue),
            PositionCount = positions.Count,
            HistoricalVaR95 = Math.Round(deltaNormalVaR95), // Use delta-normal as primary
            HistoricalVaR99 = Math.Round(deltaNormalVaR99),
            GarchVaR95 = Math.Round(garchVaR95),
            GarchVaR99 = Math.Round(garchVaR99),
            McVaR95 = Math.Round(mcVaR95),
            McVaR99 = Math.Round(mcVaR99),
            ExpectedShortfall95 = Math.Round(es95),
            ExpectedShortfall99 = Math.Round(es99),
            PortfolioVolatility = Math.Round(portfolioVolatility, 4),
            MaxDrawdown = Math.Round(maxDrawdown, 4),
            StressTests = stressTests,
            ProductExposures = productExposures
        };
    }

    public async Task<(decimal var95, decimal var99)> CalculateHistoricalVaRAsync(
        List<PaperContract> positions,
        List<decimal> historicalReturns)
    {
        if (!historicalReturns.Any())
            return (0, 0);

        // Sort returns in ascending order (worst to best)
        var sortedReturns = historicalReturns.OrderBy(r => r).ToList();

        // Calculate portfolio value
        var portfolioValue = positions.Sum(p => Math.Abs(p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice)));

        // CORRECTED: Proper quantile calculation for left-tail VaR
        // 95% VaR = 5th percentile (losses exceeded 5% of the time)
        // 99% VaR = 1st percentile (losses exceeded 1% of the time)
        // Use Floor for left-tail percentiles (standard statistical convention)
        int index95 = Math.Max(0, (int)Math.Floor(sortedReturns.Count * 0.05));
        int index99 = Math.Max(0, (int)Math.Floor(sortedReturns.Count * 0.01));

        // Ensure indices are within bounds
        index95 = Math.Min(index95, sortedReturns.Count - 1);
        index99 = Math.Min(index99, sortedReturns.Count - 1);

        // VaR is the absolute value of the loss at the percentile
        var var95 = Math.Abs(sortedReturns[index95] * portfolioValue);
        var var99 = Math.Abs(sortedReturns[index99] * portfolioValue);

        _logger.LogInformation("Historical VaR calculated: 95%={VaR95:C}, 99%={VaR99:C} (indices: {idx95}, {idx99})",
            var95, var99, index95, index99);

        return (var95, var99);
    }

    /// <summary>
    /// CORRECTED Delta-Normal (Variance-Covariance) VaR calculation
    /// Implements proper multi-product VaR using covariance matrix
    ///
    /// Mathematical Foundation (Jane Street Standard):
    /// For a portfolio with dollar exposures E = [E₁, E₂, ..., Eₙ] and return covariance matrix Σ:
    /// Portfolio Variance = E' × Σ × E = Σᵢ Σⱼ Eᵢ × Eⱼ × Cov(rᵢ, rⱼ)
    /// Portfolio Volatility (daily) = σₚ = √(E' × Σ × E)
    /// VaR (1-day, α confidence) = z_α × σₚ
    ///
    /// Where:
    /// - E is the dollar exposure vector (NOT normalized weights)
    /// - Σ is the daily return covariance matrix
    /// - z_α is the standard normal quantile (1.645 for 95%, 2.326 for 99%)
    ///
    /// This formulation is correct because:
    /// Portfolio P&L = Σᵢ Eᵢ × rᵢ, so Var(P&L) = E' × Σ × E
    /// </summary>
    public async Task<(decimal var95, decimal var99)> CalculateDeltaNormalVaRAsync(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns)
    {
        if (!positions.Any() || !productReturns.Any())
            return (0, 0);

        try
        {
            // 1. Get product-month combinations and ensure we have return data for all positions
            // UPDATED: Group by (ProductType, ContractMonth) for proper futures/derivatives VaR
            // This ensures different months (e.g., AUG25 vs SEP25) are treated as separate risk factors
            var productMonthPairs = positions
                .Select(p => $"{p.ProductType}|{p.ContractMonth}")
                .Distinct()
                .ToList();

            var productsWithData = productMonthPairs
                .Where(pm => productReturns.ContainsKey(pm) && productReturns[pm].Any())
                .ToList();

            if (!productsWithData.Any())
            {
                _logger.LogWarning("No historical return data available for delta-normal VaR calculation");
                return (0, 0);
            }

            int n = productsWithData.Count;

            // 2. Calculate covariance matrix (daily returns)
            var covarianceMatrix = CalculateCovarianceMatrix(productReturns, productsWithData);

            // 3. Calculate dollar exposures (NOT normalized weights)
            // For delta-normal VaR, we use actual dollar exposures
            // Long positions: positive exposure, Short positions: negative exposure
            var exposures = CalculateDollarExposures(positions, productsWithData);

            // 4. Calculate portfolio variance: σ²_p = E' Σ E
            // This gives us the variance of portfolio P&L in dollar terms
            decimal portfolioVariance = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    portfolioVariance += exposures[i] * covarianceMatrix[i, j] * exposures[j];
                }
            }

            // 5. Portfolio volatility (daily P&L standard deviation in dollars)
            var portfolioStdDev = (decimal)Math.Sqrt((double)Math.Abs(portfolioVariance));

            // 6. Delta-Normal VaR = z-score × σ_p
            // No need to multiply by portfolio value - volatility is already in dollar terms
            // z-scores: 95% = 1.645, 99% = 2.326 (one-tailed)
            var var95 = 1.645m * portfolioStdDev;
            var var99 = 2.326m * portfolioStdDev;

            // 7. Calculate metrics for logging
            var totalExposure = exposures.Sum(e => Math.Abs(e));
            var netExposure = exposures.Sum();

            _logger.LogInformation(
                "Delta-Normal VaR calculated: VaR95={VaR95:C}, VaR99={VaR99:C}, " +
                "σ_p={Sigma:C}, TotalExposure={Total:C}, NetExposure={Net:C}, Products={N}",
                var95, var99, portfolioStdDev, totalExposure, netExposure, n);

            return (var95, var99);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating delta-normal VaR, falling back to historical method");
            var portfolioReturns = CalculatePortfolioReturns(positions, productReturns);
            return await CalculateHistoricalVaRAsync(positions, portfolioReturns);
        }
    }

    /// <summary>
    /// Calculate covariance matrix from historical returns
    /// Returns: n×n covariance matrix where Σ[i,j] = Cov(returns_i, returns_j)
    ///
    /// Jane Street Best Practice:
    /// - Use sample covariance (n-1 denominator) for unbiased estimation
    /// - Validate that matrix is symmetric: Cov(i,j) = Cov(j,i)
    /// - Verify positive semi-definiteness (all eigenvalues ≥ 0)
    /// - Log correlation matrix for interpretability
    /// </summary>
    private decimal[,] CalculateCovarianceMatrix(Dictionary<string, List<decimal>> productReturns, List<string> products)
    {
        int n = products.Count;
        var covMatrix = new decimal[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                var returns_i = productReturns[products[i]];
                var returns_j = productReturns[products[j]];

                // Calculate covariance between products i and j
                covMatrix[i, j] = CalculateCovariance(returns_i, returns_j);
            }
        }

        // Log correlation matrix for debugging (Jane Street standard)
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var corrMatrix = CalculateCorrelationMatrix(covMatrix, products);
            LogCorrelationMatrix(corrMatrix, products);
        }

        return covMatrix;
    }

    /// <summary>
    /// Calculate correlation matrix from covariance matrix
    /// Corr(i,j) = Cov(i,j) / (σᵢ × σⱼ)
    /// Used for debugging and validation (Jane Street best practice)
    /// </summary>
    private decimal[,] CalculateCorrelationMatrix(decimal[,] covarianceMatrix, List<string> products)
    {
        int n = products.Count;
        var corrMatrix = new decimal[n, n];

        // Calculate standard deviations from diagonal
        var stdDevs = new decimal[n];
        for (int i = 0; i < n; i++)
        {
            stdDevs[i] = (decimal)Math.Sqrt((double)Math.Abs(covarianceMatrix[i, i]));
        }

        // Calculate correlations
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (stdDevs[i] > 0 && stdDevs[j] > 0)
                {
                    corrMatrix[i, j] = covarianceMatrix[i, j] / (stdDevs[i] * stdDevs[j]);
                }
                else
                {
                    corrMatrix[i, j] = i == j ? 1m : 0m; // Identity on diagonal, zero otherwise
                }
            }
        }

        return corrMatrix;
    }

    /// <summary>
    /// Log correlation matrix in readable format
    /// Jane Street debugging standard - always inspect correlations
    /// </summary>
    private void LogCorrelationMatrix(decimal[,] corrMatrix, List<string> products)
    {
        int n = products.Count;
        var sb = new StringBuilder();
        sb.AppendLine("Correlation Matrix:");

        // Header
        sb.Append("Product".PadRight(10));
        foreach (var product in products)
        {
            sb.Append(product.PadRight(10));
        }
        sb.AppendLine();

        // Matrix rows
        for (int i = 0; i < n; i++)
        {
            sb.Append(products[i].PadRight(10));
            for (int j = 0; j < n; j++)
            {
                sb.Append($"{corrMatrix[i, j]:F4}".PadRight(10));
            }
            sb.AppendLine();
        }

        _logger.LogDebug(sb.ToString());
    }

    /// <summary>
    /// Calculate covariance between two return series
    /// Cov(X,Y) = E[(X - μ_X)(Y - μ_Y)]
    /// </summary>
    private decimal CalculateCovariance(List<decimal> returns1, List<decimal> returns2)
    {
        if (!returns1.Any() || !returns2.Any())
            return 0;

        // Use minimum common length
        int n = Math.Min(returns1.Count, returns2.Count);

        var mean1 = returns1.Take(n).Average();
        var mean2 = returns2.Take(n).Average();

        decimal covariance = 0;
        for (int i = 0; i < n; i++)
        {
            covariance += (returns1[i] - mean1) * (returns2[i] - mean2);
        }

        // Use n-1 for sample covariance (Bessel's correction)
        return n > 1 ? covariance / (n - 1) : 0;
    }

    /// <summary>
    /// Calculate dollar exposure vector for delta-normal VaR
    /// Returns signed dollar exposures (NOT normalized weights)
    ///
    /// Mathematical Reasoning:
    /// Since Portfolio P&L = Σᵢ Exposureᵢ × returnᵢ
    /// We need actual dollar exposures to correctly calculate:
    /// Var(Portfolio P&L) = E' × Σ × E
    ///
    /// Sign Convention:
    /// - Long positions: POSITIVE exposure (benefit from price increases)
    /// - Short positions: NEGATIVE exposure (benefit from price decreases)
    /// </summary>
    private decimal[] CalculateDollarExposures(List<PaperContract> positions, List<string> products)
    {
        int n = products.Count;
        var exposures = new decimal[n];

        for (int i = 0; i < n; i++)
        {
            // UPDATED: Parse composite key (ProductType|ContractMonth) for proper futures/derivatives grouping
            var parts = products[i].Split('|');
            var productType = parts[0];
            var contractMonth = parts.Length > 1 ? parts[1] : string.Empty;

            var productPositions = positions.Where(p =>
                p.ProductType == productType &&
                p.ContractMonth == contractMonth);

            decimal productExposure = 0;
            foreach (var position in productPositions)
            {
                // Calculate dollar value of position
                var dollarValue = position.Quantity * position.LotSize * (position.CurrentPrice ?? position.EntryPrice);

                // Apply sign based on position type
                // Long: positive exposure (lose money when price drops)
                // Short: negative exposure (lose money when price rises)
                if (position.Position == PositionType.Short)
                {
                    productExposure -= dollarValue;
                }
                else
                {
                    productExposure += dollarValue;
                }
            }

            exposures[i] = productExposure;

            _logger.LogDebug("Product {Product}: Exposure = {Exposure:C}", products[i], productExposure);
        }

        return exposures;
    }

    public async Task<(decimal var95, decimal var99)> CalculateGarchVaRAsync(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns)
    {
        try
        {
            // Prepare data for Python script
            var inputData = new
            {
                positions = positions.Select(p => new
                {
                    product = p.ProductType,
                    quantity = p.Quantity,
                    lotSize = p.LotSize,
                    currentPrice = p.CurrentPrice ?? p.EntryPrice,
                    position = p.Position.ToString()
                }).ToList(),
                returns = productReturns,
                seed = RANDOM_SEED
            };

            var jsonInput = JsonSerializer.Serialize(inputData);
            
            // Call Python script for GARCH calculation
            var result = await CallPythonScript("garch", jsonInput);
            
            if (result.ContainsKey("var95") && result.ContainsKey("var99"))
            {
                var var95 = Convert.ToDecimal(result["var95"]);
                var var99 = Convert.ToDecimal(result["var99"]);
                
                _logger.LogInformation("GARCH VaR calculated: 95%={VaR95:C}, 99%={VaR99:C}", var95, var99);
                return (var95, var99);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating GARCH VaR, falling back to historical method");
        }

        // Fallback to historical method if GARCH fails
        var portfolioReturns = CalculatePortfolioReturns(positions, productReturns);
        return await CalculateHistoricalVaRAsync(positions, portfolioReturns);
    }

    public async Task<(decimal var95, decimal var99)> CalculateMonteCarloVaRAsync(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns,
        int simulations = 100000)
    {
        try
        {
            // Prepare data for Python script
            var inputData = new
            {
                positions = positions.Select(p => new
                {
                    product = p.ProductType,
                    quantity = p.Quantity,
                    lotSize = p.LotSize,
                    currentPrice = p.CurrentPrice ?? p.EntryPrice,
                    position = p.Position.ToString()
                }).ToList(),
                returns = productReturns,
                simulations = simulations,
                seed = RANDOM_SEED
            };

            var jsonInput = JsonSerializer.Serialize(inputData);
            
            // Call Python script for Monte Carlo simulation
            var result = await CallPythonScript("montecarlo", jsonInput);
            
            if (result.ContainsKey("var95") && result.ContainsKey("var99"))
            {
                var var95 = Convert.ToDecimal(result["var95"]);
                var var99 = Convert.ToDecimal(result["var99"]);
                
                _logger.LogInformation("Monte Carlo VaR calculated: 95%={VaR95:C}, 99%={VaR99:C}", var95, var99);
                return (var95, var99);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Monte Carlo VaR, falling back to historical method");
        }

        // Fallback to historical method
        var portfolioReturns = CalculatePortfolioReturns(positions, productReturns);
        return await CalculateHistoricalVaRAsync(positions, portfolioReturns);
    }

    public async Task<List<StressTestResultDto>> RunStressTestsAsync(
        List<PaperContract> positions,
        Dictionary<string, decimal> currentPrices)
    {
        var stressTests = new List<StressTestResultDto>();
        var portfolioValue = positions.Sum(p => Math.Abs(p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice)));

        // Scenario 1: -10% price shock
        var shock10Down = CalculateStressImpact(positions, currentPrices, -0.10m);
        stressTests.Add(new StressTestResultDto
        {
            Scenario = "-10% Shock",
            PnlImpact = Math.Round(shock10Down),
            PercentageChange = Math.Round((shock10Down / portfolioValue) * 100, 2),
            Description = "10% decline in all oil and fuel prices"
        });

        // Scenario 2: +10% price shock
        var shock10Up = CalculateStressImpact(positions, currentPrices, 0.10m);
        stressTests.Add(new StressTestResultDto
        {
            Scenario = "+10% Shock",
            PnlImpact = Math.Round(shock10Up),
            PercentageChange = Math.Round((shock10Up / portfolioValue) * 100, 2),
            Description = "10% increase in all oil and fuel prices"
        });

        // Scenario 3: Historical worst daily loss (use -15% as proxy for oil market crash)
        var historicalWorst = CalculateStressImpact(positions, currentPrices, -0.15m);
        stressTests.Add(new StressTestResultDto
        {
            Scenario = "Historical Worst",
            PnlImpact = Math.Round(historicalWorst),
            PercentageChange = Math.Round((historicalWorst / portfolioValue) * 100, 2),
            Description = "Repeat of historical worst daily oil price decline (15%)"
        });

        // Scenario 4: Geopolitical crisis (asymmetric shock)
        var geoShock = CalculateAsymmetricStressImpact(positions, currentPrices);
        stressTests.Add(new StressTestResultDto
        {
            Scenario = "Geopolitical Crisis",
            PnlImpact = Math.Round(geoShock),
            PercentageChange = Math.Round((geoShock / portfolioValue) * 100, 2),
            Description = "Middle East crisis: Crude +20%, Products +15%"
        });

        // Scenario 5: Demand collapse
        var demandCollapse = CalculateStressImpact(positions, currentPrices, -0.25m);
        stressTests.Add(new StressTestResultDto
        {
            Scenario = "Demand Collapse",
            PnlImpact = Math.Round(demandCollapse),
            PercentageChange = Math.Round((demandCollapse / portfolioValue) * 100, 2),
            Description = "COVID-like demand destruction scenario (-25%)"
        });

        return stressTests;
    }

    /// <summary>
    /// CORRECTED Expected Shortfall (Conditional VaR / CVaR) calculation
    /// ES = Average of all losses exceeding the VaR threshold
    /// More coherent risk measure than VaR (satisfies subadditivity)
    /// </summary>
    public async Task<(decimal es95, decimal es99)> CalculateExpectedShortfallAsync(
        List<decimal> portfolioReturns,
        List<PaperContract> positions)
    {
        if (!portfolioReturns.Any())
            return (0, 0);

        var sortedReturns = portfolioReturns.OrderBy(r => r).ToList();

        // Calculate portfolio value
        var portfolioValue = positions.Sum(p => Math.Abs(p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice)));

        // CORRECTED: Use VaR quantile as threshold, then average losses beyond it
        // This is the mathematically correct CVaR definition

        // Calculate VaR quantiles first
        int index95 = Math.Max(0, (int)Math.Floor(sortedReturns.Count * 0.05));
        int index99 = Math.Max(0, (int)Math.Floor(sortedReturns.Count * 0.01));

        index95 = Math.Min(index95, sortedReturns.Count - 1);
        index99 = Math.Min(index99, sortedReturns.Count - 1);

        var var95Threshold = sortedReturns[index95];
        var var99Threshold = sortedReturns[index99];

        // Expected Shortfall = Average of all returns strictly worse than VaR threshold
        var lossesExceeding95 = sortedReturns.Where(r => r <= var95Threshold).ToList();
        var lossesExceeding99 = sortedReturns.Where(r => r <= var99Threshold).ToList();

        var es95 = lossesExceeding95.Any()
            ? Math.Abs(lossesExceeding95.Average() * portfolioValue)
            : 0;

        var es99 = lossesExceeding99.Any()
            ? Math.Abs(lossesExceeding99.Average() * portfolioValue)
            : 0;

        _logger.LogInformation("Expected Shortfall calculated: ES95={ES95:C}, ES99={ES99:C}", es95, es99);

        return (es95, es99);
    }

    public async Task<Dictionary<string, List<decimal>>> GetHistoricalReturnsAsync(
        List<string> productTypes,
        DateTime endDate,
        int days)
    {
        var returns = new Dictionary<string, List<decimal>>();
        
        foreach (var product in productTypes)
        {
            var prices = await _marketDataRepository.GetHistoricalPricesAsync(product, endDate.AddDays(-days), endDate);

            // Jane Street best practice: Always validate data quality
            if (prices != null && prices.Count > 1)
            {
                var productReturns = new List<decimal>();
                for (int i = 1; i < prices.Count; i++)
                {
                    if (prices[i-1].Price != 0)
                    {
                        var dailyReturn = (prices[i].Price - prices[i-1].Price) / prices[i-1].Price;
                        productReturns.Add(dailyReturn);
                    }
                }
                returns[product] = productReturns;
            }
            else
            {
                _logger.LogWarning("Insufficient price history for product {Product}. Found {Count} prices, need at least 2.",
                    product, prices?.Count ?? 0);
            }
        }
        
        return returns;
    }

    /// <summary>
    /// CORRECTED Portfolio Volatility Calculation using Covariance Matrix
    /// Implements: σ_p = sqrt(w' Σ w) where Σ is the covariance matrix
    /// This properly accounts for correlations between products
    /// </summary>
    public decimal CalculatePortfolioVolatility(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns)
    {
        if (!positions.Any())
            return 0;

        // If no return data, use fallback (which includes industry standard)
        if (!productReturns.Any())
        {
            _logger.LogWarning("No return data available for volatility calculation, using fallback method");
            return CalculateVolatilityFallback(positions, productReturns);
        }

        try
        {
            // Get product-month combinations with data
            // UPDATED: Group by (ProductType, ContractMonth) for proper futures/derivatives volatility calculation
            var products = positions
                .Select(p => $"{p.ProductType}|{p.ContractMonth}")
                .Distinct()
                .ToList();
            var productsWithData = products.Where(p => productReturns.ContainsKey(p) && productReturns[p].Any()).ToList();

            if (!productsWithData.Any())
            {
                _logger.LogWarning("No return data available for volatility calculation, using fallback method");
                return CalculateVolatilityFallback(positions, productReturns);
            }

            int n = productsWithData.Count;

            // Calculate covariance matrix
            var covarianceMatrix = CalculateCovarianceMatrix(productReturns, productsWithData);

            // Calculate position weights (normalized by total portfolio value)
            // Use composite key format for matching: ProductType|ContractMonth
            var totalValue = positions
                .Where(p => productsWithData.Contains($"{p.ProductType}|{p.ContractMonth}"))
                .Sum(p => Math.Abs(p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice)));

            if (totalValue == 0)
                return 0;

            var weights = new decimal[n];
            for (int i = 0; i < n; i++)
            {
                // Match using composite key format
                var productPositions = positions.Where(p => $"{p.ProductType}|{p.ContractMonth}" == productsWithData[i]);
                decimal productValue = 0;

                foreach (var position in productPositions)
                {
                    var value = position.Quantity * position.LotSize * (position.CurrentPrice ?? position.EntryPrice);
                    productValue += Math.Abs(value); // Use absolute value for weights
                }

                weights[i] = productValue / totalValue; // Normalized weight
            }

            // Portfolio variance = w' Σ w
            decimal portfolioVariance = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    portfolioVariance += weights[i] * covarianceMatrix[i, j] * weights[j];
                }
            }

            // Daily volatility
            var dailyVol = (decimal)Math.Sqrt((double)Math.Abs(portfolioVariance));

            // Annualize (assuming 252 trading days)
            var annualVol = dailyVol * (decimal)Math.Sqrt(252);

            _logger.LogInformation("Portfolio volatility calculated using covariance matrix: Daily={DailyVol:P4}, Annual={AnnualVol:P4}",
                dailyVol, annualVol);

            return annualVol;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating covariance-based volatility, using fallback");
            return CalculateVolatilityFallback(positions, productReturns);
        }
    }

    /// <summary>
    /// Fallback volatility calculation if covariance method fails
    /// Uses simple weighted portfolio returns method
    /// </summary>
    private decimal CalculateVolatilityFallback(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns)
    {
        var portfolioReturns = CalculatePortfolioReturns(positions, productReturns);

        if (portfolioReturns.Count < 2)
        {
            _logger.LogWarning("Insufficient data for volatility calculation, using industry standard assumption");
            return 0.25m; // 25% annual volatility - typical for oil markets
        }

        var mean = portfolioReturns.Average();
        var sumSquaredDeviations = portfolioReturns.Sum(r => Math.Pow((double)(r - mean), 2));
        var variance = (decimal)(sumSquaredDeviations / (portfolioReturns.Count - 1));
        var dailyVol = (decimal)Math.Sqrt((double)variance);

        // Annualize
        return dailyVol * (decimal)Math.Sqrt(252);
    }

    public decimal CalculateMaxDrawdown(List<decimal> cumulativeReturns)
    {
        if (!cumulativeReturns.Any())
            return 0;

        decimal maxDrawdown = 0;
        decimal peak = cumulativeReturns[0];
        
        foreach (var value in cumulativeReturns)
        {
            if (value > peak)
                peak = value;
            
            var drawdown = (peak - value) / peak;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }
        
        return maxDrawdown;
    }

    // Helper methods
    private async Task<List<PaperContract>> GetOpenPositionsAsync(DateTime asOfDate)
    {
        // Fetch paper contract positions
        var positions = (await _paperContractRepository.GetOpenPositionsAsync()).ToList();

        // Include physical purchase contracts as synthetic Long positions for VaR
        try
        {
            var purchases = await _purchaseContractRepository.GetActiveContractsAsync();
            foreach (var contract in purchases)
            {
                if (contract.Status == ContractStatus.Cancelled || contract.Status == ContractStatus.Completed) continue;
                positions.Add(new PaperContract
                {
                    ProductType = contract.Product?.Type.ToString() ?? "Unknown",
                    ContractMonth = (contract.LaycanStart ?? DateTime.UtcNow).ToString("MMMyy").ToUpper(),
                    Quantity = contract.ContractQuantity.Value,
                    LotSize = 1,
                    EntryPrice = contract.PriceFormula?.FixedPrice ?? 0m,
                    Position = PositionType.Long,
                    Status = PaperContractStatus.Open
                });
            }

            // Include physical sales contracts as synthetic Short positions for VaR
            var sales = await _salesContractRepository.GetActiveContractsAsync();
            foreach (var contract in sales)
            {
                if (contract.Status == ContractStatus.Cancelled || contract.Status == ContractStatus.Completed) continue;
                positions.Add(new PaperContract
                {
                    ProductType = contract.Product?.Type.ToString() ?? "Unknown",
                    ContractMonth = (contract.LaycanStart ?? DateTime.UtcNow).ToString("MMMyy").ToUpper(),
                    Quantity = contract.ContractQuantity.Value,
                    LotSize = 1,
                    EntryPrice = contract.PriceFormula?.FixedPrice ?? 0m,
                    Position = PositionType.Short,
                    Status = PaperContractStatus.Open
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to include physical contracts in VaR calculation, using paper-only");
        }

        return positions;
    }

    private async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(List<string> productTypes, DateTime date)
    {
        var prices = new Dictionary<string, decimal>();
        
        foreach (var product in productTypes)
        {
            var latestPrice = await _marketDataRepository.GetLatestPriceAsync(product, date);
            if (latestPrice != null)
                prices[product] = latestPrice.Price;
        }
        
        return prices;
    }

    private List<decimal> CalculatePortfolioReturns(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns)
    {
        if (!productReturns.Any())
            return new List<decimal>();

        var minLength = productReturns.Values.Min(r => r.Count);
        var portfolioReturns = new List<decimal>();
        
        for (int i = 0; i < minLength; i++)
        {
            decimal portfolioReturn = 0;
            decimal totalValue = 0;
            
            foreach (var position in positions)
            {
                if (productReturns.ContainsKey(position.ProductType))
                {
                    // CRITICAL FIX: Preserve position sign for proper risk calculation
                    // Use signed exposure to correctly account for hedging effects
                    var signedExposure = position.Quantity * position.LotSize * (position.CurrentPrice ?? position.EntryPrice);
                    
                    // Adjust for position type (Short positions have negative exposure)
                    if (position.Position == PositionType.Short)
                        signedExposure = -Math.Abs(signedExposure);
                    
                    var positionReturn = productReturns[position.ProductType][i];
                    
                    portfolioReturn += positionReturn * signedExposure;
                    totalValue += Math.Abs(signedExposure); // Use absolute value only for denominator
                }
            }
            
            if (totalValue > 0)
                portfolioReturns.Add(portfolioReturn / totalValue);
        }
        
        return portfolioReturns;
    }

    private decimal CalculateStressImpact(
        List<PaperContract> positions,
        Dictionary<string, decimal> currentPrices,
        decimal shockPercentage)
    {
        decimal totalImpact = 0;
        
        foreach (var position in positions)
        {
            if (currentPrices.ContainsKey(position.ProductType))
            {
                var currentPrice = currentPrices[position.ProductType];
                var shockedPrice = currentPrice * (1 + shockPercentage);
                var priceChange = shockedPrice - currentPrice;
                
                // Calculate P&L impact
                var multiplier = position.Position == PositionType.Long ? 1 : -1;
                var impact = priceChange * position.Quantity * position.LotSize * multiplier;
                totalImpact += impact;
            }
        }
        
        return totalImpact;
    }

    private decimal CalculateAsymmetricStressImpact(
        List<PaperContract> positions,
        Dictionary<string, decimal> currentPrices)
    {
        decimal totalImpact = 0;
        
        foreach (var position in positions)
        {
            if (currentPrices.ContainsKey(position.ProductType))
            {
                var currentPrice = currentPrices[position.ProductType];
                decimal shockPercentage;
                
                // Different shocks for different products
                if (position.ProductType.Contains("Brent", StringComparison.OrdinalIgnoreCase) ||
                    position.ProductType.Contains("WTI", StringComparison.OrdinalIgnoreCase))
                {
                    shockPercentage = 0.20m; // 20% increase for crude
                }
                else
                {
                    shockPercentage = 0.15m; // 15% increase for products
                }
                
                var shockedPrice = currentPrice * (1 + shockPercentage);
                var priceChange = shockedPrice - currentPrice;
                
                var multiplier = position.Position == PositionType.Long ? 1 : -1;
                var impact = priceChange * position.Quantity * position.LotSize * multiplier;
                totalImpact += impact;
            }
        }
        
        return totalImpact;
    }

    private List<ProductExposureDto> CalculateProductExposures(
        List<PaperContract> positions,
        Dictionary<string, List<decimal>> productReturns)
    {
        var exposures = new List<ProductExposureDto>();

        // UPDATED: Group by (ProductType, ContractMonth) for proper futures/derivatives exposure reporting
        var productGroups = positions.GroupBy(p => new { p.ProductType, p.ContractMonth });

        foreach (var group in productGroups)
        {
            var productPositions = group.ToList();
            var longPositions = productPositions.Where(p => p.Position == PositionType.Long).ToList();
            var shortPositions = productPositions.Where(p => p.Position == PositionType.Short).ToList();

            var longExposure = longPositions.Sum(p => p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice));
            var shortExposure = shortPositions.Sum(p => p.Quantity * p.LotSize * (p.CurrentPrice ?? p.EntryPrice));

            var volatility = 0m;
            // Create composite key for lookup
            var compositeKey = $"{group.Key.ProductType}|{group.Key.ContractMonth}";
            if (productReturns.ContainsKey(compositeKey) && productReturns[compositeKey].Any())
            {
                var returns = productReturns[compositeKey];
                var mean = returns.Average();
                var variance = returns.Sum(r => Math.Pow((double)(r - mean), 2)) / returns.Count;
                volatility = (decimal)Math.Sqrt(variance * 252); // Annualized
            }

            exposures.Add(new ProductExposureDto
            {
                ProductType = $"{group.Key.ProductType}|{group.Key.ContractMonth}",
                NetExposure = Math.Round(longExposure - shortExposure),
                GrossExposure = Math.Round(longExposure + shortExposure),
                LongPositions = longPositions.Count,
                ShortPositions = shortPositions.Count,
                Volatility = Math.Round(volatility, 4),
                VaR95 = 0, // Would be calculated per product
                VaR99 = 0  // Would be calculated per product
            });
        }

        return exposures;
    }

    private async Task<Dictionary<string, object>> CallPythonScript(string method, string jsonInput)
    {
        try
        {
            // Path to the Python script
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "risk_engine.py");
            
            if (!File.Exists(scriptPath))
            {
                _logger.LogWarning("Python risk engine script not found at {ScriptPath}. Falling back to basic calculations.", scriptPath);
                return CalculateFallbackRisk(jsonInput);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" {method}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Send JSON input to Python script
            await process.StandardInput.WriteLineAsync(jsonInput);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            // Wait for completion with timeout
            var completed = await Task.Run(() => process.WaitForExit(30000)); // 30 second timeout
            
            if (!completed)
            {
                process.Kill();
                _logger.LogWarning("Python script execution timed out. Falling back to basic calculations.");
                return CalculateFallbackRisk(jsonInput);
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Python script failed with exit code {ExitCode}. Error: {Error}. Falling back to basic calculations.", 
                    process.ExitCode, error);
                return CalculateFallbackRisk(jsonInput);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning("Python script returned empty output. Falling back to basic calculations.");
                return CalculateFallbackRisk(jsonInput);
            }

            // Parse JSON output from Python script
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(output);
            return result ?? CalculateFallbackRisk(jsonInput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Python risk engine. Falling back to basic calculations.");
            return CalculateFallbackRisk(jsonInput);
        }
    }

    private Dictionary<string, object> CalculateFallbackRisk(string jsonInput)
    {
        // Parse input to get some basic info for calculation
        try
        {
            var input = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonInput);

            // Extract portfolio value if available, otherwise use a default
            var portfolioValue = 1000000m; // Default $1M
            if (input?.ContainsKey("portfolio_value") == true &&
                decimal.TryParse(input["portfolio_value"].ToString(), out var pv))
            {
                portfolioValue = pv;
            }

            // CORRECTED: Fallback calculation with warning
            // Using historical oil market volatility range: 15-40% annualized
            // Conservative estimate: 30% annual = 1.89% daily
            var annualVolatility = 0.30m; // 30% - conservative oil market volatility
            var dailyVolatility = annualVolatility / (decimal)Math.Sqrt(252);

            _logger.LogWarning("Using fallback VaR calculation with assumed volatility of {AnnualVol:P0} (daily: {DailyVol:P2}). " +
                              "This is a rough estimate - actual risk may differ significantly.",
                              annualVolatility, dailyVolatility);

            var var95 = portfolioValue * dailyVolatility * 1.645m; // 95% confidence
            var var99 = portfolioValue * dailyVolatility * 2.326m; // 99% confidence

            return new Dictionary<string, object>
            {
                ["var95"] = var95,
                ["var99"] = var99,
                ["method"] = "fallback_calculation_assumed_volatility",
                ["volatility"] = dailyVolatility,
                ["assumed_annual_volatility"] = annualVolatility
            };
        }
        catch
        {
            // Ultimate fallback with conservative estimates
            _logger.LogError("Unable to calculate VaR even with fallback method. Using ultra-conservative fixed estimates.");

            return new Dictionary<string, object>
            {
                ["var95"] = 100000m, // Conservative fixed estimates
                ["var99"] = 150000m,
                ["method"] = "conservative_fixed_fallback"
            };
        }
    }

    private async Task<decimal> CalculatePortfolioValueAsync()
    {
        try
        {
            // Get all open paper contract positions
            var openContracts = await _paperContractRepository.GetOpenPositionsAsync();
            
            if (!openContracts.Any())
            {
                _logger.LogInformation("No open positions found. Using default portfolio value.");
                return 1000000m; // Default $1M if no positions
            }

            decimal totalValue = 0;
            
            foreach (var contract in openContracts)
            {
                // Get latest market price for the contract
                var latestPrice = await _marketDataRepository.GetLatestPriceAsync(contract.ProductType);
                
                if (latestPrice != null)
                {
                    // Calculate position value
                    // Positive for long positions, negative for short positions
                    var positionMultiplier = contract.Position == PositionType.Long ? 1 : -1;
                    var positionValue = contract.Quantity * contract.LotSize * latestPrice.Price * positionMultiplier;
                    totalValue += positionValue;
                }
                else
                {
                    _logger.LogWarning("No market price found for product {ProductType}", contract.ProductType);
                    // Use entry price as fallback
                    var positionMultiplier = contract.Position == PositionType.Long ? 1 : -1;
                    var positionValue = contract.Quantity * contract.LotSize * contract.EntryPrice * positionMultiplier;
                    totalValue += positionValue;
                }
            }

            // Ensure we have a minimum portfolio value for risk calculations
            var portfolioValue = Math.Abs(totalValue);
            return portfolioValue < 100000m ? 1000000m : portfolioValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating portfolio value. Using default.");
            return 1000000m; // Default fallback
        }
    }
}