using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.ShippingOperations;
using OilTrading.Application.Queries.ShippingOperations;
using OilTrading.Application.Queries.Contracts;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Core.Repositories;
using OilTrading.Application.Services;
using OilTrading.Core.Enums;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/shipping-operations")]
[Produces("application/json")]
public class ShippingOperationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShippingOperationController> _logger;
    private readonly IShippingOperationRepository _shippingOperationRepository;
    private readonly ICacheInvalidationService _cacheInvalidationService;

    public ShippingOperationController(
        IMediator mediator,
        ILogger<ShippingOperationController> logger,
        IShippingOperationRepository shippingOperationRepository,
        ICacheInvalidationService cacheInvalidationService)
    {
        _mediator = mediator;
        _logger = logger;
        _shippingOperationRepository = shippingOperationRepository ?? throw new ArgumentNullException(nameof(shippingOperationRepository));
        _cacheInvalidationService = cacheInvalidationService ?? throw new ArgumentNullException(nameof(cacheInvalidationService));
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
        await _cacheInvalidationService.InvalidatePositionCacheAsync();

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
        await _cacheInvalidationService.InvalidatePositionCacheAsync();

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

    // ═══════════════════════════════════════════════════════════════════════════
    // SPLIT TRACKING ENDPOINTS (Data Lineage Enhancement v2.18.0)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a split from an existing shipping operation.
    /// The parent operation is marked as split and the quantity is distributed.
    /// </summary>
    /// <remarks>
    /// Split Reasons:
    /// - 1 = VesselCapacity
    /// - 2 = PortLimitation
    /// - 3 = CustomerRequest
    /// - 4 = QualitySegregation
    /// - 5 = RegulatoryRequirement
    /// - 6 = OperationalOptimization
    /// - 99 = Other
    /// </remarks>
    [HttpPost("{parentId:guid}/split")]
    [ProducesResponseType(typeof(ShippingSplitResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShippingSplitResultDto>> CreateSplit(
        Guid parentId,
        [FromBody] CreateShippingSplitRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating split for shipping operation {ParentId} with {SplitCount} split quantities",
                parentId, request.SplitQuantities?.Count ?? 0);

            // Validate request
            if (request.SplitQuantities == null || request.SplitQuantities.Count < 2)
            {
                return BadRequest(new { error = "At least 2 split quantities are required" });
            }

            if (request.SplitReason < 1)
            {
                return BadRequest(new { error = "Split reason is required" });
            }

            // Get parent shipping operation
            var parent = await _shippingOperationRepository.GetByIdAsync(parentId, cancellationToken);
            if (parent == null)
            {
                return NotFound(new { error = $"Shipping operation {parentId} not found" });
            }

            // Validate parent can be split
            if (!parent.CanBeSplit())
            {
                return BadRequest(new {
                    error = "This shipping operation cannot be split",
                    reason = parent.Status != ShippingStatus.Planned
                        ? "Only planned shipments can be split"
                        : "This shipment is already a split"
                });
            }

            // Validate quantities sum to original
            var totalSplitQuantity = request.SplitQuantities.Sum(sq => sq.Quantity);
            var tolerance = parent.PlannedQuantity.Value * 0.001m; // 0.1% tolerance
            if (Math.Abs(totalSplitQuantity - parent.PlannedQuantity.Value) > tolerance)
            {
                return BadRequest(new {
                    error = "Split quantities must sum to original planned quantity",
                    originalQuantity = parent.PlannedQuantity.Value,
                    totalSplitQuantity = totalSplitQuantity,
                    difference = parent.PlannedQuantity.Value - totalSplitQuantity
                });
            }

            var splitReason = (SplitReason)request.SplitReason;
            var createdSplits = new List<ShippingSplitItemDto>();
            var currentUser = GetCurrentUserName();

            // Mark parent as split
            parent.MarkAsSplitParent(currentUser);

            // Create split shipping operations
            for (int i = 0; i < request.SplitQuantities.Count; i++)
            {
                var splitQty = request.SplitQuantities[i];
                var splitSequence = i + 1;

                // Create new shipping operation for this split
                var splitShipping = new Core.Entities.ShippingOperation(
                    shippingNumber: $"{parent.ShippingNumber}-S{splitSequence:D2}",
                    contractId: parent.ContractId,
                    vesselName: splitQty.VesselName ?? parent.VesselName,
                    plannedQuantity: new Quantity(splitQty.Quantity, parent.PlannedQuantity.Unit),
                    loadPortETA: splitQty.LoadPortETA ?? parent.LoadPortETA,
                    dischargePortETA: splitQty.DischargePortETA ?? parent.DischargePortETA,
                    loadPort: parent.LoadPort,
                    dischargePort: parent.DischargePort
                );

                // Initialize as split with parent reference
                splitShipping.InitializeAsSplit(
                    parentShippingOperationId: parentId,
                    parentDealReferenceId: parent.DealReferenceId ?? "",
                    originalPlannedQuantity: parent.PlannedQuantity,
                    splitSequence: splitSequence,
                    splitReason: splitReason,
                    splitReasonNotes: request.SplitReasonNotes,
                    updatedBy: currentUser
                );

                splitShipping.SetCreatedBy(currentUser);
                // Force IsDeleted and RowVersion for EF Core change tracking
                splitShipping.SetIsDeleted(true);
                splitShipping.SetIsDeleted(false);
                splitShipping.SetRowVersion(new byte[] { 0 });

                await _shippingOperationRepository.AddAsync(splitShipping, cancellationToken);

                createdSplits.Add(new ShippingSplitItemDto
                {
                    Id = splitShipping.Id,
                    ShippingNumber = splitShipping.ShippingNumber,
                    SplitSequence = splitSequence,
                    PlannedQuantity = splitQty.Quantity,
                    PlannedQuantityUnit = parent.PlannedQuantity.Unit.ToString(),
                    VesselName = splitShipping.VesselName,
                    LoadPortETA = splitShipping.LoadPortETA,
                    DischargePortETA = splitShipping.DischargePortETA
                });
            }

            // Update parent
            await _shippingOperationRepository.UpdateAsync(parent, cancellationToken);

            _logger.LogInformation(
                "Created {SplitCount} splits for shipping operation {ParentId}",
                createdSplits.Count, parentId);

            var result = new ShippingSplitResultDto
            {
                ParentId = parentId,
                ParentShippingNumber = parent.ShippingNumber,
                OriginalPlannedQuantity = parent.PlannedQuantity.Value,
                OriginalPlannedQuantityUnit = parent.PlannedQuantity.Unit.ToString(),
                SplitReason = splitReason.ToString(),
                SplitReasonValue = (int)splitReason,
                SplitReasonNotes = request.SplitReasonNotes,
                CreatedSplits = createdSplits,
                TotalSplits = createdSplits.Count
            };

            return CreatedAtAction(nameof(GetSplitChain), new { operationId = parentId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating split for shipping operation {ParentId}", parentId);
            return StatusCode(500, new { error = "An error occurred while creating the split: " + ex.Message });
        }
    }

    /// <summary>
    /// Gets all shipping operations in the split chain for a given operation.
    /// Returns the parent and all splits, ordered by sequence.
    /// </summary>
    [HttpGet("{operationId:guid}/split-chain")]
    [ProducesResponseType(typeof(ShippingSplitChainDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShippingSplitChainDto>> GetSplitChain(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting split chain for shipping operation {OperationId}", operationId);

            var operation = await _shippingOperationRepository.GetByIdAsync(operationId, cancellationToken);
            if (operation == null)
            {
                return NotFound(new { error = "Shipping operation not found" });
            }

            // Determine if this is a parent or a split
            Guid parentId;
            if (operation.IsSplitShipment() && operation.ParentShippingOperationId.HasValue)
            {
                // This is a split, get the parent
                parentId = operation.ParentShippingOperationId.Value;
            }
            else
            {
                // This is the parent (or standalone)
                parentId = operationId;
            }

            // Get the parent
            var parent = await _shippingOperationRepository.GetByIdAsync(parentId, cancellationToken);
            if (parent == null)
            {
                return NotFound(new { error = "Parent shipping operation not found" });
            }

            // Get all splits for this parent
            var allOperations = await _shippingOperationRepository.GetAllAsync(cancellationToken);
            var splits = allOperations
                .Where(s => s.ParentShippingOperationId == parentId)
                .OrderBy(s => s.SplitSequence)
                .ToList();

            var response = new ShippingSplitChainDto
            {
                ParentId = parentId,
                ParentShippingNumber = parent.ShippingNumber,
                OriginalPlannedQuantity = parent.OriginalPlannedQuantity?.Value ?? parent.PlannedQuantity.Value,
                OriginalPlannedQuantityUnit = parent.PlannedQuantity.Unit.ToString(),
                CurrentPlannedQuantity = parent.PlannedQuantity.Value,
                HasBeenSplit = splits.Any(),
                TotalSplits = splits.Count,
                DealReferenceId = parent.DealReferenceId,
                Splits = splits.Select(s => new ShippingSplitItemDto
                {
                    Id = s.Id,
                    ShippingNumber = s.ShippingNumber,
                    SplitSequence = s.SplitSequence,
                    PlannedQuantity = s.PlannedQuantity.Value,
                    PlannedQuantityUnit = s.PlannedQuantity.Unit.ToString(),
                    VesselName = s.VesselName,
                    LoadPortETA = s.LoadPortETA,
                    DischargePortETA = s.DischargePortETA,
                    Status = s.Status.ToString(),
                    SplitReason = s.SplitReasonType?.ToString(),
                    SplitReasonNotes = s.SplitReasonNotes
                }).ToList()
            };

            _logger.LogInformation(
                "Found {SplitCount} splits for shipping operation {ParentId}",
                splits.Count, parentId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting split chain for shipping operation {OperationId}", operationId);
            return StatusCode(500, new { error = "An error occurred while retrieving the split chain: " + ex.Message });
        }
    }

    /// <summary>
    /// Gets the parent shipping operation for a split.
    /// Returns 404 if the operation is not a split.
    /// </summary>
    [HttpGet("{operationId:guid}/parent")]
    [ProducesResponseType(typeof(ShippingOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShippingOperationDto>> GetParentOperation(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting parent for shipping operation {OperationId}", operationId);

            var operation = await _shippingOperationRepository.GetByIdAsync(operationId, cancellationToken);
            if (operation == null)
            {
                return NotFound(new { error = "Shipping operation not found" });
            }

            if (!operation.IsSplitShipment() || !operation.ParentShippingOperationId.HasValue)
            {
                return NotFound(new { error = "This shipping operation is not a split - no parent exists" });
            }

            var query = new GetShippingOperationByIdQuery(operation.ParentShippingOperationId.Value);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parent for shipping operation {OperationId}", operationId);
            return StatusCode(500, new { error = "An error occurred while retrieving the parent: " + ex.Message });
        }
    }

    /// <summary>
    /// Gets all split shipping operations by Deal Reference ID.
    /// Useful for finding all related shipments across the system.
    /// </summary>
    [HttpGet("by-deal-reference/{dealReferenceId}")]
    [ProducesResponseType(typeof(List<ShippingOperationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ShippingOperationSummaryDto>>> GetByDealReferenceId(
        string dealReferenceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting shipping operations for Deal Reference ID {DealReferenceId}", dealReferenceId);

            var allOperations = await _shippingOperationRepository.GetAllAsync(cancellationToken);
            var matchingOperations = allOperations
                .Where(s => s.DealReferenceId != null &&
                           s.DealReferenceId.Equals(dealReferenceId.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.CreatedAt)
                .ToList();

            var result = matchingOperations.Select(s => new ShippingOperationSummaryDto
            {
                Id = s.Id,
                ShippingNumber = s.ShippingNumber,
                VesselName = s.VesselName,
                PlannedQuantity = s.PlannedQuantity.Value,
                PlannedQuantityUnit = s.PlannedQuantity.Unit.ToString(),
                Status = s.Status.ToString(),
                LoadPortETA = s.LoadPortETA,
                DischargePortETA = s.DischargePortETA,
                IsSplit = s.IsSplit,
                SplitSequence = s.SplitSequence,
                ParentShippingOperationId = s.ParentShippingOperationId,
                DealReferenceId = s.DealReferenceId
            }).ToList();

            _logger.LogInformation(
                "Found {Count} shipping operations for Deal Reference ID {DealReferenceId}",
                result.Count, dealReferenceId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipping operations by Deal Reference ID {DealReferenceId}", dealReferenceId);
            return StatusCode(500, new { error = "An error occurred while retrieving shipping operations: " + ex.Message });
        }
    }

    /// <summary>
    /// Sets the Deal Reference ID for a shipping operation.
    /// Typically inherited from the contract.
    /// </summary>
    [HttpPut("{operationId:guid}/deal-reference")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetDealReferenceId(
        Guid operationId,
        [FromBody] SetDealReferenceIdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Setting Deal Reference ID for shipping operation {OperationId} to {DealReferenceId}",
                operationId, request.DealReferenceId);

            if (string.IsNullOrWhiteSpace(request.DealReferenceId))
            {
                return BadRequest(new { error = "Deal Reference ID is required" });
            }

            var operation = await _shippingOperationRepository.GetByIdAsync(operationId, cancellationToken);
            if (operation == null)
            {
                return NotFound(new { error = "Shipping operation not found" });
            }

            operation.SetDealReferenceId(request.DealReferenceId, GetCurrentUserName());
            await _shippingOperationRepository.UpdateAsync(operation, cancellationToken);

            _logger.LogInformation(
                "Set Deal Reference ID for shipping operation {OperationId} to {DealReferenceId}",
                operationId, request.DealReferenceId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Deal Reference ID for shipping operation {OperationId}", operationId);
            return StatusCode(500, new { error = "An error occurred: " + ex.Message });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SPLIT TRACKING DTOs (Data Lineage Enhancement v2.18.0)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO for creating a shipping operation split
/// </summary>
public class CreateShippingSplitRequestDto
{
    /// <summary>
    /// Split reason:
    /// 1 = VesselCapacity, 2 = PortLimitation, 3 = CustomerRequest,
    /// 4 = QualitySegregation, 5 = RegulatoryRequirement, 6 = OperationalOptimization, 99 = Other
    /// </summary>
    public int SplitReason { get; set; }

    /// <summary>
    /// Additional notes about the split reason
    /// </summary>
    public string? SplitReasonNotes { get; set; }

    /// <summary>
    /// List of quantities for each split (must sum to original)
    /// </summary>
    public List<SplitQuantityDto> SplitQuantities { get; set; } = new();
}

/// <summary>
/// DTO for individual split quantity
/// </summary>
public class SplitQuantityDto
{
    /// <summary>
    /// Quantity for this split
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Optional vessel name for this split (defaults to parent's vessel)
    /// </summary>
    public string? VesselName { get; set; }

    /// <summary>
    /// Optional load port ETA for this split (defaults to parent's ETA)
    /// </summary>
    public DateTime? LoadPortETA { get; set; }

    /// <summary>
    /// Optional discharge port ETA for this split (defaults to parent's ETA)
    /// </summary>
    public DateTime? DischargePortETA { get; set; }
}

/// <summary>
/// Response DTO for split creation
/// </summary>
public class ShippingSplitResultDto
{
    public Guid ParentId { get; set; }
    public string ParentShippingNumber { get; set; } = string.Empty;
    public decimal OriginalPlannedQuantity { get; set; }
    public string OriginalPlannedQuantityUnit { get; set; } = string.Empty;
    public string SplitReason { get; set; } = string.Empty;
    public int SplitReasonValue { get; set; }
    public string? SplitReasonNotes { get; set; }
    public List<ShippingSplitItemDto> CreatedSplits { get; set; } = new();
    public int TotalSplits { get; set; }
}

/// <summary>
/// DTO for individual split item
/// </summary>
public class ShippingSplitItemDto
{
    public Guid Id { get; set; }
    public string ShippingNumber { get; set; } = string.Empty;
    public int SplitSequence { get; set; }
    public decimal PlannedQuantity { get; set; }
    public string PlannedQuantityUnit { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public DateTime LoadPortETA { get; set; }
    public DateTime DischargePortETA { get; set; }
    public string? Status { get; set; }
    public string? SplitReason { get; set; }
    public string? SplitReasonNotes { get; set; }
}

/// <summary>
/// Response DTO for split chain query
/// </summary>
public class ShippingSplitChainDto
{
    public Guid ParentId { get; set; }
    public string ParentShippingNumber { get; set; } = string.Empty;
    public decimal OriginalPlannedQuantity { get; set; }
    public string OriginalPlannedQuantityUnit { get; set; } = string.Empty;
    public decimal CurrentPlannedQuantity { get; set; }
    public bool HasBeenSplit { get; set; }
    public int TotalSplits { get; set; }
    public string? DealReferenceId { get; set; }
    public List<ShippingSplitItemDto> Splits { get; set; } = new();
}

/// <summary>
/// DTO for setting Deal Reference ID
/// </summary>
public class SetDealReferenceIdRequestDto
{
    public string DealReferenceId { get; set; } = string.Empty;
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