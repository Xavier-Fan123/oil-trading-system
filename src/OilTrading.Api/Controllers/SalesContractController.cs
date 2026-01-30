using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.SalesContracts;
using OilTrading.Application.Queries.SalesContracts;
using OilTrading.Application.Queries.PurchaseContracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Application.Services;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/sales-contracts")]
[Produces("application/json")]
public class SalesContractController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SalesContractController> _logger;
    private readonly IPaymentStatusCalculationService _paymentStatusService;
    private readonly ISalesContractRepository _salesContractRepository;

    public SalesContractController(
        IMediator mediator,
        ILogger<SalesContractController> logger,
        IPaymentStatusCalculationService paymentStatusService,
        ISalesContractRepository salesContractRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _paymentStatusService = paymentStatusService ?? throw new ArgumentNullException(nameof(paymentStatusService));
        _salesContractRepository = salesContractRepository ?? throw new ArgumentNullException(nameof(salesContractRepository));
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
    /// Creates a new sales contract
    /// </summary>
    /// <param name="dto">The sales contract details</param>
    /// <returns>The ID of the created contract</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateSalesContractDto dto)
    {
        var command = new CreateSalesContractCommand
        {
            ExternalContractNumber = dto.ExternalContractNumber,
            ContractType = dto.ContractType,
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            TraderId = dto.TraderId,
            LinkedPurchaseContractId = dto.LinkedPurchaseContractId,
            Quantity = dto.Quantity,
            QuantityUnit = dto.QuantityUnit,
            TonBarrelRatio = dto.TonBarrelRatio,
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
        
        _logger.LogInformation("Sales contract {ContractId} created successfully", contractId);
        
        return CreatedAtAction(nameof(GetById), new { id = contractId }, contractId);
    }

    /// <summary>
    /// Gets a paginated list of sales contracts
    /// </summary>
    /// <returns>Paginated list of sales contracts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SalesContractSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] GetSalesContractsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a sales contract by ID
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <returns>The sales contract details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SalesContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetSalesContractByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets sales contracts by external contract number
    /// </summary>
    /// <param name="externalContractNumber">The external contract number</param>
    /// <returns>Matching sales contracts</returns>
    [HttpGet("by-external/{externalContractNumber}")]
    [ProducesResponseType(typeof(PagedResult<SalesContractSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByExternalContractNumber(string externalContractNumber)
    {
        var query = new GetSalesContractsQuery
        {
            ExternalContractNumber = externalContractNumber,
            Page = 1,
            PageSize = 10
        };
        var result = await _mediator.Send(query);

        if (!result.Items.Any())
        {
            return NotFound($"No sales contracts found with external contract number: {externalContractNumber}");
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing sales contract
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <param name="dto">The updated contract details</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalesContractDto dto)
    {
        var command = new UpdateSalesContractCommand
        {
            Id = id,
            ExternalContractNumber = dto.ExternalContractNumber,
            Quantity = dto.Quantity,
            QuantityUnit = dto.QuantityUnit,
            TonBarrelRatio = dto.TonBarrelRatio,
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
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {ContractId} updated successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Activates a sales contract
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var command = new ActivateSalesContractCommand { Id = id };
        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {ContractId} activated successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Links a sales contract to a purchase contract
    /// </summary>
    /// <param name="id">The sales contract ID</param>
    /// <param name="purchaseContractId">The purchase contract ID to link</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/link-purchase/{purchaseContractId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkToPurchaseContract(Guid id, Guid purchaseContractId)
    {
        var command = new LinkSalesContractToPurchaseCommand
        {
            SalesContractId = id,
            PurchaseContractId = purchaseContractId
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {SalesContractId} linked to purchase contract {PurchaseContractId}", 
            id, purchaseContractId);
        
        return NoContent();
    }

    /// <summary>
    /// Unlinks a sales contract from its purchase contract
    /// </summary>
    /// <param name="id">The sales contract ID</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/unlink-purchase")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlinkFromPurchaseContract(Guid id)
    {
        var command = new UnlinkSalesContractFromPurchaseCommand { SalesContractId = id };
        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {SalesContractId} unlinked from purchase contract", id);
        
        return NoContent();
    }

    /// <summary>
    /// Gets available purchase contracts that can be linked to sales contracts
    /// </summary>
    /// <param name="query">Filter and pagination parameters</param>
    /// <returns>Paginated list of available purchase contracts</returns>
    [HttpGet("available-purchase-contracts")]
    [ProducesResponseType(typeof(PagedResult<AvailablePurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailablePurchaseContracts([FromQuery] GetAvailablePurchaseContractsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets sales contracts summary statistics
    /// </summary>
    /// <returns>Summary of sales contracts including totals and breakdowns</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SalesContractsSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var query = new GetSalesContractSummaryQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Approves a sales contract
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <param name="dto">Approval details</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveSalesContractDto? dto = null)
    {
        var command = new ApproveSalesContractCommand
        {
            Id = id,
            Comments = dto?.Comments,
            ApprovedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {ContractId} approved successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Rejects a sales contract
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <param name="dto">Rejection details including reason</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectSalesContractDto dto)
    {
        var command = new RejectSalesContractCommand
        {
            Id = id,
            Reason = dto.Reason,
            Comments = dto.Comments,
            RejectedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {ContractId} rejected successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Deletes a sales contract (only for draft contracts)
    /// </summary>
    /// <param name="id">The contract ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteSalesContractCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Sales contract {ContractId} deleted successfully", id);

        return NoContent();
    }

    /// <summary>
    /// Gets the current payment status for a sales contract
    /// </summary>
    /// <param name="id">Sales contract ID</param>
    /// <returns>Current ContractPaymentStatus</returns>
    [HttpGet("{id:guid}/payment-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(Guid id)
    {
        var paymentStatus = await _paymentStatusService.CalculateSalesContractPaymentStatusAsync(id);

        return Ok(new
        {
            contractId = id,
            paymentStatus = paymentStatus?.ToString() ?? "Unknown"
        });
    }

    /// <summary>
    /// Gets comprehensive payment status details for a sales contract
    /// </summary>
    /// <param name="id">Sales contract ID</param>
    /// <returns>PaymentStatusDetailsDto with amounts, dates, settlement breakdown</returns>
    [HttpGet("{id:guid}/payment-status/details")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatusDetails(Guid id)
    {
        var details = await _paymentStatusService.GetPaymentStatusDetailsAsync(id, isPurchaseContract: false);

        return Ok(details);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRICING STATUS ENDPOINTS (Data Lineage Enhancement v2.18.0)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the current pricing status for a sales contract.
    /// Returns detailed pricing information including fixed percentage, price source, and pricing dates.
    /// </summary>
    /// <param name="id">Sales contract ID</param>
    /// <returns>Pricing status details</returns>
    [HttpGet("{id:guid}/pricing-status")]
    [ProducesResponseType(typeof(SalesContractPricingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SalesContractPricingStatusDto>> GetPricingStatus(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting pricing status for sales contract {ContractId}", id);

            var contract = await _salesContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { error = $"Sales contract {id} not found" });
            }

            var result = new SalesContractPricingStatusDto
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
            _logger.LogError(ex, "Error getting pricing status for sales contract {ContractId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving pricing status: " + ex.Message });
        }
    }

    /// <summary>
    /// Updates the pricing status for a sales contract.
    /// Records a pricing event with quantity fixed and price source.
    /// </summary>
    /// <param name="id">Sales contract ID</param>
    /// <param name="request">Pricing update details</param>
    /// <returns>Updated pricing status</returns>
    [HttpPost("{id:guid}/pricing-status")]
    [ProducesResponseType(typeof(SalesContractPricingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SalesContractPricingStatusDto>> UpdatePricingStatus(
        Guid id,
        [FromBody] UpdateSalesPricingStatusRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Updating pricing status for sales contract {ContractId}: FixedQuantity={FixedQuantity}, Source={Source}",
                id, request.FixedQuantity, request.PriceSource);

            var contract = await _salesContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { error = $"Sales contract {id} not found" });
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
            await _salesContractRepository.UpdateAsync(contract);

            _logger.LogInformation(
                "Updated pricing status for sales contract {ContractId}: Status={Status}, FixedPct={FixedPct}%",
                id, contract.PricingStatus, contract.FixedPercentage);

            var result = new SalesContractPricingStatusDto
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
            _logger.LogError(ex, "Error updating pricing status for sales contract {ContractId}", id);
            return StatusCode(500, new { error = "An error occurred while updating pricing status: " + ex.Message });
        }
    }

    /// <summary>
    /// Gets sales contracts filtered by pricing status.
    /// </summary>
    /// <param name="status">Pricing status filter (1=Unpriced, 2=PartiallyPriced, 3=FullyPriced)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <returns>Paginated list of contracts with the specified pricing status</returns>
    [HttpGet("by-pricing-status/{status:int}")]
    [ProducesResponseType(typeof(PagedResult<SalesContractSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SalesContractSummaryDto>>> GetByPricingStatus(
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
            _logger.LogInformation("Getting sales contracts with pricing status {Status}", pricingStatus);

            // Use the standard query and filter by pricing status
            var query = new GetSalesContractsQuery
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
            var result = new OilTrading.Application.Common.PagedResult<SalesContractSummaryDto>(
                filteredItems,
                filteredItems.Count,
                page,
                pageSize);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales contracts by pricing status {Status}", status);
            return StatusCode(500, new { error = "An error occurred while retrieving contracts: " + ex.Message });
        }
    }

    /// <summary>
    /// Resets the pricing status for a sales contract to Unpriced.
    /// Use with caution - this clears the fixed quantity and percentage.
    /// </summary>
    /// <param name="id">Sales contract ID</param>
    /// <returns>Updated pricing status</returns>
    [HttpPost("{id:guid}/pricing-status/reset")]
    [ProducesResponseType(typeof(SalesContractPricingStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SalesContractPricingStatusDto>> ResetPricingStatus(Guid id)
    {
        try
        {
            _logger.LogWarning("Resetting pricing status for sales contract {ContractId}", id);

            var contract = await _salesContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                return NotFound(new { error = $"Sales contract {id} not found" });
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
            await _salesContractRepository.UpdateAsync(contract);

            _logger.LogInformation("Reset pricing status for sales contract {ContractId}", id);

            var result = new SalesContractPricingStatusDto
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
            _logger.LogError(ex, "Error resetting pricing status for sales contract {ContractId}", id);
            return StatusCode(500, new { error = "An error occurred while resetting pricing status: " + ex.Message });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SALES CONTRACT PRICING STATUS DTOs (Data Lineage Enhancement v2.18.0)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO for sales contract pricing status information
/// </summary>
public class SalesContractPricingStatusDto
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
/// Request DTO for updating sales contract pricing status
/// </summary>
public class UpdateSalesPricingStatusRequestDto
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