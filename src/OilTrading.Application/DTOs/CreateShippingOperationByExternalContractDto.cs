using System.ComponentModel.DataAnnotations;

namespace OilTrading.Application.DTOs;

/// <summary>
/// DTO for creating a shipping operation by specifying the external contract number
/// instead of the internal contract GUID
/// </summary>
public class CreateShippingOperationByExternalContractDto
{
    /// <summary>
    /// The external contract number (as provided by the trading partner)
    /// </summary>
    [Required(ErrorMessage = "External contract number is required")]
    [StringLength(100, ErrorMessage = "External contract number must not exceed 100 characters")]
    public string ExternalContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Contract type to help disambiguate (Purchase or Sales)
    /// </summary>
    [StringLength(50, ErrorMessage = "Expected contract type must not exceed 50 characters")]
    public string? ExpectedContractType { get; set; }

    /// <summary>
    /// Optional: Trading partner ID to help disambiguate
    /// </summary>
    public Guid? TradingPartnerId { get; set; }

    /// <summary>
    /// Optional: Product ID to help disambiguate
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Vessel name
    /// </summary>
    [Required(ErrorMessage = "Vessel name is required")]
    [StringLength(200, ErrorMessage = "Vessel name must not exceed 200 characters")]
    public string VesselName { get; set; } = string.Empty;

    /// <summary>
    /// IMO number of the vessel
    /// </summary>
    [StringLength(50, ErrorMessage = "IMO number must not exceed 50 characters")]
    public string? ImoNumber { get; set; }

    /// <summary>
    /// Optional: Charterer name
    /// </summary>
    [StringLength(200, ErrorMessage = "Charterer name must not exceed 200 characters")]
    public string? ChartererName { get; set; }

    /// <summary>
    /// Optional: Vessel capacity in tons
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Vessel capacity must be greater than or equal to zero")]
    public decimal? VesselCapacity { get; set; }

    /// <summary>
    /// Optional: Shipping agent
    /// </summary>
    [StringLength(200, ErrorMessage = "Shipping agent must not exceed 200 characters")]
    public string? ShippingAgent { get; set; }

    /// <summary>
    /// Planned quantity for shipping
    /// </summary>
    [Required(ErrorMessage = "Planned quantity is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Planned quantity must be greater than zero")]
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// Unit of planned quantity (MT, BBL, GAL, etc.)
    /// </summary>
    [StringLength(20, ErrorMessage = "Planned quantity unit must not exceed 20 characters")]
    public string PlannedQuantityUnit { get; set; } = "MT";

    /// <summary>
    /// Laycan start date (earliest loading date)
    /// </summary>
    public DateTime? LaycanStart { get; set; }

    /// <summary>
    /// Laycan end date (latest loading date)
    /// </summary>
    public DateTime? LaycanEnd { get; set; }

    /// <summary>
    /// Optional: Loading port
    /// </summary>
    [StringLength(100, ErrorMessage = "Loading port must not exceed 100 characters")]
    public string? LoadPort { get; set; }

    /// <summary>
    /// Optional: Discharge port
    /// </summary>
    [StringLength(100, ErrorMessage = "Discharge port must not exceed 100 characters")]
    public string? DischargePort { get; set; }

    /// <summary>
    /// Optional notes about the shipping operation
    /// </summary>
    public string? Notes { get; set; }
}
