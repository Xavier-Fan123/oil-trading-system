using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.DTOs;
using InventoryMovementEntity = OilTrading.Core.Entities.InventoryMovement;
using InventoryMovementDto = OilTrading.Application.DTOs.InventoryMovement;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryLocationRepository _locationRepository;
    private readonly IInventoryPositionRepository _positionRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryLocationRepository locationRepository,
        IInventoryPositionRepository positionRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<InventoryController> logger)
    {
        _locationRepository = locationRepository;
        _positionRepository = positionRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Inventory Locations
    [HttpGet("locations")]
    public async Task<ActionResult<IEnumerable<InventoryLocationDto>>> GetLocations()
    {
        var locations = await _locationRepository.GetActiveLocationsAsync();
        var dtos = locations.Select(MapToLocationDto);
        return Ok(dtos);
    }

    [HttpGet("locations/{id}")]
    public async Task<ActionResult<InventoryLocationDto>> GetLocation(Guid id)
    {
        var location = await _locationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        return Ok(MapToLocationDto(location));
    }

    [HttpGet("locations/by-type/{type}")]
    public async Task<ActionResult<IEnumerable<InventoryLocationDto>>> GetLocationsByType(InventoryLocationType type)
    {
        var locations = await _locationRepository.GetByTypeAsync(type);
        var dtos = locations.Select(MapToLocationDto);
        return Ok(dtos);
    }

    [HttpGet("locations/by-country/{country}")]
    public async Task<ActionResult<IEnumerable<InventoryLocationDto>>> GetLocationsByCountry(string country)
    {
        var locations = await _locationRepository.GetByCountryAsync(country);
        var dtos = locations.Select(MapToLocationDto);
        return Ok(dtos);
    }

    [HttpPost("locations")]
    public async Task<ActionResult<InventoryLocationDto>> CreateLocation([FromBody] CreateInventoryLocationRequest request)
    {
        // Check if location code already exists
        var existing = await _locationRepository.GetByLocationCodeAsync(request.LocationCode);
        if (existing != null)
            return BadRequest($"Location with code '{request.LocationCode}' already exists");

        var location = new InventoryLocation
        {
            LocationCode = request.LocationCode,
            LocationName = request.LocationName,
            LocationType = request.LocationType,
            Country = request.Country,
            Region = request.Region,
            Address = request.Address,
            Coordinates = request.Coordinates,
            OperatorName = request.OperatorName,
            ContactInfo = request.ContactInfo,
            TotalCapacity = new Quantity(request.TotalCapacity, request.CapacityUnit),
            AvailableCapacity = new Quantity(request.TotalCapacity, request.CapacityUnit), // Initially all available
            UsedCapacity = new Quantity(0, request.CapacityUnit),
            SupportedProducts = request.SupportedProducts,
            HandlingServices = request.HandlingServices,
            HasRailAccess = request.HasRailAccess,
            HasRoadAccess = request.HasRoadAccess,
            HasSeaAccess = request.HasSeaAccess,
            HasPipelineAccess = request.HasPipelineAccess,
            IsActive = true
        };

        await _locationRepository.AddAsync(location);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created inventory location: {LocationCode} - {LocationName}", 
            location.LocationCode, location.LocationName);

        return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, MapToLocationDto(location));
    }

    [HttpPut("locations/{id}")]
    public async Task<ActionResult<InventoryLocationDto>> UpdateLocation(Guid id, [FromBody] UpdateInventoryLocationRequest request)
    {
        if (id != request.Id)
            return BadRequest("ID mismatch");

        var location = await _locationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        // Check if location code is being changed and if new code already exists
        if (location.LocationCode != request.LocationCode)
        {
            var existing = await _locationRepository.GetByLocationCodeAsync(request.LocationCode);
            if (existing != null && existing.Id != id)
                return BadRequest($"Location with code '{request.LocationCode}' already exists");
        }

        location.LocationCode = request.LocationCode;
        location.LocationName = request.LocationName;
        location.LocationType = request.LocationType;
        location.Country = request.Country;
        location.Region = request.Region;
        location.Address = request.Address;
        location.Coordinates = request.Coordinates;
        location.OperatorName = request.OperatorName;
        location.ContactInfo = request.ContactInfo;
        location.TotalCapacity = new Quantity(request.TotalCapacity, request.CapacityUnit);
        location.SupportedProducts = request.SupportedProducts;
        location.HandlingServices = request.HandlingServices;
        location.HasRailAccess = request.HasRailAccess;
        location.HasRoadAccess = request.HasRoadAccess;
        location.HasSeaAccess = request.HasSeaAccess;
        location.HasPipelineAccess = request.HasPipelineAccess;
        location.IsActive = request.IsActive;

        await _locationRepository.UpdateAsync(location);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated inventory location: {LocationCode} - {LocationName}", 
            location.LocationCode, location.LocationName);

        return Ok(MapToLocationDto(location));
    }

    [HttpDelete("locations/{id}")]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        var location = await _locationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        // Check if location has inventory positions
        var positions = await _positionRepository.GetByLocationAsync(id);
        if (positions.Any())
            return BadRequest("Cannot delete location with existing inventory positions");

        location.IsActive = false; // Soft delete
        await _locationRepository.UpdateAsync(location);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deactivated inventory location: {LocationCode} - {LocationName}", 
            location.LocationCode, location.LocationName);

        return NoContent();
    }

    // Inventory Positions
    [HttpGet("positions")]
    public async Task<ActionResult<IEnumerable<InventoryPositionDto>>> GetPositions()
    {
        var positions = await _positionRepository.GetAllAsync();
        var dtos = new List<InventoryPositionDto>();
        
        foreach (var position in positions)
        {
            dtos.Add(await MapToPositionDtoAsync(position));
        }
        
        return Ok(dtos);
    }

    [HttpGet("positions/{id}")]
    public async Task<ActionResult<InventoryPositionDto>> GetPosition(Guid id)
    {
        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null)
            return NotFound();

        return Ok(await MapToPositionDtoAsync(position));
    }

    [HttpGet("positions/by-location/{locationId}")]
    public async Task<ActionResult<IEnumerable<InventoryPositionDto>>> GetPositionsByLocation(Guid locationId)
    {
        var positions = await _positionRepository.GetByLocationAsync(locationId);
        var dtos = new List<InventoryPositionDto>();
        
        foreach (var position in positions)
        {
            dtos.Add(await MapToPositionDtoAsync(position));
        }
        
        return Ok(dtos);
    }

    [HttpGet("positions/by-product/{productId}")]
    public async Task<ActionResult<IEnumerable<InventoryPositionDto>>> GetPositionsByProduct(Guid productId)
    {
        var positions = await _positionRepository.GetByProductAsync(productId);
        var dtos = new List<InventoryPositionDto>();
        
        foreach (var position in positions)
        {
            dtos.Add(await MapToPositionDtoAsync(position));
        }
        
        return Ok(dtos);
    }

    [HttpPost("positions")]
    public async Task<ActionResult<InventoryPositionDto>> CreatePosition([FromBody] CreateInventoryPositionRequest request)
    {
        // Check if position already exists for this location and product
        var existing = await _positionRepository.GetByLocationAndProductAsync(request.LocationId, request.ProductId);
        if (existing != null)
            return BadRequest("Inventory position already exists for this location and product");

        // Verify location and product exist
        var location = await _locationRepository.GetByIdAsync(request.LocationId);
        if (location == null)
            return BadRequest("Location not found");

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return BadRequest("Product not found");

        var position = new InventoryPosition
        {
            LocationId = request.LocationId,
            ProductId = request.ProductId,
            Quantity = new Quantity(request.Quantity, request.QuantityUnit),
            AverageCost = new Money(request.AverageCost, request.Currency),
            Grade = request.Grade,
            BatchReference = request.BatchReference,
            Sulfur = request.Sulfur,
            API = request.API,
            Viscosity = request.Viscosity,
            QualityNotes = request.QualityNotes,
            ReceivedDate = request.ReceivedDate,
            Status = request.Status,
            StatusNotes = request.StatusNotes,
            LastUpdated = DateTime.UtcNow
        };

        await _positionRepository.AddAsync(position);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created inventory position: Location {LocationId}, Product {ProductId}, Quantity {Quantity}", 
            request.LocationId, request.ProductId, request.Quantity);

        return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, await MapToPositionDtoAsync(position));
    }

    [HttpPut("positions/{id}")]
    public async Task<ActionResult<InventoryPositionDto>> UpdatePosition(Guid id, [FromBody] UpdateInventoryPositionRequest request)
    {
        if (id != request.Id)
            return BadRequest("ID mismatch");

        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null)
            return NotFound();

        position.Quantity = new Quantity(request.Quantity, request.QuantityUnit);
        position.AverageCost = new Money(request.AverageCost, request.Currency);
        position.Grade = request.Grade;
        position.BatchReference = request.BatchReference;
        position.Sulfur = request.Sulfur;
        position.API = request.API;
        position.Viscosity = request.Viscosity;
        position.QualityNotes = request.QualityNotes;
        position.ReceivedDate = request.ReceivedDate;
        position.Status = request.Status;
        position.StatusNotes = request.StatusNotes;
        position.LastUpdated = DateTime.UtcNow;

        await _positionRepository.UpdateAsync(position);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated inventory position: {PositionId}", id);

        return Ok(await MapToPositionDtoAsync(position));
    }

    [HttpDelete("positions/{id}")]
    public async Task<IActionResult> DeletePosition(Guid id)
    {
        var position = await _positionRepository.GetByIdAsync(id);
        if (position == null)
            return NotFound();

        await _positionRepository.DeleteAsync(position);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted inventory position: {PositionId}", id);

        return NoContent();
    }

    // Inventory Movements
    [HttpGet("movements")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetMovements()
    {
        var movements = await _movementRepository.GetAllAsync();
        var dtos = new List<InventoryMovementDto>();
        
        foreach (var movement in movements)
        {
            dtos.Add(await MapToMovementDtoAsync(movement));
        }
        
        return Ok(dtos);
    }

    [HttpGet("movements/{id}")]
    public async Task<ActionResult<InventoryMovementDto>> GetMovement(Guid id)
    {
        var movement = await _movementRepository.GetByIdAsync(id);
        if (movement == null)
            return NotFound();

        return Ok(await MapToMovementDtoAsync(movement));
    }

    [HttpGet("movements/pending")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetPendingMovements()
    {
        var movements = await _movementRepository.GetPendingMovementsAsync();
        var dtos = new List<InventoryMovementDto>();
        
        foreach (var movement in movements)
        {
            dtos.Add(await MapToMovementDtoAsync(movement));
        }
        
        return Ok(dtos);
    }

    [HttpPost("movements")]
    public async Task<ActionResult<InventoryMovementDto>> CreateMovement([FromBody] CreateInventoryMovementRequest request)
    {
        // Verify locations and product exist
        var fromLocation = await _locationRepository.GetByIdAsync(request.FromLocationId);
        if (fromLocation == null)
            return BadRequest("From location not found");

        var toLocation = await _locationRepository.GetByIdAsync(request.ToLocationId);
        if (toLocation == null)
            return BadRequest("To location not found");

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return BadRequest("Product not found");

        var movement = new InventoryMovementEntity
        {
            FromLocationId = request.FromLocationId,
            ToLocationId = request.ToLocationId,
            ProductId = request.ProductId,
            Quantity = new Quantity(request.Quantity, request.QuantityUnit),
            MovementType = (OilTrading.Core.Entities.InventoryMovementType)request.MovementType,
            MovementDate = request.MovementDate,
            PlannedDate = request.PlannedDate,
            Status = OilTrading.Core.Entities.InventoryMovementStatus.Planned,
            MovementReference = GenerateMovementReference(),
            TransportMode = request.TransportMode,
            VesselName = request.VesselName,
            TransportReference = request.TransportReference,
            TransportCost = request.TransportCost.HasValue ? new Money(request.TransportCost.Value, request.CostCurrency ?? "USD") : null,
            HandlingCost = request.HandlingCost.HasValue ? new Money(request.HandlingCost.Value, request.CostCurrency ?? "USD") : null,
            Notes = request.Notes,
            PurchaseContractId = request.PurchaseContractId,
            SalesContractId = request.SalesContractId,
            ShippingOperationId = request.ShippingOperationId,
            InitiatedBy = GetCurrentUserName()
        };

        // Calculate total cost
        if (movement.TransportCost != null || movement.HandlingCost != null)
        {
            var totalCost = (movement.TransportCost?.Amount ?? 0) + (movement.HandlingCost?.Amount ?? 0);
            movement.TotalCost = new Money(totalCost, request.CostCurrency ?? "USD");
        }

        await _movementRepository.AddAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created inventory movement: {MovementReference}", movement.MovementReference);

        return CreatedAtAction(nameof(GetMovement), new { id = movement.Id }, await MapToMovementDtoAsync(movement));
    }

    [HttpPut("movements/{id}")]
    public async Task<ActionResult<InventoryMovementDto>> UpdateMovement(Guid id, [FromBody] UpdateInventoryMovementRequest request)
    {
        if (id != request.Id)
            return BadRequest("ID mismatch");

        var movement = await _movementRepository.GetByIdAsync(id);
        if (movement == null)
            return NotFound();

        movement.Quantity = new Quantity(request.Quantity, request.QuantityUnit);
        movement.MovementType = (OilTrading.Core.Entities.InventoryMovementType)request.MovementType;
        movement.MovementDate = request.MovementDate;
        movement.PlannedDate = request.PlannedDate;
        movement.Status = (OilTrading.Core.Entities.InventoryMovementStatus)request.Status;
        movement.TransportMode = request.TransportMode;
        movement.VesselName = request.VesselName;
        movement.TransportReference = request.TransportReference;
        movement.TransportCost = request.TransportCost.HasValue ? new Money(request.TransportCost.Value, request.CostCurrency ?? "USD") : null;
        movement.HandlingCost = request.HandlingCost.HasValue ? new Money(request.HandlingCost.Value, request.CostCurrency ?? "USD") : null;
        movement.Notes = request.Notes;
        movement.ApprovedBy = request.ApprovedBy;
        movement.ApprovedAt = request.ApprovedAt;

        // Calculate total cost
        if (movement.TransportCost != null || movement.HandlingCost != null)
        {
            var totalCost = (movement.TransportCost?.Amount ?? 0) + (movement.HandlingCost?.Amount ?? 0);
            movement.TotalCost = new Money(totalCost, request.CostCurrency ?? "USD");
        }

        await _movementRepository.UpdateAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated inventory movement: {MovementId}", id);

        return Ok(await MapToMovementDtoAsync(movement));
    }

    [HttpDelete("movements/{id}")]
    public async Task<IActionResult> DeleteMovement(Guid id)
    {
        var movement = await _movementRepository.GetByIdAsync(id);
        if (movement == null)
            return NotFound();

        if (movement.Status == OilTrading.Core.Entities.InventoryMovementStatus.Completed)
            return BadRequest("Cannot delete completed movement");

        await _movementRepository.DeleteAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted inventory movement: {MovementId}", id);

        return NoContent();
    }

    // Summary endpoints
    [HttpGet("summary")]
    public async Task<ActionResult<InventorySummaryDto>> GetInventorySummary()
    {
        var locations = await _locationRepository.GetActiveLocationsAsync();
        var positions = await _positionRepository.GetAllAsync();
        var pendingMovements = await _movementRepository.GetPendingMovementsAsync();

        var summary = new InventorySummaryDto
        {
            TotalLocations = locations.Count(),
            ActiveLocations = locations.Count(l => l.IsActive),
            TotalProducts = positions.Select(p => p.ProductId).Distinct().Count(),
            TotalInventoryQuantity = positions.Sum(p => p.Quantity.Value),
            TotalInventoryValue = positions.Sum(p => p.TotalValue.Amount),
            Currency = "USD",
            PendingMovements = pendingMovements.Count(),
            LastUpdated = DateTime.UtcNow
        };

        return Ok(summary);
    }

    [HttpGet("locations/{id}/summary")]
    public async Task<ActionResult<LocationSummaryDto>> GetLocationSummary(Guid id)
    {
        var location = await _locationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        var positions = await _positionRepository.GetByLocationAsync(id);
        var totalValue = await _positionRepository.GetTotalValueByLocationAsync(id);

        var utilizationPercentage = location.TotalCapacity.Value > 0 
            ? (location.UsedCapacity.Value / location.TotalCapacity.Value) * 100 
            : 0;

        var summary = new LocationSummaryDto
        {
            LocationId = location.Id,
            LocationName = location.LocationName,
            LocationCode = location.LocationCode,
            LocationType = location.LocationType.ToString(),
            UtilizationPercentage = utilizationPercentage,
            ProductCount = positions.Count(),
            TotalValue = totalValue,
            Currency = "USD"
        };

        return Ok(summary);
    }

    // Helper methods
    private string GetCurrentUserName()
    {
        return User?.Identity?.Name ?? "System";
    }

    private string GenerateMovementReference()
    {
        return $"MOV{DateTime.UtcNow:yyyyMMdd}{DateTime.UtcNow.Ticks % 10000:D4}";
    }

    private InventoryLocationDto MapToLocationDto(InventoryLocation location)
    {
        return new InventoryLocationDto
        {
            Id = location.Id,
            LocationCode = location.LocationCode,
            LocationName = location.LocationName,
            LocationType = location.LocationType.ToString(),
            Country = location.Country,
            Region = location.Region,
            Address = location.Address,
            Coordinates = location.Coordinates,
            IsActive = location.IsActive,
            OperatorName = location.OperatorName,
            ContactInfo = location.ContactInfo,
            TotalCapacity = location.TotalCapacity.Value,
            AvailableCapacity = location.AvailableCapacity.Value,
            UsedCapacity = location.UsedCapacity.Value,
            CapacityUnit = location.TotalCapacity.Unit.ToString(),
            SupportedProducts = location.SupportedProducts,
            HandlingServices = location.HandlingServices,
            HasRailAccess = location.HasRailAccess,
            HasRoadAccess = location.HasRoadAccess,
            HasSeaAccess = location.HasSeaAccess,
            HasPipelineAccess = location.HasPipelineAccess,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt ?? DateTime.UtcNow,
            InventoryPositionsCount = location.Inventories?.Count ?? 0,
            TotalInventoryValue = location.Inventories?.Sum(i => i.TotalValue.Amount) ?? 0
        };
    }

    private async Task<InventoryPositionDto> MapToPositionDtoAsync(InventoryPosition position)
    {
        var location = await _locationRepository.GetByIdAsync(position.LocationId);
        var product = await _productRepository.GetByIdAsync(position.ProductId);

        return new InventoryPositionDto
        {
            Id = position.Id,
            LocationId = position.LocationId,
            LocationName = location?.LocationName ?? "",
            LocationCode = location?.LocationCode ?? "",
            ProductId = position.ProductId,
            ProductName = product?.Name ?? "",
            ProductCode = product?.Code ?? "",
            Quantity = position.Quantity.Value,
            QuantityUnit = position.Quantity.Unit.ToString(),
            AverageCost = position.AverageCost.Amount,
            Currency = position.AverageCost.Currency,
            TotalValue = position.TotalValue.Amount,
            LastUpdated = position.LastUpdated,
            Grade = position.Grade,
            BatchReference = position.BatchReference,
            Sulfur = position.Sulfur,
            API = position.API,
            Viscosity = position.Viscosity,
            QualityNotes = position.QualityNotes,
            ReceivedDate = position.ReceivedDate,
            Status = position.Status.ToString(),
            StatusNotes = position.StatusNotes,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt ?? DateTime.UtcNow
        };
    }

    private async Task<InventoryMovementDto> MapToMovementDtoAsync(InventoryMovementEntity movement)
    {
        var fromLocation = await _locationRepository.GetByIdAsync(movement.FromLocationId);
        var toLocation = await _locationRepository.GetByIdAsync(movement.ToLocationId);
        var product = await _productRepository.GetByIdAsync(movement.ProductId);

        return new InventoryMovementDto
        {
            Id = movement.Id,
            FromLocationId = movement.FromLocationId,
            ToLocationId = movement.ToLocationId,
            ProductId = movement.ProductId,
            Quantity = movement.Quantity,
            MovementType = (OilTrading.Application.DTOs.InventoryMovementType)movement.MovementType,
            MovementDate = movement.MovementDate,
            PlannedDate = movement.PlannedDate,
            Status = (OilTrading.Application.DTOs.InventoryMovementStatus)movement.Status,
            MovementReference = movement.MovementReference,
            TransportMode = movement.TransportMode,
            VesselName = movement.VesselName,
            TransportReference = movement.TransportReference,
            TransportCost = movement.TransportCost,
            HandlingCost = movement.HandlingCost,
            TotalCost = movement.TotalCost,
            InitiatedBy = movement.InitiatedBy,
            ApprovedBy = movement.ApprovedBy,
            ApprovedAt = movement.ApprovedAt,
            Notes = movement.Notes,
            PurchaseContractId = movement.PurchaseContractId,
            SalesContractId = movement.SalesContractId,
            ShippingOperationId = movement.ShippingOperationId,
            CreatedAt = movement.CreatedAt,
            UpdatedAt = movement.UpdatedAt ?? DateTime.UtcNow
        };
    }
}