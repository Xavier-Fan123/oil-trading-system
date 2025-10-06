namespace OilTrading.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Density { get; set; }
    public string Origin { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    public ICollection<PurchaseContract> PurchaseContracts { get; set; } = [];
    public ICollection<SalesContract> SalesContracts { get; set; } = [];
}

public enum ProductType
{
    CrudeOil = 1,
    RefinedProducts = 2,
    NaturalGas = 3,
    Petrochemicals = 4
}