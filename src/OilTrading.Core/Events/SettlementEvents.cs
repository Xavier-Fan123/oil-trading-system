using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

public class SettlementCreatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public string SettlementNumber { get; }
    public Guid ContractId { get; }
    public Money Amount { get; }
    public DateTime DueDate { get; }

    public SettlementCreatedEvent(
        Guid settlementId, 
        string settlementNumber,
        Guid contractId, 
        Money amount, 
        DateTime dueDate)
    {
        SettlementId = settlementId;
        SettlementNumber = settlementNumber;
        ContractId = contractId;
        Amount = amount;
        DueDate = dueDate;
    }
}

public class SettlementStatusChangedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public SettlementStatus PreviousStatus { get; }
    public SettlementStatus NewStatus { get; }
    public string ChangedBy { get; }

    public SettlementStatusChangedEvent(
        Guid settlementId, 
        SettlementStatus previousStatus, 
        SettlementStatus newStatus, 
        string changedBy)
    {
        SettlementId = settlementId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
    }
}

public class SettlementCompletedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public string SettlementNumber { get; }
    public decimal TotalPaid { get; }
    public DateTime CompletedDate { get; }

    public SettlementCompletedEvent(
        Guid settlementId, 
        string settlementNumber,
        decimal totalPaid, 
        DateTime completedDate)
    {
        SettlementId = settlementId;
        SettlementNumber = settlementNumber;
        TotalPaid = totalPaid;
        CompletedDate = completedDate;
    }
}

public class SettlementCancelledEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public string SettlementNumber { get; }
    public string CancellationReason { get; }
    public SettlementStatus PreviousStatus { get; }
    public string CancelledBy { get; }

    public SettlementCancelledEvent(
        Guid settlementId, 
        string settlementNumber,
        string cancellationReason, 
        SettlementStatus previousStatus,
        string cancelledBy)
    {
        SettlementId = settlementId;
        SettlementNumber = settlementNumber;
        CancellationReason = cancellationReason;
        PreviousStatus = previousStatus;
        CancelledBy = cancelledBy;
    }
}

public class SettlementAmountUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public Money PreviousAmount { get; }
    public Money NewAmount { get; }
    public string UpdateReason { get; }
    public string UpdatedBy { get; }

    public SettlementAmountUpdatedEvent(
        Guid settlementId, 
        Money previousAmount, 
        Money newAmount, 
        string updateReason, 
        string updatedBy)
    {
        SettlementId = settlementId;
        PreviousAmount = previousAmount;
        NewAmount = newAmount;
        UpdateReason = updateReason;
        UpdatedBy = updatedBy;
    }
}

public class SettlementDueDateUpdatedEvent : DomainEvent
{
    public Guid SettlementId { get; }
    public DateTime PreviousDueDate { get; }
    public DateTime NewDueDate { get; }
    public string UpdateReason { get; }
    public string UpdatedBy { get; }

    public SettlementDueDateUpdatedEvent(
        Guid settlementId, 
        DateTime previousDueDate, 
        DateTime newDueDate, 
        string updateReason, 
        string updatedBy)
    {
        SettlementId = settlementId;
        PreviousDueDate = previousDueDate;
        NewDueDate = newDueDate;
        UpdateReason = updateReason;
        UpdatedBy = updatedBy;
    }
}