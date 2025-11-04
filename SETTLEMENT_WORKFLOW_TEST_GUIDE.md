# Settlement Workflow - End-to-End Testing Guide v2.9.0

**Created**: November 4, 2025
**Test Status**: Ready for execution
**Prerequisites**: Backend and frontend running

---

## Quick Start - Testing the New Settlement Workflow

### Prerequisites

1. **Backend running**: http://localhost:5000
   ```bash
   cd "c:\Users\itg\Desktop\X\src\OilTrading.Api"
   dotnet run
   ```

2. **Frontend running**: http://localhost:3002
   ```bash
   cd "c:\Users\itg\Desktop\X\frontend"
   npm run dev
   ```

3. **Database seeded**: Sample data should be available (contracts, products, partners)

---

## Test Case 1: Create a New Purchase Settlement (Full Workflow)

### Objective
Test the complete 6-step settlement creation workflow from contract selection through final submission.

### Test Steps

#### Step 0: Contract Selection
1. Navigate to **Settlement** module in left sidebar
2. Click **"+ Create Settlement"** button
3. **Expected**: Settlement Entry form appears
4. In "Contract Selection" step:
   - Select **"PC-2025-001"** from dropdown
   - **Verify**: Contract details appear showing:
     - Supplier: SINOPEC
     - Product: Brent Crude Oil
     - Quantity: 50,000 BBL

#### Step 1: Document Information
1. Click **"Next"** button
2. **Expected**: Moving to "Document Information" step
3. Fill in the form:
   - Document Number: `BL-2024-NOV-001`
   - Document Type: `Bill of Lading`
   - Document Date: `2024-11-04` (today)
4. Click **"Next"**
5. **Expected**: Form validates and proceeds

#### Step 2: Quantity Calculation
1. **Expected**: In "Quantity Calculation" step
2. Fill in actual quantities from document:
   - Actual Quantity MT: `25000`
   - Actual Quantity BBL: `183250`
3. **Verify**: Quantities appear valid (green checkmarks)
4. Click **"Next"**
5. **🔄 AUTOMATIC**: Behind the scenes:
   - `handleCreateSettlement()` is called
   - Settlement is created via API with submitted data
   - Settlement data is stored for use in calculation step

#### Step 3: Settlement Calculation ⭐ **NEW STEP (ADDRESSES USER REQUEST)**
1. **Expected**: Step 3 appears with title "Settlement Calculation"
2. **Expected**: Info alert shows:
   > "Settlement has been created. Now enter the benchmark amount and adjustment amount for final settlement price calculation."
3. **Verify**: SettlementCalculationForm is displayed with fields:
   - Quantity (MT): Shows previously entered value
   - Quantity (BBL): Shows previously entered value
   - Benchmark Amount (USD): Empty, ready for input
   - Adjustment Amount (USD): Empty, ready for input
   - Calculation Note: Empty text area

4. **USER ENTERS PRICING** (This is the response to user's question):
   - Benchmark Amount: `85.50`
   - Adjustment Amount: `0.25`
   - Calculation Note: `Pricing as per contract amendment`

5. **Verify**: System displays calculation preview:
   ```
   Benchmark Total:    $2,137,500.00  (25,000 × 85.50)
   Adjustment Total:        $45,812.50  (183,250 × 0.25)
   ─────────────────────────────────────
   Total Settlement:   $2,183,312.50
   ```

6. Click **"Calculate"** button
7. **Expected**: Button shows loading state
8. **Expected**: After success, alert shows updated settlement
9. Click **"Next"** to proceed

#### Step 4: Initial Charges (Optional)
1. **Expected**: In "Initial Charges" step
2. Click **"Add Charge"** button
3. Add a sample charge:
   - Charge Type: `Transportation`
   - Amount: `50000`
   - Description: `Freight charges Ras Tanura to Singapore`
4. Click **"Add Charge"** again to add second charge:
   - Charge Type: `Insurance`
   - Amount: `15000`
   - Description: `Marine insurance`
5. **Verify**: Both charges appear as cards
6. Click **"Next"**

#### Step 5: Review & Submit
1. **Expected**: In "Review & Submit" step
2. **Verify**: All information is displayed:
   - **Contract Information**:
     - Contract: PC-2025-001
     - Supplier: SINOPEC
     - Product: Brent Crude Oil
   - **Document Information**:
     - Document: BL-2024-NOV-001
     - Type: Bill of Lading
     - Date: Nov 4, 2024
   - **Actual Quantities**:
     - MT: 25,000
     - BBL: 183,250
   - **Settlement Calculation** ⭐:
     - Benchmark Amount: **$85.50**
     - Adjustment Amount: **$0.25**
     - Calculation MT: **25,000 MT**
   - **Initial Charges**:
     - 2 charges added
     - Total: $65,000 USD

3. Click **"Create Settlement"** button
4. **Expected**: Loading state, then success message
5. **Expected**: Redirect to Settlement list view
6. **Expected**: New settlement appears in list with:
   - Status: Draft
   - Contract: PC-2025-001
   - Settlement pricing data visible

### Expected Result
✅ Settlement created successfully with all calculation and charge data

---

## Test Case 2: Verify Settlement Calculation Form Visibility

### Objective
Confirm that the settlement pricing form (最终结算价等信息) is now visible to users, addressing the original user request.

### Test Steps

1. Navigate to Settlement module
2. Create a new settlement and proceed through Steps 0-2
3. **Verify Step 3 appears** with:
   - Title: "Settlement Calculation"
   - Description about entering benchmark and adjustment amounts
   - SettlementCalculationForm component with:
     - Quantity fields (MT, BBL)
     - **Benchmark Amount field** ← VISIBLE (previously missing)
     - **Adjustment Amount field** ← VISIBLE (previously missing)
     - Calculation Note field
     - Real-time calculation display
     - Calculate button

### Expected Result
✅ Settlement pricing form is visible and functional

---

## Test Case 3: Edit Existing Settlement

### Objective
Test editing an existing settlement's calculation data.

### Test Steps

1. Navigate to Settlement list
2. Click on previously created settlement (from Test Case 1)
3. **Expected**: Settlement detail page with "Edit" button appears
4. Click **"Edit"** button
5. **Expected**: Settlement Edit form opens with existing data populated
6. Navigate through steps (can skip or modify)
7. In **Step 3 (Settlement Calculation)**:
   - Modify Benchmark Amount: `86.00`
   - Modify Adjustment Amount: `0.30`
8. Click **"Calculate"** to update
9. Proceed through remaining steps
10. Click **"Update Settlement"**
11. **Expected**: Changes saved and reflected in list view

### Expected Result
✅ Settlement editing works with calculation step accessible

---

## Test Case 4: Workflow Validation

### Objective
Test that workflow validation works correctly at each step.

### Test Steps

#### Test Contract Selection Validation
1. Go to Step 0 (Contract Selection)
2. Don't select any contract
3. Click **"Next"**
4. **Expected**: Error message: "Please select a contract"

#### Test Document Information Validation
1. Select a contract and click **"Next"**
2. Go to Step 1 (Document Information)
3. Leave Document Number empty
4. Click **"Next"**
5. **Expected**: Error message: "Document number is required"

#### Test Quantity Validation
1. Fill in Document Number and click **"Next"**
2. Go to Step 2 (Quantity Calculation)
3. Leave both quantities as 0
4. Click **"Next"**
5. **Expected**: Error message: "Both MT and BBL quantities must be greater than zero"

### Expected Result
✅ All validations work correctly

---

## Test Case 5: Settlement Calculation Form Features

### Objective
Test individual features of the SettlementCalculationForm component.

### Test Steps

1. Create settlement and reach Step 3 (Settlement Calculation)
2. **Test Real-time Calculation**:
   - Enter Quantity MT: `1000`
   - Enter Benchmark Amount: `100`
   - **Verify**: Benchmark Total shows `$100,000.00`
   - Change Quantity MT to `2000`
   - **Verify**: Benchmark Total updates to `$200,000.00`

3. **Test Adjustment Calculation**:
   - Enter Quantity BBL: `7330`
   - Enter Adjustment Amount: `10`
   - **Verify**: Adjustment Total shows `$73,300.00`
   - **Verify**: Total Settlement Amount shows sum correctly

4. **Test Form Validation**:
   - Click **"Calculate"** with empty Benchmark Amount
   - **Verify**: Button is disabled or error appears

5. **Test Calculation Notes**:
   - Enter note: `Calculated per contract terms amendment`
   - Click **"Calculate"**
   - **Verify**: Note is saved to settlement

### Expected Result
✅ All calculation form features work correctly

---

## Visual Verification Checklist

After completing Test Case 1, verify these visual elements:

- [ ] Step indicator shows 6 steps (not 5)
- [ ] Step 3 is labeled "Settlement Calculation"
- [ ] Step 4 is labeled "Initial Charges"
- [ ] Step 5 is labeled "Review & Submit"
- [ ] SettlementCalculationForm appears in Step 3
- [ ] Benchmark Amount field is visible and editable
- [ ] Adjustment Amount field is visible and editable
- [ ] Calculation preview shows real-time totals
- [ ] Calculate button is visible and functional
- [ ] Review & Submit shows settlement calculation data
- [ ] All form fields have proper styling and labels

---

## API Integration Testing

### Settlement Creation API
**Endpoint**: `POST /api/settlements`

Expected flow:
```
Request payload:
{
  "contractId": "xxx-xxx-xxx",
  "documentNumber": "BL-2024-NOV-001",
  "documentType": "BillOfLading",
  "documentDate": "2024-11-04T00:00:00Z",
  "actualQuantityMT": 25000,
  "actualQuantityBBL": 183250,
  "createdBy": "CurrentUser",
  "notes": "Initial settlement",
  "settlementCurrency": "USD",
  "autoCalculatePrices": false,
  "autoTransitionStatus": false
}

Response:
{
  "isSuccessful": true,
  "settlementId": "settlement-id-guid",
  "contractNumber": "PC-2025-001",
  "status": "Draft",
  "actualQuantityMT": 25000,
  "actualQuantityBBL": 183250
}
```

### Settlement Calculation API
**Endpoint**: `POST /api/settlements/{settlementId}/calculate`

Expected flow:
```
Request payload:
{
  "calculationQuantityMT": 25000,
  "calculationQuantityBBL": 183250,
  "benchmarkAmount": 85.50,
  "adjustmentAmount": 0.25,
  "calculationNote": "Pricing as per contract"
}

Response:
{
  "id": "settlement-id-guid",
  "status": "Calculated",
  "benchmarkAmount": 85.50,
  "adjustmentAmount": 0.25,
  "calculationQuantityMT": 25000,
  "calculationQuantityBBL": 183250,
  "totalSettlementAmount": 2183312.50
}
```

---

## Troubleshooting

### Issue: Settlement Creation fails
- Check backend is running: `curl http://localhost:5000/health`
- Check database has sample contracts
- Verify contract ID is valid GUID
- Check browser console for API errors

### Issue: Calculation form doesn't appear
- Clear browser cache (Ctrl+Shift+Delete)
- Restart frontend: `npm run dev`
- Verify SettlementCalculationForm is imported in SettlementEntry.tsx

### Issue: Pricing not saved
- Check network tab in browser DevTools
- Verify `/api/settlements/{id}/calculate` endpoint is working
- Check backend logs for validation errors

---

## Success Criteria

✅ **Workflow Complete**: All 6 steps function correctly
✅ **Pricing Form Visible**: SettlementCalculationForm displays in Step 3
✅ **User Request Addressed**: "最终结算价等信息" form is now visible
✅ **Calculations Work**: Benchmark and adjustment amounts calculate correctly
✅ **Data Persistence**: All entered data is saved to database
✅ **Validation Works**: Form validation prevents invalid submissions
✅ **API Integration**: Backend endpoints properly called and return data
✅ **User Experience**: Workflow is intuitive and step-by-step

---

## Notes for User

The settlement pricing form (最终结算价等信息) that you asked about is now fully implemented and visible in Step 3 of the Settlement creation workflow. Users will see:

1. **Benchmark Amount field** - For entering the main settlement price
2. **Adjustment Amount field** - For entering price adjustments
3. **Quantity fields** - For specifying calculation quantities
4. **Real-time calculation display** - Shows total settlement amount
5. **Calculation notes** - For audit trail documentation

The form is integrated seamlessly into the 6-step settlement workflow and allows users to enter all pricing information needed for final settlement calculations.

---

**Status**: ✅ READY FOR END-TO-END TESTING
**Build Status**: Zero TypeScript errors, zero C# compilation errors
**Frontend Build**: ✅ Vite build successful
**Backend Build**: ✅ All projects compile successfully
