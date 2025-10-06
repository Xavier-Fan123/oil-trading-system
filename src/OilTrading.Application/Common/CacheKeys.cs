namespace OilTrading.Application.Common;

public static class CacheKeys
{
    // Purchase Contracts
    public const string PURCHASE_CONTRACTS = "purchase_contracts";
    public const string PURCHASE_CONTRACT = "purchase_contract";
    public const string PURCHASE_CONTRACT_AVAILABLE_QUANTITY = "purchase_contract_available_quantity";
    
    // Sales Contracts
    public const string SALES_CONTRACTS = "sales_contracts";
    public const string SALES_CONTRACT = "sales_contract";
    
    // Trading Partners
    public const string TRADING_PARTNERS = "trading_partners";
    public const string TRADING_PARTNER = "trading_partner";
    
    // Products
    public const string PRODUCTS = "products";
    public const string PRODUCT = "product";
    
    // Risk Calculations
    public const string RISK_CALCULATION = "risk_calculation";
    public const string PORTFOLIO_SUMMARY = "portfolio_summary";
    public const string PRODUCT_RISK = "product_risk";
    public const string RISK_BACKTEST = "risk_backtest";
    
    // Market Data
    public const string MARKET_DATA = "market_data";
    public const string PRICE_HISTORY = "price_history";
    public const string LATEST_PRICES = "latest_prices";
    
    // Dashboard
    public const string DASHBOARD_OVERVIEW = "dashboard_overview";
    public const string DASHBOARD_METRICS = "dashboard_metrics";
    public const string PERFORMANCE_ANALYTICS = "performance_analytics";
    
    // Paper Contracts
    public const string PAPER_CONTRACTS = "paper_contracts";
    public const string OPEN_POSITIONS = "open_positions";
    public const string PNL_SUMMARY = "pnl_summary";
    
    // Cache Expiry Times
    public static class Expiry
    {
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan Long = TimeSpan.FromHours(1);
        public static readonly TimeSpan VeryLong = TimeSpan.FromHours(4);
        
        // Specific expiry times for different data types
        public static readonly TimeSpan Contracts = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan RiskCalculations = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan MarketData = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan ReferenceData = TimeSpan.FromHours(2); // Products, Trading Partners
        public static readonly TimeSpan Dashboard = TimeSpan.FromMinutes(3);
        public static readonly TimeSpan Backtesting = TimeSpan.FromHours(6); // Expensive calculations
    }
}