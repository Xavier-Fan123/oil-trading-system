using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.TradingPartners;

public class BlockTradingPartnerCommandHandler : IRequestHandler<BlockTradingPartnerCommand, Unit>
{
    private readonly ITradingPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BlockTradingPartnerCommandHandler> _logger;

    public BlockTradingPartnerCommandHandler(
        ITradingPartnerRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<BlockTradingPartnerCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(BlockTradingPartnerCommand request, CancellationToken cancellationToken)
    {
        // Retrieve the trading partner
        var partner = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (partner == null)
        {
            throw new NotFoundException($"Trading partner with ID {request.Id} not found");
        }

        // Block the trading partner
        partner.IsBlocked = true;
        partner.BlockReason = request.Reason;

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trading partner blocked: {Id} ({CompanyName}). Reason: {Reason}",
            partner.Id, partner.CompanyName, request.Reason);

        return Unit.Value;
    }
}
