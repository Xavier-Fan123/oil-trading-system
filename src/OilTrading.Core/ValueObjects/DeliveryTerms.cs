namespace OilTrading.Core.ValueObjects;

public enum DeliveryTerms
{
    FOB = 0,    // Free on Board
    CIF = 1,    // Cost, Insurance, and Freight
    CFR = 2,    // Cost and Freight
    DAP = 3,    // Delivered at Place
    DDP = 4,    // Delivered Duty Paid
    DES = 5,    // Delivered Ex Ship
    DDU = 6,    // Delivered Duty Unpaid
    STS = 7,    // Ship to Ship
    ITT = 8,    // Inter-Tank Transfer
    EXW = 9     // Ex Works
}

public enum ContractPaymentMethod
{
    TT = 0,     // Telegraphic Transfer
    LC = 1,     // Letter of Credit
    CAD = 2,    // Cash against Documents
    SBLC = 3,   // Standby Letter of Credit
    DP = 4      // Documents against Payment
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