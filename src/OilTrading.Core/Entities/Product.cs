namespace OilTrading.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public ProductType Type { get; set; } = ProductType.CrudeOil;
    public string Description { get; set; } = string.Empty;
    public ProductType ProductType { get; set; } = ProductType.CrudeOil;
    public string Grade { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Density { get; set; }
    public string Origin { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    public ICollection<PurchaseContract> PurchaseContracts { get; set; } = [];
    public ICollection<SalesContract> SalesContracts { get; set; } = [];

    // NOTE: MarketPrice.MarketPrices navigation removed
    // MarketPrice uses ProductCode (string) as natural key, not ProductId foreign key
    // To query market prices for a product, use: marketDataRepository.GetByProductAsync(product.ProductCode, ...)
    // ❌ public ICollection<MarketPrice> MarketPrices { get; set; } = [];
}

public enum ProductType
{
    CrudeOil = 1,
    RefinedProducts = 2,
    NaturalGas = 3,
    Petrochemicals = 4
}