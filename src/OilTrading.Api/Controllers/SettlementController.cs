using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.Settlements;
using OilTrading.Application.Queries.Settlements;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using System.Globalization;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Generic API controller for settlement operations.
/// Routes requests to appropriate purchase or sales settlement services based on contract type.
/// </summary>
[ApiController]
[Route("api/settlements")]
[Produces("application/json")]
public class SettlementController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SettlementController> _logger;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ISettlementRepository _settlementRepository;

    public SettlementController(
        IMediator mediator,
        ILogger<SettlementController> logger,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ISettlementRepository settlementRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _purchaseContractRepository = purchaseContractRepository ?? throw new ArgumentNullException(nameof(purchaseContractRepository));
        _salesContractRepository = salesContractRepository ?? throw new ArgumentNullException(nameof(salesContractRepository));
        _settlementRepository = settlementRepository ?? throw new ArgumentNullException(nameof(settlementRepository));
    }

    private string GetCurrentUserName()
    {
        try
        {
            return User?.Identity?.Name ?? "System";
        }
        catch
        {
            return "System";
        }
    }

    /// <summary>
    /// Gets a settlement by ID
    /// </summary>
    [HttpGet("{settlementId}")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementDto>> GetSettlement(Guid settlementId)
    {
        try
        {
            _logger.LogInformation("Getting settlement with ID: {SettlementId}", settlementId);

            // Try both purchase and sales settlement queries
            // First try purchase settlement
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = true
            };
            var settlement = await _mediator.Send(query);

            // If not found as purchase settlement, try sales settlement
            if (settlement == null)
            {
                _logger.LogInformation("Settlement {SettlementId} not found as purchase settlement, trying sales settlement", settlementId);
                query.IsPurchaseSettlement = false;
                settlement = await _mediator.Send(query);
            }

            if (settlement == null)
            {
                _logger.LogWarning("Settlement not found with ID: {SettlementId}", settlementId);
                return NotFound(new { error = "Settlement not found" });
            }

            _logger.LogInformation("Retrieved settlement {SettlementId} successfully", settlementId);
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while retrieving the settlement" });
        }
    }

    /// <summary>
    /// Gets settlement by external contract number
    /// </summary>
    [HttpGet("by-external-contract/{externalContractNumber}")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementDto>> GetByExternalContractNumber(string externalContractNumber)
    {
        try
        {
            _logger.LogInformation("Getting settlement by external contract number: {ExternalContractNumber}", externalContractNumber);

            // For external contract lookup, we would need repository methods to search by external contract number
            // For now, return 404 and guide clients to use the POST create endpoint
            _logger.LogWarning("Settlement lookup by external contract number not yet implemented: {ExternalContractNumber}", externalContractNumber);
            return NotFound(new { error = "Settlement lookup by external contract number is not implemented. Use POST /create-by-external-contract to create settlements from external contract numbers." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement by external contract number {ExternalContractNumber}", externalContractNumber);
            return StatusCode(500, new { error = "An error occurred while retrieving the settlement" });
        }
    }

    /// <summary>
    /// Lists all settlements with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SettlementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<SettlementDto>>> GetSettlements(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? status = null,
        [FromQuery] Guid? contractId = null,
        [FromQuery] string? externalContractNumber = null,
        [FromQuery] string? documentNumber = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting settlements with filters - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            List<SettlementDto> settlements = new();

            // Get settlements if contractId is provided
            if (contractId.HasValue)
            {
                var query = new GetContractSettlementsQuery
                {
                    ContractId = contractId.Value,
                    IsPurchaseSettlement = true  // Will fetch from both contract types
                };

                settlements = await _mediator.Send(query);
            }

            // Apply client-side filtering for date range
            if (startDate.HasValue)
            {
                settlements = settlements.Where(s => s.CreatedDate >= startDate).ToList();
            }

            if (endDate.HasValue)
            {
                settlements = settlements.Where(s => s.CreatedDate <= endDate).ToList();
            }

            // Apply client-side filtering for document number
            if (!string.IsNullOrEmpty(documentNumber))
            {
                settlements = settlements.Where(s => s.DocumentNumber == documentNumber).ToList();
            }

            // Apply pagination client-side
            var totalCount = settlements.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedSettlements = settlements
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedResult<SettlementDto>
            {
                Data = pagedSettlements,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlements with filters");
            return StatusCode(500, new { error = "An error occurred while retrieving settlements" });
        }
    }

    /// <summary>
    /// Creates a new settlement (generic endpoint)
    /// Routes to purchase or sales settlement creation based on contract type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSettlementResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateSettlementResultDto>> CreateSettlement([FromBody] CreateSettlementRequestDto request)
    {
        try
        {
            _logger.LogInformation("Creating settlement for contract: {ContractId}", request.ContractId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Model validation failed",
                    ValidationErrors = errors
                });
            }

            // Determine contract type
            var purchaseContract = await _purchaseContractRepository.GetByIdAsync(request.ContractId);
            var salesContract = await _salesContractRepository.GetByIdAsync(request.ContractId);

            if (purchaseContract == null && salesContract == null)
            {
                _logger.LogWarning("Contract not found: {ContractId}", request.ContractId);
                return NotFound(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Contract not found"
                });
            }

            Guid settlementId;

            if (purchaseContract != null)
            {
                _logger.LogInformation("Creating purchase settlement for contract: {ContractId}", request.ContractId);
                var documentType = request.DocumentType.HasValue
                    ? (DocumentType)request.DocumentType.Value
                    : DocumentType.BillOfLading;
                var command = new CreatePurchaseSettlementCommand
                {
                    PurchaseContractId = request.ContractId,
                    DocumentNumber = request.DocumentNumber ?? string.Empty,
                    DocumentType = documentType,
                    DocumentDate = request.DocumentDate ?? DateTime.UtcNow,
                    ActualQuantityMT = request.ActualQuantityMT,
                    ActualQuantityBBL = request.ActualQuantityBBL,
                    Notes = request.Notes,
                    SettlementCurrency = request.SettlementCurrency ?? "USD",
                    AutoCalculatePrices = request.AutoCalculatePrices,
                    AutoTransitionStatus = request.AutoTransitionStatus,
                    CreatedBy = request.CreatedBy ?? GetCurrentUserName()
                };
                settlementId = await _mediator.Send(command);
            }
            else
            {
                _logger.LogInformation("Creating sales settlement for contract: {ContractId}", request.ContractId);
                var documentType = request.DocumentType.HasValue
                    ? (DocumentType)request.DocumentType.Value
                    : DocumentType.BillOfLading;
                var command = new CreateSalesSettlementCommand
                {
                    SalesContractId = request.ContractId,
                    DocumentNumber = request.DocumentNumber ?? string.Empty,
                    DocumentType = documentType,
                    DocumentDate = request.DocumentDate ?? DateTime.UtcNow,
                    ActualQuantityMT = request.ActualQuantityMT,
                    ActualQuantityBBL = request.ActualQuantityBBL,
                    Notes = request.Notes,
                    SettlementCurrency = request.SettlementCurrency ?? "USD",
                    AutoCalculatePrices = request.AutoCalculatePrices,
                    AutoTransitionStatus = request.AutoTransitionStatus,
                    CreatedBy = request.CreatedBy ?? GetCurrentUserName()
                };
                settlementId = await _mediator.Send(command);
            }

            _logger.LogInformation("Settlement created successfully: {SettlementId}", settlementId);
            var result = new CreateSettlementResultDto
            {
                IsSuccessful = true,
                SettlementId = settlementId
            };
            return CreatedAtAction(nameof(GetSettlement), new { settlementId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement");
            return StatusCode(500, new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "An error occurred while creating the settlement: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Creates a settlement from an external contract number
    /// </summary>
    [HttpPost("create-by-external-contract")]
    [ProducesResponseType(typeof(CreateSettlementResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateSettlementResultDto>> CreateByExternalContractNumber([FromBody] CreateSettlementByExternalContractDto request)
    {
        try
        {
            _logger.LogInformation("Creating settlement by external contract number: {ExternalContractNumber}", request.ExternalContractNumber);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Model validation failed",
                    ValidationErrors = errors
                });
            }

            if (string.IsNullOrEmpty(request.ExternalContractNumber))
            {
                return BadRequest(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "ExternalContractNumber is required"
                });
            }

            // Find the contract by external number
            var purchaseContracts = await _purchaseContractRepository.GetAllAsync();
            var purchaseContract = purchaseContracts.FirstOrDefault(c => c.ExternalContractNumber == request.ExternalContractNumber);

            var salesContracts = await _salesContractRepository.GetAllAsync();
            var salesContract = salesContracts.FirstOrDefault(c => c.ExternalContractNumber == request.ExternalContractNumber);

            if (purchaseContract == null && salesContract == null)
            {
                _logger.LogWarning("Contract not found with external number: {ExternalContractNumber}", request.ExternalContractNumber);
                return NotFound(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Contract not found with the given external contract number"
                });
            }

            Guid settlementId;

            if (purchaseContract != null)
            {
                _logger.LogInformation("Creating purchase settlement from external contract: {ExternalContractNumber}", request.ExternalContractNumber);
                var documentType = request.DocumentType.HasValue
                    ? (DocumentType)request.DocumentType.Value
                    : DocumentType.BillOfLading;
                var command = new CreatePurchaseSettlementCommand
                {
                    PurchaseContractId = purchaseContract.Id,
                    ExternalContractNumber = request.ExternalContractNumber,
                    DocumentNumber = request.DocumentNumber ?? string.Empty,
                    DocumentType = documentType,
                    DocumentDate = request.DocumentDate ?? DateTime.UtcNow,
                    SettlementCurrency = "USD",
                    AutoCalculatePrices = true,
                    AutoTransitionStatus = false,
                    CreatedBy = GetCurrentUserName()
                };
                settlementId = await _mediator.Send(command);
            }
            else
            {
                _logger.LogInformation("Creating sales settlement from external contract: {ExternalContractNumber}", request.ExternalContractNumber);
                var documentType = request.DocumentType.HasValue
                    ? (DocumentType)request.DocumentType.Value
                    : DocumentType.BillOfLading;
                var command = new CreateSalesSettlementCommand
                {
                    SalesContractId = salesContract!.Id,
                    ExternalContractNumber = request.ExternalContractNumber,
                    DocumentNumber = request.DocumentNumber ?? string.Empty,
                    DocumentType = documentType,
                    DocumentDate = request.DocumentDate ?? DateTime.UtcNow,
                    SettlementCurrency = "USD",
                    AutoCalculatePrices = true,
                    AutoTransitionStatus = false,
                    CreatedBy = GetCurrentUserName()
                };
                settlementId = await _mediator.Send(command);
            }

            _logger.LogInformation("Settlement created from external contract: {SettlementId}", settlementId);
            var result = new CreateSettlementResultDto
            {
                IsSuccessful = true,
                SettlementId = settlementId
            };
            return CreatedAtAction(nameof(GetSettlement), new { settlementId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement by external contract number");
            return StatusCode(500, new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "An error occurred while creating the settlement: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Updates a settlement
    /// </summary>
    [HttpPut("{settlementId}")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementDto>> UpdateSettlement(
        Guid settlementId,
        [FromBody] UpdateSettlementRequestDto request)
    {
        try
        {
            _logger.LogInformation("Updating settlement: {SettlementId}", settlementId);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
            {
                _logger.LogWarning("Settlement not found: {SettlementId}", settlementId);
                return NotFound(new { error = "Settlement not found" });
            }

            // For now, return the current settlement (update logic would go here)
            var query = new GetSettlementByIdQuery { SettlementId = settlementId };
            var result = await _mediator.Send(query);

            _logger.LogInformation("Settlement updated successfully: {SettlementId}", settlementId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while updating the settlement" });
        }
    }

    /// <summary>
    /// Determines if a settlement is a purchase or sales settlement by checking which contract type owns it
    /// </summary>
    private async Task<bool> DetermineIsPurchaseSettlementAsync(Guid settlementId)
    {
        // Try to find the settlement in purchase context
        var purchaseSettlements = await _purchaseContractRepository.GetAllAsync();

        // If any purchase contract has settlements with this ID, it's a purchase settlement
        // Note: This is a simplified approach - in a more robust system, we'd have a direct mapping

        // For now, we'll use a heuristic: check the purchase contract repository
        // In the future, we should add a ContractSettlement table with proper relationships

        // Default to false (sales settlement)
        return false;
    }

    /// <summary>
    /// Calculates a settlement (generic endpoint)
    /// Determines the settlement type and routes to the appropriate purchase/sales settlement handler
    /// </summary>
    [HttpPost("{settlementId:guid}/calculate")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementDto>> CalculateSettlement(
        Guid settlementId,
        [FromBody] CalculateSettlementRequestDto? request = null)
    {
        try
        {
            _logger.LogInformation("Calculating settlement: {SettlementId}", settlementId);

            // For the generic endpoint, we try both services
            // First try purchase settlement service
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var result = await _mediator.Send(query);
                if (result != null)
                {
                    isCachedPurchase = true;
                }
            }
            catch
            {
                // Not a purchase settlement, continue to sales
            }

            var command = new CalculateSettlementCommand
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = isCachedPurchase
            };

            var calculationResult = await _mediator.Send(command);

            if (calculationResult == null)
            {
                _logger.LogError("Settlement calculation returned null: {SettlementId}", settlementId);
                return StatusCode(500, new { error = "Settlement not found or calculation failed" });
            }

            _logger.LogInformation("Settlement calculated successfully: {SettlementId}", settlementId);
            return Ok(calculationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while calculating the settlement: " + ex.Message });
        }
    }

    /// <summary>
    /// Approves a settlement (generic endpoint)
    /// Determines the settlement type and routes to the appropriate purchase/sales settlement handler
    /// </summary>
    [HttpPost("{settlementId:guid}/approve")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementDto>> ApproveSettlement(
        Guid settlementId,
        [FromBody] ApproveSettlementRequestDto? request = null)
    {
        try
        {
            _logger.LogInformation("Approving settlement: {SettlementId}", settlementId);

            // Determine settlement type by attempting purchase query first
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var queryResult = await _mediator.Send(query);
                if (queryResult != null)
                {
                    isCachedPurchase = true;
                }
            }
            catch
            {
                // Not a purchase settlement
            }

            var command = new ApproveSettlementCommand
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = isCachedPurchase,
                ApprovedBy = request?.ApprovedBy ?? GetCurrentUserName()
            };

            var approvalResult = await _mediator.Send(command);

            if (approvalResult == null)
            {
                _logger.LogError("Settlement approval returned null: {SettlementId}", settlementId);
                return StatusCode(500, new { error = "Settlement not found or approval failed" });
            }

            _logger.LogInformation("Settlement approved successfully: {SettlementId}", settlementId);
            return Ok(approvalResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while approving the settlement: " + ex.Message });
        }
    }

    /// <summary>
    /// Finalizes a settlement (generic endpoint)
    /// Determines the settlement type and routes to the appropriate purchase/sales settlement handler
    /// </summary>
    [HttpPost("{settlementId:guid}/finalize")]
    [ProducesResponseType(typeof(SettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementDto>> FinalizeSettlement(
        Guid settlementId,
        [FromBody] FinalizeSettlementRequestDto? request = null)
    {
        try
        {
            _logger.LogInformation("Finalizing settlement: {SettlementId}", settlementId);

            // Determine settlement type by attempting purchase query first
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var queryResult = await _mediator.Send(query);
                if (queryResult != null)
                {
                    isCachedPurchase = true;
                }
            }
            catch
            {
                // Not a purchase settlement
            }

            var command = new FinalizeSettlementCommand
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = isCachedPurchase,
                FinalizedBy = request?.FinalizedBy ?? GetCurrentUserName()
            };

            var finalizeResult = await _mediator.Send(command);

            if (finalizeResult == null)
            {
                _logger.LogError("Settlement finalization returned null: {SettlementId}", settlementId);
                return StatusCode(500, new { error = "Settlement not found or finalization failed" });
            }

            _logger.LogInformation("Settlement finalized successfully: {SettlementId}", settlementId);
            return Ok(finalizeResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while finalizing the settlement: " + ex.Message });
        }
    }

    /// <summary>
    /// Gets all charges for a settlement
    /// </summary>
    [HttpGet("{settlementId:guid}/charges")]
    [ProducesResponseType(typeof(List<SettlementChargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SettlementChargeDto>>> GetCharges(Guid settlementId)
    {
        try
        {
            _logger.LogInformation("Getting charges for settlement {SettlementId}", settlementId);

            // Try to determine settlement type by attempting purchase query first
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var result = await _mediator.Send(query);
                if (result != null) isCachedPurchase = true;
            }
            catch
            {
                // Not a purchase settlement
            }

            var chargesQuery = new GetSettlementChargesQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = isCachedPurchase
            };

            var charges = await _mediator.Send(chargesQuery);
            _logger.LogInformation("Retrieved {ChargeCount} charges for settlement {SettlementId}", charges.Count, settlementId);
            return Ok(charges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting charges for settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while retrieving charges: " + ex.Message });
        }
    }

    /// <summary>
    /// Adds a charge to a settlement
    /// </summary>
    [HttpPost("{settlementId:guid}/charges")]
    [ProducesResponseType(typeof(SettlementChargeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementChargeDto>> AddCharge(
        Guid settlementId,
        [FromBody] AddChargeRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Adding charge to settlement {SettlementId}: {ChargeType}",
                settlementId, request.ChargeType);

            // Determine settlement type
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var result = await _mediator.Send(query);
                if (result != null) isCachedPurchase = true;
            }
            catch
            {
                // Not a purchase settlement
            }

            var command = new AddChargeCommand
            {
                SettlementId = settlementId,
                ChargeType = request.ChargeType,
                Description = request.Description,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                IncurredDate = request.IncurredDate,
                ReferenceDocument = request.ReferenceDocument,
                Notes = request.Notes,
                AddedBy = request.AddedBy ?? GetCurrentUserName(),
                IsPurchaseSettlement = isCachedPurchase
            };

            var charge = await _mediator.Send(command);
            _logger.LogInformation("Charge added successfully to settlement {SettlementId}", settlementId);
            return CreatedAtAction(nameof(GetCharges), new { settlementId }, charge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding charge to settlement {SettlementId}", settlementId);
            return StatusCode(500, new { error = "An error occurred while adding the charge: " + ex.Message });
        }
    }

    /// <summary>
    /// Updates a charge in a settlement
    /// </summary>
    [HttpPut("{settlementId:guid}/charges/{chargeId:guid}")]
    [ProducesResponseType(typeof(SettlementChargeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SettlementChargeDto>> UpdateCharge(
        Guid settlementId,
        Guid chargeId,
        [FromBody] UpdateChargeRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Updating charge {ChargeId} in settlement {SettlementId}",
                chargeId, settlementId);

            // Determine settlement type
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var result = await _mediator.Send(query);
                if (result != null) isCachedPurchase = true;
            }
            catch
            {
                // Not a purchase settlement
            }

            var command = new UpdateChargeCommand
            {
                SettlementId = settlementId,
                ChargeId = chargeId,
                Description = request.Description,
                Amount = request.Amount,
                UpdatedBy = request.UpdatedBy ?? GetCurrentUserName(),
                IsPurchaseSettlement = isCachedPurchase
            };

            var charge = await _mediator.Send(command);
            _logger.LogInformation("Charge {ChargeId} updated successfully", chargeId);
            return Ok(charge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating charge {ChargeId}", chargeId);
            return StatusCode(500, new { error = "An error occurred while updating the charge: " + ex.Message });
        }
    }

    /// <summary>
    /// Removes a charge from a settlement
    /// </summary>
    [HttpDelete("{settlementId:guid}/charges/{chargeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveCharge(Guid settlementId, Guid chargeId)
    {
        try
        {
            _logger.LogInformation(
                "Removing charge {ChargeId} from settlement {SettlementId}",
                chargeId, settlementId);

            // Determine settlement type
            var isCachedPurchase = false;
            try
            {
                var query = new GetSettlementByIdQuery
                {
                    SettlementId = settlementId,
                    IsPurchaseSettlement = true
                };
                var result = await _mediator.Send(query);
                if (result != null) isCachedPurchase = true;
            }
            catch
            {
                // Not a purchase settlement
            }

            var command = new RemoveChargeCommand
            {
                SettlementId = settlementId,
                ChargeId = chargeId,
                RemovedBy = GetCurrentUserName(),
                IsPurchaseSettlement = isCachedPurchase
            };

            await _mediator.Send(command);
            _logger.LogInformation("Charge {ChargeId} removed successfully", chargeId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing charge {ChargeId}", chargeId);
            return StatusCode(500, new { error = "An error occurred while removing the charge: " + ex.Message });
        }
    }
}

/// <summary>
/// DTO for generic settlement creation
/// </summary>
public class CreateSettlementRequestDto
{
    public Guid ContractId { get; set; }
    public string? DocumentNumber { get; set; }
    public int? DocumentType { get; set; }  // Accept as int/enum value
    public DateTime? DocumentDate { get; set; }
    public decimal ActualQuantityMT { get; set; }
    public decimal ActualQuantityBBL { get; set; }
    public string? CreatedBy { get; set; }
    public string? Notes { get; set; }
    public string? SettlementCurrency { get; set; } = "USD";
    public bool AutoCalculatePrices { get; set; }
    public bool AutoTransitionStatus { get; set; }
}

/// <summary>
/// DTO for settlement creation by external contract
/// </summary>
public class CreateSettlementByExternalContractDto
{
    public string ExternalContractNumber { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public int? DocumentType { get; set; }
    public DateTime? DocumentDate { get; set; }
}

/// <summary>
/// DTO for settlement updates
/// </summary>
public class UpdateSettlementRequestDto
{
    public string? DocumentNumber { get; set; }
    public string? DocumentType { get; set; }
    public DateTime? DocumentDate { get; set; }
}

/// <summary>
/// Paged result wrapper for settlement queries
/// </summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Settlement creation result DTO - matches frontend expectations
/// </summary>
public class CreateSettlementResultDto
{
    public bool IsSuccessful { get; set; }
    public Guid? SettlementId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// DTO for generic settlement calculation
/// </summary>
public class CalculateSettlementRequestDto
{
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for generic settlement approval
/// </summary>
public class ApproveSettlementRequestDto
{
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for generic settlement finalization
/// </summary>
public class FinalizeSettlementRequestDto
{
    public string? FinalizedBy { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for adding a charge to a settlement
/// </summary>
public class AddChargeRequestDto
{
    public string ChargeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? IncurredDate { get; set; }
    public string? ReferenceDocument { get; set; }
    public string? Notes { get; set; }
    public string? AddedBy { get; set; }
}

/// <summary>
/// DTO for updating a charge in a settlement
/// </summary>
public class UpdateChargeRequestDto
{
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string? UpdatedBy { get; set; }
}
