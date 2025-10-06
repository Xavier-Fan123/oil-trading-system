using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Data.Configurations;

/// <summary>
/// Value converter for ProductType enum to handle string to enum conversion
/// </summary>
public class ProductTypeValueConverter : ValueConverter<ProductType, int>
{
    public ProductTypeValueConverter() : base(
        // Convert enum to int for database storage
        productType => (int)productType,
        // Convert int from database to enum
        value => (ProductType)value
    )
    {
    }
}

/// <summary>
/// Extension methods to handle string to ProductType enum conversion
/// </summary>
public static class ProductTypeExtensions
{
    /// <summary>
    /// Safely converts a string to ProductType enum
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <returns>ProductType enum value or throws ArgumentException if invalid</returns>
    public static ProductType ToProductType(this string value)
    {
        return value?.Replace(" ", "").Trim() switch
        {
            "CrudeOil" or "Crude Oil" => ProductType.CrudeOil,
            "RefinedProducts" or "Refined Products" => ProductType.RefinedProducts,
            "NaturalGas" or "Natural Gas" => ProductType.NaturalGas,
            "Petrochemicals" => ProductType.Petrochemicals,
            _ => throw new ArgumentException($"Invalid ProductType value: '{value}'. Valid values are: CrudeOil, RefinedProducts, NaturalGas, Petrochemicals")
        };
    }

    /// <summary>
    /// Safely tries to convert a string to ProductType enum
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <param name="result">The resulting ProductType if successful</param>
    /// <returns>True if conversion was successful, false otherwise</returns>
    public static bool TryToProductType(this string value, out ProductType result)
    {
        try
        {
            result = value.ToProductType();
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Gets the display name for a ProductType enum value
    /// </summary>
    /// <param name="productType">The ProductType enum value</param>
    /// <returns>User-friendly display name</returns>
    public static string GetDisplayName(this ProductType productType)
    {
        return productType switch
        {
            ProductType.CrudeOil => "Crude Oil",
            ProductType.RefinedProducts => "Refined Products",
            ProductType.NaturalGas => "Natural Gas",
            ProductType.Petrochemicals => "Petrochemicals",
            _ => productType.ToString()
        };
    }
}