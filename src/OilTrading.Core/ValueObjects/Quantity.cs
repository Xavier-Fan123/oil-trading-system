using OilTrading.Core.Common;

namespace OilTrading.Core.ValueObjects;

public class Quantity : ValueObject
{
    public decimal Value { get; private set; }
    public QuantityUnit Unit { get; private set; }

    private Quantity() { } // For EF Core

    public Quantity(decimal value, QuantityUnit unit)
    {
        if (value < 0)
            throw new DomainException("Quantity value cannot be negative");

        Value = value;
        Unit = unit;
    }

    public static Quantity MetricTons(decimal value) => new(value, QuantityUnit.MT);
    public static Quantity Barrels(decimal value) => new(value, QuantityUnit.BBL);

    public Quantity ConvertTo(QuantityUnit targetUnit, decimal conversionRatio = 7.6m)
    {
        if (Unit == targetUnit)
            return this;

        return (Unit, targetUnit) switch
        {
            (QuantityUnit.MT, QuantityUnit.BBL) => new Quantity(Value * conversionRatio, targetUnit),
            (QuantityUnit.BBL, QuantityUnit.MT) => new Quantity(Value / conversionRatio, targetUnit),
            _ => throw new DomainException($"Cannot convert from {Unit} to {targetUnit}")
        };
    }

    public Quantity Add(Quantity other)
    {
        if (Unit != other.Unit)
            throw new DomainException($"Cannot add different quantity units: {Unit} and {other.Unit}");
        
        return new Quantity(Value + other.Value, Unit);
    }

    public Quantity Subtract(Quantity other)
    {
        if (Unit != other.Unit)
            throw new DomainException($"Cannot subtract different quantity units: {Unit} and {other.Unit}");
        
        return new Quantity(Value - other.Value, Unit);
    }

    public Quantity Multiply(decimal multiplier)
    {
        return new Quantity(Value * multiplier, Unit);
    }

    public bool IsZero() => Value == 0;
    public bool IsNegative() => Value < 0;

    public static Quantity Zero(QuantityUnit unit) => new(0, unit);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Unit;
    }

    public override string ToString()
    {
        return $"{Value:N2} {Unit}";
    }

    public static Quantity operator +(Quantity left, Quantity right) => left.Add(right);
    public static Quantity operator -(Quantity left, Quantity right) => left.Subtract(right);
    public static Quantity operator *(Quantity left, decimal right) => left.Multiply(right);
}

public enum QuantityUnit
{
    MT = 0,   // Metric Tons
    BBL = 1,  // Barrels
    GAL = 2,  // Gallons
    LOTS = 3  // Trading Lots (for futures)
}