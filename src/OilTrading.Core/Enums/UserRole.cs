namespace OilTrading.Core.Enums;

public enum UserRole
{
    SystemAdmin = 1,
    TradingManager = 2,
    SeniorTrader = 3,
    Trader = 4,
    RiskManager = 5,
    RiskAnalyst = 6,
    OperationsManager = 7,
    OperationsClerk = 8,
    SettlementManager = 9,
    SettlementClerk = 10,
    InventoryManager = 11,
    InventoryClerk = 12,
    FinanceManager = 13,
    FinanceClerk = 14,
    ComplianceOfficer = 15,
    Auditor = 16,
    ReadOnlyUser = 17,
    Guest = 18
}

public enum LocationType
{
    Warehouse = 1,
    Tank = 2,
    Pipeline = 3,
    Vessel = 4,
    RefineryTank = 5,
    TerminalTank = 6,
    FloatingStorage = 7,
    Underground = 8,
    AboveGround = 9,
    Virtual = 10
}