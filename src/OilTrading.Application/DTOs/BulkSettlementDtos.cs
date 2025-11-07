namespace OilTrading.Application.DTOs;

/// <summary>
/// Request DTO for bulk approve settlements operation
/// </summary>
public class BulkApprovSettlementsRequest
{
    /// <summary>
    /// List of settlement IDs to approve
    /// </summary>
    public List<string> SettlementIds { get; set; } = new();

    /// <summary>
    /// User who is approving the settlements
    /// </summary>
    public string ApprovedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for bulk finalize settlements operation
/// </summary>
public class BulkFinalizeSettlementsRequest
{
    /// <summary>
    /// List of settlement IDs to finalize
    /// </summary>
    public List<string> SettlementIds { get; set; } = new();

    /// <summary>
    /// User who is finalizing the settlements
    /// </summary>
    public string FinalizedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for bulk export settlements operation
/// </summary>
public class BulkExportSettlementsRequest
{
    /// <summary>
    /// List of settlement IDs to export
    /// </summary>
    public List<string> SettlementIds { get; set; } = new();

    /// <summary>
    /// Export format (excel, csv, pdf)
    /// </summary>
    public string Format { get; set; } = "excel";
}

/// <summary>
/// Response DTO for bulk operation results
/// </summary>
public class BulkOperationResultDto
{
    /// <summary>
    /// Number of successfully processed settlements
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed settlements
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Detailed operation results
    /// </summary>
    public List<BulkOperationDetailDto> Details { get; set; } = new();
}

/// <summary>
/// Detailed result for individual settlement in bulk operation
/// </summary>
public class BulkOperationDetailDto
{
    /// <summary>
    /// Settlement ID
    /// </summary>
    public string SettlementId { get; set; } = string.Empty;

    /// <summary>
    /// Operation status (success, failure)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message (if operation failed)
    /// </summary>
    public string? Message { get; set; }
}
