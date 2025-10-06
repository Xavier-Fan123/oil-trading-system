using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Core.Entities;

public class BillOfLading : BaseEntity
{
    public string BLNumber { get; set; } = string.Empty;
    public BillOfLadingType BLType { get; set; }
    public DateTime IssueDate { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
    public string IssuerCompany { get; set; } = string.Empty;
    
    // Parties
    public string Shipper { get; set; } = string.Empty;
    public string ShipperAddress { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string ConsigneeAddress { get; set; } = string.Empty;
    public string? NotifyParty { get; set; }
    public string? NotifyPartyAddress { get; set; }
    
    // Vessel and voyage details
    public string VesselName { get; set; } = string.Empty;
    public string? VoyageNumber { get; set; }
    public string? IMONumber { get; set; }
    public string PortOfLoading { get; set; } = string.Empty;
    public string PortOfDischarge { get; set; } = string.Empty;
    public string? PlaceOfReceipt { get; set; }
    public string? PlaceOfDelivery { get; set; }
    
    // Cargo details
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string CargoDescription { get; set; } = string.Empty;
    public Quantity BLQuantity { get; set; } = new(0, QuantityUnit.MT);
    public Quantity? OutturnQuantity { get; set; }
    public string? PackagingType { get; set; }
    public int? NumberOfPackages { get; set; }
    public string? MarksAndNumbers { get; set; }
    
    // Quality specifications
    public string? Grade { get; set; }
    public decimal? API { get; set; }
    public decimal? SulfurContent { get; set; }
    public decimal? Viscosity { get; set; }
    public decimal? Temperature { get; set; }
    public string? QualityRemarks { get; set; }
    
    // Dates
    public DateTime? OnBoardDate { get; set; }
    public DateTime? LoadingDate { get; set; }
    public DateTime? DischargingDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    
    // Commercial terms
    public string FreightTerms { get; set; } = string.Empty; // Prepaid, Collect, etc.
    public Money? FreightAmount { get; set; }
    public string? PayableAt { get; set; }
    public int NumberOfOriginals { get; set; } = 3;
    public int NumberOfCopies { get; set; } = 3;
    
    // Status and control
    public BillOfLadingStatus Status { get; set; }
    public bool IsOriginal { get; set; }
    public bool IsNegotiable { get; set; }
    public bool IsElectronic { get; set; }
    public string? ElectronicReference { get; set; }
    
    // Related documents and contracts
    public int? PurchaseContractId { get; set; }
    public PurchaseContract? PurchaseContract { get; set; }
    public int? SalesContractId { get; set; }
    public SalesContract? SalesContract { get; set; }
    public int? ShippingOperationId { get; set; }
    public ShippingOperation? ShippingOperation { get; set; }
    public int? VesselCallId { get; set; }
    public VesselCall? VesselCall { get; set; }
    
    // Additional information
    public string? SpecialInstructions { get; set; }
    public string? DangerousGoodsDeclaration { get; set; }
    public string? CleanOnBoard { get; set; }
    public string? Remarks { get; set; }
    
    // Navigation properties
    public ICollection<QuantityCertificate> QuantityCertificates { get; set; } = new List<QuantityCertificate>();
    public ICollection<BLAmendment> Amendments { get; set; } = new List<BLAmendment>();
}

public class QuantityCertificate : BaseEntity
{
    public string CertificateNumber { get; set; } = string.Empty;
    public QuantityCertificateType CertificateType { get; set; }
    public DateTime IssueDate { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
    public string IssuerCompany { get; set; } = string.Empty;
    
    // Certificate details
    public int? BillOfLadingId { get; set; }
    public BillOfLading? BillOfLading { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Quantity measurements
    public Quantity CertifiedQuantity { get; set; } = new(0, QuantityUnit.MT);
    public string MeasurementMethod { get; set; } = string.Empty; // Shore Tank, Vessel Tank, Flow Meter
    public decimal? Density { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? WaterContent { get; set; }
    public decimal? SedimentContent { get; set; }
    
    // Measurement conditions
    public string? TankDetails { get; set; }
    public string? CalibrationReference { get; set; }
    public DateTime MeasurementDate { get; set; }
    public string MeasuredBy { get; set; } = string.Empty;
    public string? WitnessedBy { get; set; }
    
    // Quality information
    public decimal? APIGravity { get; set; }
    public decimal? SulfurContent { get; set; }
    public decimal? Viscosity { get; set; }
    public string? QualityNotes { get; set; }
    
    // Status
    public QuantityCertificateStatus Status { get; set; }
    public bool IsDisputed { get; set; }
    public string? DisputeReason { get; set; }
    public DateTime? DisputeDate { get; set; }
    
    // Related operations
    public int? VesselCallId { get; set; }
    public VesselCall? VesselCall { get; set; }
    public int? ShippingOperationId { get; set; }
    public ShippingOperation? ShippingOperation { get; set; }
    
    public string? FilePath { get; set; }
    public string? Notes { get; set; }
}

public class BLAmendment : BaseEntity
{
    public int BillOfLadingId { get; set; }
    public BillOfLading BillOfLading { get; set; } = null!;
    public int AmendmentNumber { get; set; }
    public DateTime AmendmentDate { get; set; }
    public string AmendedBy { get; set; } = string.Empty;
    public string AmendmentReason { get; set; } = string.Empty;
    public string AmendmentDetails { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public bool RequiresConsigneeApproval { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
}

public enum BillOfLadingType
{
    Master = 1,     // Master B/L
    House = 2,      // House B/L
    Switch = 3,     // Switch B/L
    Seaway = 4,     // Seaway B/L
    Charter = 5,    // Charter Party B/L
    Electronic = 6  // Electronic B/L
}

public enum BillOfLadingStatus
{
    Draft = 1,
    Issued = 2,
    InTransit = 3,
    Arrived = 4,
    Released = 5,
    Surrendered = 6,
    Amended = 7,
    Cancelled = 8
}

public enum QuantityCertificateType
{
    Loading = 1,        // Loading Certificate
    Discharge = 2,      // Discharge Certificate
    Outturn = 3,        // Outturn Certificate
    Independent = 4,    // Independent Inspector Certificate
    Joint = 5,          // Joint Inspection Certificate
    Final = 6          // Final Quantity Certificate
}

public enum QuantityCertificateStatus
{
    Draft = 1,
    Issued = 2,
    Accepted = 3,
    Disputed = 4,
    Resolved = 5,
    Final = 6
}