using OilTrading.Core.Entities;

namespace OilTrading.Tests.Builders;

/// <summary>
/// Builder class for creating TradingPartner test data using the Builder pattern.
/// Provides fluent interface for constructing test objects with sensible defaults.
/// </summary>
public class TradingPartnerTestDataBuilder
{
    private string _companyName = "Test Trading Company";
    private string _companyCode = "TTC";
    private TradingPartnerType _type = TradingPartnerType.Supplier;
    private decimal _creditLimit = 1000000m;
    private decimal _currentExposure = 250000m;
    private bool _isActive = true;
    private string? _contactPerson = "John Smith";
    private string? _email = "john.smith@testtradingcompany.com";
    private string? _phone = "+1-555-0123";
    private string? _address = "123 Business Ave, Trade City, TC 12345";
    private string _createdBy = "test.user";
    private DateTime _createdAt = DateTime.UtcNow.AddDays(-30);
    private string? _updatedBy;
    private DateTime? _updatedAt;

    public TradingPartnerTestDataBuilder WithCompanyInfo(string name, string code, TradingPartnerType type = TradingPartnerType.Supplier)
    {
        _companyName = name;
        _companyCode = code;
        _type = type;
        return this;
    }

    public TradingPartnerTestDataBuilder WithCreditInfo(decimal creditLimit, decimal currentExposure = 0)
    {
        _creditLimit = creditLimit;
        _currentExposure = currentExposure;
        return this;
    }

    public TradingPartnerTestDataBuilder WithHighCreditUtilization()
    {
        _creditLimit = 1000000m;
        _currentExposure = 900000m; // 90% utilization
        return this;
    }

    public TradingPartnerTestDataBuilder WithLowCreditUtilization()
    {
        _creditLimit = 1000000m;
        _currentExposure = 100000m; // 10% utilization
        return this;
    }

    public TradingPartnerTestDataBuilder AsSupplier()
    {
        _type = TradingPartnerType.Supplier;
        _companyName = _companyName.Contains("Supplier") ? _companyName : $"{_companyName} (Supplier)";
        return this;
    }

    public TradingPartnerTestDataBuilder AsCustomer()
    {
        _type = TradingPartnerType.Customer;
        _companyName = _companyName.Contains("Customer") ? _companyName : $"{_companyName} (Customer)";
        return this;
    }

    public TradingPartnerTestDataBuilder AsBoth()
    {
        _type = TradingPartnerType.Both;
        _companyName = _companyName.Contains("Partner") ? _companyName : $"{_companyName} (Partner)";
        return this;
    }

    public TradingPartnerTestDataBuilder AsActive(bool isActive = true)
    {
        _isActive = isActive;
        return this;
    }

    public TradingPartnerTestDataBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public TradingPartnerTestDataBuilder WithContactInfo(
        string? contactPerson = null,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        if (contactPerson != null) _contactPerson = contactPerson;
        if (email != null) _email = email;
        if (phone != null) _phone = phone;
        if (address != null) _address = address;
        return this;
    }

    public TradingPartnerTestDataBuilder WithCreatedBy(string createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public TradingPartnerTestDataBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TradingPartnerTestDataBuilder WithUpdatedBy(string updatedBy)
    {
        _updatedBy = updatedBy;
        _updatedAt = DateTime.UtcNow;
        return this;
    }

    public TradingPartner Build()
    {
        var tradingPartner = new TradingPartner
        {
            CompanyName = _companyName,
            CompanyCode = _companyCode,
            Type = _type,
            CreditLimit = _creditLimit,
            CurrentExposure = _currentExposure,
            IsActive = _isActive,
            ContactPerson = _contactPerson,
            Address = _address ?? string.Empty
        };

        tradingPartner.SetCreated(_createdBy ?? "test");
        return tradingPartner;
    }

    public TradingPartner BuildWithId(Guid id)
    {
        var entity = Build();
        SetEntityId(entity, id);
        return entity;
    }

    private static void SetEntityId(BaseEntity entity, Guid id)
    {
        var idProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
        idProperty?.SetValue(entity, id);
    }

    /// <summary>
    /// Creates a new builder instance (factory method)
    /// </summary>
    public static TradingPartnerTestDataBuilder Create() => new();

    /// <summary>
    /// Creates a builder with supplier defaults
    /// </summary>
    public static TradingPartnerTestDataBuilder CreateSupplier(string? name = null)
    {
        var companyName = name ?? "Reliable Oil Supplier Ltd";
        var companyCode = name != null ? name.Replace(" ", "").ToUpper()[..3] : "ROS";
        
        return new TradingPartnerTestDataBuilder()
            .WithCompanyInfo(companyName, companyCode, TradingPartnerType.Supplier)
            .WithCreditInfo(2000000m, 400000m); // 20% utilization
    }

    /// <summary>
    /// Creates a builder with customer defaults
    /// </summary>
    public static TradingPartnerTestDataBuilder CreateCustomer(string? name = null)
    {
        var companyName = name ?? "Global Energy Customer Corp";
        var companyCode = name != null ? name.Replace(" ", "").ToUpper()[..3] : "GEC";
        
        return new TradingPartnerTestDataBuilder()
            .WithCompanyInfo(companyName, companyCode, TradingPartnerType.Customer)
            .WithCreditInfo(1500000m, 300000m); // 20% utilization
    }

    /// <summary>
    /// Creates multiple trading partners with different profiles
    /// </summary>
    public static List<TradingPartner> CreateMultiplePartners()
    {
        return new List<TradingPartner>
        {
            CreateSupplier("Saudi Aramco Trading").WithCreditInfo(5000000m, 1000000m).Build(),
            CreateSupplier("Shell Trading International").WithCreditInfo(4000000m, 800000m).Build(),
            CreateSupplier("BP Oil Trading").WithCreditInfo(3500000m, 1400000m).Build(),
            CreateCustomer("ExxonMobil Refining").WithCreditInfo(6000000m, 2000000m).Build(),
            CreateCustomer("TotalEnergies Marketing").WithCreditInfo(3000000m, 900000m).Build(),
            Create()
                .WithCompanyInfo("Chevron Trading", "CHV", TradingPartnerType.Both)
                .WithCreditInfo(8000000m, 3200000m)
                .Build()
        };
    }

    /// <summary>
    /// Creates trading partners with different risk profiles
    /// </summary>
    public static Dictionary<string, TradingPartner> CreateRiskProfiles()
    {
        return new Dictionary<string, TradingPartner>
        {
            ["LowRisk"] = CreateSupplier("Low Risk Oil Co")
                .WithLowCreditUtilization()
                .Build(),
                
            ["HighRisk"] = CreateSupplier("High Risk Trading Ltd")
                .WithHighCreditUtilization()
                .Build(),
                
            ["Inactive"] = CreateSupplier("Inactive Former Partner")
                .AsInactive()
                .WithCreditInfo(0, 0)
                .Build(),
                
            ["NewPartner"] = CreateCustomer("New Trading Partner")
                .WithCreatedAt(DateTime.UtcNow.AddDays(-7))
                .WithCreditInfo(500000m, 0)
                .Build()
        };
    }
}