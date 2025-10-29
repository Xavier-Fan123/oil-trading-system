using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.TradingPartners;

public class UpdateTradingPartnerCommandHandler : IRequestHandler<UpdateTradingPartnerCommand, TradingPartnerDto>
{
    private readonly ITradingPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTradingPartnerCommandHandler> _logger;

    public UpdateTradingPartnerCommandHandler(
        ITradingPartnerRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTradingPartnerCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TradingPartnerDto> Handle(UpdateTradingPartnerCommand request, CancellationToken cancellationToken)
    {
        // Retrieve the trading partner
        var partner = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (partner == null)
        {
            throw new NotFoundException($"Trading partner with ID {request.Id} not found");
        }

        // Update properties if provided
        if (!string.IsNullOrEmpty(request.CompanyName))
        {
            partner.CompanyName = request.CompanyName;
            partner.Name = request.CompanyName;
        }

        if (!string.IsNullOrEmpty(request.ContactPerson))
        {
            partner.ContactPerson = request.ContactPerson;
        }

        if (!string.IsNullOrEmpty(request.ContactEmail))
        {
            partner.ContactEmail = request.ContactEmail;
        }

        if (!string.IsNullOrEmpty(request.ContactPhone))
        {
            partner.ContactPhone = request.ContactPhone;
        }

        if (!string.IsNullOrEmpty(request.Address))
        {
            partner.Address = request.Address;
        }

        if (request.TaxNumber != null)
        {
            partner.TaxNumber = request.TaxNumber;
        }

        if (request.CreditLimit.HasValue)
        {
            partner.CreditLimit = request.CreditLimit.Value;
        }

        if (request.CreditLimitValidUntil.HasValue)
        {
            partner.CreditLimitValidUntil = request.CreditLimitValidUntil.Value;
        }

        if (request.PaymentTermDays.HasValue)
        {
            partner.PaymentTermDays = request.PaymentTermDays.Value;
        }

        if (request.IsActive.HasValue)
        {
            partner.IsActive = request.IsActive.Value;
        }

        if (request.IsBlocked.HasValue)
        {
            partner.IsBlocked = request.IsBlocked.Value;
            if (!request.IsBlocked.Value)
            {
                partner.BlockReason = null; // Clear block reason if unblocking
            }
        }

        if (!string.IsNullOrEmpty(request.BlockReason))
        {
            partner.BlockReason = request.BlockReason;
        }

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trading partner updated: {Id} ({CompanyName})",
            partner.Id, partner.CompanyName);

        // Return DTO
        return new TradingPartnerDto
        {
            Id = partner.Id,
            CompanyName = partner.CompanyName,
            CompanyCode = partner.CompanyCode,
            PartnerType = partner.PartnerType,
            ContactPerson = partner.ContactPerson,
            ContactEmail = string.IsNullOrEmpty(partner.ContactEmail) ? null : partner.ContactEmail,
            ContactPhone = string.IsNullOrEmpty(partner.ContactPhone) ? null : partner.ContactPhone,
            Address = string.IsNullOrEmpty(partner.Address) ? null : partner.Address,
            TaxNumber = partner.TaxNumber,
            CreditLimit = partner.CreditLimit,
            CreditLimitValidUntil = partner.CreditLimitValidUntil,
            PaymentTermDays = partner.PaymentTermDays,
            CurrentExposure = partner.CurrentExposure,
            CreditUtilization = partner.CreditLimit > 0 ? (partner.CurrentExposure / partner.CreditLimit * 100) : 0,
            IsActive = partner.IsActive,
            IsBlocked = partner.IsBlocked,
            BlockReason = partner.BlockReason
        };
    }
}
