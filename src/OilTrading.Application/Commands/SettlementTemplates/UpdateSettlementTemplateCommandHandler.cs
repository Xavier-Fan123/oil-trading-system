using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.SettlementTemplates;

/// <summary>
/// Handler for updating an existing settlement template
/// </summary>
public class UpdateSettlementTemplateCommandHandler : IRequestHandler<UpdateSettlementTemplateCommand, SettlementTemplateDto>
{
    private readonly ISettlementTemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateSettlementTemplateCommandHandler> _logger;

    public UpdateSettlementTemplateCommandHandler(
        ISettlementTemplateRepository templateRepository,
        IUserRepository userRepository,
        ILogger<UpdateSettlementTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<SettlementTemplateDto> Handle(UpdateSettlementTemplateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating settlement template: {TemplateId}", request.Id);

        // Get existing template
        var template = await _templateRepository.GetByIdAsync(request.Id, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID {request.Id} not found");
        }

        // Check permission
        if (!await _templateRepository.UserCanEditAsync(request.Id, request.UserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to edit this template");
        }

        // Check if new name conflicts with another template
        if (template.Name != request.Name &&
            await _templateRepository.TemplateNameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            throw new InvalidOperationException($"Template with name '{request.Name}' already exists");
        }

        // Update template
        template.Name = request.Name;
        template.Description = request.Description;
        template.TemplateConfiguration = request.TemplateConfiguration;

        if (template.IsPublic != request.IsPublic)
        {
            if (request.IsPublic)
            {
                template.MakePublic(request.UpdatedBy);
            }
            else
            {
                template.MakePrivate(request.UpdatedBy);
            }
        }

        // Create new version
        template.CreateNewVersion();
        template.SetUpdatedBy(request.UpdatedBy);

        // Update in repository
        await _templateRepository.UpdateAsync(template, cancellationToken);

        _logger.LogInformation("Settlement template updated: {TemplateId} - Version {Version}", template.Id, template.Version);

        // Map to DTO
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
