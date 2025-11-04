using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.PurchaseContracts;
using OilTrading.Application.Queries.PurchaseContracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Application.Services;
using OilTrading.Api.Attributes;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/purchase-contracts")]
[Produces("application/json")]
public class PurchaseContractController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PurchaseContractController> _logger;
    private readonly IPaymentStatusCalculationService _paymentStatusService;

    public PurchaseContractController(
        IMediator mediator,
        ILogger<PurchaseContractController> logger,
        IPaymentStatusCalculationService paymentStatusService)
    {
        _mediator = mediator;
        _logger = logger;
        _paymentStatusService = paymentStatusService ?? throw new ArgumentNullException(nameof(paymentStatusService));
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
}