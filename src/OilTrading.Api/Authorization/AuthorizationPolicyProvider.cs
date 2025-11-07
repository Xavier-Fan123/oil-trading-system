using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OilTrading.Api.Authorization;

/// <summary>
/// Custom authorization policy provider that creates role-based policies dynamically.
/// Enables [Authorize(Policy = "AdminOrManager")] attribute style authorization
/// with fallback to role-based authorization.
///
/// Built-in Policies:
/// - AdminOnly: Requires SystemAdmin role
/// - ManagementTeam: SystemAdmin, TradingManager
/// - TradersAndAbove: SeniorTrader, TradingManager, SystemAdmin
/// - ReadOnlyAccess: ReadOnlyUser and all higher roles
/// </summary>
public class AuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly ILogger<AuthorizationPolicyProvider> _logger;

    public AuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        ILogger<AuthorizationPolicyProvider> logger)
        : base(options)
    {
        _logger = logger;
    }

    /// <summary>
    /// Provides custom policies on demand and fallback for unknown policies
    /// </summary>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // First try the base implementation (for standard policies defined in Program.cs)
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null)
            return policy;

        // Then provide dynamic role-based policies
        policy = CreateRolePolicy(policyName);
        if (policy != null)
        {
            _logger.LogDebug("Created dynamic policy: {PolicyName}", policyName);
            return policy;
        }

        _logger.LogWarning("Policy not found: {PolicyName}", policyName);
        return null;
    }

    /// <summary>
    /// Creates policies for common role combinations
    /// </summary>
    private AuthorizationPolicy? CreateRolePolicy(string policyName)
    {
        return policyName switch
        {
            // Admin-only access
            "AdminOnly" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin")
                .Build(),

            // Management team access
            "ManagementTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "TradingManager", "SettlementManager", "FinanceManager")
                .Build(),

            // Trading team access (traders and above)
            "TradersAndAbove" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "TradingManager", "SeniorTrader", "Trader")
                .Build(),

            // Operations team access
            "OperationsTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "OperationsManager", "OperationsClerk")
                .Build(),

            // Settlement team access
            "SettlementTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "SettlementManager", "SettlementClerk")
                .Build(),

            // Finance team access
            "FinanceTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "FinanceManager", "FinanceClerk")
                .Build(),

            // Risk management team access
            "RiskTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "RiskManager", "RiskAnalyst")
                .Build(),

            // Inventory management team access
            "InventoryTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "InventoryManager", "InventoryClerk")
                .Build(),

            // Compliance and audit team access
            "ComplianceTeam" => new AuthorizationPolicyBuilder()
                .RequireRole("SystemAdmin", "ComplianceOfficer", "Auditor")
                .Build(),

            // Read-only access (includes read-only users and all others who can read)
            "ReadOnlyAccess" => new AuthorizationPolicyBuilder()
                .RequireRole(
                    "SystemAdmin", "TradingManager", "SeniorTrader", "Trader",
                    "RiskManager", "RiskAnalyst", "OperationsManager", "OperationsClerk",
                    "SettlementManager", "SettlementClerk", "InventoryManager", "InventoryClerk",
                    "FinanceManager", "FinanceClerk", "ComplianceOfficer", "Auditor",
                    "ReadOnlyUser"
                )
                .Build(),

            // Anyone authenticated
            "AuthenticatedOnly" => new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build(),

            _ => null
        };
    }
}

/// <summary>
/// Predefined policy names for convenience
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// System administrators only - full system access
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Management team - can manage users, configurations, reports
    /// </summary>
    public const string ManagementTeam = "ManagementTeam";

    /// <summary>
    /// Traders and above - can create/modify contracts
    /// </summary>
    public const string TradersAndAbove = "TradersAndAbove";

    /// <summary>
    /// Operations team - can manage shipping and logistics
    /// </summary>
    public const string OperationsTeam = "OperationsTeam";

    /// <summary>
    /// Settlement team - can create and manage settlements
    /// </summary>
    public const string SettlementTeam = "SettlementTeam";

    /// <summary>
    /// Finance team - can view and manage financial data
    /// </summary>
    public const string FinanceTeam = "FinanceTeam";

    /// <summary>
    /// Risk management team - can view and manage risk
    /// </summary>
    public const string RiskTeam = "RiskTeam";

    /// <summary>
    /// Inventory team - can manage inventory
    /// </summary>
    public const string InventoryTeam = "InventoryTeam";

    /// <summary>
    /// Compliance and audit team - can view compliance data
    /// </summary>
    public const string ComplianceTeam = "ComplianceTeam";

    /// <summary>
    /// Read-only access to reports and dashboards
    /// </summary>
    public const string ReadOnlyAccess = "ReadOnlyAccess";

    /// <summary>
    /// Authenticated users only - minimum security level
    /// </summary>
    public const string AuthenticatedOnly = "AuthenticatedOnly";
}
