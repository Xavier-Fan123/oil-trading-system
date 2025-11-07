param(
    [string]$ApiUrl = "http://localhost:5000"
)

Write-Host "Oil Trading System - Complete Purchase Contracts"
Write-Host "=================================================="
Write-Host ""

# Test API connection
try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/health" -Method Get -ErrorAction Stop
    Write-Host "API Status: OK`n" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Cannot connect to API at $ApiUrl" -ForegroundColor Red
    Write-Host "Make sure the backend is running: dotnet run in src/OilTrading.Api" -ForegroundColor Yellow
    exit 1
}

# Contract 1: PC-2025-001 (EXT-SINOPEC-001)
Write-Host "Updating Contract 1: PC-2025-001 (Brent Crude, 50000 BBL)" -ForegroundColor Cyan
$contract1 = @{
    quantity = 50000.0
    quantityUnit = "BBL"
    pricingType = "Fixed"
    fixedPrice = 85.50
    deliveryTerms = "FOB"
    laycanStart = "2025-12-01T00:00:00Z"
    laycanEnd = "2025-12-15T00:00:00Z"
    loadPort = "Ras Tanura, Saudi Arabia"
    dischargePort = "Singapore"
    settlementType = "TT"
    creditPeriodDays = 30
    prepaymentPercentage = 0
    paymentTerms = "TT 30 days after B/L presentation"
    updatedBy = "System User"
}

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/purchase-contracts/1018c590-f739-4205-adec-02835745b691" `
        -Method Put `
        -ContentType "application/json" `
        -Body ($contract1 | ConvertTo-Json) `
        -ErrorAction Stop
    Write-Host "[OK] Contract 1 updated successfully`n" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to update contract 1: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Response: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
    }
}

# Contract 2: PC-2025-002 (EXT-PETRONAS-001)
Write-Host "Updating Contract 2: PC-2025-002 (WTI Crude, 30000 BBL)" -ForegroundColor Cyan
$contract2 = @{
    quantity = 30000.0
    quantityUnit = "BBL"
    pricingType = "Fixed"
    fixedPrice = 78.25
    deliveryTerms = "CIF"
    laycanStart = "2026-01-01T00:00:00Z"
    laycanEnd = "2026-01-20T00:00:00Z"
    loadPort = "Corpus Christi, USA"
    dischargePort = "Rotterdam, Netherlands"
    settlementType = "TT"
    creditPeriodDays = 45
    prepaymentPercentage = 10
    paymentTerms = "10% prepayment, balance TT 45 days after B/L"
    updatedBy = "System User"
}

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/purchase-contracts/75eb7a3d-04c2-4310-8d39-008d8939d9f5" `
        -Method Put `
        -ContentType "application/json" `
        -Body ($contract2 | ConvertTo-Json) `
        -ErrorAction Stop
    Write-Host "[OK] Contract 2 updated successfully`n" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to update contract 2: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Response: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
    }
}

# Contract 3: PC-2025-003 (EXT-SINOPEC-002)
Write-Host "Updating Contract 3: PC-2025-003 (Brent Crude, 25000 BBL)" -ForegroundColor Cyan
$contract3 = @{
    quantity = 25000.0
    quantityUnit = "BBL"
    pricingType = "Fixed"
    fixedPrice = 84.75
    deliveryTerms = "FOB"
    laycanStart = "2026-01-10T00:00:00Z"
    laycanEnd = "2026-01-25T00:00:00Z"
    loadPort = "Ras Tanura, Saudi Arabia"
    dischargePort = "Singapore"
    settlementType = "LC"
    creditPeriodDays = 60
    prepaymentPercentage = 0
    paymentTerms = "LC at sight, 60 days tenor"
    updatedBy = "System User"
}

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/purchase-contracts/cfa420f7-b4af-448d-a60c-baf595c48518" `
        -Method Put `
        -ContentType "application/json" `
        -Body ($contract3 | ConvertTo-Json) `
        -ErrorAction Stop
    Write-Host "[OK] Contract 3 updated successfully`n" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to update contract 3: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Response: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
    }
}

Write-Host "======================================"
Write-Host "Contract completion completed!" -ForegroundColor Green
Write-Host "======================================"
Write-Host ""
Write-Host "Summary:"
Write-Host "- Contract 1 (PC-2025-001): 50000 BBL @ USD 85.50/BBL = USD 4,275,000"
Write-Host "- Contract 2 (PC-2025-002): 30000 BBL @ USD 78.25/BBL = USD 2,347,500"
Write-Host "- Contract 3 (PC-2025-003): 25000 BBL @ USD 84.75/BBL = USD 2,118,750"
Write-Host ""
Write-Host "All contracts are now ready for activation!"
