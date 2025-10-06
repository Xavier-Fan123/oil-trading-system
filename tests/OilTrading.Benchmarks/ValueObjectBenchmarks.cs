using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, iterationCount: 10)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ValueObjectBenchmarks
{
    private readonly Faker _faker = new();
    private readonly List<Money> _moneyList = new();
    private readonly List<Quantity> _quantityList = new();
    private readonly List<string> _contractNumbers = new();

    [GlobalSetup]
    public void Setup()
    {
        // Pre-generate test data
        for (int i = 0; i < 1000; i++)
        {
            _moneyList.Add(new Money(_faker.Random.Decimal(1, 10000), _faker.PickRandom("USD", "EUR", "GBP")));
            _quantityList.Add(new Quantity(_faker.Random.Decimal(100, 50000), _faker.PickRandom<QuantityUnit>()));
            _contractNumbers.Add($"ITGR-{_faker.Date.Recent().Year}-{_faker.PickRandom("CARGO", "EXW", "DEL")}-B{_faker.Random.Int(1, 9999):D4}");
        }
    }

    [Benchmark(Description = "Money Creation and Operations")]
    public Money MoneyOperations()
    {
        var money1 = new Money(_faker.Random.Decimal(100, 1000), "USD");
        var money2 = new Money(_faker.Random.Decimal(50, 500), "USD");
        
        var sum = money1 + money2;
        var difference = money1 - money2;
        var product = money1 * 2.5m;
        var quotient = money1 / 2;
        
        return quotient; // Return to avoid compiler optimization
    }

    [Benchmark(Description = "Money Equality Comparisons")]
    public bool MoneyEqualityComparisons()
    {
        var money1 = _moneyList[_faker.Random.Int(0, 999)];
        var money2 = _moneyList[_faker.Random.Int(0, 999)];
        var money3 = new Money(money1.Amount, money1.Currency);
        
        var eq1 = money1 == money2;
        var eq2 = money1.Equals(money3);
        var hash = money1.GetHashCode();
        
        return eq1 && eq2 && hash != 0;
    }

    [Benchmark(Description = "Quantity Creation and Operations")]
    public Quantity QuantityOperations()
    {
        var quantity1 = new Quantity(_faker.Random.Decimal(1000, 5000), QuantityUnit.BBL);
        var quantity2 = new Quantity(_faker.Random.Decimal(500, 2000), QuantityUnit.BBL);
        
        var sum = quantity1 + quantity2;
        var difference = quantity1 - quantity2;
        var product = quantity1 * 1.5m;
        
        return product; // Return to avoid compiler optimization
    }

    [Benchmark(Description = "Quantity Unit Conversions")]
    public Quantity QuantityConversions()
    {
        var barrels = new Quantity(_faker.Random.Decimal(1000, 10000), QuantityUnit.BBL);
        var metricTons = barrels.ConvertTo(QuantityUnit.MT, 7.6m);
        var backToBarrels = metricTons.ConvertTo(QuantityUnit.BBL, 7.6m);
        
        return backToBarrels; // Return to avoid compiler optimization
    }

    [Benchmark(Description = "Contract Number Creation")]
    public ContractNumber ContractNumberCreation()
    {
        var contractNumber = ContractNumber.Create(
            _faker.Date.Recent().Year,
            _faker.PickRandom<ContractType>(),
            _faker.Random.Int(1, 9999));
        
        return contractNumber;
    }

    [Benchmark(Description = "Contract Number Parsing")]
    public ContractNumber ContractNumberParsing()
    {
        var contractNumberString = _contractNumbers[_faker.Random.Int(0, 999)];
        return ContractNumber.Parse(contractNumberString);
    }

    [Benchmark(Description = "Contract Number Validation")]
    public bool ContractNumberValidation()
    {
        var validNumber = _contractNumbers[_faker.Random.Int(0, 999)];
        var invalidNumber = "INVALID-FORMAT";
        
        var valid1 = ContractNumber.TryParse(validNumber, out var result1);
        var valid2 = ContractNumber.TryParse(invalidNumber, out var result2);
        
        return valid1 && !valid2;
    }

    [Benchmark(Description = "Bulk Money Calculations")]
    public decimal BulkMoneyCalculations()
    {
        decimal total = 0;
        
        for (int i = 0; i < 1000; i++)
        {
            var money = _moneyList[i];
            if (money.Currency == "USD")
            {
                total += money.Amount * 1.1m; // Apply 10% markup
            }
        }
        
        return total;
    }

    [Benchmark(Description = "Bulk Quantity Aggregations")]
    public decimal BulkQuantityAggregations()
    {
        decimal totalBarrels = 0;
        
        foreach (var quantity in _quantityList)
        {
            if (quantity.Unit == QuantityUnit.BBL)
            {
                totalBarrels += quantity.Value;
            }
            else if (quantity.Unit == QuantityUnit.MT)
            {
                // Convert MT to BBL
                var converted = quantity.ConvertTo(QuantityUnit.BBL, 7.6m);
                totalBarrels += converted.Value;
            }
        }
        
        return totalBarrels;
    }

    [Benchmark(Description = "Value Object ToString Performance")]
    public string ValueObjectToString()
    {
        var money = _moneyList[_faker.Random.Int(0, 999)];
        var quantity = _quantityList[_faker.Random.Int(0, 999)];
        var contractNumber = ContractNumber.Parse(_contractNumbers[_faker.Random.Int(0, 999)]);
        
        return $"{money} | {quantity} | {contractNumber}";
    }

    [Benchmark(Description = "Value Object Equality Chain")]
    public bool ValueObjectEqualityChain()
    {
        var money1 = new Money(1000m, "USD");
        var money2 = new Money(1000m, "USD");
        var money3 = new Money(1000m, "EUR");
        
        var quantity1 = new Quantity(5000m, QuantityUnit.BBL);
        var quantity2 = new Quantity(5000m, QuantityUnit.BBL);
        var quantity3 = new Quantity(5000m, QuantityUnit.MT);
        
        var contract1 = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var contract2 = ContractNumber.Create(2025, ContractType.CARGO, 1);
        var contract3 = ContractNumber.Create(2025, ContractType.EXW, 1);
        
        return (money1 == money2 && money1 != money3) &&
               (quantity1 == quantity2 && quantity1 != quantity3) &&
               (contract1 == contract2 && contract1 != contract3);
    }
}