using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using OilTrading.Application.Queries.Contracts;
using OilTrading.Core.Entities;
using OilTrading.Api.Attributes;
using OilTrading.Core.Common;
using System.ComponentModel.DataAnnotations;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for managing contract settlements in the oil trading system.
/// Handles settlement creation, calculation, charge management, and workflow operations.
/// </summary>
[ApiController]
[Route("api/settlements")]
[Produces("application/json")]
public class SettlementController : ControllerBase
{
    private readonly ISettlementCalculationService _settlementService;
    private readonly ILogger<SettlementController> _logger;
    private readonly IMediator _mediator;

    public SettlementController(
        ISettlementCalculationService settlementService,
        ILogger<SettlementController> logger,
        IMediator mediator)
    {
        _settlementService = settlementService;
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Helper method to get current user name from context
    /// </summary>
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

    #region Query Endpoints

    /// <summary>
    /// Gets a settlement by external contract number for easy lookup
    /// </summary>
    /// <param name="externalContractNumber">External contract number</param>
    /// <returns>Settlement details if found</returns>
    [HttpGet("by-external-contract/{externalContractNumber}")]
    [ProducesResponseType(typeof(ContractSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByExternalContractNumber(string externalContractNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(externalContractNumber))
            {
                return BadRequest("External contract number cannot be empty");
            }

            var settlement = await _settlementService.GetSettlementByExternalContractNumberAsync(externalContractNumber);
            
            if (settlement == null)
            {
                _logger.LogInformation("Settlement not found for external contract number: {ExternalContractNumber}", externalContractNumber);
                return NotFound($"Settlement not found for external contract number: {externalContractNumber}");
            }

            _logger.LogInformation("Retrieved settlement {SettlementId} for external contract {ExternalContractNumber}", 
                settlement.Id, externalContractNumber);
            
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement by external contract number: {ExternalContractNumber}", externalContractNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving the settlement");
        }
    }

    /// <summary>
    /// Gets a settlement by contract ID
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <returns>Settlement details if found</returns>
    [HttpGet("contract/{contractId:guid}")]
    [ProducesResponseType(typeof(ContractSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByContractId(Guid contractId)
    {
        try
        {
            if (contractId == Guid.Empty)
            {
                return BadRequest("Contract ID cannot be empty");
            }

            var settlement = await _settlementService.GetSettlementByContractIdAsync(contractId);
            
            if (settlement == null)
            {
                _logger.LogInformation("Settlement not found for contract ID: {ContractId}", contractId);
                return NotFound($"Settlement not found for contract ID: {contractId}");
            }

            _logger.LogInformation("Retrieved settlement {SettlementId} for contract {ContractId}", 
                settlement.Id, contractId);
            
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement by contract ID: {ContractId}", contractId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving the settlement");
        }
    }

    /// <summary>
    /// Gets a settlement by settlement ID
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>Settlement details if found</returns>
    [HttpGet("{settlementId:guid}")]
    [ProducesResponseType(typeof(ContractSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(Guid settlementId)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            var settlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            
            if (settlement == null)
            {
                _logger.LogInformation("Settlement not found: {SettlementId}", settlementId);
                return NotFound($"Settlement not found: {settlementId}");
            }

            _logger.LogInformation("Retrieved settlement {SettlementId}", settlementId);
            
            return Ok(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving the settlement");
        }
    }

    /// <summary>
    /// Gets settlements with filtering options
    /// </summary>
    /// <param name="startDate">Start date for filtering (optional)</param>
    /// <param name="endDate">End date for filtering (optional)</param>
    /// <param name="status">Settlement status for filtering (optional)</param>
    /// <param name="contractId">Contract ID for filtering (optional)</param>
    /// <param name="externalContractNumber">External contract number for filtering (optional)</param>
    /// <param name="documentNumber">Document number for filtering (optional)</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Page size for pagination (default: 20, max: 100)</param>
    /// <returns>Paginated list of settlements</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContractSettlementListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSettlements(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? contractId = null,
        [FromQuery] string? externalContractNumber = null,
        [FromQuery] string? documentNumber = null,
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20)
    {
        try
        {
            // Validate date range
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                return BadRequest("Start date cannot be after end date");
            }

            // Parse status if provided
            ContractSettlementStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ContractSettlementStatus>(status, true, out var parsedStatus))
                {
                    return BadRequest($"Invalid status value: {status}. Valid values are: {string.Join(", ", Enum.GetNames<ContractSettlementStatus>())}");
                }
                statusEnum = parsedStatus;
            }

            // Get settlements with filters
            IEnumerable<ContractSettlementDto> settlements;

            if (startDate.HasValue && endDate.HasValue)
            {
                settlements = await _settlementService.GetSettlementsByDateRangeAsync(startDate.Value, endDate.Value);
            }
            else if (statusEnum.HasValue)
            {
                settlements = await _settlementService.GetSettlementsByStatusAsync(statusEnum.Value);
            }
            else
            {
                // If no specific filters, get recent settlements (last 30 days)
                var defaultStartDate = DateTime.UtcNow.AddDays(-30);
                var defaultEndDate = DateTime.UtcNow;
                settlements = await _settlementService.GetSettlementsByDateRangeAsync(defaultStartDate, defaultEndDate);
            }

            // Apply additional filters
            var filteredSettlements = settlements.AsQueryable();

            if (contractId.HasValue)
            {
                filteredSettlements = filteredSettlements.Where(s => s.ContractId == contractId.Value);
            }

            if (!string.IsNullOrWhiteSpace(externalContractNumber))
            {
                filteredSettlements = filteredSettlements.Where(s => 
                    s.ExternalContractNumber.Contains(externalContractNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(documentNumber))
            {
                filteredSettlements = filteredSettlements.Where(s => 
                    !string.IsNullOrEmpty(s.DocumentNumber) && 
                    s.DocumentNumber.Contains(documentNumber, StringComparison.OrdinalIgnoreCase));
            }

            // Convert to list DTOs
            var settlementList = filteredSettlements.Select(s => new ContractSettlementListDto
            {
                Id = s.Id,
                ContractId = s.ContractId,
                ContractNumber = s.ContractNumber,
                ExternalContractNumber = s.ExternalContractNumber,
                DocumentNumber = s.DocumentNumber,
                DocumentType = s.DocumentType,
                DocumentDate = s.DocumentDate,
                ActualQuantityMT = s.ActualQuantityMT,
                ActualQuantityBBL = s.ActualQuantityBBL,
                TotalSettlementAmount = s.TotalSettlementAmount,
                SettlementCurrency = s.SettlementCurrency,
                Status = s.Status,
                IsFinalized = s.IsFinalized,
                CreatedDate = s.CreatedDate,
                CreatedBy = s.CreatedBy,
                ChargesCount = s.Charges.Count
            });

            // Apply pagination
            var totalCount = settlementList.Count();
            var pagedSettlements = settlementList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<ContractSettlementListDto>(pagedSettlements, totalCount, pageNumber, pageSize);

            _logger.LogInformation("Retrieved {Count} settlements (page {Page} of {TotalPages})", 
                pagedSettlements.Count, pageNumber, result.TotalPages);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlements list");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving settlements");
        }
    }

    #endregion

    #region Management Endpoints

    /// <summary>
    /// Creates a new settlement from Bill of Lading or Certificate of Quantity data
    /// </summary>
    /// <param name="dto">Settlement creation data</param>
    /// <returns>Created settlement details</returns>
    [HttpPost]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(CreateSettlementResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSettlement([FromBody] CreateSettlementDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ValidationErrors = errors,
                    ErrorMessage = "Validation failed"
                });
            }

            var createdBy = GetCurrentUserName();
            dto.CreatedBy = createdBy;

            var settlement = await _settlementService.CreateOrUpdateSettlementAsync(
                dto.ContractId,
                dto.DocumentNumber ?? string.Empty,
                dto.DocumentType,
                dto.ActualQuantityMT,
                dto.ActualQuantityBBL,
                dto.DocumentDate,
                createdBy);

            var result = new CreateSettlementResultDto
            {
                IsSuccessful = true,
                SettlementId = settlement.Id,
                Settlement = settlement
            };

            _logger.LogInformation("Settlement {SettlementId} created successfully for contract {ContractId}", 
                settlement.Id, dto.ContractId);

            return CreatedAtAction(nameof(GetById), new { settlementId = settlement.Id }, result);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Contract not found for settlement creation: {ContractId}", dto.ContractId);
            return NotFound(new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for settlement creation: {ContractId}", dto.ContractId);
            return BadRequest(new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for settlement creation: {ContractId}", dto.ContractId);
            return UnprocessableEntity(new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement for contract: {ContractId}. Exception: {Message}. Stack: {StackTrace}",
                dto.ContractId, ex.Message, ex.StackTrace);
            return StatusCode(StatusCodes.Status500InternalServerError, new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = $"An error occurred while creating the settlement: {ex.Message}",
                ValidationErrors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
            });
        }
    }

    /// <summary>
    /// Creates a new settlement by external contract number
    /// This endpoint resolves the external contract number to internal contract GUID first
    /// </summary>
    /// <param name="dto">Settlement creation data with external contract number</param>
    /// <returns>Created settlement details</returns>
    [HttpPost("create-by-external-contract")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(CreateSettlementResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateByExternalContract([FromBody] CreateSettlementByExternalContractDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ValidationErrors = errors,
                    ErrorMessage = "Validation failed"
                });
            }

            // Step 1: Resolve external contract number to contract ID
            var resolutionQuery = new ResolveContractByExternalNumberQuery
            {
                ExternalContractNumber = dto.ExternalContractNumber,
                ExpectedContractType = dto.ExpectedContractType,
                ExpectedTradingPartnerId = dto.TradingPartnerId,
                ExpectedProductId = dto.ProductId
            };

            var resolution = await _mediator.Send(resolutionQuery);

            // Handle resolution error or multiple matches
            if (!resolution.Success)
            {
                if (resolution.Candidates.Count > 0)
                {
                    _logger.LogWarning(
                        "Multiple contracts found for external number {ExternalNumber}, count={CandidateCount}",
                        dto.ExternalContractNumber, resolution.Candidates.Count);

                    return UnprocessableEntity(new CreateSettlementResultDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Multiple contracts match the external number. Please specify additional filters (contract type, trading partner, or product).",
                        ValidationErrors = resolution.Candidates
                            .Select(c => $"{c.ContractType}: {c.ContractNumber} ({c.ExternalContractNumber}) - {c.TradingPartnerName}")
                            .ToList()
                    });
                }

                _logger.LogWarning("No contract found for external number {ExternalNumber}", dto.ExternalContractNumber);
                return NotFound(new CreateSettlementResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = resolution.ErrorMessage ?? $"No contract found with external contract number: {dto.ExternalContractNumber}"
                });
            }

            // Step 2: Create settlement with resolved contract ID
            var createDto = new CreateSettlementDto
            {
                ContractId = resolution.ContractId!.Value,
                DocumentNumber = dto.DocumentNumber,
                DocumentType = dto.DocumentType,
                DocumentDate = dto.DocumentDate,
                ActualQuantityMT = dto.ActualQuantityMT,
                ActualQuantityBBL = dto.ActualQuantityBBL,
                CreatedBy = GetCurrentUserName(),
                Notes = dto.Notes,
                SettlementCurrency = dto.SettlementCurrency,
                AutoCalculatePrices = dto.AutoCalculatePrices,
                AutoTransitionStatus = dto.AutoTransitionStatus
            };

            var settlement = await _settlementService.CreateOrUpdateSettlementAsync(
                createDto.ContractId,
                createDto.DocumentNumber ?? string.Empty,
                createDto.DocumentType,
                createDto.ActualQuantityMT,
                createDto.ActualQuantityBBL,
                createDto.DocumentDate,
                createDto.CreatedBy);

            var result = new CreateSettlementResultDto
            {
                IsSuccessful = true,
                SettlementId = settlement.Id,
                Settlement = settlement
            };

            _logger.LogInformation(
                "Settlement {SettlementId} created successfully for external contract {ExternalNumber} (resolved to {ContractId})",
                settlement.Id, dto.ExternalContractNumber, resolution.ContractId);

            return CreatedAtAction(nameof(GetById), new { settlementId = settlement.Id }, result);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Contract not found for settlement creation by external number: {ExternalNumber}", dto.ExternalContractNumber);
            return NotFound(new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for settlement creation: {ExternalNumber}", dto.ExternalContractNumber);
            return BadRequest(new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for settlement creation: {ExternalNumber}", dto.ExternalContractNumber);
            return UnprocessableEntity(new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating settlement by external contract: {ExternalNumber}. Exception: {Message}",
                dto.ExternalContractNumber, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new CreateSettlementResultDto
            {
                IsSuccessful = false,
                ErrorMessage = $"An error occurred while creating the settlement: {ex.Message}",
                ValidationErrors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
            });
        }
    }

    /// <summary>
    /// Updates settlement quantities and document information
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="dto">Updated settlement data</param>
    /// <returns>Updated settlement details</returns>
    [HttpPut("{settlementId:guid}")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(ContractSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSettlement(Guid settlementId, [FromBody] UpdateSettlementDto dto)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if settlement exists
            var existingSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (existingSettlement == null)
            {
                return NotFound($"Settlement not found: {settlementId}");
            }

            if (existingSettlement.IsFinalized)
            {
                return UnprocessableEntity("Cannot update finalized settlement");
            }

            var updatedBy = GetCurrentUserName();

            // Create updated settlement from the original
            var updatedSettlement = await _settlementService.CreateOrUpdateSettlementAsync(
                existingSettlement.ContractId,
                dto.DocumentNumber ?? existingSettlement.DocumentNumber ?? string.Empty,
                dto.DocumentType ?? (DocumentType)Enum.Parse(typeof(DocumentType), existingSettlement.DocumentType),
                dto.ActualQuantityMT ?? existingSettlement.ActualQuantityMT,
                dto.ActualQuantityBBL ?? existingSettlement.ActualQuantityBBL,
                dto.DocumentDate ?? existingSettlement.DocumentDate,
                updatedBy);

            _logger.LogInformation("Settlement {SettlementId} updated successfully", settlementId);

            return Ok(updatedSettlement);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Contract not found for settlement update: {SettlementId}", settlementId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for settlement update: {SettlementId}", settlementId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for settlement update: {SettlementId}", settlementId);
            return UnprocessableEntity(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while updating the settlement");
        }
    }

    /// <summary>
    /// Triggers recalculation of settlement amounts and totals
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>Recalculated settlement details</returns>
    [HttpPost("{settlementId:guid}/recalculate")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(ContractSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecalculateSettlement(Guid settlementId)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            // Check if settlement exists
            var existingSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (existingSettlement == null)
            {
                return NotFound($"Settlement not found: {settlementId}");
            }

            if (existingSettlement.IsFinalized)
            {
                return UnprocessableEntity("Cannot recalculate finalized settlement");
            }

            var updatedBy = GetCurrentUserName();
            var recalculatedSettlement = await _settlementService.RecalculateSettlementAsync(settlementId, updatedBy);

            _logger.LogInformation("Settlement {SettlementId} recalculated successfully", settlementId);

            return Ok(recalculatedSettlement);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Contract not found for settlement recalculation: {SettlementId}", settlementId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for settlement recalculation: {SettlementId}", settlementId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for settlement recalculation: {SettlementId}", settlementId);
            return UnprocessableEntity(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while recalculating the settlement");
        }
    }

    /// <summary>
    /// Finalizes a settlement, preventing further modifications
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>Finalized settlement details</returns>
    [HttpPost("{settlementId:guid}/finalize")]
    [RiskCheck(RiskCheckLevel.Critical, allowOverride: false)]
    [ProducesResponseType(typeof(ContractSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> FinalizeSettlement(Guid settlementId)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            // Check if settlement exists
            var existingSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (existingSettlement == null)
            {
                return NotFound($"Settlement not found: {settlementId}");
            }

            if (existingSettlement.IsFinalized)
            {
                return UnprocessableEntity("Settlement is already finalized");
            }

            var finalizedBy = GetCurrentUserName();
            var finalizedSettlement = await _settlementService.FinalizeSettlementAsync(settlementId, finalizedBy);

            _logger.LogInformation("Settlement {SettlementId} finalized successfully by {User}", settlementId, finalizedBy);

            return Ok(finalizedSettlement);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Contract not found for settlement finalization: {SettlementId}", settlementId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for settlement finalization: {SettlementId}", settlementId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for settlement finalization: {SettlementId}", settlementId);
            return UnprocessableEntity(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while finalizing the settlement");
        }
    }

    #endregion

    #region Charge Management Endpoints

    /// <summary>
    /// Adds a new charge to a settlement
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="dto">Charge details</param>
    /// <returns>Added charge details</returns>
    [HttpPost("{settlementId:guid}/charges")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(ChargeOperationResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddCharge(Guid settlementId, [FromBody] AddChargeDto dto)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new ChargeOperationResultDto
                {
                    IsSuccessful = false,
                    ValidationErrors = errors,
                    ErrorMessage = "Validation failed"
                });
            }

            // Validate settlement ID matches
            if (dto.SettlementId != settlementId)
            {
                return BadRequest("Settlement ID in URL does not match settlement ID in request body");
            }

            // Check if settlement exists
            var existingSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (existingSettlement == null)
            {
                return NotFound(new ChargeOperationResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Settlement not found: {settlementId}"
                });
            }

            if (existingSettlement.IsFinalized)
            {
                return UnprocessableEntity(new ChargeOperationResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Cannot add charges to finalized settlement"
                });
            }

            var addedBy = GetCurrentUserName();
            dto.AddedBy = addedBy;

            var addedCharge = await _settlementService.AddOrUpdateChargeAsync(
                settlementId,
                dto.ChargeType,
                dto.Amount,
                dto.Description,
                dto.ReferenceDocument,
                addedBy);

            // Get updated settlement for totals
            var updatedSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);

            var result = new ChargeOperationResultDto
            {
                IsSuccessful = true,
                ChargeId = addedCharge.Id,
                Charge = addedCharge,
                UpdatedTotals = new SettlementTotalsDto
                {
                    CargoValue = updatedSettlement?.CargoValue ?? 0,
                    TotalCharges = updatedSettlement?.TotalCharges ?? 0,
                    TotalSettlementAmount = updatedSettlement?.TotalSettlementAmount ?? 0,
                    Currency = updatedSettlement?.SettlementCurrency ?? "USD",
                    ChargesCount = updatedSettlement?.Charges.Count ?? 0
                }
            };

            _logger.LogInformation("Charge {ChargeId} added to settlement {SettlementId}", addedCharge.Id, settlementId);

            return CreatedAtAction(nameof(GetCharges), new { settlementId }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for adding charge to settlement: {SettlementId}", settlementId);
            return BadRequest(new ChargeOperationResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for adding charge to settlement: {SettlementId}", settlementId);
            return UnprocessableEntity(new ChargeOperationResultDto
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding charge to settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ChargeOperationResultDto
            {
                IsSuccessful = false,
                ErrorMessage = "An error occurred while adding the charge"
            });
        }
    }

    /// <summary>
    /// Updates an existing charge in a settlement
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="chargeId">Charge ID</param>
    /// <param name="dto">Updated charge details</param>
    /// <returns>Updated charge details</returns>
    [HttpPut("{settlementId:guid}/charges/{chargeId:guid}")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(ChargeOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCharge(Guid settlementId, Guid chargeId, [FromBody] UpdateChargeDto dto)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            if (chargeId == Guid.Empty)
            {
                return BadRequest("Charge ID cannot be empty");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate settlement and charge IDs match
            if (dto.SettlementId != settlementId || dto.ChargeId != chargeId)
            {
                return BadRequest("Settlement ID or Charge ID in URL does not match request body");
            }

            // Check if settlement exists
            var existingSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (existingSettlement == null)
            {
                return NotFound($"Settlement not found: {settlementId}");
            }

            if (existingSettlement.IsFinalized)
            {
                return UnprocessableEntity("Cannot update charges in finalized settlement");
            }

            // Find the existing charge
            var existingCharge = existingSettlement.Charges.FirstOrDefault(c => c.Id == chargeId);
            if (existingCharge == null)
            {
                return NotFound($"Charge not found: {chargeId}");
            }

            var updatedBy = GetCurrentUserName();
            dto.UpdatedBy = updatedBy;

            // For updates, we need to remove the old charge and add the updated one
            // This is a limitation of the current service interface
            await _settlementService.RemoveChargeAsync(settlementId, chargeId, updatedBy);

            var updatedCharge = await _settlementService.AddOrUpdateChargeAsync(
                settlementId,
                dto.ChargeType ?? (ChargeType)Enum.Parse(typeof(ChargeType), existingCharge.ChargeType),
                dto.Amount ?? existingCharge.Amount,
                dto.Description ?? existingCharge.Description,
                dto.ReferenceDocument ?? existingCharge.ReferenceDocument,
                updatedBy);

            // Get updated settlement for totals
            var updatedSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);

            var result = new ChargeOperationResultDto
            {
                IsSuccessful = true,
                ChargeId = updatedCharge.Id,
                Charge = updatedCharge,
                UpdatedTotals = new SettlementTotalsDto
                {
                    CargoValue = updatedSettlement?.CargoValue ?? 0,
                    TotalCharges = updatedSettlement?.TotalCharges ?? 0,
                    TotalSettlementAmount = updatedSettlement?.TotalSettlementAmount ?? 0,
                    Currency = updatedSettlement?.SettlementCurrency ?? "USD",
                    ChargesCount = updatedSettlement?.Charges.Count ?? 0
                }
            };

            _logger.LogInformation("Charge {ChargeId} updated in settlement {SettlementId}", chargeId, settlementId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating charge: {ChargeId} in settlement: {SettlementId}", chargeId, settlementId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for updating charge: {ChargeId} in settlement: {SettlementId}", chargeId, settlementId);
            return UnprocessableEntity(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating charge: {ChargeId} in settlement: {SettlementId}", chargeId, settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while updating the charge");
        }
    }

    /// <summary>
    /// Removes a charge from a settlement
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <param name="chargeId">Charge ID</param>
    /// <returns>Operation result</returns>
    [HttpDelete("{settlementId:guid}/charges/{chargeId:guid}")]
    [RiskCheck(RiskCheckLevel.Standard, allowOverride: true, "RiskManager", "SeniorTrader")]
    [ProducesResponseType(typeof(ChargeOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteCharge(Guid settlementId, Guid chargeId)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            if (chargeId == Guid.Empty)
            {
                return BadRequest("Charge ID cannot be empty");
            }

            // Check if settlement exists
            var existingSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (existingSettlement == null)
            {
                return NotFound($"Settlement not found: {settlementId}");
            }

            if (existingSettlement.IsFinalized)
            {
                return UnprocessableEntity("Cannot remove charges from finalized settlement");
            }

            // Find the existing charge
            var existingCharge = existingSettlement.Charges.FirstOrDefault(c => c.Id == chargeId);
            if (existingCharge == null)
            {
                return NotFound($"Charge not found: {chargeId}");
            }

            var removedBy = GetCurrentUserName();
            var isRemoved = await _settlementService.RemoveChargeAsync(settlementId, chargeId, removedBy);

            if (!isRemoved)
            {
                return UnprocessableEntity("Failed to remove charge");
            }

            // Get updated settlement for totals
            var updatedSettlement = await _settlementService.GetSettlementByIdAsync(settlementId);

            var result = new ChargeOperationResultDto
            {
                IsSuccessful = true,
                ChargeId = chargeId,
                UpdatedTotals = new SettlementTotalsDto
                {
                    CargoValue = updatedSettlement?.CargoValue ?? 0,
                    TotalCharges = updatedSettlement?.TotalCharges ?? 0,
                    TotalSettlementAmount = updatedSettlement?.TotalSettlementAmount ?? 0,
                    Currency = updatedSettlement?.SettlementCurrency ?? "USD",
                    ChargesCount = updatedSettlement?.Charges.Count ?? 0
                }
            };

            _logger.LogInformation("Charge {ChargeId} removed from settlement {SettlementId}", chargeId, settlementId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for removing charge: {ChargeId} from settlement: {SettlementId}", chargeId, settlementId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for removing charge: {ChargeId} from settlement: {SettlementId}", chargeId, settlementId);
            return UnprocessableEntity(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing charge: {ChargeId} from settlement: {SettlementId}", chargeId, settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while removing the charge");
        }
    }

    /// <summary>
    /// Gets all charges for a settlement
    /// </summary>
    /// <param name="settlementId">Settlement ID</param>
    /// <returns>List of charges for the settlement</returns>
    [HttpGet("{settlementId:guid}/charges")]
    [ProducesResponseType(typeof(IEnumerable<SettlementChargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharges(Guid settlementId)
    {
        try
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest("Settlement ID cannot be empty");
            }

            var settlement = await _settlementService.GetSettlementByIdAsync(settlementId);
            if (settlement == null)
            {
                return NotFound($"Settlement not found: {settlementId}");
            }

            _logger.LogInformation("Retrieved {ChargeCount} charges for settlement {SettlementId}", 
                settlement.Charges.Count, settlementId);

            return Ok(settlement.Charges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving charges for settlement: {SettlementId}", settlementId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while retrieving charges");
        }
    }

    #endregion
}

/// <summary>
/// DTO for updating settlement information
/// </summary>
public class UpdateSettlementDto
{
    /// <summary>
    /// Updated document number (optional)
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Updated document type (optional)
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Updated document date (optional)
    /// </summary>
    public DateTime? DocumentDate { get; set; }

    /// <summary>
    /// Updated actual quantity in MT (optional)
    /// </summary>
    public decimal? ActualQuantityMT { get; set; }

    /// <summary>
    /// Updated actual quantity in BBL (optional)
    /// </summary>
    public decimal? ActualQuantityBBL { get; set; }

    /// <summary>
    /// Notes for the update
    /// </summary>
    public string? Notes { get; set; }
}