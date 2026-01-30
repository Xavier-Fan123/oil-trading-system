-- =====================================================================
-- Migration: Fix Futures Product Codes
-- Purpose: Extract clean ProductCode and ContractMonth from embedded format
-- Description:
--   Legacy data has format "SG380 2511" in ProductCode field
--   Need to split into:
--   - ProductCode: "SG380" (clean base product code)
--   - ContractMonth: "202511" (YYYYMM format)
--
-- Target: All MarketPrice records with:
--   - PriceType = 'FuturesSettlement'
--   - ProductCode contains space + 4-digit YYMM pattern
--
-- Examples:
--   "SG380 2511" → ProductCode="SG380", ContractMonth="202511"
--   "GO 10ppm 2601" → ProductCode="GO 10ppm", ContractMonth="202601"
--   "MF 0.5 2512" → ProductCode="MF 0.5", ContractMonth="202512"
--   "Brt Fut 2601" → ProductCode="Brt Fut", ContractMonth="202601"
--
-- Safety:
--   - Only updates records where ContractMonth is NULL or empty
--   - Uses REGEXP pattern matching to validate format
--   - Validates month range (01-12)
--
-- Author: Claude Code (Automated Migration)
-- Date: 2025-12-03
-- =====================================================================

-- SQLite Version
-- =====================================================================
-- Note: SQLite has limited string manipulation, so we use SUBSTR and INSTR

-- Step 1: Update ProductCode (remove " YYMM" suffix) and set ContractMonth
UPDATE MarketPrices
SET
    ContractMonth = CASE
        -- Extract YYMM from end of ProductCode
        WHEN SUBSTR(ProductCode, -4, 4) GLOB '[0-9][0-9][0-9][0-9]'
             AND SUBSTR(ProductCode, -5, 1) = ' '
             AND CAST(SUBSTR(ProductCode, -2, 2) AS INTEGER) BETWEEN 1 AND 12
        THEN
            -- Convert YYMM to YYYYMM
            '20' || SUBSTR(ProductCode, -4, 4)
        ELSE
            ContractMonth
    END,
    ProductCode = CASE
        -- Remove " YYMM" suffix from ProductCode
        WHEN SUBSTR(ProductCode, -4, 4) GLOB '[0-9][0-9][0-9][0-9]'
             AND SUBSTR(ProductCode, -5, 1) = ' '
             AND CAST(SUBSTR(ProductCode, -2, 2) AS INTEGER) BETWEEN 1 AND 12
        THEN
            TRIM(SUBSTR(ProductCode, 1, LENGTH(ProductCode) - 5))
        ELSE
            ProductCode
    END
WHERE
    PriceType = 'FuturesSettlement'
    AND (ContractMonth IS NULL OR ContractMonth = '')
    AND ProductCode LIKE '% ____'
    AND SUBSTR(ProductCode, -4, 4) GLOB '[0-9][0-9][0-9][0-9]'
    AND SUBSTR(ProductCode, -5, 1) = ' '
    AND CAST(SUBSTR(ProductCode, -2, 2) AS INTEGER) BETWEEN 1 AND 12;

-- =====================================================================
-- PostgreSQL Version (for production)
-- =====================================================================
-- Uncomment for PostgreSQL production database

/*
-- Step 1: Verify pattern matches before migration (dry run)
SELECT
    ProductCode AS OriginalProductCode,
    TRIM(SUBSTRING(ProductCode FROM '^(.+)\s+\d{4}$')) AS NewProductCode,
    '20' || SUBSTRING(ProductCode FROM '\s+(\d{4})$') AS NewContractMonth,
    COUNT(*) AS RecordCount
FROM MarketPrices
WHERE
    PriceType = 'FuturesSettlement'
    AND (ContractMonth IS NULL OR ContractMonth = '')
    AND ProductCode ~ '^.+\s+\d{4}$'
    AND CAST(SUBSTRING(ProductCode FROM '\s+\d{2}(\d{2})$') AS INTEGER) BETWEEN 1 AND 12
GROUP BY ProductCode
ORDER BY ProductCode;

-- Step 2: Execute migration (production)
UPDATE MarketPrices
SET
    ProductCode = TRIM(SUBSTRING(ProductCode FROM '^(.+)\s+\d{4}$')),
    ContractMonth = '20' || SUBSTRING(ProductCode FROM '\s+(\d{4})$')
WHERE
    PriceType = 'FuturesSettlement'
    AND (ContractMonth IS NULL OR ContractMonth = '')
    AND ProductCode ~ '^.+\s+\d{4}$'
    AND CAST(SUBSTRING(ProductCode FROM '\s+\d{2}(\d{2})$') AS INTEGER) BETWEEN 1 AND 12;

-- Step 3: Verify migration results
SELECT
    ProductCode,
    ContractMonth,
    PriceType,
    COUNT(*) AS RecordCount
FROM MarketPrices
WHERE PriceType = 'FuturesSettlement'
GROUP BY ProductCode, ContractMonth, PriceType
ORDER BY ProductCode, ContractMonth;
*/

-- =====================================================================
-- Rollback Script (if needed)
-- =====================================================================
-- WARNING: This rollback only works if you have a backup of original data
-- DO NOT run this unless you have a full database backup

/*
-- SQLite Rollback (requires backup table)
CREATE TABLE IF NOT EXISTS MarketPrices_Backup AS
SELECT * FROM MarketPrices WHERE 1=0;

-- Restore from backup
DELETE FROM MarketPrices WHERE Id IN (
    SELECT Id FROM MarketPrices_Backup
);
INSERT INTO MarketPrices SELECT * FROM MarketPrices_Backup;
DROP TABLE MarketPrices_Backup;
*/

-- =====================================================================
-- Verification Queries
-- =====================================================================

-- Query 1: Count records affected by migration
SELECT
    'Total Futures Records' AS Description,
    COUNT(*) AS Count
FROM MarketPrices
WHERE PriceType = 'FuturesSettlement';

-- Query 2: Show sample of migrated records
SELECT
    ProductCode,
    ContractMonth,
    PriceDate,
    Price,
    PriceType
FROM MarketPrices
WHERE PriceType = 'FuturesSettlement'
ORDER BY ProductCode, ContractMonth, PriceDate DESC
LIMIT 20;

-- Query 3: Identify any records that still need migration
SELECT
    ProductCode,
    ContractMonth,
    COUNT(*) AS RecordCount
FROM MarketPrices
WHERE
    PriceType = 'FuturesSettlement'
    AND (ContractMonth IS NULL OR ContractMonth = '')
    AND ProductCode LIKE '% ____'
GROUP BY ProductCode, ContractMonth;

-- =====================================================================
-- Expected Results (Example)
-- =====================================================================
-- Before Migration:
-- ProductCode        | ContractMonth | Count
-- "SG380 2511"       | NULL          | 45
-- "GO 10ppm 2601"    | NULL          | 30
--
-- After Migration:
-- ProductCode        | ContractMonth | Count
-- "SG380"            | "202511"      | 45
-- "GO 10ppm"         | "202601"      | 30
-- =====================================================================
