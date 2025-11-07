using Microsoft.AspNetCore.Authorization;

namespace OilTrading.Api.Authorization;

/// <summary>
/// Specialized authorization attributes for common access patterns
/// Usage: [RequireAdminRole], [RequireTraderRole], etc. on controller methods
/// </summary>

/// <summary>
/// Requires SystemAdmin role - full system access
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAdminRoleAttribute : AuthorizeAttribute
{
    public RequireAdminRoleAttribute() => Roles = "SystemAdmin";
}

/// <summary>
/// Requires management level - SystemAdmin, TradingManager, SettlementManager, FinanceManager
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireManagementRoleAttribute : AuthorizeAttribute
{
    public RequireManagementRoleAttribute()
        => Roles = "SystemAdmin,TradingManager,SettlementManager,FinanceManager";
}

/// <summary>
/// Requires trading permissions - SystemAdmin, TradingManager, SeniorTrader, Trader
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireTraderRoleAttribute : AuthorizeAttribute
{
    public RequireTraderRoleAttribute()
        => Roles = "SystemAdmin,TradingManager,SeniorTrader,Trader";
}

/// <summary>
/// Requires operations permissions - SystemAdmin, OperationsManager, OperationsClerk
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireOperationsRoleAttribute : AuthorizeAttribute
{
    public RequireOperationsRoleAttribute()
        => Roles = "SystemAdmin,OperationsManager,OperationsClerk";
}

/// <summary>
/// Requires settlement permissions - SystemAdmin, SettlementManager, SettlementClerk
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireSettlementRoleAttribute : AuthorizeAttribute
{
    public RequireSettlementRoleAttribute()
        => Roles = "SystemAdmin,SettlementManager,SettlementClerk";
}

/// <summary>
/// Requires risk management permissions - SystemAdmin, RiskManager, RiskAnalyst
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRiskRoleAttribute : AuthorizeAttribute
{
    public RequireRiskRoleAttribute()
        => Roles = "SystemAdmin,RiskManager,RiskAnalyst";
}

/// <summary>
/// Requires inventory permissions - SystemAdmin, InventoryManager, InventoryClerk
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireInventoryRoleAttribute : AuthorizeAttribute
{
    public RequireInventoryRoleAttribute()
        => Roles = "SystemAdmin,InventoryManager,InventoryClerk";
}

/// <summary>
/// Requires finance permissions - SystemAdmin, FinanceManager, FinanceClerk
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireFinanceRoleAttribute : AuthorizeAttribute
{
    public RequireFinanceRoleAttribute()
        => Roles = "SystemAdmin,FinanceManager,FinanceClerk";
}

/// <summary>
/// Requires compliance permissions - SystemAdmin, ComplianceOfficer, Auditor
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireComplianceRoleAttribute : AuthorizeAttribute
{
    public RequireComplianceRoleAttribute()
        => Roles = "SystemAdmin,ComplianceOfficer,Auditor";
}

/// <summary>
/// Requires any authenticated user - basic authentication level
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAuthenticationAttribute : AuthorizeAttribute
{
    public RequireAuthenticationAttribute()
    {
        // AuthorizeAttribute with no Roles specified requires authenticated user
    }
}

/// <summary>
/// Allows read-only access - all authenticated users except guest
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AllowReadOnlyAccessAttribute : AuthorizeAttribute
{
    public AllowReadOnlyAccessAttribute()
        => Roles = "SystemAdmin,TradingManager,SeniorTrader,Trader,RiskManager,RiskAnalyst," +
                   "OperationsManager,OperationsClerk,SettlementManager,SettlementClerk," +
                   "InventoryManager,InventoryClerk,FinanceManager,FinanceClerk," +
                   "ComplianceOfficer,Auditor,ReadOnlyUser";
}
