using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for calculating contract payment status based on settlement information.
///
/// Payment status is a derived value calculated from:
/// 1. Contract's EstimatedPaymentDate - user-entered estimated payment date
/// 2. Related settlements' ActualPayableDueDate - the actual due date filled when creating settlement
/// 3. Related settlements' ActualPaymentDate - the actual payment date filled after payment is made
///
/// Payment status is NOT persisted to database - calculated dynamically at query time.
/// </summary>
public class PaymentStatusCalculationService : IPaymentStatusCalculationService
{
    private readonly IContractSettlementRepository _settlementRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;

    public PaymentStatusCalculationService(
        IContractSettlementRepository settlementRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository)
    {
        _settlementRepository = settlementRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
    }

    /// <summary>
    /// Calculate payment status for a purchase contract.
    ///
    /// Logic:
    /// 1. Get all settlements for the contract
    /// 2. If no settlements exist, return null (not applicable yet)
    /// 3. Determine status based on payment and due date information:
    ///    - Priority 1: If any unpaid settlement is overdue, status = Overdue (most severe)
    ///    - Priority 2: If any unpaid settlement is due today, status = Due
    ///    - Priority 3: If ALL settlements are paid, status = Paid
    ///    - Priority 4: If SOME settlements are paid, status = PartiallyPaid
    ///    - Priority 5: If all unpaid settlements have future due dates, status = NotDue
    /// </summary>
    public async Task<ContractPaymentStatus?> CalculatePurchaseContractPaymentStatusAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        // Get all settlements for this contract
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);

        // If no settlements, payment status is not applicable
        if (!settlements.Any())
        {
            return null;
        }

        // Count paid and unpaid settlements
        var paidSettlements = settlements.Where(s => s.ActualPaymentDate.HasValue).ToList();
        var unpaidSettlements = settlements.Where(s => !s.ActualPaymentDate.HasValue).ToList();

        // Get current date for due date comparison
        var now = DateTime.UtcNow;
        var nowDate = now.Date; // Get just the date part (no time)

        // Priority 1: Check for overdue (most severe status)
        var overdueDueDate = unpaidSettlements
            .Where(s => s.ActualPayableDueDate.HasValue)
            .Any(s => nowDate > s.ActualPayableDueDate.Value.Date);

        if (overdueDueDate)
        {
            return ContractPaymentStatus.Overdue;
        }

        // Priority 2: Check if any settlement is due today
        var isDueToday = unpaidSettlements
            .Where(s => s.ActualPayableDueDate.HasValue)
            .Any(s => nowDate == s.ActualPayableDueDate.Value.Date);

        if (isDueToday)
        {
            return ContractPaymentStatus.Due;
        }

        // Priority 3: If all settlements are paid
        if (unpaidSettlements.Count == 0)
        {
            return ContractPaymentStatus.Paid;
        }

        // Priority 4: If some settlements are paid
        if (paidSettlements.Count > 0)
        {
            return ContractPaymentStatus.PartiallyPaid;
        }

        // Priority 5: All due dates are in the future
        return ContractPaymentStatus.NotDue;
    }

    /// <summary>
    /// Calculate payment status for a sales contract.
    ///
    /// Logic same as purchase contracts with priority-based status determination:
    /// 1. Get all settlements for the contract
    /// 2. If no settlements exist, return null (not applicable yet)
    /// 3. Determine status based on payment and due date information:
    ///    - Priority 1: If any uncollected settlement is overdue, status = Overdue (most severe)
    ///    - Priority 2: If any uncollected settlement is due today, status = Due
    ///    - Priority 3: If ALL settlements are collected, status = Paid
    ///    - Priority 4: If SOME settlements are collected, status = PartiallyPaid
    ///    - Priority 5: If all uncollected settlements have future due dates, status = NotDue
    /// </summary>
    public async Task<ContractPaymentStatus?> CalculateSalesContractPaymentStatusAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        // Get all settlements for this contract
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);

        // If no settlements, payment status is not applicable
        if (!settlements.Any())
        {
            return null;
        }

        // Count collected and uncollected settlements
        var collectedSettlements = settlements.Where(s => s.ActualPaymentDate.HasValue).ToList();
        var uncollectedSettlements = settlements.Where(s => !s.ActualPaymentDate.HasValue).ToList();

        // Get current date for due date comparison
        var now = DateTime.UtcNow;
        var nowDate = now.Date; // Get just the date part (no time)

        // Priority 1: Check for overdue (most severe status)
        var overdueDueDate = uncollectedSettlements
            .Where(s => s.ActualPayableDueDate.HasValue)
            .Any(s => nowDate > s.ActualPayableDueDate.Value.Date);

        if (overdueDueDate)
        {
            return ContractPaymentStatus.Overdue;
        }

        // Priority 2: Check if any settlement is due today
        var isDueToday = uncollectedSettlements
            .Where(s => s.ActualPayableDueDate.HasValue)
            .Any(s => nowDate == s.ActualPayableDueDate.Value.Date);

        if (isDueToday)
        {
            return ContractPaymentStatus.Due;
        }

        // Priority 3: If all settlements are collected
        if (uncollectedSettlements.Count == 0)
        {
            return ContractPaymentStatus.Paid;
        }

        // Priority 4: If some settlements are collected
        if (collectedSettlements.Count > 0)
        {
            return ContractPaymentStatus.PartiallyPaid;
        }

        // Priority 5: All due dates are in the future
        return ContractPaymentStatus.NotDue;
    }

    /// <summary>
    /// Get the earliest due date from all unpaid settlements for a contract.
    /// Returns null if no unpaid settlements exist or none have a due date.
    /// </summary>
    public async Task<DateTime?> GetEarliestUnpaidDueDateAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);
        var unpaidSettlements = settlements.Where(s => !s.ActualPaymentDate.HasValue).ToList();

        var dueDates = unpaidSettlements
            .Where(s => s.ActualPayableDueDate.HasValue)
            .Select(s => s.ActualPayableDueDate.Value)
            .ToList();

        return dueDates.Any() ? dueDates.Min() : null;
    }

    /// <summary>
    /// Get total amount still unpaid for a contract (sum of unpaid settlements).
    /// </summary>
    public async Task<decimal> GetUnpaidAmountAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);
        var unpaidAmount = settlements
            .Where(s => !s.ActualPaymentDate.HasValue)
            .Sum(s => s.TotalSettlementAmount);

        return unpaidAmount;
    }

    /// <summary>
    /// Get total paid amount for a contract (sum of paid settlements).
    /// </summary>
    public async Task<decimal> GetPaidAmountAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);
        var paidAmount = settlements
            .Where(s => s.ActualPaymentDate.HasValue)
            .Sum(s => s.TotalSettlementAmount);

        return paidAmount;
    }

    /// <summary>
    /// Get payment status details for reporting/display purposes.
    /// Includes status, amounts, dates, and settlement breakdown.
    /// </summary>
    public async Task<PaymentStatusDetailsDto> GetPaymentStatusDetailsAsync(
        Guid contractId,
        bool isPurchaseContract,
        CancellationToken cancellationToken = default)
    {
        var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);

        if (!settlements.Any())
        {
            return new PaymentStatusDetailsDto
            {
                PaymentStatus = null,
                TotalAmount = 0,
                PaidAmount = 0,
                UnpaidAmount = 0,
                EarliestUnpaidDueDate = null,
                SettlementCount = 0,
                PaidSettlementCount = 0,
                UnpaidSettlementCount = 0,
                Settlements = new List<SettlementPaymentStatusDto>()
            };
        }

        var paymentStatus = isPurchaseContract
            ? await CalculatePurchaseContractPaymentStatusAsync(contractId, cancellationToken)
            : await CalculateSalesContractPaymentStatusAsync(contractId, cancellationToken);

        var totalAmount = settlements.Sum(s => s.TotalSettlementAmount);
        var paidAmount = settlements.Where(s => s.ActualPaymentDate.HasValue).Sum(s => s.TotalSettlementAmount);
        var unpaidAmount = settlements.Where(s => !s.ActualPaymentDate.HasValue).Sum(s => s.TotalSettlementAmount);
        var earliestDueDate = await GetEarliestUnpaidDueDateAsync(contractId, cancellationToken);

        var settlementDetails = settlements.Select(s => new SettlementPaymentStatusDto
        {
            SettlementId = s.Id,
            SettlementNumber = s.Id.ToString().Substring(0, 8),
            ContractNumber = s.ContractNumber,
            Amount = s.TotalSettlementAmount,
            ActualPayableDueDate = s.ActualPayableDueDate,
            ActualPaymentDate = s.ActualPaymentDate,
            IsPaid = s.ActualPaymentDate.HasValue,
            DaysOverdue = s.ActualPayableDueDate.HasValue && !s.ActualPaymentDate.HasValue
                ? (int)(DateTime.UtcNow - s.ActualPayableDueDate.Value).TotalDays
                : null
        }).ToList();

        return new PaymentStatusDetailsDto
        {
            PaymentStatus = paymentStatus,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            UnpaidAmount = unpaidAmount,
            EarliestUnpaidDueDate = earliestDueDate,
            SettlementCount = settlements.Count,
            PaidSettlementCount = settlements.Count(s => s.ActualPaymentDate.HasValue),
            UnpaidSettlementCount = settlements.Count(s => !s.ActualPaymentDate.HasValue),
            Settlements = settlementDetails
        };
    }
}

/// <summary>
/// DTO for payment status details including breakdown by settlement.
/// </summary>
public class PaymentStatusDetailsDto
{
    public ContractPaymentStatus? PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public DateTime? EarliestUnpaidDueDate { get; set; }
    public int SettlementCount { get; set; }
    public int PaidSettlementCount { get; set; }
    public int UnpaidSettlementCount { get; set; }
    public List<SettlementPaymentStatusDto> Settlements { get; set; } = new();
}

/// <summary>
/// DTO for individual settlement payment status.
/// </summary>
public class SettlementPaymentStatusDto
{
    public Guid SettlementId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? ActualPayableDueDate { get; set; }
    public DateTime? ActualPaymentDate { get; set; }
    public bool IsPaid { get; set; }
    public int? DaysOverdue { get; set; }
}
