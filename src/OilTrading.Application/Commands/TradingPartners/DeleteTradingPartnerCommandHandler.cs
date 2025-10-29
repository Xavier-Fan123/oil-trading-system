using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.TradingPartners;

public class DeleteTradingPartnerCommandHandler : IRequestHandler<DeleteTradingPartnerCommand, Unit>
{
    private readonly ITradingPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTradingPartnerCommandHandler> _logger;

    public DeleteTradingPartnerCommandHandler(
        ITradingPartnerRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteTradingPartnerCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteTradingPartnerCommand request, CancellationToken cancellationToken)
    {
        // Retrieve the trading partner
        var partner = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (partner == null)
        {
            throw new NotFoundException($"Trading partner with ID {request.Id} not found");
        }

        // Check if trading partner has related contracts (if needed for business logic)
        // For now, we'll allow deletion even with related contracts
        // You may want to add validation to prevent deletion if contracts exist

        // Delete the trading partner
        await _repository.DeleteAsync(partner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trading partner deleted: {Id} ({CompanyName})",
            partner.Id, partner.CompanyName);

        return Unit.Value;
    }
}
