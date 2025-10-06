using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.DTOs;
using CoreSettlement = OilTrading.Core.Entities.Settlement;
using DtoSettlement = OilTrading.Application.DTOs.Settlement;
using CoreSettlementStatus = OilTrading.Core.Entities.SettlementStatus;
using DtoSettlementStatus = OilTrading.Application.DTOs.SettlementStatus;
using CorePaymentStatus = OilTrading.Core.Entities.PaymentStatus;
using DtoPaymentStatus = OilTrading.Application.DTOs.PaymentStatus;
using CoreSettlementType = OilTrading.Core.Entities.SettlementType;
using DtoSettlementType = OilTrading.Application.DTOs.SettlementType;
using CorePayment = OilTrading.Core.Entities.Payment;
using DtoPayment = OilTrading.Application.DTOs.Payment;
using DtoBankAccount = OilTrading.Application.DTOs.BankAccount;
using CoreBankAccount = OilTrading.Core.Entities.BankAccount;

namespace OilTrading.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly ISettlementRepository _settlementRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ITradingPartnerRepository _tradingPartnerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(
        ISettlementRepository settlementRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ITradingPartnerRepository tradingPartnerRepository,
        IUnitOfWork unitOfWork,
        ILogger<SettlementService> logger)
    {
        _settlementRepository = settlementRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _tradingPartnerRepository = tradingPartnerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SettlementResult> CreateSettlementAsync(SettlementRequest request)
    {
        _logger.LogInformation("Creating settlement for contract {ContractId} with amount {Amount}", 
            request.ContractId, request.Amount);

        try
        {
            // Validate contract exists
            var contract = await GetContractAsync(request.ContractId);
            if (contract == null)
            {
                return new SettlementResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Contract {request.ContractId} not found"
                };
            }

            // Validate trading partners exist
            var payerExists = await _tradingPartnerRepository.ExistsAsync(tp => tp.Id == request.PayerPartyId);
            var payeeExists = await _tradingPartnerRepository.ExistsAsync(tp => tp.Id == request.PayeePartyId);

            if (!payerExists || !payeeExists)
            {
                return new SettlementResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Invalid payer or payee party"
                };
            }

            // Create settlement
            var settlement = new CoreSettlement(
                request.ContractId,
                (Core.Entities.SettlementType)request.Type,
                request.Amount,
                request.DueDate,
                request.PayerPartyId,
                request.PayeePartyId,
                request.Terms,
                request.Description,
                "System"); // In real implementation, use current user

            await _settlementRepository.AddAsync(settlement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Settlement {SettlementNumber} created successfully for contract {ContractId}", 
                settlement.SettlementNumber, request.ContractId);

            return new SettlementResult
            {
                IsSuccessful = true,
                SettlementId = settlement.Id,
                SettlementNumber = settlement.SettlementNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement for contract {ContractId}", request.ContractId);
            return new SettlementResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SettlementResult> ProcessSettlementAsync(Guid settlementId)
    {
        _logger.LogInformation("Processing settlement {SettlementId}", settlementId);

        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
            {
                return new SettlementResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Settlement not found"
                };
            }

            settlement.Process("System");
            await _settlementRepository.UpdateAsync(settlement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Settlement {SettlementNumber} processed successfully", settlement.SettlementNumber);

            return new SettlementResult
            {
                IsSuccessful = true,
                SettlementId = settlement.Id,
                SettlementNumber = settlement.SettlementNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing settlement {SettlementId}", settlementId);
            return new SettlementResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SettlementResult> CancelSettlementAsync(Guid settlementId, string reason)
    {
        _logger.LogInformation("Cancelling settlement {SettlementId} with reason: {Reason}", settlementId, reason);

        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
            {
                return new SettlementResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Settlement not found"
                };
            }

            settlement.Cancel(reason, "System");
            await _settlementRepository.UpdateAsync(settlement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Settlement {SettlementNumber} cancelled successfully", settlement.SettlementNumber);

            return new SettlementResult
            {
                IsSuccessful = true,
                SettlementId = settlement.Id,
                SettlementNumber = settlement.SettlementNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling settlement {SettlementId}", settlementId);
            return new SettlementResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SettlementResult> UpdateSettlementAsync(Guid settlementId, SettlementUpdateRequest request)
    {
        _logger.LogInformation("Updating settlement {SettlementId}", settlementId);

        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
            {
                return new SettlementResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Settlement not found"
                };
            }

            if (request.Amount != null)
            {
                settlement.UpdateAmount(request.Amount, request.Comments ?? "Amount updated", "System");
            }

            if (request.DueDate.HasValue)
            {
                settlement.UpdateDueDate(request.DueDate.Value, request.Comments ?? "Due date updated", "System");
            }

            if (request.Status.HasValue)
            {
                switch (request.Status.Value)
                {
                    case DtoSettlementStatus.Processing:
                        settlement.Process("System");
                        break;
                    case DtoSettlementStatus.Completed:
                        settlement.Complete("System");
                        break;
                    case DtoSettlementStatus.Cancelled:
                        settlement.Cancel(request.Comments ?? "Cancelled", "System");
                        break;
                }
            }

            await _settlementRepository.UpdateAsync(settlement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Settlement {SettlementNumber} updated successfully", settlement.SettlementNumber);

            return new SettlementResult
            {
                IsSuccessful = true,
                SettlementId = settlement.Id,
                SettlementNumber = settlement.SettlementNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement {SettlementId}", settlementId);
            return new SettlementResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DtoSettlement> GetSettlementAsync(Guid settlementId)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        if (settlement == null)
            throw new NotFoundException($"Settlement {settlementId} not found");

        return MapToDto(settlement);
    }

    public async Task<List<DtoSettlement>> GetSettlementsByContractAsync(Guid contractId)
    {
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId);
        return settlements.Select(MapToDto).ToList();
    }

    public async Task<List<DtoSettlement>> GetPendingSettlementsAsync()
    {
        var settlements = await _settlementRepository.GetPendingSettlementsAsync();
        return settlements.Select(MapToDto).ToList();
    }

    public async Task<SettlementSummary> GetSettlementSummaryAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating settlement summary from {StartDate} to {EndDate}", startDate, endDate);

        var settlements = await _settlementRepository.GetByCreatedDateRangeAsync(startDate, endDate);
        var settlementList = settlements.ToList();

        var totalAmount = settlementList.Sum(s => s.Amount.Amount);
        var statusCounts = settlementList.GroupBy(s => s.Status).ToDictionary(g => g.Key, g => g.Count());
        var typeCounts = settlementList.GroupBy(s => s.Type).ToDictionary(g => g.Key, g => g.Count());
        var currencyAmounts = settlementList.GroupBy(s => s.Amount.Currency)
            .ToDictionary(g => g.Key, g => Money.Dollar(g.Sum(s => s.Amount.Amount)));

        var trends = GenerateSettlementTrends(settlementList, startDate, endDate);

        return new SettlementSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalSettlements = settlementList.Count,
            TotalAmount = Money.Dollar(totalAmount),
            SettlementsByStatus = statusCounts,
            AmountsByCurrency = currencyAmounts,
            SettlementsByType = typeCounts,
            Trends = trends
        };
    }

    public async Task<List<SettlementResult>> ProcessDueSettlementsAsync()
    {
        _logger.LogInformation("Processing due settlements");

        var dueSettlements = await _settlementRepository.GetDueSettlementsAsync();
        var results = new List<SettlementResult>();

        foreach (var settlement in dueSettlements.Where(s => s.Status == CoreSettlementStatus.Approved))
        {
            try
            {
                if (settlement.Terms.EnableAutomaticProcessing)
                {
                    settlement.Process("AutoProcessor");
                    await _settlementRepository.UpdateAsync(settlement);

                    results.Add(new SettlementResult
                    {
                        IsSuccessful = true,
                        SettlementId = settlement.Id,
                        SettlementNumber = settlement.SettlementNumber
                    });

                    _logger.LogInformation("Auto-processed settlement {SettlementNumber}", settlement.SettlementNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-processing settlement {SettlementId}", settlement.Id);
                results.Add(new SettlementResult
                {
                    IsSuccessful = false,
                    SettlementId = settlement.Id,
                    ErrorMessage = ex.Message
                });
            }
        }

        if (results.Any(r => r.IsSuccessful))
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return results;
    }

    public async Task<SettlementScheduleResult> ScheduleSettlementAsync(Guid contractId, SettlementScheduleRequest request)
    {
        _logger.LogInformation("Scheduling settlement for contract {ContractId}", contractId);

        try
        {
            var dueDates = GenerateScheduledDueDates(request);
            
            var result = new SettlementScheduleResult
            {
                IsSuccessful = true,
                ScheduleId = Guid.NewGuid(),
                GeneratedDueDates = dueDates
            };

            // In a real implementation, you would create a SettlementSchedule entity
            // and store it in the database for future processing

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling settlement for contract {ContractId}", contractId);
            return new SettlementScheduleResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<SettlementSchedule>> GetScheduledSettlementsAsync(DateTime? fromDate = null)
    {
        // In a real implementation, this would query the SettlementSchedule table
        // For now, return empty list as placeholder
        return new List<SettlementSchedule>();
    }

    public async Task<ReconciliationResult> ReconcileSettlementAsync(Guid settlementId)
    {
        _logger.LogInformation("Reconciling settlement {SettlementId}", settlementId);

        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        if (settlement == null)
        {
            throw new NotFoundException($"Settlement {settlementId} not found");
        }

        var netAmount = settlement.GetNetAmount();
        var totalPaid = settlement.Payments.Where(p => p.Status == Core.Entities.PaymentStatus.Completed).Sum(p => p.Amount.Amount);
        var variance = Math.Abs(netAmount - totalPaid);

        var isReconciled = variance <= 0.01m; // Allow for small rounding differences
        var issues = new List<ReconciliationIssue>();

        if (!isReconciled)
        {
            issues.Add(new ReconciliationIssue
            {
                SettlementId = settlementId,
                Type = ReconciliationIssueType.AmountMismatch,
                Description = $"Payment amount {totalPaid:C} does not match settlement amount {netAmount:C}",
                AmountDifference = Money.Dollar(variance),
                Severity = variance > 1000 ? ReconciliationSeverity.High : ReconciliationSeverity.Medium
            });
        }

        return new ReconciliationResult
        {
            IsReconciled = isReconciled,
            Issues = issues,
            ReconciledAmount = Money.Dollar(Math.Min(netAmount, totalPaid)),
            VarianceAmount = Money.Dollar(variance),
            ReconciliationDate = DateTime.UtcNow
        };
    }

    public async Task<List<ReconciliationIssue>> GetReconciliationIssuesAsync()
    {
        // In a real implementation, this would query a ReconciliationIssues table
        // For now, scan recent settlements for potential issues
        var settlements = await _settlementRepository.GetRecentSettlementsAsync(100);
        var issues = new List<ReconciliationIssue>();

        foreach (var settlement in settlements)
        {
            var reconciliation = await ReconcileSettlementAsync(settlement.Id);
            issues.AddRange(reconciliation.Issues);
        }

        return issues;
    }

    public async Task<ReconciliationSummary> GetReconciliationSummaryAsync(DateTime date)
    {
        var settlements = await _settlementRepository.GetByCreatedDateRangeAsync(date.Date, date.Date.AddDays(1));
        var settlementList = settlements.ToList();

        var totalSettlements = settlementList.Count;
        var reconciled = 0;
        var totalVariance = 0m;
        var issues = new List<ReconciliationIssue>();

        foreach (var settlement in settlementList)
        {
            var reconciliation = await ReconcileSettlementAsync(settlement.Id);
            if (reconciliation.IsReconciled)
                reconciled++;
            
            totalVariance += reconciliation.VarianceAmount.Amount;
            issues.AddRange(reconciliation.Issues);
        }

        return new ReconciliationSummary
        {
            Date = date,
            TotalSettlements = totalSettlements,
            ReconciledSettlements = reconciled,
            UnreconciledSettlements = totalSettlements - reconciled,
            TotalVariance = Money.Dollar(totalVariance),
            OutstandingIssues = issues.Where(i => !i.IsResolved).ToList()
        };
    }

    public async Task<PaymentResult> InitiatePaymentAsync(Guid settlementId, PaymentRequest request)
    {
        _logger.LogInformation("Initiating payment for settlement {SettlementId}", settlementId);

        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
            {
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Settlement not found"
                };
            }

            var payment = settlement.CreatePayment(
                (Core.Entities.PaymentMethod)request.Method,
                request.Amount,
                ConvertBankAccount(request.PayerAccount),
                ConvertBankAccount(request.PayeeAccount),
                request.Instructions);

            await _settlementRepository.UpdateAsync(settlement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentReference} initiated for settlement {SettlementNumber}", 
                payment.PaymentReference, settlement.SettlementNumber);

            return new PaymentResult
            {
                IsSuccessful = true,
                PaymentId = payment.Id,
                PaymentReference = payment.PaymentReference,
                Status = ConvertToDtoPaymentStatus(payment.Status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for settlement {SettlementId}", settlementId);
            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> ConfirmPaymentReceiptAsync(Guid settlementId, PaymentConfirmationRequest request)
    {
        _logger.LogInformation("Confirming payment receipt for settlement {SettlementId}", settlementId);

        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
            {
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Settlement not found"
                };
            }

            var payment = settlement.Payments.FirstOrDefault(p => p.PaymentReference == request.PaymentReference);
            if (payment == null)
            {
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Payment not found"
                };
            }

            if ((int)request.Status == (int)DtoPaymentStatus.Completed)
            {
                payment.Complete(request.BankReference ?? "", "System");
                
                // Check if settlement can be completed
                if (settlement.GetRemainingAmount() <= 0)
                {
                    settlement.Complete("System");
                }
            }
            else if ((int)request.Status == (int)DtoPaymentStatus.Failed)
            {
                payment.Fail(request.Comments ?? "Payment failed", "System");
            }

            await _settlementRepository.UpdateAsync(settlement);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResult
            {
                IsSuccessful = true,
                PaymentId = payment.Id,
                PaymentReference = payment.PaymentReference,
                Status = ConvertToDtoPaymentStatus(payment.Status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment receipt for settlement {SettlementId}", settlementId);
            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<DTOs.Payment>> GetPaymentHistoryAsync(Guid? contractId = null, Guid? settlementId = null)
    {
        var payments = new List<DTOs.Payment>();

        if (settlementId.HasValue)
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId.Value);
            if (settlement != null)
            {
                payments.AddRange(settlement.Payments.Select(ConvertPaymentToDto));
            }
        }
        else if (contractId.HasValue)
        {
            var settlements = await _settlementRepository.GetByContractIdAsync(contractId.Value);
            payments.AddRange(settlements.SelectMany(s => s.Payments.Select(ConvertPaymentToDto)));
        }

        return payments.OrderByDescending(p => p.CreatedDate).ToList();
    }

    public async Task<SettlementMatchingResult> MatchSettlementsAsync(SettlementMatchingRequest request)
    {
        // Implementation for settlement matching would go here
        // This is a complex feature that would match settlements based on various criteria
        return new SettlementMatchingResult
        {
            IsSuccessful = true,
            Matches = new List<SettlementMatch>(),
            UnmatchedSettlements = request.SettlementIds.ToList()
        };
    }

    public async Task<List<SettlementMatchingRecommendation>> GetMatchingRecommendationsAsync()
    {
        // Implementation for matching recommendations would go here
        return new List<SettlementMatchingRecommendation>();
    }

    public async Task<SettlementAnalytics> GetSettlementAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating settlement analytics from {StartDate} to {EndDate}", startDate, endDate);

        var settlements = await _settlementRepository.GetByCreatedDateRangeAsync(startDate, endDate);
        var settlementList = settlements.ToList();

        var volume = CalculateVolumeMetrics(settlementList);
        var performance = await CalculatePerformanceMetricsAsync(settlementList, startDate, endDate);
        var cashFlow = CalculateCashFlowMetrics(settlementList);
        var trends = GenerateSettlementTrends(settlementList, startDate, endDate);

        var kpis = new Dictionary<string, decimal>
        {
            ["TotalSettlements"] = settlementList.Count,
            ["TotalAmount"] = volume.TotalAmount.Amount,
            ["AverageSettlementAmount"] = volume.AverageSettlementAmount.Amount,
            ["OnTimePaymentRate"] = (decimal)performance.OnTimePaymentRate,
            ["ReconciliationRate"] = (decimal)performance.ReconciliationRate,
            ["AverageSettlementDays"] = (decimal)performance.AveragePaymentDelay.TotalDays
        };

        return new SettlementAnalytics
        {
            StartDate = startDate,
            EndDate = endDate,
            Volume = volume,
            Performance = performance,
            CashFlow = cashFlow,
            Trends = trends,
            KPIs = kpis
        };
    }

    public async Task<CashFlowForecast> GenerateCashFlowForecastAsync(int forecastDays)
    {
        _logger.LogInformation("Generating cash flow forecast for {ForecastDays} days", forecastDays);

        var currentDate = DateTime.UtcNow.Date;
        var endDate = currentDate.AddDays(forecastDays);

        // Get all pending and approved settlements within the forecast period
        var futureSettlements = await _settlementRepository.GetByDateRangeAsync(currentDate, endDate);
        var settlementList = futureSettlements.Where(s => 
            s.Status == Core.Entities.SettlementStatus.Pending || 
            s.Status == Core.Entities.SettlementStatus.Approved || 
            s.Status == Core.Entities.SettlementStatus.Processing).ToList();

        var periods = new List<CashFlowForecastPeriod>();
        var cumulativeBalance = 0m;

        for (int i = 0; i <= forecastDays; i++)
        {
            var forecastDate = currentDate.AddDays(i);
            var daySettlements = settlementList.Where(s => s.DueDate.Date == forecastDate).ToList();

            var inflows = daySettlements.Where(s => s.Type == Core.Entities.SettlementType.ContractPayment).Sum(s => s.Amount.Amount);
            var outflows = daySettlements.Where(s => s.Type == Core.Entities.SettlementType.Refund || s.Type == Core.Entities.SettlementType.Penalty).Sum(s => s.Amount.Amount);
            var netFlow = inflows - outflows;
            cumulativeBalance += netFlow;

            periods.Add(new CashFlowForecastPeriod
            {
                Date = forecastDate,
                PredictedInflows = Money.Dollar(inflows),
                PredictedOutflows = Money.Dollar(outflows),
                PredictedNetFlow = Money.Dollar(netFlow),
                CumulativeBalance = Money.Dollar(cumulativeBalance),
                ConfidenceLevel = CalculateConfidenceLevel(forecastDate, currentDate)
            });
        }

        var risks = IdentifyCashFlowRisks(periods);

        return new CashFlowForecast
        {
            GeneratedDate = DateTime.UtcNow,
            ForecastDays = forecastDays,
            Periods = periods,
            Accuracy = new ForecastAccuracy
            {
                HistoricalAccuracy = 85m, // This would be calculated from historical data
                ForecastHorizon = TimeSpan.FromDays(forecastDays),
                AssumptionsMade = new List<string>
                {
                    "All pending settlements will be processed on schedule",
                    "No new settlements will be created",
                    "Exchange rates remain stable"
                }
            },
            Risks = risks
        };
    }

    public async Task<List<SettlementAlert>> GetSettlementAlertsAsync()
    {
        var alerts = new List<SettlementAlert>();

        // Check for overdue settlements
        var overdueSettlements = await _settlementRepository.GetOverdueSettlementsAsync();
        foreach (var settlement in overdueSettlements)
        {
            alerts.Add(new SettlementAlert
            {
                Type = SettlementAlertType.PaymentOverdue,
                Description = $"Settlement {settlement.SettlementNumber} is {settlement.GetDaysOverdue().Days} days overdue",
                Severity = settlement.GetDaysOverdue().Days > 30 ? SettlementAlertSeverity.Critical : SettlementAlertSeverity.High,
                SettlementId = settlement.Id,
                Data = new Dictionary<string, object>
                {
                    ["DaysOverdue"] = settlement.GetDaysOverdue().Days,
                    ["Amount"] = settlement.Amount.Amount,
                    ["DueDate"] = settlement.DueDate
                }
            });
        }

        // Check for large cash movements
        var today = DateTime.UtcNow.Date;
        var largeSettlements = await _settlementRepository.GetLargeSettlementsAsync(1000000m); // $1M threshold
        var todayLargeSettlements = largeSettlements.Where(s => s.DueDate.Date == today);

        foreach (var settlement in todayLargeSettlements)
        {
            alerts.Add(new SettlementAlert
            {
                Type = SettlementAlertType.LargeCashMovement,
                Description = $"Large settlement {settlement.SettlementNumber} of {settlement.Amount.Amount:C} due today",
                Severity = SettlementAlertSeverity.Warning,
                SettlementId = settlement.Id,
                Data = new Dictionary<string, object>
                {
                    ["Amount"] = settlement.Amount.Amount,
                    ["DueDate"] = settlement.DueDate
                }
            });
        }

        return alerts;
    }

    // Helper methods
    private async Task<object?> GetContractAsync(Guid contractId)
    {
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(contractId);
        if (purchaseContract != null) return purchaseContract;

        var salesContract = await _salesContractRepository.GetByIdAsync(contractId);
        return salesContract;
    }

    private List<DateTime> GenerateScheduledDueDates(SettlementScheduleRequest request)
    {
        var dates = new List<DateTime>();
        var currentDate = request.StartDate;
        var paymentCount = 0;

        while ((request.EndDate == null || currentDate <= request.EndDate) && 
               (request.NumberOfPayments == null || paymentCount < request.NumberOfPayments))
        {
            dates.Add(currentDate);
            paymentCount++;

            currentDate = request.Frequency switch
            {
                SettlementFrequency.Daily => currentDate.AddDays(1),
                SettlementFrequency.Weekly => currentDate.AddDays(7),
                SettlementFrequency.Monthly => currentDate.AddMonths(1),
                SettlementFrequency.Quarterly => currentDate.AddMonths(3),
                SettlementFrequency.SemiAnnually => currentDate.AddMonths(6),
                SettlementFrequency.Annually => currentDate.AddYears(1),
                _ => currentDate.AddDays(1)
            };
        }

        return dates;
    }

    private List<SettlementTrend> GenerateSettlementTrends(List<CoreSettlement> settlements, DateTime startDate, DateTime endDate)
    {
        var trends = new List<SettlementTrend>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var daySettlements = settlements.Where(s => s.CreatedDate.Date == currentDate).ToList();
            
            trends.Add(new SettlementTrend
            {
                Date = currentDate,
                Count = daySettlements.Count,
                Amount = Money.Dollar(daySettlements.Sum(s => s.Amount.Amount))
            });

            currentDate = currentDate.AddDays(1);
        }

        return trends;
    }

    private SettlementVolumeMetrics CalculateVolumeMetrics(List<CoreSettlement> settlements)
    {
        if (!settlements.Any())
        {
            return new SettlementVolumeMetrics
            {
                TotalAmount = Money.Zero("USD"),
                AverageSettlementAmount = Money.Zero("USD"),
                LargestSettlement = Money.Zero("USD"),
                SmallestSettlement = Money.Zero("USD")
            };
        }

        var totalAmount = settlements.Sum(s => s.Amount.Amount);
        var averageAmount = totalAmount / settlements.Count;
        var largest = settlements.Max(s => s.Amount.Amount);
        var smallest = settlements.Min(s => s.Amount.Amount);

        return new SettlementVolumeMetrics
        {
            TotalSettlements = settlements.Count,
            TotalAmount = Money.Dollar(totalAmount),
            AverageSettlementAmount = Money.Dollar(averageAmount),
            LargestSettlement = Money.Dollar(largest),
            SmallestSettlement = Money.Dollar(smallest),
            VolumeByType = settlements.GroupBy(s => s.Type).ToDictionary(g => g.Key, g => g.Count()),
            VolumeByCounterparty = settlements.GroupBy(s => s.PayerPartyId.ToString())
                .ToDictionary(g => g.Key, g => Money.Dollar(g.Sum(s => s.Amount.Amount)))
        };
    }

    private async Task<SettlementPerformanceMetrics> CalculatePerformanceMetricsAsync(List<CoreSettlement> settlements, DateTime startDate, DateTime endDate)
    {
        var completedSettlements = settlements.Where(s => s.Status == Core.Entities.SettlementStatus.Completed).ToList();
        
        var onTimeCount = completedSettlements.Count(s => s.CompletedDate <= s.DueDate);
        var onTimeRate = completedSettlements.Any() ? (double)onTimeCount / completedSettlements.Count * 100 : 0;
        
        var avgDelay = completedSettlements.Any() 
            ? TimeSpan.FromDays(completedSettlements.Average(s => (s.CompletedDate!.Value - s.DueDate).TotalDays))
            : TimeSpan.Zero;

        var failedPayments = settlements.SelectMany(s => s.Payments).Count(p => p.Status == Core.Entities.PaymentStatus.Failed);

        return new SettlementPerformanceMetrics
        {
            OnTimePaymentRate = onTimeRate,
            AveragePaymentDelay = avgDelay,
            ReconciliationRate = 95.0, // This would be calculated from actual reconciliation data
            FailedPayments = failedPayments,
            LostToLateFees = Money.Zero("USD"), // This would be calculated from actual late fee data
            SavedFromEarlyPaymentDiscounts = Money.Zero("USD") // This would be calculated from actual discount data
        };
    }

    private CashFlowMetrics CalculateCashFlowMetrics(List<CoreSettlement> settlements)
    {
        var inflows = settlements.Where(s => s.Type == Core.Entities.SettlementType.ContractPayment).Sum(s => s.Amount.Amount);
        var outflows = settlements.Where(s => s.Type == Core.Entities.SettlementType.Refund || s.Type == Core.Entities.SettlementType.Penalty).Sum(s => s.Amount.Amount);
        
        return new CashFlowMetrics
        {
            NetCashFlow = Money.Dollar(inflows - outflows),
            TotalInflows = Money.Dollar(inflows),
            TotalOutflows = Money.Dollar(outflows),
            PeriodBreakdown = new List<CashFlowPeriod>() // This would be calculated by period
        };
    }

    private decimal CalculateConfidenceLevel(DateTime forecastDate, DateTime currentDate)
    {
        var daysAhead = (forecastDate - currentDate).TotalDays;
        
        // Confidence decreases over time
        return daysAhead switch
        {
            <= 7 => 95m,
            <= 30 => 80m,
            <= 90 => 65m,
            _ => 50m
        };
    }

    private List<CashFlowRisk> IdentifyCashFlowRisks(List<CashFlowForecastPeriod> periods)
    {
        var risks = new List<CashFlowRisk>();

        // Check for negative cash flow periods
        var negativeFlowPeriods = periods.Where(p => p.PredictedNetFlow.Amount < 0).ToList();
        if (negativeFlowPeriods.Any())
        {
            var totalNegativeFlow = negativeFlowPeriods.Sum(p => Math.Abs(p.PredictedNetFlow.Amount));
            risks.Add(new CashFlowRisk
            {
                Description = $"Negative cash flow expected on {negativeFlowPeriods.Count} days",
                PotentialImpact = Money.Dollar(totalNegativeFlow),
                Probability = 0.7m,
                PotentialDate = negativeFlowPeriods.First().Date,
                Mitigation = "Consider accelerating receivables or delaying payables"
            });
        }

        return risks;
    }

    public async Task<DtoSettlement> GetSettlementByIdAsync(Guid settlementId)
    {
        _logger.LogInformation("Getting settlement by ID {SettlementId}", settlementId);
        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        return settlement != null ? MapToDto(settlement) : null!;
    }

    public async Task<List<DtoSettlement>> GetSettlementsForContractAsync(Guid contractId)
    {
        _logger.LogInformation("Getting settlements for contract {ContractId}", contractId);
        var settlementEntities = await _settlementRepository.GetByContractIdAsync(contractId);
        return settlementEntities.Select(MapToDto).ToList();
    }

    private static DtoSettlement MapToDto(CoreSettlement entity)
    {
        return new DtoSettlement
        {
            Id = entity.Id,
            ContractId = entity.ContractId,
            SettlementNumber = entity.SettlementNumber,
            Type = ConvertToDtoSettlementType(entity.Type),
            Amount = entity.Amount,
            DueDate = entity.DueDate,
            CreatedDate = entity.CreatedDate,
            Status = ConvertToDtoSettlementStatus(entity.Status),
            PayerPartyId = entity.PayerPartyId,
            PayeePartyId = entity.PayeePartyId,
            Description = entity.Description,
            ProcessedDate = entity.ProcessedDate,
            CompletedDate = entity.CompletedDate
        };
    }

    private static CoreBankAccount? ConvertBankAccount(DtoBankAccount? dtoAccount)
    {
        if (dtoAccount == null) return null;
        
        return new CoreBankAccount
        {
            AccountNumber = dtoAccount.AccountNumber,
            BankName = dtoAccount.BankName,
            SwiftCode = dtoAccount.SwiftCode,
            IBAN = dtoAccount.IBAN,
            AccountHolderName = dtoAccount.AccountHolderName,
            Currency = dtoAccount.Currency,
            RoutingNumber = dtoAccount.RoutingNumber,
            BranchCode = dtoAccount.BranchCode,
            AdditionalDetails = dtoAccount.AdditionalDetails
        };
    }


    private static DtoPayment ConvertPaymentToDto(CorePayment entityPayment)
    {
        return new DtoPayment
        {
            Id = entityPayment.Id,
            SettlementId = entityPayment.SettlementId,
            PaymentReference = entityPayment.PaymentReference,
            Method = (DTOs.PaymentMethod)entityPayment.Method,
            Amount = entityPayment.Amount,
            Status = ConvertToDtoPaymentStatus(entityPayment.Status),
            PaymentDate = entityPayment.PaymentDate,
            CreatedDate = entityPayment.CreatedDate,
            BankReference = entityPayment.BankReference,
            Instructions = entityPayment.Instructions,
            InitiatedDate = entityPayment.InitiatedDate,
            CompletedDate = entityPayment.CompletedDate,
            FailureReason = entityPayment.FailureReason
        };
    }

    private static DtoPaymentStatus ConvertToDtoPaymentStatus(CorePaymentStatus coreStatus)
    {
        return coreStatus switch
        {
            CorePaymentStatus.Pending => DtoPaymentStatus.Pending,
            CorePaymentStatus.Initiated => DtoPaymentStatus.Initiated,
            CorePaymentStatus.InTransit => DtoPaymentStatus.InProgress,
            CorePaymentStatus.Completed => DtoPaymentStatus.Completed,
            CorePaymentStatus.Failed => DtoPaymentStatus.Failed,
            CorePaymentStatus.Cancelled => DtoPaymentStatus.Cancelled,
            CorePaymentStatus.Returned => DtoPaymentStatus.Failed,
            _ => DtoPaymentStatus.Pending
        };
    }

    private static DtoSettlementStatus ConvertToDtoSettlementStatus(CoreSettlementStatus coreStatus)
    {
        return coreStatus switch
        {
            CoreSettlementStatus.Draft => DtoSettlementStatus.Pending,
            CoreSettlementStatus.Pending => DtoSettlementStatus.Pending,
            CoreSettlementStatus.PendingApproval => DtoSettlementStatus.Pending,
            CoreSettlementStatus.Approved => DtoSettlementStatus.Processing,
            CoreSettlementStatus.Processing => DtoSettlementStatus.Processing,
            CoreSettlementStatus.InProgress => DtoSettlementStatus.Processing,
            CoreSettlementStatus.Completed => DtoSettlementStatus.Completed,
            CoreSettlementStatus.Failed => DtoSettlementStatus.Failed,
            CoreSettlementStatus.Cancelled => DtoSettlementStatus.Cancelled,
            CoreSettlementStatus.OnHold => DtoSettlementStatus.Processing,
            _ => DtoSettlementStatus.Pending
        };
    }

    private static DtoSettlementType ConvertToDtoSettlementType(CoreSettlementType coreType)
    {
        return coreType switch
        {
            CoreSettlementType.ContractPayment => DtoSettlementType.ContractPayment,
            CoreSettlementType.PartialPayment => DtoSettlementType.PartialPayment,
            CoreSettlementType.FinalPayment => DtoSettlementType.FinalPayment,
            CoreSettlementType.Adjustment => DtoSettlementType.Adjustment,
            CoreSettlementType.Refund => DtoSettlementType.Refund,
            CoreSettlementType.Penalty => DtoSettlementType.Adjustment,
            CoreSettlementType.Interest => DtoSettlementType.Adjustment,
            CoreSettlementType.Advance => DtoSettlementType.Prepayment,
            CoreSettlementType.Commission => DtoSettlementType.Adjustment,
            _ => DtoSettlementType.ContractPayment
        };
    }
}


// ... (other supporting classes would be added here as needed)