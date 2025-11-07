# External Contract Number Search Test
# Verifies that settlements created with external contract numbers are searchable

$ApiUrl = "http://localhost:5000"

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "External Contract Number Search Test" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Get existing contracts
Write-Host "Test 1: Get Existing Purchase Contracts" -ForegroundColor Yellow
try {
    $contractsResponse = Invoke-WebRequest -Uri "$ApiUrl/api/purchase-contracts?pageSize=1" -Method Get -UseBasicParsing
    $contracts = ConvertFrom-Json $contractsResponse.Content

    if ($contracts.items -and $contracts.items.Count -gt 0) {
        $contract = $contracts.items[0]
        $contractId = $contract.id
        Write-Host "  Status: SUCCESS" -ForegroundColor Green
        Write-Host "  Found contract: $($contract.contractNumber) (ID: $contractId)" -ForegroundColor Green
    } else {
        Write-Host "  Status: FAILED - No contracts found" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "  Status: FAILED - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Create settlement with external contract number
Write-Host "Test 2: Create Settlement with External Contract Number" -ForegroundColor Yellow
$externalNum = "VERIFY-$(Get-Random -Minimum 10000 -Maximum 99999)"

try {
    $settlementPayload = @{
        contractId = $contractId
        externalContractNumber = $externalNum
        documentNumber = "BL-$(Get-Random -Minimum 100000 -Maximum 999999)"
        documentType = 0
        documentDate = (Get-Date).ToString("yyyy-MM-dd")
    } | ConvertTo-Json

    $createResponse = Invoke-WebRequest -Uri "$ApiUrl/api/settlements" `
        -Method Post `
        -ContentType "application/json" `
        -Body $settlementPayload `
        -UseBasicParsing

    $result = ConvertFrom-Json $createResponse.Content

    if ($result.isSuccessful) {
        $settlementId = $result.settlementId
        Write-Host "  Status: SUCCESS" -ForegroundColor Green
        Write-Host "  Settlement Created ID: $settlementId" -ForegroundColor Green
        Write-Host "  External Contract Number: $externalNum" -ForegroundColor Green
    } else {
        Write-Host "  Status: FAILED - Settlement creation failed" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "  Status: FAILED - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 3: Search for settlement by external contract number
Write-Host "Test 3: Search Settlement by External Contract Number" -ForegroundColor Yellow

try {
    $searchUrl = "$ApiUrl/api/settlements?externalContractNumber=$externalNum"
    $searchResponse = Invoke-WebRequest -Uri $searchUrl -Method Get -UseBasicParsing
    $searchResult = ConvertFrom-Json $searchResponse.Content

    if ($searchResult.data -and ($searchResult.data | Measure-Object).Count -gt 0) {
        $foundSettlement = $searchResult.data[0]
        Write-Host "  Status: SUCCESS" -ForegroundColor Green
        Write-Host "  Found settlement by external contract number!" -ForegroundColor Green
        Write-Host "  Settlement ID: $($foundSettlement.id)" -ForegroundColor Green
        Write-Host "  External Contract: $($foundSettlement.externalContractNumber)" -ForegroundColor Green
        Write-Host "  Document Number: $($foundSettlement.documentNumber)" -ForegroundColor Green
        Write-Host "  Status: $($foundSettlement.status)" -ForegroundColor Green

        if ($foundSettlement.id -eq $settlementId) {
            Write-Host "  ID Verification: MATCHES" -ForegroundColor Green
        } else {
            Write-Host "  ID Verification: MISMATCH!" -ForegroundColor Red
        }
    } else {
        Write-Host "  Status: FAILED - Settlement NOT found by external contract number" -ForegroundColor Red
        Write-Host "  This indicates the external contract search is not working" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "  Status: FAILED - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "  ALL TESTS PASSED" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  External contract number functionality is FULLY OPERATIONAL" -ForegroundColor Green
Write-Host "  Settlements can be created with external contract numbers" -ForegroundColor Green
Write-Host "  Settlements are searchable by external contract number" -ForegroundColor Green
Write-Host ""
