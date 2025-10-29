using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.TradingPartners;

public class UnblockTradingPartnerCommandHandler : IRequestHandler<UnblockTradingPartnerCommand, Unit>
{
    private readonly ITradingPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnblockTradingPartnerCommandHandler> _logger;

    public UnblockTradingPartnerCommandHandler(
        ITradingPartnerRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UnblockTradingPartnerCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(UnblockTradingPartnerCommand request, CancellationToken cancellationToken)
    {
        // Retrieve the trading partner
        var partner = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (partner == null)
        {
            throw new NotFoundException($"Trading partner with ID {request.Id} not found");
        }

        // Unblock the trading partner
        partner.IsBlocked = false;
        partner.BlockReason = null;

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trading partner unblocked: {Id} ({CompanyName})",
            partner.Id, partner.CompanyName);

        return Unit.Value;
    }
}
