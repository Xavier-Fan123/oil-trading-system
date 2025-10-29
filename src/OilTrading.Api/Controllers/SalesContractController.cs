using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.SalesContracts;
using OilTrading.Application.Queries.SalesContracts;
using OilTrading.Application.Queries.PurchaseContracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/sales-contracts")]
[Produces("application/json")]
public class SalesContractController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SalesContractController> _logger;

    public SalesContractController(IMediator mediator, ILogger<SalesContractController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
}