# Settlement Module Database Migration Plan (Phase 11)

## Overview
This document outlines the migration path from the legacy polymorphic `ContractSettlement` table to the new architecture with separate `PurchaseSettlement` and `SalesSettlement` tables.

## Current State (Legacy)
- **Table**: `ContractSettlements` (polymorphic)
- **Foreign Keys**: Ambiguous handling of both purchase and sales contract references
- **Issues**:
  - Conflicting foreign key constraints
  - Polymorphic complexity requiring application-level type discrimination
  - Limited validation at database level

## Target State (v2.8.0)
- **Tables**:
  - `PurchaseSettlements` - Dedicated table for purchase contract settlements
  - `SalesSettlements` - Dedicated table for sales contract settlements
- **Benefits**:
  - Clear type safety and referential integrity
  - Separate CQRS query handlers per type
  - Application-level polymorphism removed
  - Better database constraints and indexes

## Migration Steps

### Phase 1: Backup
```sql
-- Create backup of legacy data
CREATE TABLE ContractSettlements_Backup AS
SELECT * FROM ContractSettlements;
```

### Phase 2: Data Analysis
Before migration, analyze legacy data:
```sql
-- Count settlements by type
SELECT ContractType, COUNT(*)
FROM ContractSettlements
GROUP BY ContractType;

-- Identify settlements with invalid references
SELECT * FROM ContractSettlements c
WHERE PurchaseContractId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM PurchaseContracts WHERE Id = c.PurchaseContractId)
  AND SalesContractId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM SalesContracts WHERE Id = c.SalesContractId);
```

### Phase 3: Validate Target Tables
Verify new settlement tables exist:
```sql
-- Check PurchaseSettlements exists
SELECT * FROM PurchaseSettlements LIMIT 0;

-- Check SalesSettlements exists
SELECT * FROM SalesSettlements LIMIT 0;
```

### Phase 4: Migrate Purchase Settlements
```sql
-- Insert purchase settlements
INSERT INTO PurchaseSettlements (
  Id, PurchaseContractId, DocumentNumber, DocumentType,
  DocumentDate, CalculationQuantityMT, CalculationQuantityBBL,
  BenchmarkAmount, AdjustmentAmount, TotalAmount, Currency,
  Status, CreatedBy, CreatedDate, CreatedDate_Ticks
)
SELECT
  Id, PurchaseContractId, DocumentNumber, DocumentType,
  DocumentDate, CalculationQuantityMT, CalculationQuantityBBL,
  BenchmarkAmount, AdjustmentAmount, TotalAmount, Currency,
  Status, CreatedBy, CreatedDate, CreatedDate_Ticks
FROM ContractSettlements
WHERE ContractType = 'Purchase' AND PurchaseContractId IS NOT NULL;
```

### Phase 5: Migrate Sales Settlements
```sql
-- Insert sales settlements
INSERT INTO SalesSettlements (
  Id, SalesContractId, DocumentNumber, DocumentType,
  DocumentDate, CalculationQuantityMT, CalculationQuantityBBL,
  BenchmarkAmount, AdjustmentAmount, TotalAmount, Currency,
  Status, CreatedBy, CreatedDate, CreatedDate_Ticks
)
SELECT
  Id, SalesContractId, DocumentNumber, DocumentType,
  DocumentDate, CalculationQuantityMT, CalculationQuantityBBL,
  BenchmarkAmount, AdjustmentAmount, TotalAmount, Currency,
  Status, CreatedBy, CreatedDate, CreatedDate_Ticks
FROM ContractSettlements
WHERE ContractType = 'Sales' AND SalesContractId IS NOT NULL;
```

### Phase 6: Validate Migration
Verify data integrity:
```sql
-- Verify counts match
SELECT
  'PurchaseSettlements' as Table_Name, COUNT(*) as Count
FROM PurchaseSettlements
UNION ALL
SELECT
  'SalesSettlements', COUNT(*)
FROM SalesSettlements
UNION ALL
SELECT
  'Total_Migrated',
  (SELECT COUNT(*) FROM PurchaseSettlements) +
  (SELECT COUNT(*) FROM SalesSettlements);

-- Verify no data loss
SELECT
  (SELECT COUNT(*) FROM ContractSettlements) as Legacy_Total,
  (SELECT COUNT(*) FROM PurchaseSettlements) +
  (SELECT COUNT(*) FROM SalesSettlements) as New_Total;

-- Verify all settlements have valid contracts
SELECT 'PurchaseSettlements Missing Contracts' as Issue, COUNT(*) as Count
FROM PurchaseSettlements ps
WHERE NOT EXISTS (SELECT 1 FROM PurchaseContracts WHERE Id = ps.PurchaseContractId)
UNION ALL
SELECT 'SalesSettlements Missing Contracts', COUNT(*)
FROM SalesSettlements ss
WHERE NOT EXISTS (SELECT 1 FROM SalesContracts WHERE Id = ss.SalesContractId);
```

### Phase 7: Update Application
1. Update repository queries to use new tables
2. Verify CQRS queries work correctly
3. Test API endpoints against migrated data

### Phase 8: Deprecation
Option A (Immediate):
- Drop `ContractSettlements` table immediately

Option B (Gradual):
- Keep legacy table for read-only access
- Create view for backward compatibility:
```sql
CREATE VIEW ContractSettlements_Deprecated AS
SELECT
  Id, PurchaseContractId, SalesContractId, 'Purchase' as ContractType,
  DocumentNumber, DocumentType, DocumentDate,
  CalculationQuantityMT, CalculationQuantityBBL,
  BenchmarkAmount, AdjustmentAmount, TotalAmount, Currency,
  Status, CreatedBy, CreatedDate
FROM PurchaseSettlements
UNION ALL
SELECT
  Id, PurchaseContractId, SalesContractId, 'Sales' as ContractType,
  DocumentNumber, DocumentType, DocumentDate,
  CalculationQuantityMT, CalculationQuantityBBL,
  BenchmarkAmount, AdjustmentAmount, TotalAmount, Currency,
  Status, CreatedBy, CreatedDate
FROM SalesSettlements;
```

## Rollback Plan
If migration fails, restore from backup:
```sql
-- Restore legacy data
TRUNCATE TABLE ContractSettlements;
INSERT INTO ContractSettlements SELECT * FROM ContractSettlements_Backup;

-- Drop new tables if needed
DROP TABLE PurchaseSettlements;
DROP TABLE SalesSettlements;
```

## Testing Strategy

### Unit Tests
- ✅ PurchaseSettlement entity tests
- ✅ SalesSettlement entity tests
- ✅ Repository tests for new schema

### Integration Tests
- ✅ SettlementControllerIntegrationTests (Phase 9)
- ✅ Database query tests
- ✅ Migration validation

### Migration Validation
1. Run migration on test database
2. Verify all data integrity checks pass
3. Run integration tests against migrated database
4. Performance testing on large datasets
5. Comparison of query performance (legacy vs. new)

## Performance Considerations

### Indexes Strategy
```sql
-- Purchase settlements
CREATE INDEX IX_PurchaseSettlements_PurchaseContractId
ON PurchaseSettlements(PurchaseContractId);

CREATE INDEX IX_PurchaseSettlements_Status
ON PurchaseSettlements(Status);

CREATE INDEX IX_PurchaseSettlements_CreatedDate
ON PurchaseSettlements(CreatedDate DESC);

-- Sales settlements
CREATE INDEX IX_SalesSettlements_SalesContractId
ON SalesSettlements(SalesContractId);

CREATE INDEX IX_SalesSettlements_Status
ON SalesSettlements(Status);

CREATE INDEX IX_SalesSettlements_CreatedDate
ON SalesSettlements(CreatedDate DESC);
```

### Query Optimization
- Use indexed columns in WHERE clauses
- Avoid SELECT * queries
- Use pagination for large result sets
- Consider materialized views for complex aggregations

## Documentation Updates
1. Update CLAUDE.md with migration completion
2. Update README.md with new settlement schema
3. Update API documentation with new endpoints
4. Create migration runbook for future deployments

## Success Criteria
- ✅ All data migrated without loss
- ✅ Zero referential integrity violations
- ✅ All integration tests passing
- ✅ Query performance maintained or improved
- ✅ No breaking changes to API contracts
- ✅ Backward compatibility (optional via views)
