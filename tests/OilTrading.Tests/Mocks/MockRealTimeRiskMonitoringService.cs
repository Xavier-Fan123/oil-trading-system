using OilTrading.Application.Services;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Tests.Mocks;

/// <summary>
/// Mock implementation of IRealTimeRiskMonitoringService for integration testing.
/// Returns safe default values that allow all risk checks to pass.
/// </summary>
public class MockRealTimeRiskMonitoringService : IRealTimeRiskMonitoringService
{
    // Real-time risk monitoring
    public Task<RealTimeRiskSnapshot> GetRealTimeRiskSnapshotAsync()
    {
        return Task.FromResult(new RealTimeRiskSnapshot
        {
            Timestamp = DateTime.UtcNow,
            VaR = new RealTimeVaRMetrics
            {
                VaR95 = 50000m, // Below $100K limit
                VaR99 = 75000m,
                ExpectedShortfall95 = 60000m,
                ExpectedShortfall99 = 85000m,
                PortfolioValue = 1000000m,
                DailyPnL = 5000m,
                IsBreaching = false
            },
            Concentration = new ConcentrationRiskMetrics
            {
                RiskLevel = ConcentrationRiskLevel.Low
            },
            Counterparty = new CounterpartyRiskSummary
            {
                TotalCounterparties = 5,
                TotalExposure = Money.Dollar(500000m),
                AverageRating = 3.5m,
                TopRisks = new List<CounterpartyRiskDetail>(),
                PotentialLoss = Money.Dollar(10000m)
            },
            Liquidity = new LiquidityRiskMetrics
            {
                LiquidityRatio = 1.5m,
                AverageLiquidationTime = TimeSpan.FromHours(4),
                IlliquidAssets = Money.Dollar(50000m),
                RiskLevel = LiquidityRiskLevel.Liquid
            },
            Operational = new OperationalRiskMetrics
            {
                ActiveIncidents = 0,
                SystemDowntime = 0,
                ProcessingErrorRate = 0.001m,
                PotentialOperationalLoss = Money.Dollar(1000m),
                RiskLevel = OperationalRiskLevel.Low
            },
            ActiveAlertsCount = 0,
            OverallStatus = RiskHealthStatus.Normal
        });
    }

    public Task<List<RiskAlert>> GetActiveRiskAlertsAsync()
    {
        return Task.FromResult(new List<RiskAlert>());
    }

    public Task<RiskMonitoringStatus> GetMonitoringStatusAsync()
    {
        return Task.FromResult(new RiskMonitoringStatus
        {
            IsActive = true,
            LastUpdate = DateTime.UtcNow,
            MonitoringModules = new Dictionary<string, bool>
            {
                ["VaRMonitoring"] = true,
                ["LimitMonitoring"] = true,
                ["ConcentrationMonitoring"] = true
            },
            ActiveMonitors = new List<string> { "VaR", "Limits", "Concentration" },
            FailedMonitors = new List<string>(),
            SystemHealth = new RiskSystemHealth
            {
                IsHealthy = true,
                LastHealthCheck = DateTime.UtcNow,
                Issues = new List<string>(),
                ComponentStatus = new Dictionary<string, string>
                {
                    ["VaREngine"] = "Healthy",
                    ["LimitEngine"] = "Healthy"
                }
            }
        });
    }

    // VaR monitoring
    public Task<RealTimeVaRMetrics> GetRealTimeVaRAsync()
    {
        return Task.FromResult(new RealTimeVaRMetrics
        {
            VaR95 = 50000m, // Below $100K limit - critical for risk checks
            VaR99 = 75000m,
            ExpectedShortfall95 = 60000m,
            ExpectedShortfall99 = 85000m,
            PortfolioValue = 1000000m,
            DailyPnL = 5000m,
            LastCalculated = DateTime.UtcNow,
            ModelUsed = VaRModelType.HistoricalSimulation,
            ComponentVaR = new Dictionary<string, decimal>
            {
                ["Crude"] = 30000m,
                ["Products"] = 20000m
            },
            IsBreaching = false,
            BreachAmount = 0m
        });
    }

    public Task<VaRBreachAlert?> CheckVaRBreachAsync()
    {
        return Task.FromResult<VaRBreachAlert?>(null); // No breaches
    }

    public Task<List<VaRTrend>> GetVaRTrendsAsync(int hours = 24)
    {
        var trends = new List<VaRTrend>();
        var now = DateTime.UtcNow;

        for (int i = 0; i < hours; i++)
        {
            trends.Add(new VaRTrend
            {
                Timestamp = now.AddHours(-i),
                VaR95 = 50000m + (i * 100m),
                VaR99 = 75000m + (i * 150m),
                PortfolioValue = 1000000m,
                PnL = 5000m
            });
        }

        return Task.FromResult(trends);
    }

    // Risk limit monitoring
    public Task<List<RiskLimitStatus>> GetRiskLimitStatusesAsync()
    {
        return Task.FromResult(new List<RiskLimitStatus>
        {
            new RiskLimitStatus
            {
                LimitId = Guid.NewGuid(),
                LimitName = "VaR 95% Limit",
                LimitType = RiskLimitType.VaR,
                LimitValue = 100000m,
                CurrentValue = 50000m,
                UtilizationPercentage = 50m,
                Status = RiskLimitSeverity.Normal,
                IsBreaching = false
            }
        });
    }

    public Task<RiskLimitBreachResult> CheckRiskLimitsAsync()
    {
        return Task.FromResult(new RiskLimitBreachResult
        {
            HasBreaches = false, // Critical: No breaches for risk checks to pass
            Breaches = new List<RiskLimitBreach>(),
            TotalBreaches = 0,
            CriticalBreaches = 0,
            CheckTime = DateTime.UtcNow
        });
    }

    public Task<List<RiskLimitAlert>> GetRiskLimitAlertsAsync()
    {
        return Task.FromResult(new List<RiskLimitAlert>());
    }

    // Position risk monitoring
    public Task<List<PositionRisk>> GetPositionRisksAsync()
    {
        return Task.FromResult(new List<PositionRisk>
        {
            new PositionRisk
            {
                PositionId = Guid.NewGuid(),
                ProductType = "Crude",
                Position = Quantity.MetricTons(1000),
                MarketValue = Money.Dollar(75000m),
                DeltaEquivalent = 1000m,
                VaRContribution = 25000m,
                Beta = 1.2m,
                RiskRating = RiskRating.Medium
            }
        });
    }

    public Task<ConcentrationRiskMetrics> GetConcentrationRiskAsync()
    {
        return Task.FromResult(new ConcentrationRiskMetrics
        {
            HerfindahlIndex = 0.3m,
            TopConcentrationPercentage = 40m,
            NumberOfPositions = 10,
            ConcentrationByProduct = new List<ConcentrationBreakdown>(),
            ConcentrationByCounterparty = new List<ConcentrationBreakdown>(),
            ConcentrationByTrader = new List<ConcentrationBreakdown>(),
            RiskLevel = ConcentrationRiskLevel.Low
        });
    }

    public Task<CounterpartyRiskSummary> GetCounterpartyRiskAsync()
    {
        return Task.FromResult(new CounterpartyRiskSummary
        {
            TotalCounterparties = 5,
            TotalExposure = Money.Dollar(500000m),
            AverageRating = 3.5m,
            TopRisks = new List<CounterpartyRiskDetail>(),
            CounterpartiesAboveThreshold = 0,
            PotentialLoss = Money.Dollar(10000m)
        });
    }

    // Real-time alerting
    public Task<AlertResult> CreateRiskAlertAsync(RiskAlertRequest request)
    {
        return Task.FromResult(new AlertResult
        {
            IsSuccessful = true,
            AlertId = Guid.NewGuid(),
            NotificationsSent = new List<string> { "TestNotification" }
        });
    }

    public Task<bool> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ResolveAlertAsync(Guid alertId, string resolvedBy, string resolution)
    {
        return Task.FromResult(true);
    }

    // Risk dashboard data
    public Task<RiskDashboardData> GetRiskDashboardDataAsync()
    {
        return Task.FromResult(new RiskDashboardData
        {
            GeneratedAt = DateTime.UtcNow,
            RiskSnapshot = new RealTimeRiskSnapshot(),
            RecentAlerts = new List<RiskAlert>(),
            LimitStatuses = new List<RiskLimitStatus>(),
            Trends = new List<RiskTrendPoint>(),
            KeyMetrics = new Dictionary<string, decimal>
            {
                ["VaR95"] = 50000m,
                ["VaR99"] = 75000m
            }
        });
    }

    public Task<List<RiskMetricTimeSeries>> GetRiskTimeSeriesAsync(string metricName, TimeSpan period)
    {
        return Task.FromResult(new List<RiskMetricTimeSeries>
        {
            new RiskMetricTimeSeries
            {
                MetricName = metricName,
                DataPoints = new List<TimeSeriesPoint>(),
                Period = period,
                StartTime = DateTime.UtcNow.Subtract(period),
                EndTime = DateTime.UtcNow
            }
        });
    }

    // Risk monitoring configuration
    public Task ConfigureRiskThresholdsAsync(RiskThresholdConfiguration config)
    {
        return Task.CompletedTask;
    }

    public Task<List<RiskThreshold>> GetRiskThresholdsAsync()
    {
        return Task.FromResult(new List<RiskThreshold>
        {
            new RiskThreshold
            {
                Id = Guid.NewGuid(),
                Name = "VaR 95% Warning",
                MetricType = RiskMetricType.VaR95,
                WarningThreshold = 80000m,
                CriticalThreshold = 100000m,
                IsActive = true
            }
        });
    }

    public Task EnableRiskMonitoringAsync(string riskType)
    {
        return Task.CompletedTask;
    }

    public Task DisableRiskMonitoringAsync(string riskType)
    {
        return Task.CompletedTask;
    }

    // Stress testing integration
    public Task<StressTestResult> RunRealTimeStressTestAsync(StressTestScenario scenario)
    {
        return Task.FromResult(new StressTestResult
        {
            Id = Guid.NewGuid(),
            ScenarioName = scenario.Name,
            ExecutedAt = DateTime.UtcNow,
            PotentialLoss = Money.Dollar(200000m),
            PortfolioImpactPercentage = 20m,
            ComponentResults = new List<StressTestComponentResult>(),
            ExceedsRiskTolerance = false,
            Severity = StressTestSeverity.Low,
            WorstCaseLoss = 200000m // Below $500K stress loss limit
        });
    }

    public Task<List<StressTestAlert>> GetStressTestAlertsAsync()
    {
        return Task.FromResult(new List<StressTestAlert>());
    }

    // Risk reporting
    public Task<RiskReport> GenerateRealTimeRiskReportAsync()
    {
        return Task.FromResult(new RiskReport
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = new RealTimeRiskSnapshot(),
            Alerts = new List<RiskAlert>(),
            LimitStatuses = new List<RiskLimitStatus>(),
            DetailedAnalysis = null
        });
    }

    public Task<byte[]> ExportRiskDataAsync(RiskExportRequest request)
    {
        return Task.FromResult(new byte[] { 1, 2, 3 }); // Dummy export data
    }

    // Additional methods needed by RiskCheckAttribute
    public Task<SystemRiskStatus> GetSystemRiskStatusAsync()
    {
        return Task.FromResult(SystemRiskStatus.Normal); // Critical: Must return Normal for checks to pass
    }

    public Task<RealTimeVaRMetrics> GetRealTimeRiskAsync()
    {
        return GetRealTimeVaRAsync(); // Reuse existing implementation
    }

    public Task<List<StressTestResult>> RunRealTimeStressTestAsync()
    {
        return Task.FromResult(new List<StressTestResult>
        {
            new StressTestResult
            {
                Id = Guid.NewGuid(),
                ScenarioName = "Oil Price Shock",
                ExecutedAt = DateTime.UtcNow,
                PotentialLoss = Money.Dollar(200000m),
                PortfolioImpactPercentage = 20m,
                ComponentResults = new List<StressTestComponentResult>(),
                ExceedsRiskTolerance = false,
                Severity = StressTestSeverity.Low,
                WorstCaseLoss = 200000m // Below $500K limit
            }
        });
    }

    public Task<MonteCarloResult> RunMonteCarloSimulationAsync(int iterations)
    {
        return Task.FromResult(new MonteCarloResult
        {
            VaR95 = 50000m,
            VaR99 = 750000m, // Below $1M VaR99 limit for critical operations
            ExpectedShortfall95 = 60000m,
            ExpectedShortfall99 = 850000m,
            WorstCaseLoss = 900000m,
            BestCaseGain = 100000m,
            Iterations = iterations,
            CalculatedAt = DateTime.UtcNow
        });
    }

    public Task<decimal> CalculateCorrelationRiskAsync()
    {
        return Task.FromResult(0.5m); // Below 0.8 high correlation threshold
    }

    public Task TriggerRiskAlertAsync(RiskAlert alert)
    {
        // Mock implementation - just log or ignore
        return Task.CompletedTask;
    }

    // Additional methods needed by RiskCheckMiddleware
    public Task<OperationRiskCheckResult> CheckOperationRiskAsync(OperationDetails details)
    {
        return Task.FromResult(new OperationRiskCheckResult
        {
            PassesAllChecks = true, // Critical: Allow all operations to pass
            OverallRiskScore = 20m,
            Violations = new List<string>(),
            CheckedAt = DateTime.UtcNow
        });
    }
}
