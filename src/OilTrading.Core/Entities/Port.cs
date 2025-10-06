using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class Port : BaseEntity
{
    public string PortCode { get; set; } = string.Empty; // UNLOCODE
    public string PortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Coordinates { get; set; } // GPS coordinates
    public bool IsActive { get; set; } = true;
    
    // Port characteristics
    public PortType PortType { get; set; }
    public decimal? MaxDraft { get; set; } // Maximum draft in meters
    public decimal? MaxLOA { get; set; } // Maximum length overall in meters
    public decimal? MaxBeam { get; set; } // Maximum beam in meters
    public bool HasPilotage { get; set; }
    public bool HasTugService { get; set; }
    public string? TimeZone { get; set; }
    
    // Berth information
    public int TotalBerths { get; set; }
    public int AvailableBerths { get; set; }
    public string[]? SupportedCargos { get; set; }
    
    // Operational details
    public string? PortAuthority { get; set; }
    public string? ContactInfo { get; set; }
    public string[]? Services { get; set; }
    public string? WeatherRestrictions { get; set; }
    public string? OperatingHours { get; set; }
    
    // Navigation properties
    public ICollection<PortBerth> Berths { get; set; } = new List<PortBerth>();
    public ICollection<VesselCall> VesselCalls { get; set; } = new List<VesselCall>();
}

public class PortBerth : BaseEntity
{
    public int PortId { get; set; }
    public Port Port { get; set; } = null!;
    public string BerthNumber { get; set; } = string.Empty;
    public string BerthName { get; set; } = string.Empty;
    public BerthStatus Status { get; set; }
    
    // Physical characteristics
    public decimal Length { get; set; } // Berth length in meters
    public decimal Draft { get; set; } // Maximum draft in meters
    public decimal? AirDraft { get; set; } // Air draft restriction in meters
    
    // Capabilities
    public string[]? SupportedCargos { get; set; }
    public decimal MaxLoadingRate { get; set; } // MT per hour
    public decimal MaxDischargingRate { get; set; } // MT per hour
    public bool HasRailConnection { get; set; }
    public bool HasRoadConnection { get; set; }
    public bool HasPipelineConnection { get; set; }
    
    // Current occupation
    public int? CurrentVesselCallId { get; set; }
    public VesselCall? CurrentVesselCall { get; set; }
    public DateTime? OccupiedFrom { get; set; }
    public DateTime? EstimatedFree { get; set; }
    
    // Navigation properties
    public ICollection<VesselCall> VesselCalls { get; set; } = new List<VesselCall>();
}

public class VesselCall : BaseEntity
{
    public string VesselName { get; set; } = string.Empty;
    public string? IMONumber { get; set; }
    public string? VesselType { get; set; }
    public decimal? VesselLOA { get; set; }
    public decimal? VesselBeam { get; set; }
    public decimal? VesselDraft { get; set; }
    public decimal? DeadWeight { get; set; } // DWT
    
    public int PortId { get; set; }
    public Port Port { get; set; } = null!;
    public int? BerthId { get; set; }
    public PortBerth? Berth { get; set; }
    
    // Call details
    public VesselCallPurpose Purpose { get; set; }
    public VesselCallStatus Status { get; set; }
    public DateTime? ETA { get; set; } // Estimated time of arrival
    public DateTime? ETD { get; set; } // Estimated time of departure
    public DateTime? ATA { get; set; } // Actual time of arrival
    public DateTime? ATD { get; set; } // Actual time of departure
    
    // Cargo information
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public Quantity? CargoQuantity { get; set; }
    public string? CargoGrade { get; set; }
    public string? CargoDescription { get; set; }
    
    // Commercial details
    public int? TradingPartnerId { get; set; }
    public TradingPartner? TradingPartner { get; set; }
    public string? CharterParty { get; set; }
    public string? BillOfLading { get; set; }
    
    // Operations
    public DateTime? LoadingStart { get; set; }
    public DateTime? LoadingEnd { get; set; }
    public DateTime? DischargingStart { get; set; }
    public DateTime? DischargingEnd { get; set; }
    public decimal? ActualLoadedQuantity { get; set; }
    public decimal? ActualDischargedQuantity { get; set; }
    
    // Related contracts
    public int? PurchaseContractId { get; set; }
    public PurchaseContract? PurchaseContract { get; set; }
    public int? SalesContractId { get; set; }
    public SalesContract? SalesContract { get; set; }
    public int? ShippingOperationId { get; set; }
    public ShippingOperation? ShippingOperation { get; set; }
    
    public string? Agent { get; set; }
    public string? Notes { get; set; }
}

public enum PortType
{
    Seaport = 1,
    River = 2,
    Lake = 3,
    Inland = 4,
    Offshore = 5
}

public enum BerthStatus
{
    Available = 1,
    Occupied = 2,
    Maintenance = 3,
    OutOfService = 4,
    Reserved = 5
}

public enum VesselCallPurpose
{
    Loading = 1,
    Discharging = 2,
    LoadingDischarging = 3,
    Bunkers = 4,
    Repairs = 5,
    Supplies = 6,
    Shelter = 7
}

public enum VesselCallStatus
{
    Scheduled = 1,
    InTransit = 2,
    Arrived = 3,
    Berthed = 4,
    Operating = 5, // Loading/Discharging
    Completed = 6,
    Departed = 7,
    Cancelled = 8,
    Delayed = 9
}