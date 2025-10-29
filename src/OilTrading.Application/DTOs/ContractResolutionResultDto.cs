namespace OilTrading.Application.DTOs;

/// <summary>
/// Result of attempting to resolve a contract by external contract number
/// </summary>
public class ContractResolutionResultDto
{
    /// <summary>
    /// Whether the contract was successfully resolved to a single contract
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The resolved contract ID (GUID) if successful
    /// </summary>
    public Guid? ContractId { get; set; }

    /// <summary>
    /// The type of contract (Purchase or Sales)
    /// </summary>
    public string? ContractType { get; set; }

    /// <summary>
    /// Error message if resolution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Candidate contracts if multiple matches found (for user disambiguation)
    /// </summary>
    public List<ContractCandidateDto> Candidates { get; set; } = new();
}

/// <summary>
/// A candidate contract matching the external contract number search
/// </summary>
public class ContractCandidateDto
{
    /// <summary>
    /// Contract ID (GUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Internal contract number (e.g., PC-2025-001)
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// External contract number provided by trading partner
    /// </summary>
    public string ExternalContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Contract type: "Purchase" or "Sales"
    /// </summary>
    public string ContractType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the trading partner (supplier or customer)
    /// </summary>
    public string TradingPartnerName { get; set; } = string.Empty;

    /// <summary>
    /// Product name
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Contract quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Quantity unit (MT, BBL, GAL, etc.)
    /// </summary>
    public string QuantityUnit { get; set; } = string.Empty;

    /// <summary>
    /// Current contract status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Contract creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
