using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

namespace OilTrading.Tests.TestBuilders;

/// <summary>
/// Fluent builder for creating PaperContract test instances
/// </summary>
public class PaperContractBuilder
{
    private string _contractNumber = "PC-TEST-001";
    private string _productType = "Brent";
    private string _contractMonth = "DEC25";
    private PositionType _positionType = PositionType.Long;
    private decimal _quantity = 10m;
    private decimal _lotSize = 1000m;
    private decimal _entryPrice = 75.50m;
    private DateTime _tradeDate = DateTime.UtcNow;
    private PaperContractStatus _status = PaperContractStatus.Open;

    // Hedge designation fields
    private Guid? _hedgedContractId;
    private HedgedContractType? _hedgedContractType;
    private decimal _hedgeRatio = 1.0m;
    private string _hedgeDesignationUpdatedBy = "TestUser";

    // Spread contract fields
    private bool _isSpread;
    private string? _leg1Product;
    private string? _leg2Product;
    private decimal? _spreadValue;

    // Trade Group
    private Guid? _tradeGroupId;

    public PaperContractBuilder()
    {
    }

    public PaperContractBuilder WithContractNumber(string contractNumber)
    {
        _contractNumber = contractNumber;
        return this;
    }

    public PaperContractBuilder WithProductType(string productType)
    {
        _productType = productType;
        return this;
    }

    public PaperContractBuilder WithContractMonth(string contractMonth)
    {
        _contractMonth = contractMonth;
        return this;
    }

    public PaperContractBuilder WithPosition(PositionType positionType)
    {
        _positionType = positionType;
        return this;
    }

    public PaperContractBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        return this;
    }

    public PaperContractBuilder WithLotSize(decimal lotSize)
    {
        _lotSize = lotSize;
        return this;
    }

    public PaperContractBuilder WithEntryPrice(decimal entryPrice)
    {
        _entryPrice = entryPrice;
        return this;
    }

    public PaperContractBuilder WithTradeDate(DateTime tradeDate)
    {
        _tradeDate = tradeDate;
        return this;
    }

    public PaperContractBuilder WithStatus(PaperContractStatus status)
    {
        _status = status;
        return this;
    }

    public PaperContractBuilder AsLong()
    {
        _positionType = PositionType.Long;
        return this;
    }

    public PaperContractBuilder AsShort()
    {
        _positionType = PositionType.Short;
        return this;
    }

    public PaperContractBuilder WithHedgeDesignation(
        Guid physicalContractId,
        HedgedContractType contractType,
        decimal hedgeRatio = 1.0m,
        string updatedBy = "TestUser")
    {
        _hedgedContractId = physicalContractId;
        _hedgedContractType = contractType;
        _hedgeRatio = hedgeRatio;
        _hedgeDesignationUpdatedBy = updatedBy;
        return this;
    }

    public PaperContractBuilder AsSpread(string leg1Product, string leg2Product, decimal spreadValue)
    {
        _isSpread = true;
        _leg1Product = leg1Product;
        _leg2Product = leg2Product;
        _spreadValue = spreadValue;
        return this;
    }

    public PaperContractBuilder WithTradeGroupId(Guid tradeGroupId)
    {
        _tradeGroupId = tradeGroupId;
        return this;
    }

    public PaperContract Build()
    {
        var contract = new PaperContract
        {
            ContractNumber = _contractNumber,
            ProductType = _productType,
            ContractMonth = _contractMonth,
            Position = _positionType,
            Quantity = _quantity,
            LotSize = _lotSize,
            EntryPrice = _entryPrice,
            TradeDate = _tradeDate,
            Status = _status,
            IsSpread = _isSpread,
            Leg1Product = _leg1Product,
            Leg2Product = _leg2Product,
            SpreadValue = _spreadValue
        };

        // Apply hedge designation if specified
        if (_hedgedContractId.HasValue && _hedgedContractType.HasValue)
        {
            contract.DesignateAsHedge(
                _hedgedContractId.Value,
                _hedgedContractType.Value,
                _hedgeRatio,
                _hedgeDesignationUpdatedBy);
        }

        // Apply trade group if specified
        if (_tradeGroupId.HasValue)
        {
            contract.AssignToTradeGroup(_tradeGroupId.Value, "TestUser");
        }

        return contract;
    }

    /// <summary>
    /// Create a basic open paper contract for testing
    /// </summary>
    public static PaperContract CreateBasicOpen() =>
        new PaperContractBuilder()
            .WithContractNumber("PC-TEST-BASIC")
            .WithProductType("Brent")
            .WithContractMonth("DEC25")
            .AsLong()
            .WithQuantity(10)
            .WithLotSize(1000)
            .WithEntryPrice(75.50m)
            .WithStatus(PaperContractStatus.Open)
            .Build();

    /// <summary>
    /// Create a closed paper contract for testing
    /// </summary>
    public static PaperContract CreateClosed()
    {
        var contract = new PaperContractBuilder()
            .WithContractNumber("PC-TEST-CLOSED")
            .WithProductType("WTI")
            .WithContractMonth("NOV25")
            .AsShort()
            .WithQuantity(5)
            .WithLotSize(1000)
            .WithEntryPrice(70.00m)
            .WithStatus(PaperContractStatus.Open)
            .Build();

        contract.ClosePosition(68.00m, DateTime.UtcNow);
        return contract;
    }

    /// <summary>
    /// Create a paper contract designated as a hedge
    /// </summary>
    public static PaperContract CreateWithHedge(Guid physicalContractId, HedgedContractType contractType) =>
        new PaperContractBuilder()
            .WithContractNumber("PC-TEST-HEDGE")
            .WithProductType("Brent")
            .WithContractMonth("DEC25")
            .AsShort()
            .WithQuantity(10)
            .WithLotSize(1000)
            .WithEntryPrice(76.00m)
            .WithHedgeDesignation(physicalContractId, contractType, 1.0m)
            .Build();
}
