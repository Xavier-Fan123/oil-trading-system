using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Events;

namespace OilTrading.Core.Entities;

public class InventoryReservation : BaseEntity
{
    private InventoryReservation() { } // For EF Core

    public InventoryReservation(
        Guid contractId,
        string contractType,
        string productCode,
        string locationCode,
        Quantity quantity,
        DateTime reservationDate,
        DateTime? expiryDate = null,
        string? notes = null,
        string reservedBy = "System")
    {
        if (string.IsNullOrWhiteSpace(contractType))
            throw new DomainException("Contract type cannot be empty");

        if (string.IsNullOrWhiteSpace(productCode))
            throw new DomainException("Product code cannot be empty");

        if (string.IsNullOrWhiteSpace(locationCode))
            throw new DomainException("Location code cannot be empty");

        if (quantity.IsZero())
            throw new DomainException("Reservation quantity cannot be zero");

        ContractId = contractId;
        ContractType = contractType.Trim();
        ProductCode = productCode.Trim();
        LocationCode = locationCode.Trim();
        Quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));
        ReservationDate = reservationDate;
        ExpiryDate = expiryDate;
        Status = InventoryReservationStatus.Active;
        Notes = notes?.Trim();
        ReservedBy = reservedBy;

        AddDomainEvent(new InventoryReservedEvent(Id, ContractId, ProductCode, LocationCode, Quantity, ReservationDate));
    }

    public Guid ContractId { get; private set; }
    public string ContractType { get; private set; } = string.Empty; // "Purchase" or "Sales"
    public string ProductCode { get; private set; } = string.Empty;
    public string LocationCode { get; private set; } = string.Empty;
    public Quantity Quantity { get; private set; } = null!;
    public Quantity ReleasedQuantity { get; private set; } = Quantity.Zero(QuantityUnit.MT);
    public DateTime ReservationDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public DateTime? ReleasedDate { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string ReservedBy { get; private set; } = string.Empty;
    public string? ReleasedBy { get; private set; }
    public string? ReleaseReason { get; private set; }

    // Navigation Properties
    public PurchaseContract? PurchaseContract { get; private set; }
    public SalesContract? SalesContract { get; private set; }

    // Business Methods
    public Quantity GetRemainingQuantity()
    {
        var remaining = Quantity.Value - ReleasedQuantity.Value;
        return new Quantity(Math.Max(0, remaining), Quantity.Unit);
    }

    public void PartialRelease(Quantity releaseQuantity, string reason, string releasedBy)
    {
        if (Status != InventoryReservationStatus.Active)
            throw new DomainException($"Cannot release from reservation in {Status} status");

        if (releaseQuantity.IsZero() || releaseQuantity.IsNegative())
            throw new DomainException("Release quantity must be positive");

        if (releaseQuantity.Value > GetRemainingQuantity().Value)
            throw new DomainException($"Release quantity {releaseQuantity.Value} exceeds remaining reserved quantity {GetRemainingQuantity().Value}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Release reason is required");

        ReleasedQuantity = new Quantity(ReleasedQuantity.Value + releaseQuantity.Value, Quantity.Unit);
        ReleaseReason = reason.Trim();
        SetUpdatedBy(releasedBy);

        // Check if fully released
        if (GetRemainingQuantity().IsZero())
        {
            Status = InventoryReservationStatus.FullyReleased;
            ReleasedDate = DateTime.UtcNow;
            ReleasedBy = releasedBy;
        }
        else
        {
            Status = InventoryReservationStatus.PartiallyReleased;
        }

        AddDomainEvent(new InventoryPartiallyReleasedEvent(Id, ContractId, releaseQuantity, GetRemainingQuantity(), reason));
    }

    public void FullRelease(string reason, string releasedBy)
    {
        if (Status != InventoryReservationStatus.Active && Status != InventoryReservationStatus.PartiallyReleased)
            throw new DomainException($"Cannot release reservation in {Status} status");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Release reason is required");

        var remainingQuantity = GetRemainingQuantity();
        ReleasedQuantity = Quantity;
        Status = InventoryReservationStatus.FullyReleased;
        ReleasedDate = DateTime.UtcNow;
        ReleasedBy = releasedBy;
        ReleaseReason = reason.Trim();
        SetUpdatedBy(releasedBy);

        AddDomainEvent(new InventoryFullyReleasedEvent(Id, ContractId, ProductCode, LocationCode, remainingQuantity, reason));
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == InventoryReservationStatus.FullyReleased)
            throw new DomainException("Cannot cancel fully released reservation");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required");

        var remainingQuantity = GetRemainingQuantity();
        Status = InventoryReservationStatus.Cancelled;
        ReleasedDate = DateTime.UtcNow;
        ReleasedBy = cancelledBy;
        ReleaseReason = reason.Trim();
        SetUpdatedBy(cancelledBy);

        AddDomainEvent(new InventoryReservationCancelledEvent(Id, ContractId, ProductCode, LocationCode, remainingQuantity, reason));
    }

    public void Extend(DateTime newExpiryDate, string reason, string updatedBy)
    {
        if (Status != InventoryReservationStatus.Active && Status != InventoryReservationStatus.PartiallyReleased)
            throw new DomainException($"Cannot extend reservation in {Status} status");

        if (newExpiryDate <= DateTime.UtcNow)
            throw new DomainException("New expiry date must be in the future");

        if (ExpiryDate.HasValue && newExpiryDate <= ExpiryDate.Value)
            throw new DomainException("New expiry date must be later than current expiry date");

        var oldExpiryDate = ExpiryDate;
        ExpiryDate = newExpiryDate;
        Notes = $"{Notes}; Extended: {reason}".Trim(';', ' ');
        SetUpdatedBy(updatedBy);

        AddDomainEvent(new InventoryReservationExtendedEvent(Id, ContractId, oldExpiryDate, newExpiryDate, reason));
    }

    public bool IsExpired()
    {
        return ExpiryDate.HasValue && DateTime.UtcNow > ExpiryDate.Value && 
               (Status == InventoryReservationStatus.Active || Status == InventoryReservationStatus.PartiallyReleased);
    }

    public bool IsActive()
    {
        return Status == InventoryReservationStatus.Active || Status == InventoryReservationStatus.PartiallyReleased;
    }

    public TimeSpan GetReservationDuration()
    {
        var endDate = ReleasedDate ?? DateTime.UtcNow;
        return endDate - ReservationDate;
    }

    public decimal GetUtilizationPercentage()
    {
        if (Quantity.IsZero()) return 0;
        return (ReleasedQuantity.Value / Quantity.Value) * 100;
    }
}

public enum InventoryReservationStatus
{
    Active = 1,
    PartiallyReleased = 2,
    FullyReleased = 3,
    Cancelled = 4,
    Expired = 5
}