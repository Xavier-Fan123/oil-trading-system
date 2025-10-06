using Bogus;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.UnitTests.Builders;

public class PurchaseContractBuilder
{
    private readonly Faker _faker = new();
    private ContractNumber? _contractNumber;
    private ContractType _contractType = ContractType.CARGO;
    private Guid _tradingPartnerId = Guid.NewGuid();
    private Guid _productId = Guid.NewGuid();
    private Guid _traderId = Guid.NewGuid();
    private Quantity _quantity = new(10000, QuantityUnit.BBL);
    private decimal _tonBarrelRatio = 7.6m;

    public static PurchaseContractBuilder Create() => new();

    public PurchaseContractBuilder WithContractNumber(ContractNumber contractNumber)
    {
        _contractNumber = contractNumber;
        return this;
    }

    public PurchaseContractBuilder WithContractType(ContractType contractType)
    {
        _contractType = contractType;
        return this;
    }

    public PurchaseContractBuilder WithTradingPartner(Guid tradingPartnerId)
    {
        _tradingPartnerId = tradingPartnerId;
        return this;
    }

    public PurchaseContractBuilder WithProduct(Guid productId)
    {
        _productId = productId;
        return this;
    }

    public PurchaseContractBuilder WithTrader(Guid traderId)
    {
        _traderId = traderId;
        return this;
    }

    public PurchaseContractBuilder WithQuantity(decimal value, QuantityUnit unit)
    {
        _quantity = new Quantity(value, unit);
        return this;
    }

    public PurchaseContractBuilder WithTonBarrelRatio(decimal ratio)
    {
        _tonBarrelRatio = ratio;
        return this;
    }

    public PurchaseContractBuilder WithRandomData()
    {
        _contractNumber = ContractNumber.Create(_faker.Date.Recent().Year, _faker.PickRandom<ContractType>(), _faker.Random.Int(1, 9999));
        _contractType = _faker.PickRandom<ContractType>();
        _tradingPartnerId = Guid.NewGuid();
        _productId = Guid.NewGuid();
        _traderId = Guid.NewGuid();
        _quantity = new Quantity(_faker.Random.Decimal(1000, 50000), _faker.PickRandom<QuantityUnit>());
        _tonBarrelRatio = _faker.Random.Decimal(6, 9);
        
        return this;
    }

    public PurchaseContract Build()
    {
        var contractNumber = _contractNumber ?? ContractNumber.Create(DateTime.UtcNow.Year, _contractType, _faker.Random.Int(1, 9999));
        
        return new PurchaseContract(
            contractNumber,
            _contractType,
            _tradingPartnerId,
            _productId,
            _traderId,
            _quantity,
            _tonBarrelRatio);
    }

    public List<PurchaseContract> BuildList(int count)
    {
        var contracts = new List<PurchaseContract>();
        for (int i = 0; i < count; i++)
        {
            WithRandomData();
            contracts.Add(Build());
        }
        return contracts;
    }
}