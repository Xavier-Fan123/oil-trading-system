namespace OilTrading.Core.ValueObjects;

public enum DeliveryTerms
{
    FOB = 1,    // Free on Board
    CIF = 2,    // Cost, Insurance, and Freight
    CFR = 3,    // Cost and Freight
    DAP = 4,    // Delivered at Place
    DDP = 5,    // Delivered Duty Paid
    DES = 6,    // Delivered Ex Ship
    DDU = 7,    // Delivered Duty Unpaid
    STS = 8,    // Ship to Ship
    ITT = 9,    // Inter-Tank Transfer
    EXW = 10     // Ex Works
}

public enum ContractPaymentMethod
{
    TT = 1,     // Telegraphic Transfer
    LC = 2,     // Letter of Credit
    CAD = 3,    // Cash against Documents
    SBLC = 4,   // Standby Letter of Credit
    DP = 5      // Documents against Payment
}

public enum ShippingStatus
{
    Planned = 1,
    Loading = 2,
    InTransit = 3,
    Discharged = 4,
    Cancelled = 5
}

public enum PricingEventType
{
    BL = 1,     // Bill of Lading
    NOR = 2,    // Notice of Readiness
    COD = 3     // Certificate of Discharge
}