using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

public class PaymentCreatedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid SettlementId { get; }
    public Money Amount { get; }
    public PaymentMethod Method { get; }

    public PaymentCreatedEvent(Guid paymentId, Guid settlementId, Money amount, PaymentMethod method)
    {
        PaymentId = paymentId;
        SettlementId = settlementId;
        Amount = amount;
        Method = method;
    }
}

public class PaymentStatusChangedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public PaymentStatus PreviousStatus { get; }
    public PaymentStatus NewStatus { get; }
    public string ChangedBy { get; }

    public PaymentStatusChangedEvent(
        Guid paymentId, 
        PaymentStatus previousStatus, 
        PaymentStatus newStatus, 
        string changedBy)
    {
        PaymentId = paymentId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}

public class PaymentCompletedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid SettlementId { get; }
    public Money Amount { get; }
    public DateTime CompletedDate { get; }

    public PaymentCompletedEvent(
        Guid paymentId, 
        Guid settlementId, 
        Money amount, 
        DateTime completedDate)
    {
        PaymentId = paymentId;
        SettlementId = settlementId;
        Amount = amount;
        CompletedDate = completedDate;
    }
}

public class PaymentFailedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid SettlementId { get; }
    public string FailureReason { get; }

    public PaymentFailedEvent(
        Guid paymentId, 
        Guid settlementId, 
        string failureReason)
    {
        PaymentId = paymentId;
        SettlementId = settlementId;
        FailureReason = failureReason;
    }
}

public class PaymentCancelledEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid SettlementId { get; }
    public string CancellationReason { get; }

    public PaymentCancelledEvent(
        Guid paymentId, 
        Guid settlementId, 
        string cancellationReason)
    {
        PaymentId = paymentId;
        SettlementId = settlementId;
        CancellationReason = cancellationReason;
    }
}

public class PaymentReturnedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid SettlementId { get; }
    public string ReturnReason { get; }

    public PaymentReturnedEvent(
        Guid paymentId, 
        Guid settlementId, 
        string returnReason)
    {
        PaymentId = paymentId;
        SettlementId = settlementId;
        ReturnReason = returnReason;
    }
}