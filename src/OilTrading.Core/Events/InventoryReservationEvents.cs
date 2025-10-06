using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Events;

// Inventory Reservation Events
public record InventoryReservedEvent(
    Guid ReservationId,
    Guid ContractId,
    string ProductCode,
    string LocationCode,
    Quantity Quantity,
    DateTime ReservationDate) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record InventoryPartiallyReleasedEvent(
    Guid ReservationId,
    Guid ContractId,
    Quantity ReleasedQuantity,
    Quantity RemainingQuantity,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record InventoryFullyReleasedEvent(
    Guid ReservationId,
    Guid ContractId,
    string ProductCode,
    string LocationCode,
    Quantity ReleasedQuantity,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record InventoryReservationCancelledEvent(
    Guid ReservationId,
    Guid ContractId,
    string ProductCode,
    string LocationCode,
    Quantity CancelledQuantity,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record InventoryReservationExtendedEvent(
    Guid ReservationId,
    Guid ContractId,
    DateTime? OldExpiryDate,
    DateTime NewExpiryDate,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record InventoryReservationExpiredEvent(
    Guid ReservationId,
    Guid ContractId,
    string ProductCode,
    string LocationCode,
    Quantity ExpiredQuantity) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}