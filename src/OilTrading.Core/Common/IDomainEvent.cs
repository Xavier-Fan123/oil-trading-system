namespace OilTrading.Core.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}