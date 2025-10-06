namespace OilTrading.Core.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    public ICollection<PurchaseContract> PurchaseContracts { get; set; } = [];
    public ICollection<SalesContract> SalesContracts { get; set; } = [];
}

public enum UserRole
{
    Trader = 1,
    RiskManager = 2,
    Administrator = 3,
    Viewer = 4
}