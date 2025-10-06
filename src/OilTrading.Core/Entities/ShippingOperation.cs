using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class ShippingOperation : BaseEntity
{
    private ShippingOperation() { } // For EF Core

    public ShippingOperation(
        string shippingNumber,
        Guid contractId,
        string vesselName,
        Quantity plannedQuantity,
        DateTime loadPortETA,
        DateTime dischargePortETA,
        string? loadPort = null,
        string? dischargePort = null)
    {
        if (string.IsNullOrWhiteSpace(shippingNumber))
            throw new DomainException("Shipping number cannot be empty");
        
        if (string.IsNullOrWhiteSpace(vesselName))
            throw new DomainException("Vessel name cannot be empty");

        if (loadPortETA >= dischargePortETA)
            throw new DomainException("Load port ETA must be before discharge port ETA");

        ShippingNumber = shippingNumber.Trim().ToUpper();
        ContractId = contractId;
        VesselName = vesselName.Trim();
        PlannedQuantity = plannedQuantity ?? throw new ArgumentNullException(nameof(plannedQuantity));
        LoadPortETA = loadPortETA;
        DischargePortETA = dischargePortETA;
        LoadPort = loadPort?.Trim();
        DischargePort = dischargePort?.Trim();
        Status = ShippingStatus.Planned;
        
        AddDomainEvent(new ShippingOperationCreatedEvent(Id, ShippingNumber, ContractId));
    }

    public string ShippingNumber { get; private set; } = string.Empty;
    public Guid ContractId { get; private set; }
    public string VesselName { get; private set; } = string.Empty;
    public Quantity PlannedQuantity { get; private set; } = null!;
    public Quantity? ActualQuantity { get; private set; }
    public DateTime LoadPortETA { get; private set; }
    public DateTime DischargePortETA { get; private set; }
    public string? LoadPort { get; private set; }
    public string? DischargePort { get; private set; }
    public ShippingStatus Status { get; private set; }
    
    // Actual dates and documents
    public DateTime? LoadPortATA { get; private set; } // Actual Time of Arrival
    public DateTime? DischargePortATA { get; private set; }
    public DateTime? BillOfLadingDate { get; private set; }
    public DateTime? NoticeOfReadinessDate { get; private set; }
    public DateTime? CertificateOfDischargeDate { get; private set; }
    
    // Additional shipping details
    public string? ChartererName { get; private set; }
    public string? IMONumber { get; private set; }
    public decimal? VesselCapacity { get; private set; }
    public string? ShippingAgent { get; private set; }
    public string? Notes { get; private set; }

    // Navigation Properties
    public PurchaseContract? PurchaseContract { get; private set; }
    public SalesContract? SalesContract { get; private set; }
    public ICollection<PricingEvent> PricingEvents { get; private set; } = new List<PricingEvent>();

    // Business Methods
    public void UpdateVesselDetails(string vesselName, string? imoNumber = null, decimal? vesselCapacity = null, string updatedBy = "")
    {
        if (Status == ShippingStatus.Discharged)
            throw new DomainException("Cannot update vessel details for discharged shipment");

        if (string.IsNullOrWhiteSpace(vesselName))
            throw new DomainException("Vessel name cannot be empty");

        var oldVesselName = VesselName;
        VesselName = vesselName.Trim();
        IMONumber = imoNumber?.Trim();
        VesselCapacity = vesselCapacity;
        
        SetUpdatedBy(updatedBy);
        
        if (oldVesselName != VesselName)
        {
            AddDomainEvent(new ShippingVesselChangedEvent(Id, oldVesselName, VesselName));
        }
    }

    public void UpdateSchedule(DateTime loadPortETA, DateTime dischargePortETA, string updatedBy = "")
    {
        if (Status == ShippingStatus.Discharged)
            throw new DomainException("Cannot update schedule for discharged shipment");

        if (loadPortETA >= dischargePortETA)
            throw new DomainException("Load port ETA must be before discharge port ETA");

        LoadPortETA = loadPortETA;
        DischargePortETA = dischargePortETA;
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new ShippingScheduleUpdatedEvent(Id, loadPortETA, dischargePortETA));
    }

    public void StartLoading(DateTime loadPortATA, DateTime? noticeOfReadinessDate = null, string updatedBy = "")
    {
        if (Status != ShippingStatus.Planned)
            throw new DomainException($"Cannot start loading from {Status} status");

        LoadPortATA = loadPortATA;
        NoticeOfReadinessDate = noticeOfReadinessDate ?? loadPortATA;
        Status = ShippingStatus.Loading;
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new ShippingLoadingStartedEvent(Id, loadPortATA, NoticeOfReadinessDate.Value));
        
        // Create pricing event for NOR if applicable
        CreatePricingEventIfNeeded(PricingEventType.NOR, NoticeOfReadinessDate.Value);
    }

    public void CompletedLoading(DateTime billOfLadingDate, Quantity actualQuantity, string updatedBy = "")
    {
        if (Status != ShippingStatus.Loading)
            throw new DomainException($"Cannot complete loading from {Status} status");

        if (actualQuantity.IsZero())
            throw new DomainException("Actual quantity must be greater than zero");

        BillOfLadingDate = billOfLadingDate;
        ActualQuantity = actualQuantity;
        Status = ShippingStatus.InTransit;
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new ShippingLoadingCompletedEvent(Id, billOfLadingDate, actualQuantity));
        
        // Create pricing event for BL if applicable
        CreatePricingEventIfNeeded(PricingEventType.BL, billOfLadingDate);
    }

    public void CompleteDischarge(DateTime dischargePortATA, DateTime certificateOfDischargeDate, string updatedBy = "")
    {
        if (Status != ShippingStatus.InTransit)
            throw new DomainException($"Cannot complete discharge from {Status} status");

        DischargePortATA = dischargePortATA;
        CertificateOfDischargeDate = certificateOfDischargeDate;
        Status = ShippingStatus.Discharged;
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new ShippingDischargeCompletedEvent(Id, dischargePortATA, certificateOfDischargeDate));
        
        // Create pricing event for COD if applicable
        CreatePricingEventIfNeeded(PricingEventType.COD, certificateOfDischargeDate);
    }

    public void Cancel(string reason, string updatedBy = "")
    {
        if (Status == ShippingStatus.Discharged)
            throw new DomainException("Cannot cancel discharged shipment");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");

        Status = ShippingStatus.Cancelled;
        Notes = $"Cancelled: {reason}";
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new ShippingOperationCancelledEvent(Id, reason));
    }

    public void UpdateQuantity(Quantity newPlannedQuantity, string updatedBy = "")
    {
        if (Status != ShippingStatus.Planned)
            throw new DomainException($"Cannot update quantity for {Status} shipment");

        if (newPlannedQuantity.IsZero())
            throw new DomainException("Planned quantity must be greater than zero");

        var oldQuantity = PlannedQuantity;
        PlannedQuantity = newPlannedQuantity;
        SetUpdatedBy(updatedBy);
        
        AddDomainEvent(new ShippingQuantityUpdatedEvent(Id, oldQuantity, newPlannedQuantity));
    }

    public decimal GetVariancePercentage()
    {
        if (ActualQuantity == null || PlannedQuantity.IsZero())
            return 0;

        var plannedInActualUnit = PlannedQuantity.Unit == ActualQuantity.Unit 
            ? PlannedQuantity.Value 
            : PlannedQuantity.ConvertTo(ActualQuantity.Unit).Value;

        return ((ActualQuantity.Value - plannedInActualUnit) / plannedInActualUnit) * 100;
    }

    public bool IsOverTolerance(decimal tolerancePercentage = 5.0m)
    {
        return Math.Abs(GetVariancePercentage()) > tolerancePercentage;
    }

    private void CreatePricingEventIfNeeded(PricingEventType eventType, DateTime eventDate)
    {
        // Check if contract supports event-based pricing
        // This would be determined by the contract's pricing formula
        // For now, we just create the pricing event - the pricing logic can use it if needed
        
        if (PurchaseContract?.PriceFormula?.IsFloatingPrice() == true ||
            SalesContract?.PriceFormula?.IsFloatingPrice() == true)
        {
            var pricingEvent = new PricingEvent(
                ContractId,
                eventType,
                eventDate,
                beforeDays: 5, // Default pricing window
                afterDays: 0,
                hasIndexOnEventDay: true,
                notes: $"Auto-created from shipping operation {ShippingNumber}"
            );

            // This would typically be handled by a domain service
            AddDomainEvent(new PricingEventCreatedEvent(pricingEvent.Id, ContractId, eventType, eventDate));
        }
    }

    public TimeSpan GetTransitTime()
    {
        if (LoadPortATA.HasValue && DischargePortATA.HasValue)
            return DischargePortATA.Value - LoadPortATA.Value;
        
        return DischargePortETA - LoadPortETA; // Planned transit time
    }

    public bool IsDelayed()
    {
        return (LoadPortATA?.Date > LoadPortETA.Date) || 
               (DischargePortATA?.Date > DischargePortETA.Date);
    }

    public override string ToString()
    {
        return $"Shipping {ShippingNumber} - {VesselName} ({Status})";
    }
}