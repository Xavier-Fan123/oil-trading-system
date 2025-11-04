#!/usr/bin/env pwsh

# Test Settlement Creation and Retrieval Flow
$API_BASE = "http://localhost:5000/api"

Write-Host "=== Testing Settlement Workflow ===" -ForegroundColor Cyan

# Step 1: Get a purchase contract
Write-Host "`n[1/3] Getting available contracts..." -ForegroundColor Yellow
$contractsResult = Invoke-RestMethod -Uri "$API_BASE/purchase-contracts?pageNumber=1&pageSize=10" -Method GET -ErrorAction SilentlyContinue

# The API returns "items" not "data"
$contracts = if ($contractsResult.items) { $contractsResult.items } elseif ($contractsResult.data) { $contractsResult.data } else { $null }

if ($contracts -and $contracts.Count -gt 0) {
    $contract = $contracts[0]
    $contractId = $contract.id
    Write-Host "✅ Found contract: $($contract.contractNumber) (ID: $contractId)" -ForegroundColor Green
} else {
    Write-Host "❌ No contracts found" -ForegroundColor Red
    write "Response: $($contractsResult | ConvertTo-Json)"
    exit 1
}

# Step 2: Create a settlement
Write-Host "`n[2/3] Creating settlement..." -ForegroundColor Yellow
$settlementPayload = @{
    contractId = $contractId
    documentNumber = "BL-TEST-001"
    documentType = 1
    documentDate = (Get-Date).ToString("yyyy-MM-ddT00:00:00Z")
    actualQuantityMT = 100
    actualQuantityBBL = 730
    createdBy = "TestUser"
    notes = "Test settlement for verification"
    settlementCurrency = "USD"
    autoCalculatePrices = $false
    autoTransitionStatus = $false
}

try {
    $createResult = Invoke-RestMethod -Uri "$API_BASE/settlements" -Method POST -Body ($settlementPayload | ConvertTo-Json) -ContentType "application/json" -ErrorAction Stop
    $settlementId = $createResult.settlementId
    Write-Host "✅ Settlement created: $settlementId" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to create settlement: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Retrieve the settlement
Write-Host "`n[3/3] Retrieving settlement..." -ForegroundColor Yellow
try {
    $getResult = Invoke-RestMethod -Uri "$API_BASE/settlements/$settlementId" -Method GET -ErrorAction Stop
    Write-Host "✅ Settlement retrieved successfully!" -ForegroundColor Green
    Write-Host "   ID: $($getResult.id)" -ForegroundColor Green
    Write-Host "   Status: $($getResult.status)" -ForegroundColor Green
    Write-Host "   Created: $($getResult.createdDate)" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to retrieve settlement: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== ✅ All tests passed! ===" -ForegroundColor Green
