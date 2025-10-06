-- Setup TimescaleDB hypertables for time-series data
-- Run this script after the initial migration

-- Enable TimescaleDB extension if not already enabled
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Create hypertable for MarketData
-- This converts the regular table to a hypertable partitioned by time
SELECT create_hypertable('MarketData', 'Timestamp', 
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

-- Create hypertable for PriceIndices
SELECT create_hypertable('PriceIndices', 'Timestamp', 
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

-- Create hypertable for ContractEvents
SELECT create_hypertable('ContractEvents', 'Timestamp', 
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

-- Create additional indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_marketdata_symbol_time_desc 
    ON MarketData (Symbol, Timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_priceindex_indexname_time_desc 
    ON PriceIndices (IndexName, Timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_contractevents_contractid_time_desc 
    ON ContractEvents (ContractId, Timestamp DESC);

-- Create materialized views for common aggregations
CREATE MATERIALIZED VIEW IF NOT EXISTS daily_market_data AS
SELECT 
    Symbol,
    Exchange,
    Currency,
    DATE_TRUNC('day', Timestamp) AS TradeDate,
    FIRST(Price, Timestamp) AS OpenPrice,
    MAX(High) AS HighPrice,
    MIN(Low) AS LowPrice,
    LAST(Price, Timestamp) AS ClosePrice,
    AVG(Price) AS AvgPrice,
    SUM(Volume) AS TotalVolume,
    COUNT(*) AS TradeCount
FROM MarketData
GROUP BY Symbol, Exchange, Currency, DATE_TRUNC('day', Timestamp);

-- Create unique index for materialized view
CREATE UNIQUE INDEX IF NOT EXISTS idx_daily_market_data_unique 
    ON daily_market_data (Symbol, Exchange, Currency, TradeDate);

-- Create materialized view for daily price indices
CREATE MATERIALIZED VIEW IF NOT EXISTS daily_price_indices AS
SELECT 
    IndexName,
    Region,
    Grade,
    Currency,
    DATE_TRUNC('day', Timestamp) AS PriceDate,
    LAST(Price, Timestamp) AS Price,
    LAST(Change, Timestamp) AS Change,
    LAST(ChangePercent, Timestamp) AS ChangePercent
FROM PriceIndices
GROUP BY IndexName, Region, Grade, Currency, DATE_TRUNC('day', Timestamp);

-- Create unique index for price indices materialized view
CREATE UNIQUE INDEX IF NOT EXISTS idx_daily_price_indices_unique 
    ON daily_price_indices (IndexName, Region, Grade, Currency, PriceDate);

-- Set up automatic refresh for materialized views (optional)
-- This requires the pg_cron extension
-- SELECT cron.schedule('refresh-daily-market-data', '0 1 * * *', 'REFRESH MATERIALIZED VIEW CONCURRENTLY daily_market_data;');
-- SELECT cron.schedule('refresh-daily-price-indices', '0 2 * * *', 'REFRESH MATERIALIZED VIEW CONCURRENTLY daily_price_indices;');

-- Create compression policy for older data (optional)
-- This will compress chunks older than 30 days
SELECT add_compression_policy('MarketData', INTERVAL '30 days');
SELECT add_compression_policy('PriceIndices', INTERVAL '30 days');
SELECT add_compression_policy('ContractEvents', INTERVAL '90 days');

-- Create retention policy for very old data (optional)
-- This will drop chunks older than 2 years
-- SELECT add_retention_policy('MarketData', INTERVAL '2 years');
-- SELECT add_retention_policy('PriceIndices', INTERVAL '2 years');
-- SELECT add_retention_policy('ContractEvents', INTERVAL '5 years');

-- Grant appropriate permissions
GRANT SELECT, INSERT, UPDATE ON MarketData TO postgres;
GRANT SELECT, INSERT, UPDATE ON PriceIndices TO postgres;
GRANT SELECT, INSERT, UPDATE ON ContractEvents TO postgres;
GRANT SELECT ON daily_market_data TO postgres;
GRANT SELECT ON daily_price_indices TO postgres;