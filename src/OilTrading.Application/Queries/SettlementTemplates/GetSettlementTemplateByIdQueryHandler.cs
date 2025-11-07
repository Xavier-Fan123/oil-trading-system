using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.SettlementTemplates;

/// <summary>
/// Handler for retrieving a specific settlement template by ID
/// </summary>
public class GetSettlementTemplateByIdQueryHandler : IRequestHandler<GetSettlementTemplateByIdQuery, SettlementTemplateDto?>
{
    private readonly ISettlementTemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetSettlementTemplateByIdQueryHandler> _logger;

    public GetSettlementTemplateByIdQueryHandler(
        ISettlementTemplateRepository templateRepository,
        IUserRepository userRepository,
        ILogger<GetSettlementTemplateByIdQueryHandler> logger)
    {
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<SettlementTemplateDto?> Handle(GetSettlementTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving settlement template: {TemplateId}", request.TemplateId);

        // Get template with includes
        var template = await _templateRepository.GetByIdWithIncludesAsync(request.TemplateId, cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("Template not found: {TemplateId}", request.TemplateId);
            return null;
        }

        // Check access permission
        var hasAccess = await _templateRepository.UserHasAccessAsync(request.TemplateId, request.UserId, cancellationToken);
        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} does not have access to template {TemplateId}", request.UserId, request.TemplateId);
            return null;
        }

        return await MapTemplateToDto(template, request.UserId, cancellationToken);
    }

    private async Task<SettlementTemplateDto> MapTemplateToDto(Core.Entities.SettlementTemplate template, Guid userId, CancellationToken cancellationToken)
    {
        var creator = await _userRepository.GetByIdAsync(template.CreatedByUserId, cancellationToken);
        var canEdit = await _templateRepository.UserCanEditAsync(template.Id, userId, cancellationToken);
        var permissions = await _templateRepository.GetPermissionsAsync(template.Id, cancellationToken);

        return new SettlementTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            CreatedByUserId = template.CreatedByUserId,
            CreatedByUserName = creator?.Name ?? "Unknown",
            CreatedAt = template.CreatedAt,
            Version = template.Version,
            IsActive = template.IsActive,
            IsPublic = template.IsPublic,
            TemplateConfiguration = template.TemplateConfiguration,
            TimesUsed = template.TimesUsed,
            LastUsedAt = template.LastUsedAt,
            SharedWith = permissions.Count,
            CanEdit = canEdit,
            CanDelete = template.CreatedByUserId == userId
        };
    }
}
