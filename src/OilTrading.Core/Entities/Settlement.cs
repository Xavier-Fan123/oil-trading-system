using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class Settlement : BaseEntity
{
    private Settlement() { } // For EF Core

    public Settlement(
        Guid contractId,
        SettlementType type,
        Money amount,
        DateTime dueDate,
        Guid payerPartyId,
        Guid payeePartyId,
        SettlementTerms terms,
        string? description = null,
        string createdBy = "System")
    {
        if (amount.IsZero())
            throw new DomainException("Settlement amount cannot be zero");

        if (dueDate < DateTime.UtcNow.Date)
            throw new DomainException("Settlement due date cannot be in the past");

        ContractId = contractId;
        SettlementNumber = GenerateSettlementNumber();
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        DueDate = dueDate;
        CreatedDate = DateTime.UtcNow;
        Status = SettlementStatus.Draft;
        PayerPartyId = payerPartyId;
        PayeePartyId = payeePartyId;
        Terms = terms ?? new SettlementTerms();
        Description = description?.Trim();
        CreatedBy = createdBy;

        AddDomainEvent(new SettlementCreatedEvent(Id, SettlementNumber, ContractId, Amount, DueDate));
    }

    public Guid ContractId { get; private set; }
    public string SettlementNumber { get; private set; } = string.Empty;
    public SettlementType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime DueDate { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public SettlementStatus Status { get; private set; }
    public Guid PayerPartyId { get; private set; }
    public Guid PayeePartyId { get; private set; }
    public SettlementTerms Terms { get; private set; } = new();
    public string? Description { get; private set; }
    public new string CreatedBy { get; private set; } = string.Empty;
    public DateTime? ProcessedDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public string? ProcessedBy { get; private set; }
    public string? CompletedBy { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation Properties
    public PurchaseContract? PurchaseContract { get; private set; }
    public SalesContract? SalesContract { get; private set; }
    public TradingPartner PayerParty { get; private set; } = null!;
    public TradingPartner PayeeParty { get; private set; } = null!;
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
    public ICollection<SettlementAdjustment> Adjustments { get; private set; } = new List<SettlementAdjustment>();

    // Business Methods
    public void Approve(string approvedBy)
    {
        if (Status != SettlementStatus.Pending)
            throw new DomainException($"Cannot approve settlement in {Status} status");

        Status = SettlementStatus.Approved;
        SetUpdatedBy(approvedBy);
        
        AddDomainEvent(new SettlementStatusChangedEvent(Id, SettlementStatus.Pending, SettlementStatus.Approved, approvedBy));
    }

    public void Process(string processedBy)
    {
        if (Status != SettlementStatus.Approved)
            throw new DomainException($"Cannot process settlement in {Status} status. Must be Approved first.");

        Status = SettlementStatus.Processing;
        ProcessedDate = DateTime.UtcNow;
        ProcessedBy = processedBy;
        SetUpdatedBy(processedBy);
        
        AddDomainEvent(new SettlementStatusChangedEvent(Id, SettlementStatus.Approved, SettlementStatus.Processing, processedBy));
    }

    public void Complete(string completedBy)
    {
        if (Status != SettlementStatus.Processing)
            throw new DomainException($"Cannot complete settlement in {Status} status. Must be Processing first.");

        // Verify that payments cover the settlement amount
        var totalPaid = Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount.Amount);
        var netAmount = GetNetAmount();

        if (totalPaid < netAmount)
            throw new DomainException($"Cannot complete settlement. Total payments ({totalPaid:C}) is less than net amount ({netAmount:C})");

        Status = SettlementStatus.Completed;
        CompletedDate = DateTime.UtcNow;
        CompletedBy = completedBy;
        SetUpdatedBy(completedBy);
        
        AddDomainEvent(new SettlementCompletedEvent(Id, SettlementNumber, totalPaid, CompletedDate.Value));
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == SettlementStatus.Completed)
            throw new DomainException("Cannot cancel completed settlement");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");

        var previousStatus = Status;
        Status = SettlementStatus.Cancelled;
        CancellationReason = reason.Trim();
        SetUpdatedBy(cancelledBy);
        
        // Cancel any pending payments
        foreach (var payment in Payments.Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Initiated))
        {
            payment.Cancel(reason, cancelledBy);
        }
        
        AddDomainEvent(new SettlementCancelledEvent(Id, SettlementNumber, reason, previousStatus, cancelledBy));
    }

    public void PutOnHold(string reason, string updatedBy)
    {
        if (Status == SettlementStatus.Completed || Status == SettlementStatus.Cancelled)
            throw new DomainException($"Cannot put {Status} settlement on hold");

        var previousStatus = Status;
        Status = SettlementStatus.OnHold;
        Description = $"On Hold: {reason}";
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new SettlementStatusChangedEvent(Id, previousStatus, SettlementStatus.OnHold, updatedBy));
    }

    public void Resume(string updatedBy)
    {
        if (Status != SettlementStatus.OnHold)
            throw new DomainException($"Cannot resume settlement that is not on hold. Current status: {Status}");

        Status = SettlementStatus.Approved; // Resume to approved status
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new SettlementStatusChangedEvent(Id, SettlementStatus.OnHold, SettlementStatus.Approved, updatedBy));
    }

    public void UpdateAmount(Money newAmount, string reason, string updatedBy)
    {
        if (Status == SettlementStatus.Completed || Status == SettlementStatus.Cancelled)
            throw new DomainException($"Cannot update amount for {Status} settlement");

        if (newAmount.IsZero())
            throw new DomainException("Settlement amount cannot be zero");

        var oldAmount = Amount;
        Amount = newAmount;
        SetUpdatedBy(updatedBy);

        // Create adjustment record
        var adjustmentType = newAmount.Amount > oldAmount.Amount 
            ? SettlementAdjustmentType.AmountIncrease 
            : SettlementAdjustmentType.AmountDecrease;

        var adjustment = new SettlementAdjustment(
            adjustmentType,
            Money.Dollar(Math.Abs(newAmount.Amount - oldAmount.Amount)),
            reason,
            Guid.Parse(updatedBy)); // Assuming updatedBy is a user ID

        Adjustments.Add(adjustment);
        
        AddDomainEvent(new SettlementAmountUpdatedEvent(Id, oldAmount, newAmount, reason, updatedBy));
    }

    public void UpdateDueDate(DateTime newDueDate, string reason, string updatedBy)
    {
        if (Status == SettlementStatus.Completed || Status == SettlementStatus.Cancelled)
            throw new DomainException($"Cannot update due date for {Status} settlement");

        if (newDueDate < DateTime.UtcNow.Date)
            throw new DomainException("New due date cannot be in the past");

        var oldDueDate = DueDate;
        DueDate = newDueDate;
        SetUpdatedBy(updatedBy);

        var adjustment = new SettlementAdjustment(
            SettlementAdjustmentType.DueDateChange,
            Money.Zero("USD"),
            reason,
            Guid.Parse(updatedBy));

        Adjustments.Add(adjustment);
        
        AddDomainEvent(new SettlementDueDateUpdatedEvent(Id, oldDueDate, newDueDate, reason, updatedBy));
    }

    public Payment CreatePayment(PaymentMethod method, Money amount, BankAccount? payerAccount = null, BankAccount? payeeAccount = null, string? instructions = null)
    {
        if (Status != SettlementStatus.Approved && Status != SettlementStatus.Processing)
            throw new DomainException($"Cannot create payment for settlement in {Status} status");

        var remainingAmount = GetRemainingAmount();
        if (amount.Amount > remainingAmount)
            throw new DomainException($"Payment amount {amount.Amount:C} exceeds remaining settlement amount {remainingAmount:C}");

        var payment = new Payment(
            Id,
            GeneratePaymentReference(),
            method,
            amount,
            payerAccount,
            payeeAccount,
            instructions);

        Payments.Add(payment);
        
        AddDomainEvent(new PaymentCreatedEvent(payment.Id, Id, amount, method));
        
        return payment;
    }

    public decimal GetNetAmount()
    {
        var baseAmount = Amount.Amount;
        var totalAdjustments = Adjustments.Sum(a => a.GetAdjustmentValue());
        return baseAmount + totalAdjustments;
    }

    public decimal GetRemainingAmount()
    {
        var netAmount = GetNetAmount();
        var totalPaid = Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount.Amount);
        return Math.Max(0, netAmount - totalPaid);
    }

    public bool IsOverdue()
    {
        return Status != SettlementStatus.Completed && Status != SettlementStatus.Cancelled && DateTime.UtcNow.Date > DueDate.Date;
    }

    public TimeSpan GetDaysOverdue()
    {
        if (!IsOverdue()) return TimeSpan.Zero;
        return DateTime.UtcNow.Date - DueDate.Date;
    }

    public bool HasPendingPayments()
    {
        return Payments.Any(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Initiated || p.Status == PaymentStatus.InTransit);
    }

    public decimal GetCompletionPercentage()
    {
        var netAmount = GetNetAmount();
        if (netAmount == 0) return 100;

        var totalPaid = Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount.Amount);
        return Math.Min(100, (totalPaid / netAmount) * 100);
    }

    private static string GenerateSettlementNumber()
    {
        var today = DateTime.UtcNow;
        var sequence = Random.Shared.Next(1000, 9999);
        return $"STL-{today:yyyyMMdd}-{sequence}";
    }

    private static string GeneratePaymentReference()
    {
        var today = DateTime.UtcNow;
        var sequence = Random.Shared.Next(100000, 999999);
        return $"PAY-{today:yyyyMMddHHmmss}-{sequence}";
    }
}

// Value object for settlement terms
public class SettlementTerms
{
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public SettlementMethod Method { get; set; } = SettlementMethod.TelegraphicTransfer;
    public string Currency { get; set; } = "USD";
    public decimal? DiscountRate { get; set; }
    public int? EarlyPaymentDays { get; set; }
    public decimal? LateFeeRate { get; set; }
    public bool EnableAutomaticProcessing { get; set; } = false;
    public Dictionary<string, object> CustomTerms { get; set; } = new();
}

// Enums
public enum SettlementType
{
    ContractPayment = 1,
    PartialPayment = 2,
    FinalPayment = 3,
    Adjustment = 4,
    Refund = 5,
    Penalty = 6,
    Interest = 7,
    Advance = 8,
    Commission = 9
}

public enum SettlementStatus
{
    Draft = 1,
    Pending = 2,
    PendingApproval = 3,
    Approved = 4,
    Processing = 5,
    InProgress = 6,
    Completed = 7,
    Failed = 8,
    Cancelled = 9,
    OnHold = 10
}

public enum PaymentTerms
{
    Prepaid = 1,
    COD = 2,
    Net15 = 3,
    Net30 = 4,
    Net45 = 5,
    Net60 = 6,
    Net90 = 7,
    Custom = 8
}

public enum SettlementMethod
{
    TelegraphicTransfer = 1,
    LetterOfCredit = 2,
    DocumentaryCollection = 3,
    Cash = 4,
    Netting = 5,
    SWIFT = 6,
    ACH = 7,
    Check = 8
}