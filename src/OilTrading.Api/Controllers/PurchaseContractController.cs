using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.PurchaseContracts;
using OilTrading.Application.Queries.PurchaseContracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Application.Services;
using OilTrading.Api.Attributes;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/purchase-contracts")]
[Produces("application/json")]
public class PurchaseContractController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PurchaseContractController> _logger;
    private readonly IPaymentStatusCalculationService _paymentStatusService;
    private readonly IPurchaseContractRepository _purchaseContractRepository;

    public PurchaseContractController(
        IMediator mediator,
        ILogger<PurchaseContractController> logger,
        IPaymentStatusCalculationService paymentStatusService,
        IPurchaseContractRepository purchaseContractRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _paymentStatusService = paymentStatusService ?? throw new ArgumentNullException(nameof(paymentStatusService));
        _purchaseContractRepository = purchaseContractRepository ?? throw new ArgumentNullException(nameof(purchaseContractRepository));
    }

    private string GetCurrentUserName()
    {
        try
        {
            return User?.Identity?.Name ?? 
                   HttpContext?.User?.Identity?.Name ?? 
                   "System";
        }
        catch
        {
            return "System";
        }
    }

    /// <summary>
    /// Creates a new purchase contract
    /// </summary>
    /// <param name="dto">The purchase contract details</param>
    /// <returns>The ID of the created contract</returns>
    [HttpPost]
    [RiskCheck(RiskCheckLevel.Enhanced, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseContractDto dto)
    {
        var command = new CreatePurchaseContractCommand
        {
            ExternalContractNumber = dto.ExternalContractNumber,
            ContractType = dto.ContractType,
            SupplierId = dto.SupplierId,
            ProductId = dto.ProductId,
            TraderId = dto.TraderId,
            Quantity = dto.Quantity,
            QuantityUnit = dto.QuantityUnit,
            TonBarrelRatio = dto.TonBarrelRatio,
            PriceBenchmarkId = dto.PriceBenchmarkId,
            PricingType = dto.PricingType,
            FixedPrice = dto.FixedPrice,
            PricingFormula = dto.PricingFormula,
            PricingPeriodStart = dto.PricingPeriodStart,
            PricingPeriodEnd = dto.PricingPeriodEnd,
            DeliveryTerms = dto.DeliveryTerms,
            LaycanStart = dto.LaycanStart,
            LaycanEnd = dto.LaycanEnd,
            LoadPort = dto.LoadPort,
            DischargePort = dto.DischargePort,
            SettlementType = dto.SettlementType,
            CreditPeriodDays = dto.CreditPeriodDays,
            PrepaymentPercentage = dto.PrepaymentPercentage,
            PaymentTerms = dto.PaymentTerms,
            QualitySpecifications = dto.QualitySpecifications,
            InspectionAgency = dto.InspectionAgency,
            Notes = dto.Notes,
            CreatedBy = GetCurrentUserName()
        };

        var contractId = await _mediator.Send(command);
        
        _logger.LogInformation("Purchase contract {ContractId} created successfully", contractId);
        
        return CreatedAtAction(nameof(GetById), new { id = contractId }, contractId);
    }

    /// <summary>
    /// Gets a paginated list of purchase contracts
    /// </summary>
    /// <param name="query">Filter and pagination parameters</param>
    /// <returns>Paginated list of purchase contracts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseContractListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] GetPurchaseContractsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a purchase contract by ID
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <returns>The purchase contract details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetPurchaseContractByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets purchase contracts by external contract number
    /// </summary>
    /// <param name="externalContractNumber">The external contract number</param>
    /// <returns>Matching purchase contracts</returns>
    [HttpGet("by-external/{externalContractNumber}")]
    [ProducesResponseType(typeof(PagedResult<PurchaseContractListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByExternalContractNumber(string externalContractNumber)
    {
        var query = new GetPurchaseContractsQuery
        {
            ExternalContractNumber = externalContractNumber,
            Page = 1,
            PageSize = 10
        };
        var result = await _mediator.Send(query);

        if (!result.Items.Any())
        {
            return NotFound($"No purchase contracts found with external contract number: {externalContractNumber}");
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing purchase contract
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <param name="dto">The updated contract details</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{id:guid}")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseContractDto dto)
    {
        var command = new UpdatePurchaseContractCommand
        {
            Id = id,
            ExternalContractNumber = dto.ExternalContractNumber,
            PriceBenchmarkId = dto.PriceBenchmarkId,
            Quantity = dto.Quantity,
            QuantityUnit = dto.QuantityUnit?.ToString(),
            TonBarrelRatio = dto.TonBarrelRatio,
            PricingType = dto.PricingType?.ToString(),
            FixedPrice = dto.FixedPrice,
            PricingFormula = dto.PricingFormula,
            PricingPeriodStart = dto.PricingPeriodStart,
            PricingPeriodEnd = dto.PricingPeriodEnd,
            DeliveryTerms = dto.DeliveryTerms?.ToString(),
            LaycanStart = dto.LaycanStart,
            LaycanEnd = dto.LaycanEnd,
            LoadPort = dto.LoadPort,
            DischargePort = dto.DischargePort,
            SettlementType = dto.SettlementType?.ToString(),
            CreditPeriodDays = dto.CreditPeriodDays,
            PrepaymentPercentage = dto.PrepaymentPercentage,
            PaymentTerms = dto.PaymentTerms,
            QualitySpecifications = dto.QualitySpecifications,
            InspectionAgency = dto.InspectionAgency,
            Notes = dto.Notes,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Purchase contract {ContractId} updated successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Activates a purchase contract
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/activate")]
    [RiskCheck(RiskCheckLevel.Critical, allowOverride: false)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var command = new ActivatePurchaseContractCommand
        {
            Id = id,
            ActivatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Purchase contract {ContractId} activated successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Creates a linked sales contract for a purchase contract
    /// </summary>
    /// <param name="id">The purchase contract ID</param>
    /// <param name="dto">The sales contract details</param>
    /// <returns>The ID of the created sales contract</returns>
    [HttpPost("{id:guid}/sales-contracts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateLinkedSales(Guid id, [FromBody] CreateSalesContractDto dto)
    {
        // This would require implementing CreateSalesContractCommand
        // For now, return NotImplemented
        return StatusCode(StatusCodes.Status501NotImplemented, "Linked sales contract creation not yet implemented");
    }

    /// <summary>
    /// Gets the available quantity for a purchase contract (not yet allocated to sales contracts)
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <returns>Available quantity details</returns>
    [HttpGet("{id:guid}/available-quantity")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableQuantity(Guid id)
    {
        // This would require a specific query implementation
        // For now, return basic info from the contract
        var query = new GetPurchaseContractByIdQuery(id);
        var contract = await _mediator.Send(query);
        
        // Calculate available quantity (total - allocated to sales contracts)
        var allocatedQuantity = contract.LinkedSalesContracts.Sum(sc => sc.Quantity);
        var availableQuantity = contract.Quantity - allocatedQuantity;
        
        var result = new
        {
            ContractId = id,
            TotalQuantity = contract.Quantity,
            AllocatedQuantity = allocatedQuantity,
            AvailableQuantity = availableQuantity,
            QuantityUnit = contract.QuantityUnit,
            LinkedSalesContractsCount = contract.LinkedSalesContracts.Count
        };
        
        return Ok(result);
    }

    /// <summary>
    /// Gets the current payment status for a purchase contract
    /// </summary>
    /// <param name="id">Purchase contract ID</param>
    /// <returns>Current ContractPaymentStatus</returns>
    [HttpGet("{id:guid}/payment-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(Guid id)
    {
        var paymentStatus = await _paymentStatusService.CalculatePurchaseContractPaymentStatusAsync(id);

        return Ok(new
        {
            contractId = id,
            paymentStatus = paymentStatus?.ToString() ?? "Unknown"
        });
    }

    /// <summary>
    /// Gets comprehensive payment status details for a purchase contract
    /// </summary>
    /// <param name="id">Purchase contract ID</param>
    /// <returns>PaymentStatusDetailsDto with amounts, dates, settlement breakdown</returns>
    [HttpGet("{id:guid}/payment-status/details")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatusDetails(Guid id)
    {
        var details = await _paymentStatusService.GetPaymentStatusDetailsAsync(id, isPurchaseContract: true);

        return Ok(details);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRICING STATUS ENDPOINTS (Data Lineage Enhancement v2.18.0)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the current pricing status for a purchase contract.
    /// Returns detailed pricing information including fixed percentage, price source, and pricing dates.
    /// </summary>
    /// <param name="id">Purchase contract ID</param>
    /// <returns>Pricing status details</returns>
    [HttpGet("{id:guid}/pricing-status")]
    [ProducesResponseType(typeof(ContractPricingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractPricingStatusDto>> GetPricingStatus(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting pricing status for purchase contract {ContractId}", id);

            var contract = await _purchaseContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { error = $"Purchase contract {id} not found" });
            }

            var result = new ContractPricingStatusDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber?.Value ?? "",
                PricingStatus = contract.PricingStatus.ToString(),
                PricingStatusValue = (int)contract.PricingStatus,
                FixedPercentage = contract.FixedPercentage,
                FixedQuantity = contract.FixedQuantity,
                TotalQuantity = contract.ContractQuantity?.Value ?? 0,
                UnfixedQuantity = (contract.ContractQuantity?.Value ?? 0) - contract.FixedQuantity,
                PriceSource = contract.PriceSource.ToString(),
                PriceSourceValue = (int)contract.PriceSource,
                LastPricingDate = contract.LastPricingDate,
                PricingPeriodStart = contract.PricingPeriodStart,
                PricingPeriodEnd = contract.PricingPeriodEnd,
                IsPriceFinalized = contract.IsPriceFinalized,
                CanUpdatePricing = !contract.IsPriceFinalized && contract.Status != Core.Entities.ContractStatus.Completed
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing status for contract {ContractId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving pricing status: " + ex.Message });
        }
    }

    /// <summary>
    /// Updates the pricing status for a purchase contract.
    /// Records a pricing event with quantity fixed and price source.
    /// </summary>
    /// <param name="id">Purchase contract ID</param>
    /// <param name="request">Pricing update details</param>
    /// <returns>Updated pricing status</returns>
    [HttpPost("{id:guid}/pricing-status")]
    [ProducesResponseType(typeof(ContractPricingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractPricingStatusDto>> UpdatePricingStatus(
        Guid id,
        [FromBody] UpdatePricingStatusRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Updating pricing status for contract {ContractId}: FixedQuantity={FixedQuantity}, Source={Source}",
                id, request.FixedQuantity, request.PriceSource);

            var contract = await _purchaseContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { error = $"Purchase contract {id} not found" });
            }

            if (contract.IsPriceFinalized)
            {
                return BadRequest(new { error = "Cannot update pricing status for a finalized contract" });
            }

            if (contract.Status == Core.Entities.ContractStatus.Completed)
            {
                return BadRequest(new { error = "Cannot update pricing status for a completed contract" });
            }

            var priceSource = (PriceSourceType)request.PriceSource;
            var updatedBy = request.UpdatedBy ?? GetCurrentUserName();

            contract.UpdatePricingStatus(request.FixedQuantity, priceSource, updatedBy);
            await _purchaseContractRepository.UpdateAsync(contract);

            _logger.LogInformation(
                "Updated pricing status for contract {ContractId}: Status={Status}, FixedPct={FixedPct}%",
                id, contract.PricingStatus, contract.FixedPercentage);

            var result = new ContractPricingStatusDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber?.Value ?? "",
                PricingStatus = contract.PricingStatus.ToString(),
                PricingStatusValue = (int)contract.PricingStatus,
                FixedPercentage = contract.FixedPercentage,
                FixedQuantity = contract.FixedQuantity,
                TotalQuantity = contract.ContractQuantity?.Value ?? 0,
                UnfixedQuantity = (contract.ContractQuantity?.Value ?? 0) - contract.FixedQuantity,
                PriceSource = contract.PriceSource.ToString(),
                PriceSourceValue = (int)contract.PriceSource,
                LastPricingDate = contract.LastPricingDate,
                PricingPeriodStart = contract.PricingPeriodStart,
                PricingPeriodEnd = contract.PricingPeriodEnd,
                IsPriceFinalized = contract.IsPriceFinalized,
                CanUpdatePricing = !contract.IsPriceFinalized && contract.Status != Core.Entities.ContractStatus.Completed
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing status for contract {ContractId}", id);
            return StatusCode(500, new { error = "An error occurred while updating pricing status: " + ex.Message });
        }
    }

    /// <summary>
    /// Gets purchase contracts filtered by pricing status.
    /// </summary>
    /// <param name="status">Pricing status filter (1=Unpriced, 2=PartiallyPriced, 3=FullyPriced)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <returns>Paginated list of contracts with the specified pricing status</returns>
    [HttpGet("by-pricing-status/{status:int}")]
    [ProducesResponseType(typeof(PagedResult<PurchaseContractListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PurchaseContractListDto>>> GetByPricingStatus(
        int status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (status < 1 || status > 3)
            {
                return BadRequest(new { error = "Invalid pricing status. Valid values: 1=Unpriced, 2=PartiallyPriced, 3=FullyPriced" });
            }

            var pricingStatus = (ContractPricingStatus)status;
            _logger.LogInformation("Getting purchase contracts with pricing status {Status}", pricingStatus);

            // Use the standard query and filter results
            var query = new GetPurchaseContractsQuery
            {
                Page = page,
                PageSize = pageSize * 10 // Fetch more to filter
            };

            var allContracts = await _mediator.Send(query);
            var filteredItems = allContracts.Items
                .Where(c => c.PricingStatus == pricingStatus.ToString())
                .Take(pageSize)
                .ToList();

            // Use fully-qualified name to avoid conflict with SettlementController's local PagedResult
            var result = new OilTrading.Application.Common.PagedResult<PurchaseContractListDto>(
                filteredItems,
                filteredItems.Count,
                page,
                pageSize);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contracts by pricing status {Status}", status);
            return StatusCode(500, new { error = "An error occurred while retrieving contracts: " + ex.Message });
        }
    }

    /// <summary>
    /// Resets the pricing status for a contract to Unpriced.
    /// Use with caution - this clears the fixed quantity and percentage.
    /// </summary>
    /// <param name="id">Purchase contract ID</param>
    /// <returns>Updated pricing status</returns>
    [HttpPost("{id:guid}/pricing-status/reset")]
    [ProducesResponseType(typeof(ContractPricingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractPricingStatusDto>> ResetPricingStatus(Guid id)
    {
        try
        {
            _logger.LogWarning("Resetting pricing status for contract {ContractId}", id);

            var contract = await _purchaseContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { error = $"Purchase contract {id} not found" });
            }

            if (contract.IsPriceFinalized)
            {
                return BadRequest(new { error = "Cannot reset pricing status for a finalized contract" });
            }

            if (contract.Status == Core.Entities.ContractStatus.Completed)
            {
                return BadRequest(new { error = "Cannot reset pricing status for a completed contract" });
            }

            contract.ResetPricingStatus(GetCurrentUserName());
            await _purchaseContractRepository.UpdateAsync(contract);

            _logger.LogInformation("Reset pricing status for contract {ContractId}", id);

            var result = new ContractPricingStatusDto
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber?.Value ?? "",
                PricingStatus = contract.PricingStatus.ToString(),
                PricingStatusValue = (int)contract.PricingStatus,
                FixedPercentage = contract.FixedPercentage,
                FixedQuantity = contract.FixedQuantity,
                TotalQuantity = contract.ContractQuantity?.Value ?? 0,
                UnfixedQuantity = (contract.ContractQuantity?.Value ?? 0) - contract.FixedQuantity,
                PriceSource = contract.PriceSource.ToString(),
                PriceSourceValue = (int)contract.PriceSource,
                LastPricingDate = contract.LastPricingDate,
                PricingPeriodStart = contract.PricingPeriodStart,
                PricingPeriodEnd = contract.PricingPeriodEnd,
                IsPriceFinalized = contract.IsPriceFinalized,
                CanUpdatePricing = true
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting pricing status for contract {ContractId}", id);
            return StatusCode(500, new { error = "An error occurred while resetting pricing status: " + ex.Message });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PRICING STATUS DTOs (Data Lineage Enhancement v2.18.0)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO for contract pricing status information
/// </summary>
public class ContractPricingStatusDto
{
    /// <summary>Contract ID</summary>
    public Guid ContractId { get; set; }

    /// <summary>Contract number</summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>Current pricing status (Unpriced, PartiallyPriced, FullyPriced)</summary>
    public string PricingStatus { get; set; } = "Unpriced";

    /// <summary>Pricing status as integer (1=Unpriced, 2=PartiallyPriced, 3=FullyPriced)</summary>
    public int PricingStatusValue { get; set; } = 1;

    /// <summary>Percentage of contract quantity that has been priced (0-100)</summary>
    public decimal FixedPercentage { get; set; }

    /// <summary>Quantity that has been priced</summary>
    public decimal FixedQuantity { get; set; }

    /// <summary>Total contract quantity</summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>Quantity remaining to be priced</summary>
    public decimal UnfixedQuantity { get; set; }

    /// <summary>Source of the price (Manual, MarketData, Formula, Estimate, Import)</summary>
    public string PriceSource { get; set; } = "Manual";

    /// <summary>Price source as integer</summary>
    public int PriceSourceValue { get; set; } = 1;

    /// <summary>Date of last pricing update</summary>
    public DateTime? LastPricingDate { get; set; }

    /// <summary>Start of pricing period for floating prices</summary>
    public DateTime? PricingPeriodStart { get; set; }

    /// <summary>End of pricing period for floating prices</summary>
    public DateTime? PricingPeriodEnd { get; set; }

    /// <summary>Whether the price has been finalized (no further changes allowed)</summary>
    public bool IsPriceFinalized { get; set; }

    /// <summary>Whether pricing can still be updated</summary>
    public bool CanUpdatePricing { get; set; }
}

/// <summary>
/// Request DTO for updating pricing status
/// </summary>
public class UpdatePricingStatusRequestDto
{
    /// <summary>
    /// Quantity that has been priced (cumulative, not incremental)
    /// </summary>
    public decimal FixedQuantity { get; set; }

    /// <summary>
    /// Source of the price:
    /// 1 = Manual entry
    /// 2 = Market data feed
    /// 3 = Formula calculation
    /// 4 = System estimate
    /// 5 = External import
    /// </summary>
    public int PriceSource { get; set; } = 1;

    /// <summary>
    /// User performing the update
    /// </summary>
    public string? UpdatedBy { get; set; }
}