using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Services;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace OilTrading.Application.Services;

/// <summary>
/// Service implementation for contract settlement calculations and management.
/// Handles complex pricing calculations with mixed units (MT/BBL), benchmark pricing,
/// quantity calculations, and comprehensive charge management for oil trading contracts.
/// </summary>
public class SettlementCalculationService : ISettlementCalculationService
{
    private readonly IContractSettlementRepository _settlementRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IPricingService _pricingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SettlementCalculationService> _logger;

    public SettlementCalculationService(
        IContractSettlementRepository settlementRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IMarketDataRepository marketDataRepository,
        IPricingService pricingService,
        IUnitOfWork unitOfWork,
        ILogger<SettlementCalculationService> logger)
    {
        _settlementRepository = settlementRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _marketDataRepository = marketDataRepository;
        _pricingService = pricingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractSettlementDto> CreateOrUpdateSettlementAsync(
        Guid contractId,
        string documentNumber,
        DocumentType documentType,
        decimal actualMT,
        decimal actualBBL,
        DateTime documentDate,
        string createdBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating or updating settlement for contract {ContractId} with document {DocumentNumber}", 
                contractId, documentNumber);

            // Validate inputs
            if (actualMT < 0 || actualBBL < 0)
                throw new DomainException("Actual quantities cannot be negative");

            // Get contract information (try both purchase and sales)
            var (contractNumber, externalContractNumber, contractEntity) = await GetContractInfoAsync(contractId, cancellationToken);

            // Check if settlement already exists for this contract
            // Note: A contract can have multiple settlements (one-to-many).
            // For update, we modify the most recent one (first in the list).
            var existingSettlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);
            var existingSettlement = existingSettlements.FirstOrDefault();

            ContractSettlement settlement;
            if (existingSettlement != null)
            {
                // Update existing settlement
                settlement = existingSettlement;
                settlement.UpdateActualQuantities(actualMT, actualBBL, createdBy);
                
                if (!string.IsNullOrEmpty(documentNumber))
                {
                    // Update document information if provided
                    typeof(ContractSettlement).GetProperty("DocumentNumber")?.SetValue(settlement, documentNumber);
                    typeof(ContractSettlement).GetProperty("DocumentType")?.SetValue(settlement, documentType);
                    typeof(ContractSettlement).GetProperty("DocumentDate")?.SetValue(settlement, documentDate);
                }

                _logger.LogInformation("Updating existing settlement {SettlementId}", settlement.Id);
            }
            else
            {
                // Create new settlement
                settlement = new ContractSettlement(
                    contractId,
                    contractNumber,
                    externalContractNumber,
                    documentNumber,
                    documentType,
                    documentDate,
                    createdBy);

                settlement.UpdateActualQuantities(actualMT, actualBBL, createdBy);
                settlement = await _settlementRepository.AddAsync(settlement, cancellationToken);
                
                _logger.LogInformation("Created new settlement {SettlementId}", settlement.Id);
            }

            // Calculate quantities based on contract terms and actual quantities
            await CalculateSettlementQuantitiesAsync(settlement, contractEntity, createdBy, cancellationToken);

            // Auto-calculate benchmark prices if contract has pricing formula
            await AutoCalculateBenchmarkPricesAsync(settlement, contractEntity, createdBy, cancellationToken);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await MapToDto(settlement, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating settlement for contract {ContractId}", contractId);
            throw;
        }
    }

    public async Task<decimal> CalculateBenchmarkPriceAsync(
        string indexName,
        PricingMethod method,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating benchmark price for {IndexName} using {Method} from {StartDate} to {EndDate}", 
                indexName, method, startDate, endDate);

            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Index name cannot be null or empty", nameof(indexName));

            if (startDate > endDate)
                throw new ArgumentException("Start date cannot be after end date");

            // Get historical prices for the period
            var prices = await _marketDataRepository.GetByProductAsync(indexName, startDate, endDate, cancellationToken);
            
            if (!prices.Any())
            {
                _logger.LogWarning("No price data found for {IndexName} between {StartDate} and {EndDate}", 
                    indexName, startDate, endDate);
                throw new InvalidOperationException($"No price data found for {indexName} between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}");
            }

            var priceValues = prices.Select(p => p.Price).ToArray();
            
            var calculatedPrice = method switch
            {
                PricingMethod.Fixed => throw new InvalidOperationException("Fixed pricing method should not use market data"),
                PricingMethod.AVG => priceValues.Average(),
                PricingMethod.MIN => priceValues.Min(),
                PricingMethod.MAX => priceValues.Max(),
                PricingMethod.FIRST => priceValues.First(),
                PricingMethod.LAST => priceValues.Last(),
                PricingMethod.WAVG => CalculateWeightedAverage(priceValues),
                PricingMethod.MEDIAN => CalculateMedian(priceValues),
                PricingMethod.MODE => CalculateMode(priceValues),
                _ => throw new ArgumentException($"Unsupported pricing method: {method}")
            };

            _logger.LogInformation("Calculated benchmark price {Price} for {IndexName} using {Method}", 
                calculatedPrice, indexName, method);

            return calculatedPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating benchmark price for {IndexName}", indexName);
            throw;
        }
    }

    public async Task<SettlementChargeDto> AddOrUpdateChargeAsync(
        Guid settlementId,
        ChargeType chargeType,
        decimal amount,
        string description,
        string? referenceDocument = null,
        string addedBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding charge {ChargeType} of {Amount} to settlement {SettlementId}", 
                chargeType, amount, settlementId);

            var settlement = await _settlementRepository.GetWithChargesAsync(settlementId, cancellationToken);
            if (settlement == null)
                throw new NotFoundException($"Settlement with ID {settlementId} not found");

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty", nameof(description));

            // Check if charge of same type already exists - if so, update it
            var existingCharge = settlement.Charges.FirstOrDefault(c => c.ChargeType == chargeType && c.Description == description);
            
            SettlementCharge charge;
            if (existingCharge != null)
            {
                // Update existing charge
                existingCharge.UpdateAmount(amount, addedBy);
                charge = existingCharge;
                _logger.LogInformation("Updated existing charge {ChargeId}", charge.Id);
            }
            else
            {
                // Add new charge
                charge = settlement.AddCharge(
                    chargeType,
                    description,
                    amount,
                    "USD", // Default currency
                    DateTime.UtcNow,
                    referenceDocument,
                    null,
                    addedBy);
                _logger.LogInformation("Added new charge {ChargeId}", charge.Id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapChargeToDto(charge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/updating charge for settlement {SettlementId}", settlementId);
            throw;
        }
    }

    public async Task<ContractSettlementDto> RecalculateSettlementAsync(
        Guid settlementId,
        string updatedBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Recalculating settlement {SettlementId}", settlementId);

            var settlement = await _settlementRepository.GetWithChargesAsync(settlementId, cancellationToken);
            if (settlement == null)
                throw new NotFoundException($"Settlement with ID {settlementId} not found");

            if (!settlement.CanBeModified())
                throw new DomainException("Cannot recalculate finalized settlement");

            // Get contract information for recalculation
            var (_, _, contractEntity) = await GetContractInfoAsync(settlement.ContractId, cancellationToken);

            // Recalculate quantities
            await CalculateSettlementQuantitiesAsync(settlement, contractEntity, updatedBy, cancellationToken);

            // Recalculate benchmark prices
            await AutoCalculateBenchmarkPricesAsync(settlement, contractEntity, updatedBy, cancellationToken);

            // Update status to Calculated if all calculations are complete
            if (settlement.Status == ContractSettlementStatus.Draft && 
                settlement.BenchmarkAmount > 0 && 
                settlement.CalculationQuantityMT > 0)
            {
                settlement.UpdateStatus(ContractSettlementStatus.Calculated, updatedBy);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully recalculated settlement {SettlementId}", settlementId);

            return await MapToDto(settlement, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating settlement {SettlementId}", settlementId);
            throw;
        }
    }

    public async Task<ContractSettlementDto?> GetSettlementByExternalContractNumberAsync(
        string externalContractNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settlement = await _settlementRepository.GetByExternalContractNumberAsync(externalContractNumber, cancellationToken);
            return settlement != null ? await MapToDto(settlement, cancellationToken) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlement by external contract number {ExternalContractNumber}", externalContractNumber);
            throw;
        }
    }

    public async Task<ContractSettlementDto?> GetSettlementByContractIdAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // A contract can have multiple settlements. Return the most recent one (first in list).
            var settlements = await _settlementRepository.GetByContractIdAsync(contractId, cancellationToken);
            var settlement = settlements.FirstOrDefault();
            return settlement != null ? await MapToDto(settlement, cancellationToken) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlement by contract ID {ContractId}", contractId);
            throw;
        }
    }

    public async Task<ContractSettlementDto?> GetSettlementByIdAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settlement = await _settlementRepository.GetWithChargesAsync(settlementId, cancellationToken);
            return settlement != null ? await MapToDto(settlement, cancellationToken) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlement by ID {SettlementId}", settlementId);
            throw;
        }
    }

    public async Task<bool> RemoveChargeAsync(
        Guid settlementId,
        Guid chargeId,
        string removedBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing charge {ChargeId} from settlement {SettlementId}", chargeId, settlementId);

            var settlement = await _settlementRepository.GetWithChargesAsync(settlementId, cancellationToken);
            if (settlement == null)
                throw new NotFoundException($"Settlement with ID {settlementId} not found");

            settlement.RemoveCharge(chargeId, removedBy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully removed charge {ChargeId}", chargeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing charge {ChargeId} from settlement {SettlementId}", chargeId, settlementId);
            return false;
        }
    }

    public async Task<ContractSettlementDto> UpdateSettlementStatusAsync(
        Guid settlementId,
        ContractSettlementStatus newStatus,
        string updatedBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId, cancellationToken);
            if (settlement == null)
                throw new NotFoundException($"Settlement with ID {settlementId} not found");

            settlement.UpdateStatus(newStatus, updatedBy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await MapToDto(settlement, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement status for {SettlementId}", settlementId);
            throw;
        }
    }

    public async Task<ContractSettlementDto> FinalizeSettlementAsync(
        Guid settlementId,
        string finalizedBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId, cancellationToken);
            if (settlement == null)
                throw new NotFoundException($"Settlement with ID {settlementId} not found");

            settlement.Finalize(finalizedBy);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Settlement {SettlementId} finalized by {FinalizedBy}", settlementId, finalizedBy);

            return await MapToDto(settlement, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing settlement {SettlementId}", settlementId);
            throw;
        }
    }

    public async Task<IEnumerable<ContractSettlementDto>> GetSettlementsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settlements = await _settlementRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
            var dtos = new List<ContractSettlementDto>();

            foreach (var settlement in settlements)
            {
                dtos.Add(await MapToDto(settlement, cancellationToken));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlements by date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<ContractSettlementDto>> GetSettlementsByStatusAsync(
        ContractSettlementStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settlements = await _settlementRepository.GetByStatusAsync(status, cancellationToken);
            var dtos = new List<ContractSettlementDto>();

            foreach (var settlement in settlements)
            {
                dtos.Add(await MapToDto(settlement, cancellationToken));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlements by status {Status}", status);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<(string contractNumber, string externalContractNumber, object contractEntity)> GetContractInfoAsync(
        Guid contractId, 
        CancellationToken cancellationToken)
    {
        // Try purchase contract first
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(contractId, cancellationToken);
        if (purchaseContract != null)
        {
            return (
                purchaseContract.ContractNumber.Value,
                purchaseContract.ExternalContractNumber ?? "",
                purchaseContract
            );
        }

        // Try sales contract
        var salesContract = await _salesContractRepository.GetByIdAsync(contractId, cancellationToken);
        if (salesContract != null)
        {
            return (
                salesContract.ContractNumber.Value,
                salesContract.ExternalContractNumber ?? "",
                salesContract
            );
        }

        throw new NotFoundException($"Contract with ID {contractId} not found");
    }

    private async Task CalculateSettlementQuantitiesAsync(
        ContractSettlement settlement,
        object contractEntity,
        string updatedBy,
        CancellationToken cancellationToken)
    {
        decimal calculationMT = settlement.ActualQuantityMT;
        decimal calculationBBL = settlement.ActualQuantityBBL;
        string calculationNote = "Using actual B/L quantities";

        // For mixed-unit pricing, we might need to use contractual conversion ratios
        // This logic would be expanded based on specific business requirements
        
        if (contractEntity is PurchaseContract purchaseContract)
        {
            // Use contractual conversion ratio if specified in the contract
            if (purchaseContract.TonBarrelRatio > 0)
            {
                // Verify consistency with actual quantities
                var expectedBBL = calculationMT * purchaseContract.TonBarrelRatio;
                var variance = Math.Abs(calculationBBL - expectedBBL) / expectedBBL;
                
                if (variance > 0.05m) // 5% variance threshold
                {
                    calculationNote = $"Using contractual ratio {purchaseContract.TonBarrelRatio:F2} BBL/MT. " +
                                     $"Actual variance: {variance:P2}";
                }
            }
        }
        else if (contractEntity is SalesContract salesContract)
        {
            // Similar logic for sales contracts
            if (salesContract.TonBarrelRatio > 0)
            {
                var expectedBBL = calculationMT * salesContract.TonBarrelRatio;
                var variance = Math.Abs(calculationBBL - expectedBBL) / expectedBBL;
                
                if (variance > 0.05m)
                {
                    calculationNote = $"Using contractual ratio {salesContract.TonBarrelRatio:F2} BBL/MT. " +
                                     $"Actual variance: {variance:P2}";
                }
            }
        }

        settlement.SetCalculationQuantities(calculationMT, calculationBBL, calculationNote, updatedBy);
    }

    private async Task AutoCalculateBenchmarkPricesAsync(
        ContractSettlement settlement,
        object contractEntity,
        string updatedBy,
        CancellationToken cancellationToken)
    {
        try
        {
            PriceFormula? priceFormula = null;
            DateTime? pricingStart = null;
            DateTime? pricingEnd = null;

            // Extract pricing information from contract
            if (contractEntity is PurchaseContract purchaseContract && purchaseContract.PriceFormula != null)
            {
                priceFormula = purchaseContract.PriceFormula;
                pricingStart = purchaseContract.PricingPeriodStart;
                pricingEnd = purchaseContract.PricingPeriodEnd;
            }
            else if (contractEntity is SalesContract salesContract && salesContract.PriceFormula != null)
            {
                priceFormula = salesContract.PriceFormula;
                pricingStart = salesContract.PricingPeriodStart;
                pricingEnd = salesContract.PricingPeriodEnd;
            }

            if (priceFormula == null || !pricingStart.HasValue || !pricingEnd.HasValue)
            {
                _logger.LogWarning("No pricing formula or period found for settlement {SettlementId}", settlement.Id);
                return;
            }

            // Calculate benchmark price based on formula
            if (priceFormula.Method == PricingMethod.Fixed && priceFormula.FixedPrice.HasValue)
            {
                var benchmarkPrice = priceFormula.FixedPrice.Value;
                settlement.UpdateBenchmarkPrice(
                    benchmarkPrice,
                    priceFormula.Formula,
                    pricingStart.Value,
                    pricingEnd.Value,
                    priceFormula.Currency ?? "USD",
                    updatedBy);
            }
            else if (!string.IsNullOrEmpty(priceFormula.IndexName))
            {
                var benchmarkPrice = await CalculateBenchmarkPriceAsync(
                    priceFormula.IndexName,
                    priceFormula.Method,
                    pricingStart.Value,
                    pricingEnd.Value,
                    cancellationToken);

                // Apply adjustments (premium/discount)
                if (priceFormula.Premium != null)
                    benchmarkPrice += priceFormula.Premium.Amount;
                if (priceFormula.Discount != null)
                    benchmarkPrice -= priceFormula.Discount.Amount;

                settlement.UpdateBenchmarkPrice(
                    benchmarkPrice,
                    priceFormula.Formula,
                    pricingStart.Value,
                    pricingEnd.Value,
                    priceFormula.Currency ?? "USD",
                    updatedBy);
            }

            // Calculate amounts based on quantities and prices
            await CalculateSettlementAmountsAsync(settlement, priceFormula, updatedBy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not auto-calculate benchmark prices for settlement {SettlementId}", settlement.Id);
            // Don't throw - this is auto-calculation, manual calculation can be done later
        }
    }

    private async Task CalculateSettlementAmountsAsync(
        ContractSettlement settlement,
        PriceFormula priceFormula,
        string updatedBy)
    {
        decimal benchmarkAmount = 0;
        decimal adjustmentAmount = 0;

        // Determine which quantity to use for calculation based on pricing formula
        var quantityForCalculation = priceFormula.BenchmarkUnit switch
        {
            QuantityUnit.MT => settlement.CalculationQuantityMT,
            QuantityUnit.BBL => settlement.CalculationQuantityBBL,
            _ => settlement.CalculationQuantityMT // Default to MT
        };

        // Calculate benchmark amount
        benchmarkAmount = settlement.BenchmarkPrice * quantityForCalculation;

        // Calculate adjustment amount if specified
        if (priceFormula.Adjustment != null)
        {
            var adjustmentQuantity = priceFormula.AdjustmentUnit switch
            {
                QuantityUnit.MT => settlement.CalculationQuantityMT,
                QuantityUnit.BBL => settlement.CalculationQuantityBBL,
                _ => quantityForCalculation // Use same as benchmark if not specified
            };

            adjustmentAmount = priceFormula.Adjustment.Amount * adjustmentQuantity;
        }

        var cargoValue = benchmarkAmount + adjustmentAmount;

        settlement.UpdateCalculationResults(benchmarkAmount, adjustmentAmount, cargoValue, updatedBy);
    }

    private async Task<ContractSettlementDto> MapToDto(ContractSettlement settlement, CancellationToken cancellationToken = default)
    {
        // Get contract information for the DTO
        var (_, _, contractEntity) = await GetContractInfoAsync(settlement.ContractId, cancellationToken);

        return new ContractSettlementDto
        {
            Id = settlement.Id,
            ContractId = settlement.ContractId,
            ContractNumber = settlement.ContractNumber,
            ExternalContractNumber = settlement.ExternalContractNumber,
            DocumentNumber = settlement.DocumentNumber,
            DocumentType = settlement.DocumentType.ToString(),
            DocumentDate = settlement.DocumentDate,
            ActualQuantityMT = settlement.ActualQuantityMT,
            ActualQuantityBBL = settlement.ActualQuantityBBL,
            CalculationQuantityMT = settlement.CalculationQuantityMT,
            CalculationQuantityBBL = settlement.CalculationQuantityBBL,
            QuantityCalculationNote = settlement.QuantityCalculationNote,
            BenchmarkPrice = settlement.BenchmarkPrice,
            BenchmarkPriceFormula = settlement.BenchmarkPriceFormula,
            PricingStartDate = settlement.PricingStartDate,
            PricingEndDate = settlement.PricingEndDate,
            BenchmarkPriceCurrency = settlement.BenchmarkPriceCurrency,
            BenchmarkAmount = settlement.BenchmarkAmount,
            AdjustmentAmount = settlement.AdjustmentAmount,
            CargoValue = settlement.CargoValue,
            TotalCharges = settlement.TotalCharges,
            TotalSettlementAmount = settlement.TotalSettlementAmount,
            SettlementCurrency = settlement.SettlementCurrency,
            ExchangeRate = settlement.ExchangeRate,
            ExchangeRateNote = settlement.ExchangeRateNote,
            Status = settlement.Status.ToString(),
            IsFinalized = settlement.IsFinalized,
            CreatedDate = settlement.CreatedDate,
            LastModifiedDate = settlement.LastModifiedDate,
            CreatedBy = settlement.CreatedBy,
            LastModifiedBy = settlement.LastModifiedBy,
            FinalizedDate = settlement.FinalizedDate,
            FinalizedBy = settlement.FinalizedBy,
            // Data Lineage - Deal Reference ID & Amendment Chain
            DealReferenceId = settlement.DealReferenceId,
            PreviousSettlementId = settlement.PreviousSettlementId,
            OriginalSettlementId = settlement.OriginalSettlementId,
            SettlementSequence = settlement.SettlementSequence,
            AmendmentType = settlement.AmendmentType.ToString(),
            AmendmentReason = settlement.AmendmentReason,
            IsLatestVersion = settlement.IsLatestVersion,
            SupersededDate = settlement.SupersededDate,
            Charges = settlement.Charges.Select(MapChargeToDto).ToList(),
            // Contract navigation properties would be populated based on contractEntity type
            PurchaseContract = contractEntity is PurchaseContract ? MapPurchaseContractSummary(contractEntity as PurchaseContract) : null,
            SalesContract = contractEntity is SalesContract ? MapSalesContractSummary(contractEntity as SalesContract) : null
        };
    }

    private SettlementChargeDto MapChargeToDto(SettlementCharge charge)
    {
        return new SettlementChargeDto
        {
            Id = charge.Id,
            SettlementId = charge.SettlementId,
            ChargeType = charge.ChargeType.ToString(),
            ChargeTypeDisplayName = GetChargeTypeDisplayName(charge.ChargeType),
            Description = charge.Description,
            Amount = charge.Amount,
            Currency = charge.Currency,
            IncurredDate = charge.IncurredDate,
            ReferenceDocument = charge.ReferenceDocument,
            Notes = charge.Notes,
            CreatedDate = charge.CreatedDate,
            CreatedBy = charge.CreatedBy
        };
    }

    private string GetChargeTypeDisplayName(ChargeType chargeType)
    {
        return chargeType switch
        {
            ChargeType.Demurrage => "Demurrage",
            ChargeType.Despatch => "Despatch",
            ChargeType.InspectionFee => "Inspection Fee",
            ChargeType.PortCharges => "Port Charges",
            ChargeType.FreightCost => "Freight Cost",
            ChargeType.InsurancePremium => "Insurance Premium",
            ChargeType.BankCharges => "Bank Charges",
            ChargeType.StorageFee => "Storage Fee",
            ChargeType.AgencyFee => "Agency Fee",
            ChargeType.Other => "Other",
            _ => chargeType.ToString()
        };
    }

    private PurchaseContractSummaryDto? MapPurchaseContractSummary(PurchaseContract? contract)
    {
        if (contract == null) return null;

        return new PurchaseContractSummaryDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber.Value,
            Status = contract.Status,
            SupplierName = contract.TradingPartner?.Name ?? "",
            Quantity = contract.ContractQuantity.Value,
            QuantityUnit = contract.ContractQuantity.Unit,
            ContractValue = contract.ContractValue?.Amount,
            LaycanStart = contract.LaycanStart,
            LaycanEnd = contract.LaycanEnd,
            CreatedAt = contract.CreatedAt
        };
    }

    private SalesContractSummaryDto? MapSalesContractSummary(SalesContract? contract)
    {
        if (contract == null) return null;

        return new SalesContractSummaryDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber.Value,
            ExternalContractNumber = contract.ExternalContractNumber,
            Status = contract.Status,
            CustomerName = contract.TradingPartner?.Name ?? "",
            Quantity = contract.ContractQuantity.Value,
            QuantityUnit = contract.ContractQuantity.Unit,
            ContractValue = contract.ContractValue?.Amount,
            LaycanStart = contract.LaycanStart,
            LaycanEnd = contract.LaycanEnd,
            CreatedAt = contract.CreatedAt
        };
    }

    private static decimal CalculateWeightedAverage(decimal[] prices)
    {
        // Simple implementation - in practice, you'd use actual weights
        return prices.Average();
    }

    private static decimal CalculateMedian(decimal[] prices)
    {
        var sorted = prices.OrderBy(x => x).ToArray();
        var mid = sorted.Length / 2;
        
        return sorted.Length % 2 == 0 
            ? (sorted[mid - 1] + sorted[mid]) / 2 
            : sorted[mid];
    }

    private static decimal CalculateMode(decimal[] prices)
    {
        return prices
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }

    /// <summary>
    /// NEW: Get settlement price using unified IPricingService for ProductId-based lookups.
    /// This method demonstrates the new pricing service integration supporting ContractMonth filtering.
    ///
    /// Migration Path:
    /// - Legacy pricing: Direct IMarketDataRepository.GetByProductAsync(productCode, startDate, endDate)
    /// - New pricing: IPricingService.GetSettlementPriceAsync(productId, contractMonth, priceDate, priceType)
    ///
    /// Benefits:
    /// - Type-safe ProductId instead of string productCode
    /// - ContractMonth support for futures/derivatives pricing
    /// - Unified entry point for all pricing queries across Settlement, Risk, Position modules
    /// - Comprehensive logging and error handling
    /// </summary>
    public async Task<decimal> GetSettlementPriceAsync(
        Guid productId,
        string contractMonth,
        DateTime priceDate,
        MarketPriceType priceType = MarketPriceType.Spot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting settlement price via IPricingService: ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}, Type={Type}",
                productId, contractMonth, priceDate.Date, priceType);

            var price = await _pricingService.GetSettlementPriceAsync(
                productId,
                contractMonth,
                priceDate,
                priceType,
                cancellationToken);

            if (price == null)
            {
                _logger.LogWarning(
                    "No settlement price found: ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}",
                    productId, contractMonth, priceDate.Date);
                throw new InvalidOperationException(
                    $"No price data found for ProductId {productId}, ContractMonth {contractMonth} on {priceDate:yyyy-MM-dd}");
            }

            _logger.LogInformation(
                "Settlement price retrieved: {PriceValue} {Currency}",
                price.Price, price.Currency);

            return price.Price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving settlement price: ProductId={ProductId}, ContractMonth={ContractMonth}, Date={Date}",
                productId, contractMonth, priceDate.Date);
            throw;
        }
    }

    #endregion

    #region Amendment Chain Methods (Data Lineage Enhancement)

    /// <inheritdoc />
    public async Task<ContractSettlementDto> CreateAmendmentAsync(
        Guid originalSettlementId,
        SettlementAmendmentType amendmentType,
        string amendmentReason,
        string createdBy = "System",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating amendment for settlement {SettlementId} with type {AmendmentType}",
                originalSettlementId, amendmentType);

            // Get the settlement to amend (must be the latest version)
            var previousSettlement = await _settlementRepository.GetByIdAsync(originalSettlementId, cancellationToken);
            if (previousSettlement == null)
                throw new NotFoundException($"Settlement {originalSettlementId} not found");

            if (!previousSettlement.IsLatestVersion)
                throw new DomainException("Cannot amend a superseded settlement. Please use the latest version.");

            // Create a new settlement as an amendment
            var newSettlement = new ContractSettlement(
                previousSettlement.ContractId,
                previousSettlement.ContractNumber,
                previousSettlement.ExternalContractNumber,
                previousSettlement.DocumentNumber,
                previousSettlement.DocumentType,
                previousSettlement.DocumentDate,
                createdBy);

            // Copy over quantities from previous settlement
            newSettlement.UpdateActualQuantities(
                previousSettlement.ActualQuantityMT,
                previousSettlement.ActualQuantityBBL,
                createdBy);

            newSettlement.SetCalculationQuantities(
                previousSettlement.CalculationQuantityMT,
                previousSettlement.CalculationQuantityBBL,
                previousSettlement.QuantityCalculationNote ?? "",
                createdBy);

            // Initialize amendment chain
            var originalId = previousSettlement.OriginalSettlementId ?? previousSettlement.Id;
            newSettlement.InitializeAsAmendment(
                previousSettlement.Id,
                originalId,
                previousSettlement.SettlementSequence,
                amendmentType,
                amendmentReason,
                createdBy);

            // Propagate Deal Reference ID
            if (!string.IsNullOrEmpty(previousSettlement.DealReferenceId))
            {
                newSettlement.SetDealReferenceId(previousSettlement.DealReferenceId, createdBy);
            }

            // Mark the previous settlement as superseded
            previousSettlement.MarkAsSuperseded(createdBy);

            // Save the new settlement
            var savedSettlement = await _settlementRepository.AddAsync(newSettlement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created amendment settlement {NewSettlementId} (sequence {Sequence}) for original {OriginalId}",
                savedSettlement.Id, savedSettlement.SettlementSequence, originalId);

            return await MapToDto(savedSettlement, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating amendment for settlement {SettlementId}", originalSettlementId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContractSettlementDto>> GetAmendmentChainAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting amendment chain for settlement {SettlementId}", settlementId);

            // First, get the settlement to find the original
            var settlement = await _settlementRepository.GetByIdAsync(settlementId, cancellationToken);
            if (settlement == null)
                throw new NotFoundException($"Settlement {settlementId} not found");

            var originalId = settlement.OriginalSettlementId ?? settlement.Id;

            // Get all settlements for the same contract
            var allSettlements = await _settlementRepository.GetByContractIdAsync(
                settlement.ContractId, cancellationToken);

            // Filter to only those in the same chain (sharing the same original ID)
            var chainSettlements = allSettlements
                .Where(s => (s.OriginalSettlementId ?? s.Id) == originalId)
                .OrderBy(s => s.SettlementSequence)
                .ToList();

            var dtos = new List<ContractSettlementDto>();
            foreach (var s in chainSettlements)
            {
                dtos.Add(await MapToDto(s, cancellationToken));
            }

            _logger.LogInformation(
                "Found {Count} settlements in amendment chain for original {OriginalId}",
                dtos.Count, originalId);

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting amendment chain for settlement {SettlementId}", settlementId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ContractSettlementDto?> GetLatestVersionAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting latest version for settlement {SettlementId}", settlementId);

            var chain = await GetAmendmentChainAsync(settlementId, cancellationToken);
            var latest = chain.FirstOrDefault(s => s.IsLatestVersion);

            if (latest != null)
            {
                _logger.LogInformation(
                    "Latest version is {LatestId} (sequence {Sequence})",
                    latest.Id, latest.SettlementSequence);
            }

            return latest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest version for settlement {SettlementId}", settlementId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ContractSettlementDto?> GetOriginalSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting original settlement for {SettlementId}", settlementId);

            var chain = await GetAmendmentChainAsync(settlementId, cancellationToken);
            var original = chain.FirstOrDefault(s => s.SettlementSequence == 1);

            if (original != null)
            {
                _logger.LogInformation("Original settlement is {OriginalId}", original.Id);
            }

            return original;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting original settlement for {SettlementId}", settlementId);
            throw;
        }
    }

    #endregion
}