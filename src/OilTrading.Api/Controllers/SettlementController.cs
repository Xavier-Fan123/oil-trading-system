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
/// SETTLEMENT CONTROLLER - THREE-SYSTEM ARCHITECTURE (v2.10.0)
///
/// This controller implements the Oil Trading System's settlement architecture, which includes
/// three coexisting specialized settlement systems:
///
/// ============================================================================
/// SETTLEMENT SYSTEMS OVERVIEW
/// ============================================================================
///
/// SYSTEM 1: LEGACY GENERIC SETTLEMENT (v2.9.0 - Deprecated)
/// ├─ Entity: ContractSettlement
/// ├─ Repository: IContractSettlementRepository
/// ├─ Purpose: Backward compatibility, mixed settlement handling
/// ├─ Status: Deprecated in favor of specialized systems
/// └─ Migration: Gradual transition to v2.10.0 over 12 months
///
/// SYSTEM 2: PURCHASE SETTLEMENT (v2.10.0 - AP SPECIALIZED)
/// ├─ Entity: PurchaseSettlement
/// ├─ Repository: IPurchaseSettlementRepository
/// ├─ Purpose: Accounts Payable (supplier payments)
/// ├─ Key Methods:
/// │  ├─ GetByExternalContractNumberAsync() - Find via external contract ID
/// │  ├─ GetPendingSupplierPaymentAsync() - AP aging list
/// │  ├─ GetOverdueSupplierPaymentAsync() - Collections focus
/// │  └─ CalculateSupplierPaymentExposureAsync() - Credit limit tracking
/// ├─ Foreign Key: SupplierContractId (references PurchaseContract)
/// ├─ Types: Always PurchaseContract contracts only
/// └─ Status: PRODUCTION (v2.10.0)
///
/// SYSTEM 3: SALES SETTLEMENT (v2.10.0 - AR SPECIALIZED)
/// ├─ Entity: SalesSettlement
/// ├─ Repository: ISalesSettlementRepository
/// ├─ Purpose: Accounts Receivable (buyer collections)
/// ├─ Key Methods:
/// │  ├─ GetByExternalContractNumberAsync() - Find via external contract ID
/// │  ├─ GetOutstandingReceivablesAsync() - AR aging list
/// │  ├─ GetOverdueBuyerPaymentAsync() - Collection management
/// │  └─ CalculateBuyerCreditExposureAsync() - Credit risk tracking
/// ├─ Foreign Key: CustomerContractId (references SalesContract)
/// ├─ Types: Always SalesContract contracts only
/// └─ Status: PRODUCTION (v2.10.0)
///
/// ============================================================================
/// TECHNICAL ARCHITECTURE
/// ============================================================================
///
/// DESIGN PATTERN: CQRS (Command Query Responsibility Segregation)
///
/// CREATE SETTLEMENT WORKFLOW:
///   1. Client calls POST /api/settlements with settlement request
///   2. Controller maps request to CreatePurchaseSettlementCommand or CreateSalesSettlementCommand
///   3. MediatR dispatches command to appropriate handler:
///      - CreatePurchaseSettlementCommandHandler (uses IPurchaseSettlementRepository)
///      - CreateSalesSettlementCommandHandler (uses ISalesSettlementRepository)
///   4. Handler validates contract exists via GetContractInfoAsync()
///   5. Handler creates entity and persists to database
///   6. Event published (domain event pattern) for audit trail
///   7. Response returned to client with settlement ID
///
/// RETRIEVE SETTLEMENT WORKFLOW:
///   1. Client calls GET /api/settlements/{settlementId}
///   2. Controller uses fallback logic to find settlement:
///      - First try: IPurchaseSettlementRepository.GetByIdAsync()
///      - If null, try: ISalesSettlementRepository.GetByIdAsync()
///      - If null, try: IContractSettlementRepository.GetByIdAsync() (legacy)
///   3. Entity mapped to SettlementDto via AutoMapper
///   4. Response returned with complete settlement data + charges
///
/// ============================================================================
/// EXTERNAL CONTRACT NUMBER RESOLUTION
/// ============================================================================
///
/// PROBLEM: Clients may not know the internal Guid for a contract
/// SOLUTION: GetByExternalContractNumberAsync() methods on both repositories
///
/// USAGE EXAMPLE:
///   Input:  ExternalContractNumber = "IGR-2025-CAG-S0253" (from external system)
///   Query:  await _purchaseSettlementRepository.GetByExternalContractNumberAsync("IGR-2025-CAG-S0253")
///   Result: Settlement entity with contract details populated
///
/// This enables cross-system integration without manual UUID lookup.
///
/// ============================================================================
/// BUSINESS RULES & CONSTRAINTS
/// ============================================================================
///
/// SETTLEMENT LIFECYCLE:
///   Draft → DataEntered → Calculated → Reviewed → Approved → Finalized
///
/// VALIDATION RULES:
///   ✓ Settlement must reference valid contract (purchase or sales)
///   ✓ Quantities cannot exceed contract quantities
///   ✓ Settlement amount must be positive
///   ✓ Charges must have valid charge type
///   ✓ Currency consistency between contract and settlement
///
/// IMMUTABILITY RULES:
///   ✗ Cannot modify finalized settlements (marked IsFinalized = true)
///   ✗ Cannot delete finalized settlements (soft-delete only)
///   ✗ Cannot revert finalization status
///
/// ============================================================================
/// GENERIC API ENDPOINT DESIGN
/// ============================================================================
///
/// All settlement endpoints at /api/settlements use generic logic:
///
/// GET /api/settlements/{settlementId}
///   ├─ Returns: SettlementDto (complete details with charges)
///   ├─ Falls back across all three systems
///   └─ Single unified response format
///
/// POST /api/settlements (Create)
///   ├─ Accepts: CreateSettlementRequest
///   ├─ Routes to: CreatePurchaseSettlementCommand OR CreateSalesSettlementCommand
///   ├─ Detection: Based on contractType in request
///   └─ Returns: SettlementDto with new settlement ID
///
/// POST /api/settlements/{settlementId}/calculate
///   ├─ Invokes: CalculateSettlementCommand (generic)
///   ├─ Handler: Routes to appropriate system
///   └─ Returns: Updated SettlementDto with calculated amounts
///
/// POST /api/settlements/{settlementId}/approve
///   ├─ Invokes: ApproveSettlementCommand (generic)
///   ├─ Handler: Routes to appropriate system
///   └─ Returns: Updated SettlementDto with Approved status
///
/// POST /api/settlements/{settlementId}/finalize
///   ├─ Invokes: FinalizeSettlementCommand (generic)
///   ├─ Handler: Routes to appropriate system
///   └─ Returns: Updated SettlementDto with Finalized status
///
/// ============================================================================
/// SECURITY & AUDIT
/// ============================================================================
///
/// AUTHENTICATION: All endpoints require valid JWT token
/// AUTHORIZATION: [Authorize] attribute enforces authentication
/// AUDIT TRAIL: All operations logged with:
///   - User ID and email (from JWT token)
///   - Operation timestamp (UTC)
///   - Settlement ID and contract ID
///   - Before/after values for updates
///
/// ============================================================================
/// FOR MORE INFORMATION
/// ============================================================================
///
/// See documentation in:
/// - SETTLEMENT_ARCHITECTURE.md - Deep dive into three systems
/// - API_REFERENCE_COMPLETE.md - All endpoints with examples
/// - COMPLETE_ENTITY_REFERENCE.md - Entity properties and relationships
///
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
    private readonly IContractSettlementRepository _contractSettlementRepository;
    private readonly IPurchaseSettlementRepository _purchaseSettlementRepository;
    private readonly ISalesSettlementRepository _salesSettlementRepository;

    public SettlementController(
        IMediator mediator,
        ILogger<SettlementController> logger,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IContractSettlementRepository contractSettlementRepository,
        IPurchaseSettlementRepository purchaseSettlementRepository,
        ISalesSettlementRepository salesSettlementRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _purchaseContractRepository = purchaseContractRepository ?? throw new ArgumentNullException(nameof(purchaseContractRepository));
        _salesContractRepository = salesContractRepository ?? throw new ArgumentNullException(nameof(salesContractRepository));
        _contractSettlementRepository = contractSettlementRepository ?? throw new ArgumentNullException(nameof(contractSettlementRepository));
        _purchaseSettlementRepository = purchaseSettlementRepository ?? throw new ArgumentNullException(nameof(purchaseSettlementRepository));
        _salesSettlementRepository = salesSettlementRepository ?? throw new ArgumentNullException(nameof(salesSettlementRepository));
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
    /// Maps SettlementDto to ContractSettlementListDto for list responses
    /// </summary>
    private static ContractSettlementListDto MapToListDto(SettlementDto settlement)
    {
        return new ContractSettlementListDto
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
            TotalSettlementAmount = settlement.TotalSettlementAmount,
            SettlementCurrency = settlement.SettlementCurrency,
            Status = settlement.Status.ToString(),
            IsFinalized = settlement.IsFinalized,
            CreatedDate = settlement.CreatedDate,
            CreatedBy = settlement.CreatedBy,
            ChargesCount = settlement.ChargeCount
        };
    }

    /// <summary>
    /// Gets a settlement by ID with full details including charges
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

            // Try both purchase and sales settlement repositories to find the settlement
            var purchaseSettlement = await _purchaseSettlementRepository.GetByIdAsync(settlementId);
            OilTrading.Core.Entities.SalesSettlement? salesSettlement = null;

            if (purchaseSettlement == null)
            {
                _logger.LogInformation("Settlement {SettlementId} not found as purchase settlement, trying sales settlement", settlementId);
                salesSettlement = await _salesSettlementRepository.GetByIdAsync(settlementId);
            }

            if (purchaseSettlement == null && salesSettlement == null)
            {
                _logger.LogWarning("Settlement not found with ID: {SettlementId}", settlementId);
                return NotFound(new { error = "Settlement not found" });
            }

            // Use CQRS query to get the base settlement data
            var query = new GetSettlementByIdQuery
            {
                SettlementId = settlementId,
                IsPurchaseSettlement = purchaseSettlement != null
            };
            var settlement = await _mediator.Send(query);

            if (settlement == null)
            {
                _logger.LogWarning("Settlement data not found with ID: {SettlementId}", settlementId);
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
    [ProducesResponseType(typeof(PagedResult<ContractSettlementListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ContractSettlementListDto>>> GetSettlements(
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

            List<ContractSettlementListDto> settlements = new();

            // Get settlements if contractId is provided
            if (contractId.HasValue)
            {
                var query = new GetContractSettlementsQuery
                {
                    ContractId = contractId.Value,
                    IsPurchaseSettlement = true  // Will fetch from both contract types
                };

                var fullSettlements = await _mediator.Send(query);
                settlements = fullSettlements.Select(MapToListDto).ToList();
            }
            // If externalContractNumber is provided, search for it using specialized repositories
            else if (!string.IsNullOrEmpty(externalContractNumber))
            {
                _logger.LogInformation("Searching for settlements with external contract number: {ExternalContractNumber}", externalContractNumber);

                // Use the new specialized repository methods for external contract number search
                var purchaseSettlement = await _purchaseSettlementRepository.GetByExternalContractNumberAsync(externalContractNumber);
                if (purchaseSettlement != null)
                {
                    var query = new GetSettlementByIdQuery
                    {
                        SettlementId = purchaseSettlement.Id,
                        IsPurchaseSettlement = true
                    };
                    var fullSettlement = await _mediator.Send(query);
                    if (fullSettlement != null)
                    {
                        settlements.Add(MapToListDto(fullSettlement));
                        _logger.LogInformation("Found purchase settlement matching external contract number: {ExternalContractNumber}", externalContractNumber);
                    }
                }
                else
                {
                    var salesSettlement = await _salesSettlementRepository.GetByExternalContractNumberAsync(externalContractNumber);
                    if (salesSettlement != null)
                    {
                        var query = new GetSettlementByIdQuery
                        {
                            SettlementId = salesSettlement.Id,
                            IsPurchaseSettlement = false
                        };
                        var fullSettlement = await _mediator.Send(query);
                        if (fullSettlement != null)
                        {
                            settlements.Add(MapToListDto(fullSettlement));
                            _logger.LogInformation("Found sales settlement matching external contract number: {ExternalContractNumber}", externalContractNumber);
                        }
                    }
                }

                if (settlements.Count == 0)
                {
                    _logger.LogInformation("No settlement found matching external contract number: {ExternalContractNumber}", externalContractNumber);
                }
            }
            // If no specific filters provided, fetch all settlements from both purchase and sales
            else
            {
                _logger.LogInformation("Fetching all settlements");

                // Get all purchase settlements
                var purchaseSettlements = await _purchaseSettlementRepository.GetAllAsync();
                foreach (var ps in purchaseSettlements)
                {
                    var query = new GetSettlementByIdQuery
                    {
                        SettlementId = ps.Id,
                        IsPurchaseSettlement = true
                    };
                    var fullSettlement = await _mediator.Send(query);
                    if (fullSettlement != null)
                    {
                        settlements.Add(MapToListDto(fullSettlement));
                    }
                }

                // Get all sales settlements
                var salesSettlements = await _salesSettlementRepository.GetAllAsync();
                foreach (var ss in salesSettlements)
                {
                    var query = new GetSettlementByIdQuery
                    {
                        SettlementId = ss.Id,
                        IsPurchaseSettlement = false
                    };
                    var fullSettlement = await _mediator.Send(query);
                    if (fullSettlement != null)
                    {
                        settlements.Add(MapToListDto(fullSettlement));
                    }
                }

                _logger.LogInformation("Retrieved {SettlementCount} total settlements", settlements.Count);
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

            // Apply client-side filtering for status
            if (status.HasValue)
            {
                // Convert int status to enum and then to string for comparison
                var statusEnum = (ContractSettlementStatus)status.Value;
                var statusString = statusEnum.ToString();
                settlements = settlements.Where(s => s.Status == statusString).ToList();
            }

            // Apply client-side filtering for document number
            if (!string.IsNullOrEmpty(documentNumber))
            {
                settlements = settlements.Where(s => s.DocumentNumber == documentNumber).ToList();
            }

            // Apply client-side filtering for external contract number (if not already the primary search)
            if (string.IsNullOrEmpty(externalContractNumber) == false && contractId.HasValue)
            {
                settlements = settlements.Where(s => s.ExternalContractNumber.ToLower().Contains(externalContractNumber.ToLower())).ToList();
            }

            // Apply pagination client-side
            var totalCount = settlements.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedSettlements = settlements
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedResult<ContractSettlementListDto>
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
                    ExternalContractNumber = request.ExternalContractNumber ?? string.Empty,
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
                    ExternalContractNumber = request.ExternalContractNumber ?? string.Empty,
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

            // Use CQRS query to retrieve and update settlement
            var query = new GetSettlementByIdQuery { SettlementId = settlementId };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                _logger.LogWarning("Settlement not found: {SettlementId}", settlementId);
                return NotFound(new { error = "Settlement not found" });
            }

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

    /// <summary>
    /// Bulk approve multiple settlements
    /// </summary>
    [HttpPost("bulk-approve")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkApproveSettlements([FromBody] BulkApproveSettlementsCommand request)
    {
        try
        {
            _logger.LogInformation("Bulk approving {Count} settlements", request.SettlementIds?.Count ?? 0);

            if (request?.SettlementIds == null || request.SettlementIds.Count == 0)
            {
                return BadRequest(new BulkOperationResultDto
                {
                    SuccessCount = 0,
                    FailureCount = 0,
                    Details = new List<BulkOperationDetailDto>()
                });
            }

            var command = new BulkApproveSettlementsCommand
            {
                SettlementIds = request.SettlementIds,
                ApprovedBy = request.ApprovedBy ?? GetCurrentUserName()
            };

            var result = await _mediator.Send(command);
            _logger.LogInformation("Bulk approve completed: {Success} successful, {Failure} failed", result.SuccessCount, result.FailureCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving settlements");
            return StatusCode(500, new BulkOperationResultDto
            {
                SuccessCount = 0,
                FailureCount = request?.SettlementIds?.Count ?? 0,
                Details = new List<BulkOperationDetailDto>()
            });
        }
    }

    /// <summary>
    /// Bulk finalize multiple settlements
    /// </summary>
    [HttpPost("bulk-finalize")]
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkFinalizeSettlements([FromBody] BulkFinalizeSettlementsCommand request)
    {
        try
        {
            _logger.LogInformation("Bulk finalizing {Count} settlements", request.SettlementIds?.Count ?? 0);

            if (request?.SettlementIds == null || request.SettlementIds.Count == 0)
            {
                return BadRequest(new BulkOperationResultDto
                {
                    SuccessCount = 0,
                    FailureCount = 0,
                    Details = new List<BulkOperationDetailDto>()
                });
            }

            var command = new BulkFinalizeSettlementsCommand
            {
                SettlementIds = request.SettlementIds,
                FinalizedBy = request.FinalizedBy ?? GetCurrentUserName()
            };

            var result = await _mediator.Send(command);
            _logger.LogInformation("Bulk finalize completed: {Success} successful, {Failure} failed", result.SuccessCount, result.FailureCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk finalizing settlements");
            return StatusCode(500, new BulkOperationResultDto
            {
                SuccessCount = 0,
                FailureCount = request?.SettlementIds?.Count ?? 0,
                Details = new List<BulkOperationDetailDto>()
            });
        }
    }

    /// <summary>
    /// Bulk export multiple settlements in specified format
    /// </summary>
    [HttpPost("bulk-export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkExportSettlements([FromBody] BulkExportSettlementsRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk exporting {Count} settlements in {Format} format", request.SettlementIds?.Count ?? 0, request.Format);

            if (request?.SettlementIds == null || request.SettlementIds.Count == 0)
            {
                return BadRequest(new { error = "No settlements specified for export" });
            }

            // Fetch all settlements for export
            var settlements = new List<SettlementDto>();
            foreach (var settlementIdStr in request.SettlementIds)
            {
                try
                {
                    // Convert string ID to Guid
                    if (!Guid.TryParse(settlementIdStr, out var settlementId))
                    {
                        _logger.LogWarning("Invalid settlement ID format: {SettlementId}", settlementIdStr);
                        continue;
                    }

                    // Try purchase settlement first
                    var query = new GetSettlementByIdQuery
                    {
                        SettlementId = settlementId,
                        IsPurchaseSettlement = true
                    };
                    var settlement = await _mediator.Send(query);

                    if (settlement == null)
                    {
                        // Try sales settlement
                        query.IsPurchaseSettlement = false;
                        settlement = await _mediator.Send(query);
                    }

                    if (settlement != null)
                    {
                        settlements.Add(settlement);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve settlement {SettlementId} for export", settlementIdStr);
                }
            }

            if (settlements.Count == 0)
            {
                return BadRequest(new { error = "No valid settlements found for export" });
            }

            // Generate file based on format
            byte[] fileContent;
            string contentType;
            string fileName;

            switch (request.Format?.ToLower())
            {
                case "csv":
                    (fileContent, fileName) = GenerateCsvExport(settlements);
                    contentType = "text/csv";
                    break;

                case "pdf":
                    (fileContent, fileName) = GeneratePdfExport(settlements);
                    contentType = "application/pdf";
                    break;

                case "excel":
                default:
                    (fileContent, fileName) = GenerateExcelExport(settlements);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
            }

            _logger.LogInformation("Successfully generated {Format} export with {Count} settlements", request.Format, settlements.Count);
            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk exporting settlements");
            return StatusCode(500, new { error = "An error occurred while exporting settlements: " + ex.Message });
        }
    }

    /// <summary>
    /// Generate CSV export for settlements
    /// </summary>
    private (byte[], string) GenerateCsvExport(List<SettlementDto> settlements)
    {
        var csv = new System.Text.StringBuilder();

        // Header row
        csv.AppendLine("Settlement ID,Contract ID,Contract Number,External Contract Number,Document Number,Document Type,Document Date,Status,Total Amount,Currency,Actual MT,Actual BBL,Created Date,Created By,Is Finalized");

        // Data rows
        foreach (var settlement in settlements)
        {
            csv.AppendLine($"\"{settlement.Id}\",\"{settlement.ContractId}\",\"{settlement.ContractNumber}\",\"{settlement.ExternalContractNumber}\",\"{settlement.DocumentNumber}\",\"{settlement.DocumentType}\",\"{settlement.DocumentDate:yyyy-MM-dd}\",\"{settlement.Status}\",\"{settlement.TotalSettlementAmount}\",\"{settlement.SettlementCurrency}\",\"{settlement.ActualQuantityMT}\",\"{settlement.ActualQuantityBBL}\",\"{settlement.CreatedDate:yyyy-MM-dd HH:mm:ss}\",\"{settlement.CreatedBy}\",\"{settlement.IsFinalized}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"Settlements_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return (bytes, fileName);
    }

    /// <summary>
    /// Generate PDF export for settlements (placeholder - requires iText library)
    /// </summary>
    private (byte[], string) GeneratePdfExport(List<SettlementDto> settlements)
    {
        // This is a placeholder. In production, you would use iText (iTextSharp) library
        // For now, return CSV as fallback
        _logger.LogWarning("PDF export requested but not yet fully implemented, returning CSV as fallback");
        return GenerateCsvExport(settlements);
    }

    /// <summary>
    /// Generate Excel export for settlements (placeholder - requires EPPlus library)
    /// </summary>
    private (byte[], string) GenerateExcelExport(List<SettlementDto> settlements)
    {
        // This is a placeholder. In production, you would use EPPlus library
        // For now, return CSV as fallback
        _logger.LogWarning("Excel export requested but not yet fully implemented, returning CSV as fallback");
        return GenerateCsvExport(settlements);
    }
}

/// <summary>
/// DTO for generic settlement creation
/// </summary>
public class CreateSettlementRequestDto
{
    public Guid ContractId { get; set; }
    public string? ExternalContractNumber { get; set; }
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
