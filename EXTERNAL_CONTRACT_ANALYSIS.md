# External Contract Number (ExternalContractNumber) and Contract ID (GUID) Mapping Analysis

## EXECUTIVE SUMMARY

The Oil Trading System has a **critical architectural disconnection** between two contract identification systems:

1. **Internal System (GUID)**: Contract IDs as primary keys
2. **External Business (String)**: External contract numbers for partner reconciliation

### Root Cause
Both systems are properly implemented at the **database and domain layers**, but external contract numbers are **NOT propagated through API filters, query handlers, or frontend workflows**.

**Result**: Users cannot practically use externalContractNumber in business operations despite the system storing them.

---

## KEY FINDINGS

### Database Layer: PARTIALLY CORRECT

**PurchaseContracts**:
- Has index: `IX_PurchaseContracts_ExternalContractNumber`
- Configuration properly defined

**SalesContracts**:
- **MISSING** index for externalContractNumber
- Configuration lacks index definition
- Creates performance gap between contract types

### Domain Model: CORRECT but INCOMPLETE

**What Works**:
- Both contract entities define externalContractNumber
- SetExternalContractNumber() methods exist
- Proper validation in constructors

**What's Missing**:
- No domain events for external number assignment
- Navigation properties use GUID only (ShippingOperation, ContractMatching, Settlement)
- Cannot navigate from external identifier to child entities

### API Layer: CRITICAL GAPS

**Missing Support**:
- ✗ No filter for externalContractNumber in GetPurchaseContractsQuery
- ✗ No filter for externalContractNumber in GetSalesContractsQuery
- ✗ No endpoint: GET /purchase-contracts/by-external-contract/{externalContractNumber}
- ✗ No endpoint: GET /sales-contracts/by-external-contract/{externalContractNumber}
- ✗ CreateSettlementDto accepts ContractId (GUID) only
- ✗ No support for external numbers in ShippingOperationController

**Workaround Complexity**:
- SettlementController has GetByExternalContractNumber() but requires service-layer two-step lookup
- Cannot create settlement by externalContractNumber directly
- Two database queries required instead of one

### Frontend Layer: TYPES DEFINED but UNUSABLE

**What Exists**:
- externalContractNumber defined in PurchaseContract interface
- externalContractNumber defined in SalesContractListDto interface

**What's Missing**:
- ✗ No API method to getByExternalContractNumber()
- ✗ No contract filter by externalContractNumber
- ✗ SettlementEntry form uses ContractId (GUID) dropdown only
- ✗ ShippingOperationForm cannot search by externalContractNumber
- ✗ No smart autocomplete showing both contract identifiers

---

## CROSS-MODULE IMPACT

### Settlement Module
- Must use two-step process: `externalContractNumber → ContractId → Settlement`
- API endpoint exists but as workaround only
- Cannot directly create settlement with externalContractNumber

### Shipping Operations Module
- No support for externalContractNumber in any workflow
- Cannot create shipping operation referencing contract by external number

### Contract Matching Module
- Pure GUID-based relationships
- No external number tracking for audit trail
- Cannot reconcile matches against partner confirmations

### Risk Management Module
- Cannot filter risk by externalContractNumber
- Position calculations use ContractId only

---

## DATA INTEGRITY ISSUES

### Issue 1: Duplicate External Numbers Allowed
- Multiple contracts can share same externalContractNumber (non-unique)
- No application validation prevents duplicates
- Risk: Settlement created with ambiguous reference

### Issue 2: Missing Audit Trail
- SetExternalContractNumber() method exists but raises no domain event
- No tracking of who/when external numbers were assigned
- Violates contract management compliance requirements

### Issue 3: Configuration Asymmetry
- PurchaseContractConfiguration has index
- SalesContractConfiguration lacks index
- Creates different performance/integrity characteristics

---

## BUSINESS WORKFLOW PROBLEMS

### Scenario: Create Settlement from Partner Invoice
**Current Workflow**:
1. User receives partner's contract number (externalContractNumber)
2. User manually finds matching contract in system
3. User copies internal ContractId (GUID)
4. User creates settlement with copied GUID
5. User manually enters quantities

**Issues**: Error-prone, 5+ steps, requires GUID copying

### Scenario: Create Shipping Operation
**Current Workflow**:
1. User selects contract from dropdown (shows internal ContractNumber)
2. User might not know which matches partner's externalContractNumber
3. User creates shipping operation

**Issue**: Dropdown doesn't show externalContractNumber

### Scenario: Settlement Search
**Current Limitation**:
- Cannot search settlements by partner's contract number
- Must know internal ContractId first
- Two-database-query workaround required

---

## RECOMMENDED SOLUTIONS (PHASED)

### Phase 1: Data Layer (1-2 days, URGENT)
1. Add missing SalesContractConfiguration index
2. Add domain event: ExternalContractNumberSetEvent
3. Add application-level uniqueness validation

### Phase 2: API Layer (2-3 days, URGENT)
1. Add externalContractNumber filter to contract queries
2. Add endpoints: GET /contracts/by-external-contract/{externalContractNumber}
3. Enhance Settlement API to accept externalContractNumber
4. Add Shipping Operations endpoints for external contracts

### Phase 3: Frontend (3-4 days, HIGH)
1. Add smart autocomplete showing both contract identifiers
2. Update SettlementEntry form to support external contract selection
3. Update ShippingOperationForm for external contract lookup
4. Create dual-search contract listing page

### Phase 4: Integration (2-3 days, MEDIUM)
1. Add external contract numbers to ContractMatching audit trail
2. Add Risk module support for external contract filtering
3. Implement business rule validation

---

## EFFORT & IMPACT SUMMARY

| Phase | Tasks | Effort | Impact | Timeline |
|-------|-------|--------|--------|----------|
| Phase 1 | Database fixes | 5-6h | Enables other phases | 1 day |
| Phase 2 | API endpoints | 14-16h | Enables external workflows | 2 days |
| Phase 3 | UI/Forms | 12-16h | User-facing improvements | 2 days |
| Phase 4 | Integration | 12-16h | Complete system alignment | 2 days |
| **TOTAL** | | **10-12 person-days** | **HIGH value, HIGH impact** | **1 week** |

---

## CRITICAL FILES AFFECTED

### Domain Layer
- PurchaseContract.cs (Lines 38-39, 188-204)
- SalesContract.cs (Lines 40-41, 346-362)
- ShippingOperation.cs (Line 44)
- ContractMatching.cs (Lines 34-35)
- Settlement.cs (Line 44)

### Configuration Layer
- PurchaseContractConfiguration.cs (Lines 29-36) ✓
- SalesContractConfiguration.cs (MISSING index) ✗

### API Layer
- PurchaseContractController.cs (Lines 49-87, 129-156)
- SalesContractController.cs (Lines 48-86, 126-150)
- SettlementController.cs (Lines 55-87 - WORKAROUND)
- ShippingOperationController.cs (NO external number support)

### Frontend Layer
- contracts.ts (Type definitions present)
- salesContracts.ts (Type definitions present)
- contractsApi.ts (Methods missing)
- SettlementEntry.tsx (Form UI missing)
- ShippingOperationForm.tsx (Form UI missing)

---

## CONCLUSION

**Status**: Partially implemented feature providing no business value

**Complexity**: Medium - Requires coordinated changes across 4 layers

**Business Impact**: HIGH - Improves trader efficiency, reduces errors

**Technical Debt**: CRITICAL - Completes half-implemented feature

**Recommendation**: Implement all phases in 1-week sprint for maximum ROI

---

*Report Generated: October 29, 2025*
*System Version: 2.6.7 (Production Ready)*
*Scope: Complete (Database, Domain, API, Frontend, Cross-Module)*

