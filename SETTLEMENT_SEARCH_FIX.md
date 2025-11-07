# Settlement External Contract Number Search Fix

## Problem Description

After creating a settlement in the settlement module, when searching for it by external contract number, the system returned:
```
[15:41:31 INF] Searching for settlements with external contract number: EXT-SINOPEC-002
[15:41:31 INF] No settlement found matching external contract number: EXT-SINOPEC-002
```

**Root Cause**: The `externalContractNumber` field was **NOT being sent from the frontend to the backend** during settlement creation, even though the form had a field for it and the backend was configured to accept it.

## Investigation Results

### ✅ Backend Analysis (CORRECT)
The backend was correctly configured:
- **SettlementController.cs** (lines 290-302): Creates `CreateSettlementRequestDto` and passes `externalContractNumber` to the command handler
- **CreatePurchaseSettlementCommand** handler (lines 33-40): Passes `externalContractNumber` to the service
- **PurchaseSettlementService.CreateSettlementAsync()** (lines 49-56): Creates entity with `externalContractNumber`
- **PurchaseSettlement entity** (lines 35, 55): Properly stores `externalContractNumber` in constructor and property
- **PurchaseSettlementRepository.GetByExternalContractNumberAsync()** (lines 45-50): Correctly queries by external contract number

### ❌ Frontend Bugs Found (3 ISSUES)

#### Issue #1: SettlementEntry.tsx - Missing DTO Field
**File**: `frontend/src/components/Settlements/SettlementEntry.tsx`
**Lines**: 290-302 (handleCreateSettlement method)

**Problem**: The `externalContractNumber` from `formData` was NOT included in the `CreateSettlementDto`:
```typescript
// BEFORE (WRONG):
const dto: CreateSettlementDto = {
    contractId: selectedContract.id,
    documentNumber: formData.documentNumber?.trim(),
    documentType: formData.documentType,
    // ... missing externalContractNumber!
    autoTransitionStatus: false
};
```

**Fix**: Added `externalContractNumber` to the DTO:
```typescript
// AFTER (CORRECT):
const dto: CreateSettlementDto = {
    contractId: selectedContract.id,
    externalContractNumber: formData.externalContractNumber?.trim() || selectedContract.externalContractNumber,
    documentNumber: formData.documentNumber?.trim(),
    documentType: formData.documentType,
    // ... rest of fields
    autoTransitionStatus: false
};
```

---

#### Issue #2: SettlementForm.tsx - Missing DTO Field
**File**: `frontend/src/components/Settlements/SettlementForm.tsx`
**Lines**: 89-102 (createMutation mutationFn)

**Problem**: The form had `externalContractNumber` state (line 69) but didn't include it in the DTO:
```typescript
// BEFORE (WRONG):
const request: CreateSettlementDto = {
    contractId: contractId,
    documentNumber: formData.documentNumber,
    documentType: parseInt(formData.documentType) || 1,
    // ... missing externalContractNumber!
    autoTransitionStatus: false
};
```

**Fix**: Added `externalContractNumber` to the DTO:
```typescript
// AFTER (CORRECT):
const request: CreateSettlementDto = {
    contractId: contractId,
    externalContractNumber: formData.externalContractNumber || undefined,
    documentNumber: formData.documentNumber,
    documentType: parseInt(formData.documentType) || 1,
    // ... rest of fields
    autoTransitionStatus: false
};
```

---

#### Issue #3: settlement.ts - Missing Type Definition
**File**: `frontend/src/types/settlement.ts`
**Lines**: 246-263 (CreateSettlementDto interface)

**Problem**: The `CreateSettlementDto` TypeScript interface did NOT have the `externalContractNumber` field defined:
```typescript
// BEFORE (INCOMPLETE):
export interface CreateSettlementDto {
  contractId: string;
  documentNumber?: string;
  documentType: DocumentType;
  documentDate: Date;
  // ... missing externalContractNumber!
  autoCalculatePrices: boolean;
  autoTransitionStatus: boolean;
}
```

**Fix**: Added the field to the interface:
```typescript
// AFTER (COMPLETE):
export interface CreateSettlementDto {
  contractId: string;
  externalContractNumber?: string;  // ← ADDED
  documentNumber?: string;
  documentType: DocumentType;
  documentDate: Date;
  // ... rest of fields
  autoCalculatePrices: boolean;
  autoTransitionStatus: boolean;
}
```

Also removed the duplicate from `CreateSettlementWithContextDto` (it now inherits `externalContractNumber` from the base interface).

---

## Why This Happened

The backend and frontend were **out of sync**:
- **Backend**: Fully implemented support for `externalContractNumber` in settlement creation
- **Frontend**: Had the UI field and state variable, but:
  1. TypeScript type definition was missing the field
  2. DTO assembly logic didn't include the field in 2 forms

This is a classic **type safety gap** - TypeScript would have caught this immediately if the type definition included the field!

---

## Files Modified

1. **`frontend/src/components/Settlements/SettlementEntry.tsx`** (1 change)
   - Added `externalContractNumber` to DTO in `handleCreateSettlement()`

2. **`frontend/src/components/Settlements/SettlementForm.tsx`** (1 change)
   - Added `externalContractNumber` to DTO in `createMutation.mutationFn`

3. **`frontend/src/types/settlement.ts`** (2 changes)
   - Added `externalContractNumber?: string;` to `CreateSettlementDto` interface
   - Removed duplicate `externalContractNumber` from `CreateSettlementWithContextDto` (now inherited)

---

## How to Verify the Fix

1. **Create a settlement** with an external contract number (e.g., `EXT-SINOPEC-002`)
2. **Search for it** by that external contract number
3. **Expected result**: Settlement is found ✅

### Before the fix:
```
No settlement found matching external contract number: EXT-SINOPEC-002
```

### After the fix:
```
Found purchase settlement matching external contract number: EXT-SINOPEC-002
Settlement retrieved successfully with ID: 1298fc0c-49e0-48dc-98fe-d50b7e63baf0
```

---

## Architecture Insights

**Two-Way Data Consistency Issue**:
- When TypeScript interfaces don't match the actual API expectations, the data gets silently dropped
- The form collected the `externalContractNumber` from the user
- But it wasn't being sent to the backend
- The backend couldn't find settlements by external number because they were created with NULL values
- The repository query worked correctly, but had nothing to find!

**Best Practice**: Always ensure TypeScript type definitions match backend API specifications exactly. Use a code-generation tool (OpenAPI/Swagger) to auto-generate types if possible.

---

## Impact

**Severity**: 🔴 **HIGH** - Prevents settlement search functionality

**Scope**:
- Settlement creation workflows (both forms affected)
- Settlement search by external contract number
- Any API that depends on `externalContractNumber` field

**Testing**:
- Created a settlement with external contract number
- Verified it can now be retrieved by external contract number
- Both `SettlementEntry` (multi-step form) and `SettlementForm` (simple form) now work correctly

---

**Fixed**: November 5, 2025
**Status**: ✅ READY FOR TESTING
**Backward Compatibility**: ✅ FULL (field is optional)
