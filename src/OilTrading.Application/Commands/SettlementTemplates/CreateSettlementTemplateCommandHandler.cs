using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.SettlementTemplates;

/// <summary>
/// Handler for creating a new settlement template
/// </summary>
public class CreateSettlementTemplateCommandHandler : IRequestHandler<CreateSettlementTemplateCommand, SettlementTemplateDto>
{
    private readonly ISettlementTemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateSettlementTemplateCommandHandler> _logger;

    public CreateSettlementTemplateCommandHandler(
        ISettlementTemplateRepository templateRepository,
        IUserRepository userRepository,
        ILogger<CreateSettlementTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<SettlementTemplateDto> Handle(CreateSettlementTemplateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new settlement template: {Name}", request.Name);

        // Check if template name already exists
        if (await _templateRepository.TemplateNameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Template with name '{request.Name}' already exists");
        }

        // Create template entity
        var template = new SettlementTemplate
        {
            Name = request.Name,
            Description = request.Description,
            CreatedByUserId = request.CreatedByUserId,
            TemplateConfiguration = request.TemplateConfiguration,
            IsPublic = request.IsPublic,
            IsActive = true,
            Version = 1,
            TimesUsed = 0
        };

        template.SetCreated(request.CreatedBy);

        // Add to repository
        await _templateRepository.AddAsync(template, cancellationToken);

        _logger.LogInformation("Settlement template created: {TemplateId}", template.Id);

        // Map to DTO
        return await MapTemplateToDto(template, request.CreatedByUserId, cancellationToken);
    }

    private async Task<SettlementTemplateDto> MapTemplateToDto(SettlementTemplate template, Guid userId, CancellationToken cancellationToken)
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
            CanDelete = template.CreatedByUserId == userId // Only creator can delete
        };
    }
}
