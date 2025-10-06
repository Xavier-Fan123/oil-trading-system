using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class Payment : BaseEntity
{
    private Payment() { } // For EF Core

    public Payment(
        Guid settlementId,
        string paymentReference,
        PaymentMethod method,
        Money amount,
        BankAccount? payerAccount = null,
        BankAccount? payeeAccount = null,
        string? instructions = null)
    {
        if (string.IsNullOrWhiteSpace(paymentReference))
            throw new DomainException("Payment reference cannot be empty");

        if (amount.IsZero())
            throw new DomainException("Payment amount cannot be zero");

        SettlementId = settlementId;
        PaymentReference = paymentReference.Trim();
        Method = method;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Status = PaymentStatus.Pending;
        PaymentDate = DateTime.UtcNow;
        CreatedDate = DateTime.UtcNow;
        PayerAccount = payerAccount;
        PayeeAccount = payeeAccount;
        Instructions = instructions?.Trim();

        StatusHistory.Add(new PaymentStatusChange
        {
            FromStatus = PaymentStatus.Pending,
            ToStatus = PaymentStatus.Pending,
            ChangeDate = DateTime.UtcNow,
            Reason = "Payment created",
            ChangedBy = Guid.Empty // System
        });

        AddDomainEvent(new PaymentCreatedEvent(Id, SettlementId, Amount, Method));
    }

    public Guid SettlementId { get; private set; }
    public string PaymentReference { get; private set; } = string.Empty;
    public PaymentMethod Method { get; private set; }
    public Money Amount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public BankAccount? PayerAccount { get; private set; }
    public BankAccount? PayeeAccount { get; private set; }
    public string? BankReference { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime? InitiatedDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public string? FailureReason { get; private set; }

    // Navigation Properties
    public Settlement Settlement { get; private set; } = null!;
    public ICollection<PaymentStatusChange> StatusHistory { get; private set; } = new List<PaymentStatusChange>();

    // Business Methods
    public void Initiate(string bankReference, string initiatedBy)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot initiate payment in {Status} status");

        if (string.IsNullOrWhiteSpace(bankReference))
            throw new DomainException("Bank reference is required for payment initiation");

        var previousStatus = Status;
        Status = PaymentStatus.Initiated;
        BankReference = bankReference.Trim();
        InitiatedDate = DateTime.UtcNow;

        AddStatusChange(previousStatus, Status, "Payment initiated", Guid.Parse(initiatedBy));
        AddDomainEvent(new PaymentStatusChangedEvent(Id, previousStatus, Status, initiatedBy));
    }

    public void MarkInTransit(string updatedBy)
    {
        if (Status != PaymentStatus.Initiated)
            throw new DomainException($"Cannot mark payment as in-transit from {Status} status");

        var previousStatus = Status;
        Status = PaymentStatus.InTransit;

        AddStatusChange(previousStatus, Status, "Payment in transit", Guid.Parse(updatedBy));
        AddDomainEvent(new PaymentStatusChangedEvent(Id, previousStatus, Status, updatedBy));
    }

    public void Complete(string bankConfirmation, string completedBy)
    {
        if (Status != PaymentStatus.InTransit && Status != PaymentStatus.Initiated)
            throw new DomainException($"Cannot complete payment from {Status} status");

        var previousStatus = Status;
        Status = PaymentStatus.Completed;
        CompletedDate = DateTime.UtcNow;
        
        if (!string.IsNullOrWhiteSpace(bankConfirmation))
        {
            BankReference = bankConfirmation.Trim();
        }

        AddStatusChange(previousStatus, Status, "Payment completed", Guid.Parse(completedBy));
        AddDomainEvent(new PaymentCompletedEvent(Id, SettlementId, Amount, CompletedDate.Value));
    }

    public void Fail(string reason, string updatedBy)
    {
        if (Status == PaymentStatus.Completed)
            throw new DomainException("Cannot fail completed payment");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Failure reason is required");

        var previousStatus = Status;
        Status = PaymentStatus.Failed;
        FailureReason = reason.Trim();

        AddStatusChange(previousStatus, Status, reason, Guid.Parse(updatedBy));
        AddDomainEvent(new PaymentFailedEvent(Id, SettlementId, reason));
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == PaymentStatus.Completed)
            throw new DomainException("Cannot cancel completed payment");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");

        var previousStatus = Status;
        Status = PaymentStatus.Cancelled;
        FailureReason = reason.Trim();

        AddStatusChange(previousStatus, Status, reason, Guid.Parse(cancelledBy));
        AddDomainEvent(new PaymentCancelledEvent(Id, SettlementId, reason));
    }

    public void Return(string reason, string returnedBy)
    {
        if (Status != PaymentStatus.Completed && Status != PaymentStatus.InTransit)
            throw new DomainException($"Cannot return payment from {Status} status");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Return reason is required");

        var previousStatus = Status;
        Status = PaymentStatus.Returned;
        FailureReason = reason.Trim();

        AddStatusChange(previousStatus, Status, reason, Guid.Parse(returnedBy));
        AddDomainEvent(new PaymentReturnedEvent(Id, SettlementId, reason));
    }

    public void UpdateBankAccounts(BankAccount? payerAccount, BankAccount? payeeAccount, string updatedBy)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot update bank accounts for payment in {Status} status");

        PayerAccount = payerAccount;
        PayeeAccount = payeeAccount;
        SetUpdatedBy(updatedBy);
    }

    public void UpdateInstructions(string instructions, string updatedBy)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Cannot update instructions for payment in {Status} status");

        Instructions = instructions?.Trim();
        SetUpdatedBy(updatedBy);
    }

    public bool IsProcessable()
    {
        return Status == PaymentStatus.Pending && 
               (Method != PaymentMethod.TelegraphicTransfer || (PayerAccount != null && PayeeAccount != null));
    }

    public bool IsOverdue(int standardProcessingDays = 3)
    {
        if (Status == PaymentStatus.Completed || Status == PaymentStatus.Cancelled)
            return false;

        var expectedCompletionDate = CreatedDate.AddDays(standardProcessingDays);
        return DateTime.UtcNow > expectedCompletionDate;
    }

    public TimeSpan GetProcessingTime()
    {
        if (CompletedDate.HasValue)
            return CompletedDate.Value - CreatedDate;
        
        return DateTime.UtcNow - CreatedDate;
    }

    private void AddStatusChange(PaymentStatus fromStatus, PaymentStatus toStatus, string reason, Guid changedBy)
    {
        StatusHistory.Add(new PaymentStatusChange
        {
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangeDate = DateTime.UtcNow,
            Reason = reason,
            ChangedBy = changedBy
        });
    }
}

public class PaymentStatusChange
{
    public PaymentStatus FromStatus { get; set; }
    public PaymentStatus ToStatus { get; set; }
    public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public Guid ChangedBy { get; set; }
}

public class BankAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string? RoutingNumber { get; set; }
    public string? BranchCode { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; } = new();

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AccountNumber) &&
               !string.IsNullOrWhiteSpace(BankName) &&
               !string.IsNullOrWhiteSpace(AccountHolderName);
    }

    public string GetDisplayName()
    {
        return $"{BankName} - {AccountNumber[^4..]}";
    }
}

public enum PaymentMethod
{
    TelegraphicTransfer = 1,
    SWIFT = 2,
    ACH = 3,
    Check = 4,
    LetterOfCredit = 5,
    DocumentaryCollection = 6,
    CreditCard = 7,
    Digital = 8,
    Wire = 9,
    LocalTransfer = 10
}

public enum PaymentStatus
{
    Pending = 1,
    Initiated = 2,
    InTransit = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
    Returned = 7
}