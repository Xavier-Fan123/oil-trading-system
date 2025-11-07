using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.SettlementTemplates;

/// <summary>
/// Handler for deleting a settlement template
/// </summary>
public class DeleteSettlementTemplateCommandHandler : IRequestHandler<DeleteSettlementTemplateCommand, bool>
{
    private readonly ISettlementTemplateRepository _templateRepository;
    private readonly ILogger<DeleteSettlementTemplateCommandHandler> _logger;

    public DeleteSettlementTemplateCommandHandler(
        ISettlementTemplateRepository templateRepository,
        ILogger<DeleteSettlementTemplateCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteSettlementTemplateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting settlement template: {TemplateId}", request.TemplateId);

        // Get template
        var template = await _templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID {request.TemplateId} not found");
        }

        // Check permission - only creator can delete
        if (template.CreatedByUserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Only the template creator can delete it");
        }

        // Soft delete
        template.SoftDelete(request.DeletedBy);

        // Update in repository
        await _templateRepository.UpdateAsync(template, cancellationToken);

        _logger.LogInformation("Settlement template deleted: {TemplateId}", request.TemplateId);

        return true;
    }
}
