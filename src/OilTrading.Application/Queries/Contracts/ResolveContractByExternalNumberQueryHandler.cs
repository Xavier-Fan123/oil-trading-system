using MediatR;
using AutoMapper;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Contracts;

/// <summary>
/// Handles resolving contracts by external contract number
/// Supports finding single or multiple matching contracts
/// </summary>
public class ResolveContractByExternalNumberQueryHandler
    : IRequestHandler<ResolveContractByExternalNumberQuery, ContractResolutionResultDto>
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IMapper _mapper;

    public ResolveContractByExternalNumberQueryHandler(
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IMapper mapper)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _mapper = mapper;
    }

    public async Task<ContractResolutionResultDto> Handle(
        ResolveContractByExternalNumberQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalContractNumber))
        {
            return new ContractResolutionResultDto
            {
                Success = false,
                ErrorMessage = "External contract number is required"
            };
        }

        var externalNumber = request.ExternalContractNumber.Trim();
        var candidates = new List<(ContractCandidateDto dto, object entity, string type)>();

        // Search purchase contracts if no specific type restriction or Purchase expected
        if (string.IsNullOrEmpty(request.ExpectedContractType) ||
            request.ExpectedContractType.Equals("Purchase", StringComparison.OrdinalIgnoreCase))
        {
            var purchaseContracts = await _purchaseContractRepository
                .GetByExternalContractNumberAsync(externalNumber, cancellationToken);

            foreach (var contract in purchaseContracts)
            {
                // Apply additional filters if specified
                if (request.ExpectedTradingPartnerId.HasValue &&
                    contract.TradingPartnerId != request.ExpectedTradingPartnerId.Value)
                    continue;

                if (request.ExpectedProductId.HasValue &&
                    contract.ProductId != request.ExpectedProductId.Value)
                    continue;

                var candidateDto = MapPurchaseContractToCandidate(contract);
                candidates.Add((candidateDto, contract, "Purchase"));
            }
        }

        // Search sales contracts if no specific type restriction or Sales expected
        if (string.IsNullOrEmpty(request.ExpectedContractType) ||
            request.ExpectedContractType.Equals("Sales", StringComparison.OrdinalIgnoreCase))
        {
            var salesContracts = await _salesContractRepository
                .GetByExternalContractNumberAsync(externalNumber, cancellationToken);

            foreach (var contract in salesContracts)
            {
                // Apply additional filters if specified
                if (request.ExpectedTradingPartnerId.HasValue &&
                    contract.TradingPartnerId != request.ExpectedTradingPartnerId.Value)
                    continue;

                if (request.ExpectedProductId.HasValue &&
                    contract.ProductId != request.ExpectedProductId.Value)
                    continue;

                var candidateDto = MapSalesContractToCandidate(contract);
                candidates.Add((candidateDto, contract, "Sales"));
            }
        }

        // No matches found
        if (candidates.Count == 0)
        {
            return new ContractResolutionResultDto
            {
                Success = false,
                ErrorMessage = $"No contract found with external contract number: {externalNumber}"
            };
        }

        // Single match - return success
        if (candidates.Count == 1)
        {
            var (dto, _, type) = candidates[0];
            return new ContractResolutionResultDto
            {
                Success = true,
                ContractId = dto.Id,
                ContractType = type,
                Candidates = new List<ContractCandidateDto> { dto }
            };
        }

        // Multiple matches - return candidates for user to select
        return new ContractResolutionResultDto
        {
            Success = false,
            ErrorMessage = $"Multiple contracts found with external contract number: {externalNumber}. Please select one.",
            Candidates = candidates.Select(c => c.dto).ToList()
        };
    }

    private ContractCandidateDto MapPurchaseContractToCandidate(PurchaseContract contract)
    {
        return new ContractCandidateDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber.Value,
            ExternalContractNumber = contract.ExternalContractNumber ?? string.Empty,
            ContractType = "Purchase",
            TradingPartnerName = contract.TradingPartner?.Name ?? "Unknown Supplier",
            ProductName = contract.Product?.Name ?? "Unknown Product",
            Quantity = contract.ContractQuantity.Value,
            QuantityUnit = GetQuantityUnitLabel(contract.ContractQuantity.Unit),
            Status = contract.Status.ToString(),
            CreatedAt = contract.CreatedAt
        };
    }

    private ContractCandidateDto MapSalesContractToCandidate(SalesContract contract)
    {
        return new ContractCandidateDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber.Value,
            ExternalContractNumber = contract.ExternalContractNumber ?? string.Empty,
            ContractType = "Sales",
            TradingPartnerName = contract.TradingPartner?.Name ?? "Unknown Customer",
            ProductName = contract.Product?.Name ?? "Unknown Product",
            Quantity = contract.ContractQuantity.Value,
            QuantityUnit = GetQuantityUnitLabel(contract.ContractQuantity.Unit),
            Status = contract.Status.ToString(),
            CreatedAt = contract.CreatedAt
        };
    }

    private string GetQuantityUnitLabel(QuantityUnit unit)
    {
        return unit switch
        {
            QuantityUnit.MT => "MT",
            QuantityUnit.BBL => "BBL",
            QuantityUnit.GAL => "GAL",
            QuantityUnit.LOTS => "LOTS",
            _ => unit.ToString()
        };
    }
}
