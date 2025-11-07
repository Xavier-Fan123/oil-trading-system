using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Common;

namespace OilTrading.Application.Queries.SettlementTemplates;

/// <summary>
/// Query to get paged list of settlement templates accessible to user
/// </summary>
public class GetSettlementTemplatesQuery : IRequest<PagedResult<SettlementTemplateSummaryDto>>
{
    /// <summary>
    /// Current user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Search term for filtering
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by public templates only
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Filter by active templates only
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field: "name", "createdAt", "lastUsedAt", "timesUsed"
    /// </summary>
    public string SortBy { get; set; } = "createdAt";

    /// <summary>
    /// Sort descending
    /// </summary>
    public bool SortDescending { get; set; } = true;
}
