using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.ShippingOperations;
using OilTrading.Application.Queries.ShippingOperations;
using OilTrading.Application.Queries.Contracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/shipping-operations")]
[Produces("application/json")]
public class ShippingOperationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShippingOperationController> _logger;

    public ShippingOperationController(IMediator mediator, ILogger<ShippingOperationController> logger)
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
    /// Creates a new shipping operation
    /// </summary>
    /// <param name="dto">The shipping operation details</param>
    /// <returns>The ID of the created shipping operation</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateShippingOperationDto dto)
    {
        _logger.LogInformation("=== RAW DTO RECEIVED ===");
        _logger.LogInformation("DTO JSON: {@Dto}", dto);
        _logger.LogInformation("CreateShippingOperationCommand received: ContractId={ContractId}, VesselName={VesselName}, PlannedQuantity={PlannedQuantity}, PlannedQuantityUnit={PlannedQuantityUnit}, LaycanStart={LaycanStart}, LaycanEnd={LaycanEnd}",
            dto.ContractId, dto.VesselName, dto.PlannedQuantity, dto.PlannedQuantityUnit, dto.LaycanStart, dto.LaycanEnd);

        var command = new CreateShippingOperationCommand
        {
            ContractId = dto.ContractId,
            VesselName = dto.VesselName,
            IMONumber = dto.ImoNumber,
            PlannedQuantity = dto.PlannedQuantity,
            PlannedQuantityUnit = dto.PlannedQuantityUnit,
            LoadPortETA = dto.LaycanStart ?? DateTime.UtcNow.AddDays(30),
            DischargePortETA = dto.LaycanEnd ?? DateTime.UtcNow.AddDays(45),
            Notes = dto.Notes,
            CreatedBy = GetCurrentUserName()
        };

        _logger.LogInformation("=== COMMAND TO HANDLER ===");
        _logger.LogInformation("Command: {@Command}", command);
        _logger.LogInformation("CreateShippingOperationCommand: LoadPortETA={LoadPortETA}, DischargePortETA={DischargePortETA}, CreatedBy={CreatedBy}",
            command.LoadPortETA, command.DischargePortETA, command.CreatedBy);

        var operationId = await _mediator.Send(command);

        _logger.LogInformation("Shipping operation {OperationId} created successfully", operationId);

        return CreatedAtAction(nameof(GetById), new { id = operationId }, operationId);
    }

    /// <summary>
    /// Creates a new shipping operation by resolving an external contract number
    /// </summary>
    /// <remarks>
    /// This endpoint allows creating a shipping operation by providing an external contract number instead of a GUID.
    /// If the external contract number resolves to multiple contracts, it returns a 422 response with candidates for disambiguation.
    /// If the external contract number is not found, it returns a 404 response.
    /// </remarks>
    /// <param name="dto">The shipping operation details including the external contract number</param>
    /// <returns>The ID of the created shipping operation</returns>
    [HttpPost("create-by-external-contract")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateByExternalContract([FromBody] CreateShippingOperationByExternalContractDto dto)
    {
        _logger.LogInformation("CreateByExternalContract request: ExternalContractNumber={ExternalContractNumber}, VesselName={VesselName}, PlannedQuantity={PlannedQuantity}",
            dto.ExternalContractNumber, dto.VesselName, dto.PlannedQuantity);

        // Validate input
        if (string.IsNullOrWhiteSpace(dto.ExternalContractNumber))
        {
            _logger.LogWarning("CreateByExternalContract: External contract number is required");
            return BadRequest(new
            {
                success = false,
                errorMessage = "External contract number is required",
                validationErrors = new[] { "ExternalContractNumber is required" }
            });
        }

        try
        {
            // Step 1: Resolve external contract number to GUID
            var resolutionQuery = new ResolveContractByExternalNumberQuery
            {
                ExternalContractNumber = dto.ExternalContractNumber,
                ExpectedContractType = dto.ExpectedContractType,
                ExpectedTradingPartnerId = dto.TradingPartnerId,
                ExpectedProductId = dto.ProductId
            };

            var resolution = await _mediator.Send(resolutionQuery);

            // Step 2: Handle disambiguation (multiple matches)
            if (!resolution.Success && resolution.Candidates.Count > 0)
            {
                _logger.LogWarning("CreateByExternalContract: External contract number {ExternalContractNumber} resolved to {CandidateCount} candidates",
                    dto.ExternalContractNumber, resolution.Candidates.Count);

                return StatusCode(
                    StatusCodes.Status422UnprocessableEntity,
                    new
                    {
                        success = false,
                        errorMessage = "External contract number is ambiguous - multiple contracts match",
                        candidates = resolution.Candidates,
                        hint = "Please provide ExpectedContractType, TradingPartnerId, or ProductId to disambiguate"
                    });
            }

            // Step 3: Handle not found
            if (!resolution.Success || !resolution.ContractId.HasValue)
            {
                _logger.LogWarning("CreateByExternalContract: External contract number {ExternalContractNumber} not found",
                    dto.ExternalContractNumber);

                return NotFound(new
                {
                    success = false,
                    errorMessage = $"Contract with external number '{dto.ExternalContractNumber}' not found"
                });
            }

            // Step 4: Create shipping operation with resolved contract ID
            var command = new CreateShippingOperationCommand
            {
                ContractId = resolution.ContractId.Value,
                VesselName = dto.VesselName,
                IMONumber = dto.ImoNumber,
                ChartererName = dto.ChartererName,
                VesselCapacity = dto.VesselCapacity,
                ShippingAgent = dto.ShippingAgent,
                PlannedQuantity = dto.PlannedQuantity,
                PlannedQuantityUnit = dto.PlannedQuantityUnit,
                LoadPortETA = dto.LaycanStart ?? DateTime.UtcNow.AddDays(30),
                DischargePortETA = dto.LaycanEnd ?? DateTime.UtcNow.AddDays(45),
                LoadPort = dto.LoadPort,
                DischargePort = dto.DischargePort,
                Notes = dto.Notes,
                CreatedBy = GetCurrentUserName()
            };

            _logger.LogInformation("CreateByExternalContract: Creating shipping operation with resolved ContractId={ContractId}", resolution.ContractId);

            var operationId = await _mediator.Send(command);

            _logger.LogInformation("Shipping operation {OperationId} created successfully from external contract {ExternalContractNumber}",
                operationId, dto.ExternalContractNumber);

            return CreatedAtAction(nameof(GetById), new { id = operationId }, operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipping operation by external contract number: {ExternalContractNumber}",
                dto.ExternalContractNumber);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    errorMessage = "An error occurred while creating the shipping operation",
                    details = ex.Message
                });
        }
    }

    /// <summary>
    /// Gets a paginated list of shipping operations
    /// </summary>
    /// <returns>Paginated list of shipping operations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShippingOperationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] GetShippingOperationsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a shipping operation by ID
    /// </summary>
    /// <param name="id">The shipping operation ID</param>
    /// <returns>The shipping operation details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShippingOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetShippingOperationByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing shipping operation
    /// </summary>
    /// <param name="id">The shipping operation ID</param>
    /// <param name="dto">The updated shipping operation details</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShippingOperationDto dto)
    {
        var command = new UpdateShippingOperationCommand
        {
            Id = id,
            VesselName = dto.VesselName,
            IMONumber = dto.ImoNumber,
            PlannedQuantity = dto.PlannedQuantity,
            PlannedQuantityUnit = dto.PlannedQuantityUnit,
            LoadPortETA = dto.LaycanStart,
            DischargePortETA = dto.LaycanEnd,
            Notes = dto.Notes,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Shipping operation {OperationId} updated successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Records lifting operation details (NOR, BL, Discharge dates and actual quantity)
    /// </summary>
    /// <param name="dto">The lifting operation details</param>
    /// <returns>No content if successful</returns>
    [HttpPost("record-lifting")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordLifting([FromBody] RecordLiftingOperationDto dto)
    {
        var command = new RecordLiftingOperationCommand
        {
            ShippingOperationId = dto.ShippingOperationId,
            NorDate = dto.NorDate,
            BillOfLadingDate = dto.BillOfLadingDate,
            DischargeDate = dto.DischargeDate,
            ActualQuantity = dto.ActualQuantity,
            ActualQuantityUnit = dto.ActualQuantityUnit,
            Notes = dto.Notes,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Lifting operation recorded for shipping operation {OperationId}", dto.ShippingOperationId);
        
        return NoContent();
    }

    /// <summary>
    /// Gets shipping operations for a specific contract
    /// </summary>
    /// <param name="contractId">The contract ID</param>
    /// <returns>List of shipping operations for the contract</returns>
    [HttpGet("by-contract/{contractId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByContract(Guid contractId)
    {
        var query = new GetShippingOperationsByContractQuery(contractId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Starts loading operation
    /// </summary>
    /// <param name="operationId">The shipping operation ID</param>
    /// <param name="dto">Loading operation details</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{operationId:guid}/start-loading")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartLoading(Guid operationId, [FromBody] StartLoadingDto dto)
    {
        var command = new StartLoadingCommand
        {
            ShippingOperationId = operationId,
            LoadPortATA = dto.LoadPortATA,
            NoticeOfReadinessDate = dto.NoticeOfReadinessDate,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Loading started for shipping operation {OperationId}", operationId);
        
        return NoContent();
    }

    /// <summary>
    /// Completes loading operation
    /// </summary>
    /// <param name="operationId">The shipping operation ID</param>
    /// <param name="dto">Loading completion details</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{operationId:guid}/complete-loading")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteLoading(Guid operationId, [FromBody] CompleteLoadingDto dto)
    {
        var command = new CompleteLoadingCommand
        {
            ShippingOperationId = operationId,
            BillOfLadingDate = dto.BillOfLadingDate,
            ActualQuantity = dto.ActualQuantity,
            ActualQuantityUnit = dto.ActualQuantityUnit,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Loading completed for shipping operation {OperationId}", operationId);
        
        return NoContent();
    }

    /// <summary>
    /// Completes discharge operation
    /// </summary>
    /// <param name="operationId">The shipping operation ID</param>
    /// <param name="dto">Discharge completion details</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{operationId:guid}/complete-discharge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteDischarge(Guid operationId, [FromBody] CompleteDischargeDto dto)
    {
        var command = new CompleteDischargeCommand
        {
            ShippingOperationId = operationId,
            DischargePortATA = dto.DischargePortATA,
            CertificateOfDischargeDate = dto.CertificateOfDischargeDate,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Discharge completed for shipping operation {OperationId}", operationId);
        
        return NoContent();
    }

    /// <summary>
    /// Cancels a shipping operation
    /// </summary>
    /// <param name="operationId">The shipping operation ID</param>
    /// <param name="dto">Cancellation details</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{operationId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid operationId, [FromBody] CancelShippingOperationDto dto)
    {
        var command = new CancelShippingOperationCommand
        {
            ShippingOperationId = operationId,
            Reason = dto.Reason,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Shipping operation {OperationId} cancelled", operationId);
        
        return NoContent();
    }

    /// <summary>
    /// Deletes a shipping operation (only for planned operations)
    /// </summary>
    /// <param name="id">The shipping operation ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteShippingOperationCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Shipping operation {OperationId} deleted successfully", id);
        
        return NoContent();
    }
}

// Additional DTOs for shipping operation lifecycle management
public class StartLoadingDto
{
    public DateTime LoadPortATA { get; set; }
    public DateTime? NoticeOfReadinessDate { get; set; }
}

public class CompleteLoadingDto
{
    public DateTime BillOfLadingDate { get; set; }
    public decimal ActualQuantity { get; set; }
    public string ActualQuantityUnit { get; set; } = "MT";
}

public class CompleteDischargeDto
{
    public DateTime DischargePortATA { get; set; }
    public DateTime CertificateOfDischargeDate { get; set; }
}

public class CancelShippingOperationDto
{
    public string Reason { get; set; } = string.Empty;
}