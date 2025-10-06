using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.PaperContracts;

public class UpdateMTMCommandHandler : IRequestHandler<UpdateMTMCommand, IEnumerable<MTMUpdateDto>>
{
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateMTMCommandHandler> _logger;

    public UpdateMTMCommandHandler(
        IPaperContractRepository paperContractRepository,
        IMarketDataRepository marketDataRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateMTMCommandHandler> logger)
    {
        _paperContractRepository = paperContractRepository;
        _marketDataRepository = marketDataRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<MTMUpdateDto>> Handle(UpdateMTMCommand request, CancellationToken cancellationToken)
    {
        var results = new List<MTMUpdateDto>();

        try
        {
            // Get contracts to update
            var contracts = request.ContractIds?.Any() == true
                ? await GetSpecificContracts(request.ContractIds, cancellationToken)
                : await _paperContractRepository.GetOpenPositionsAsync(cancellationToken);

            foreach (var contract in contracts)
            {
                var mtmResult = await UpdateContractMTM(contract, request.MTMDate, request.UpdatedBy, cancellationToken);
                if (mtmResult != null)
                {
                    results.Add(mtmResult);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "MTM update completed for {ContractCount} contracts on {MTMDate}",
                results.Count, request.MTMDate);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MTM for contracts on {MTMDate}", request.MTMDate);
            throw;
        }
    }

    private async Task<IEnumerable<PaperContract>> GetSpecificContracts(List<Guid> contractIds, CancellationToken cancellationToken)
    {
        var contracts = new List<PaperContract>();
        foreach (var id in contractIds)
        {
            var contract = await _paperContractRepository.GetByIdAsync(id, cancellationToken);
            if (contract != null)
                contracts.Add(contract);
        }
        return contracts;
    }

    private async Task<MTMUpdateDto?> UpdateContractMTM(
        PaperContract contract, 
        DateTime mtmDate, 
        string updatedBy,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current market price for this contract
            var currentPrice = await GetCurrentPrice(contract, mtmDate, cancellationToken);
            
            if (!currentPrice.HasValue)
            {
                _logger.LogWarning(
                    "No market price found for contract {ContractId} ({ProductType} {ContractMonth}) on {MTMDate}",
                    contract.Id, contract.ProductType, contract.ContractMonth, mtmDate);
                return null;
            }

            // Calculate daily P&L if this is not the first MTM
            decimal? dailyPnL = null;
            if (contract.LastMTMDate.HasValue && contract.CurrentPrice.HasValue)
            {
                var priceDiff = currentPrice.Value - contract.CurrentPrice.Value;
                var multiplier = contract.Position == PositionType.Long ? 1 : -1;
                dailyPnL = priceDiff * contract.Quantity * contract.LotSize * multiplier;
            }

            // Update the contract
            contract.UpdateMTM(currentPrice.Value, mtmDate);
            contract.DailyPnL = dailyPnL;
            contract.SetUpdatedBy(updatedBy);

            await _paperContractRepository.UpdateAsync(contract, cancellationToken);

            return new MTMUpdateDto
            {
                ContractId = contract.Id,
                CurrentPrice = currentPrice.Value,
                MTMDate = mtmDate,
                UnrealizedPnL = contract.UnrealizedPnL ?? 0,
                DailyPnL = dailyPnL
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MTM for contract {ContractId}", contract.Id);
            return null;
        }
    }

    private async Task<decimal?> GetCurrentPrice(
        PaperContract contract, 
        DateTime mtmDate, 
        CancellationToken cancellationToken)
    {
        // Build product code based on contract type
        var productCode = BuildProductCode(contract.ProductType, contract.ContractMonth);
        
        // Try to get exact date price first
        var exactPrice = await _marketDataRepository.GetByProductAndDateAsync(
            productCode, mtmDate, cancellationToken);
            
        if (exactPrice != null)
            return exactPrice.Price;

        // If no exact price, get the latest available price for this product
        var latestPrice = await _marketDataRepository.GetLatestPriceAsync(
            productCode, cancellationToken);
            
        if (latestPrice != null && latestPrice.PriceDate <= mtmDate)
        {
            _logger.LogInformation(
                "Using latest available price from {PriceDate} for contract {ContractId} MTM on {MTMDate}",
                latestPrice.PriceDate, contract.Id, mtmDate);
            return latestPrice.Price;
        }

        return null;
    }

    private static string BuildProductCode(string productType, string contractMonth)
    {
        // Map product types to our market data product codes
        var productCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "380cst", "380CST" },
            { "0.5%", "05PCT" },
            { "Gasoil", "GASOIL" },
            { "Brent", "BRENT" }
        };

        var baseCode = productCodeMap.TryGetValue(productType, out var mappedCode) 
            ? mappedCode 
            : productType.ToUpper();

        return $"{baseCode}_{contractMonth}";
    }
}