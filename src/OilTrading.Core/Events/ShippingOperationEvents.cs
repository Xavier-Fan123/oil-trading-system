using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

public class ShippingOperationCreatedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public string ShippingNumber { get; }
    public Guid ContractId { get; }

    public ShippingOperationCreatedEvent(Guid shippingOperationId, string shippingNumber, Guid contractId)
    {
        ShippingOperationId = shippingOperationId;
        ShippingNumber = shippingNumber;
        ContractId = contractId;
    }
}

public class ShippingVesselChangedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public string OldVesselName { get; }
    public string NewVesselName { get; }

    public ShippingVesselChangedEvent(Guid shippingOperationId, string oldVesselName, string newVesselName)
    {
        ShippingOperationId = shippingOperationId;
        OldVesselName = oldVesselName;
        NewVesselName = newVesselName;
    }
}

public class ShippingScheduleUpdatedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public DateTime LoadPortETA { get; }
    public DateTime DischargePortETA { get; }

    public ShippingScheduleUpdatedEvent(Guid shippingOperationId, DateTime loadPortETA, DateTime dischargePortETA)
    {
        ShippingOperationId = shippingOperationId;
        LoadPortETA = loadPortETA;
        DischargePortETA = dischargePortETA;
    }
}

public class ShippingLoadingStartedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public DateTime LoadPortATA { get; }
    public DateTime NoticeOfReadinessDate { get; }

    public ShippingLoadingStartedEvent(Guid shippingOperationId, DateTime loadPortATA, DateTime noticeOfReadinessDate)
    {
        ShippingOperationId = shippingOperationId;
        LoadPortATA = loadPortATA;
        NoticeOfReadinessDate = noticeOfReadinessDate;
    }
}

public class ShippingLoadingCompletedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public DateTime BillOfLadingDate { get; }
    public Quantity ActualQuantity { get; }

    public ShippingLoadingCompletedEvent(Guid shippingOperationId, DateTime billOfLadingDate, Quantity actualQuantity)
    {
        ShippingOperationId = shippingOperationId;
        BillOfLadingDate = billOfLadingDate;
        ActualQuantity = actualQuantity;
    }
}

public class ShippingDischargeCompletedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public DateTime DischargePortATA { get; }
    public DateTime CertificateOfDischargeDate { get; }

    public ShippingDischargeCompletedEvent(Guid shippingOperationId, DateTime dischargePortATA, DateTime certificateOfDischargeDate)
    {
        ShippingOperationId = shippingOperationId;
        DischargePortATA = dischargePortATA;
        CertificateOfDischargeDate = certificateOfDischargeDate;
    }
}

public class ShippingOperationCancelledEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public string Reason { get; }

    public ShippingOperationCancelledEvent(Guid shippingOperationId, string reason)
    {
        ShippingOperationId = shippingOperationId;
        Reason = reason;
    }
}

public class ShippingQuantityUpdatedEvent : DomainEvent
{
    public Guid ShippingOperationId { get; }
    public Quantity OldQuantity { get; }
    public Quantity NewQuantity { get; }

    public ShippingQuantityUpdatedEvent(Guid shippingOperationId, Quantity oldQuantity, Quantity newQuantity)
    {
        ShippingOperationId = shippingOperationId;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
    }
}

public class PricingEventCreatedEvent : DomainEvent
{
    public Guid PricingEventId { get; }
    public Guid ContractId { get; }
    public PricingEventType EventType { get; }
    public DateTime EventDate { get; }

    public PricingEventCreatedEvent(Guid pricingEventId, Guid contractId, PricingEventType eventType, DateTime eventDate)
    {
        PricingEventId = pricingEventId;
        ContractId = contractId;
        EventType = eventType;
        EventDate = eventDate;
    }
}