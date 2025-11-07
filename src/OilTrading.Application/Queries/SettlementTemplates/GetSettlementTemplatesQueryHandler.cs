using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Common;
using OilTrading.Core.Repositories;
using System.Linq.Expressions;

namespace OilTrading.Application.Queries.SettlementTemplates;

/// <summary>
/// Handler for retrieving paged list of settlement templates
/// </summary>
public class GetSettlementTemplatesQueryHandler : IRequestHandler<GetSettlementTemplatesQuery, PagedResult<SettlementTemplateSummaryDto>>
{
    private readonly ISettlementTemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetSettlementTemplatesQueryHandler> _logger;

    public GetSettlementTemplatesQueryHandler(
        ISettlementTemplateRepository templateRepository,
        IUserRepository userRepository,
        ILogger<GetSettlementTemplatesQueryHandler> logger)
    {
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<PagedResult<SettlementTemplateSummaryDto>> Handle(GetSettlementTemplatesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving settlement templates for user: {UserId}", request.UserId);

        // Build filter expression
        Expression<Func<Core.Entities.SettlementTemplate, bool>>? filter = null;

        // Filter by public/private
        if (request.IsPublic.HasValue)
        {
            if (request.IsPublic.Value)
            {
                filter = t => t.IsPublic;
            }
            else
            {
                filter = t => t.CreatedByUserId == request.UserId;
            }
        }

        // Filter by active
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                filter = filter == null
                    ? t => t.IsActive
                    : CombineExpressions(filter, t => t.IsActive);
            }
        }

        // Build order by expression
        Expression<Func<Core.Entities.SettlementTemplate, object>> orderBy = request.SortBy switch
        {
            "name" => t => t.Name,
            "lastUsedAt" => t => t.LastUsedAt ?? DateTime.MinValue,
            "timesUsed" => t => t.TimesUsed,
            _ => t => t.CreatedAt
        };

        // Get paged results
        var pagedResult = await _templateRepository.GetPagedAsync(
            filter: filter,
            orderBy: orderBy,
            orderByDescending: request.SortDescending,
            page: request.PageNumber,
            pageSize: request.PageSize,
            includeProperties: new[] { "Permissions" },
            cancellationToken: cancellationToken);

        // If search term provided, filter in memory (after paging for efficiency)
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            pagedResult.Items = pagedResult.Items
                .Where(t => t.Name.ToLower().Contains(searchLower) ||
                           t.Description.ToLower().Contains(searchLower))
                .ToList();
        }

        // Map to DTOs
        var dtos = new List<SettlementTemplateSummaryDto>();
        foreach (var template in pagedResult.Items)
        {
            var creator = await _userRepository.GetByIdAsync(template.CreatedByUserId, cancellationToken);
            dtos.Add(new SettlementTemplateSummaryDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                TimesUsed = template.TimesUsed,
                LastUsedAt = template.LastUsedAt,
                IsPublic = template.IsPublic,
                IsActive = template.IsActive,
                CreatedByUserName = creator?.Name ?? "Unknown",
                CreatedAt = template.CreatedAt
            });
        }

        return new PagedResult<SettlementTemplateSummaryDto>
        {
            Items = dtos,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
    }

    private static Expression<Func<Core.Entities.SettlementTemplate, bool>> CombineExpressions(
        Expression<Func<Core.Entities.SettlementTemplate, bool>> expr1,
        Expression<Func<Core.Entities.SettlementTemplate, bool>> expr2)
    {
        var param = Expression.Parameter(typeof(Core.Entities.SettlementTemplate));
        var body = Expression.AndAlso(
            Expression.Invoke(expr1, param),
            Expression.Invoke(expr2, param));
        return Expression.Lambda<Func<Core.Entities.SettlementTemplate, bool>>(body, param);
    }
}
