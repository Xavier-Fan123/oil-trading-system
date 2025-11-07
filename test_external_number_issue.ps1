# Test script to diagnose external contract number issue

param(
    [string]$ApiUrl = "http://localhost:5000"
)

Write-Host "Settlement External Contract Number Diagnostic" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Get products
$productsResponse = Invoke-WebRequest -Uri "$ApiUrl/api/products?pageSize=50" -Method Get -UseBasicParsing
$products = ConvertFrom-Json $productsResponse.Content
$productId = $products.data[0].id

# Get trading partners
$partnersResponse = Invoke-WebRequest -Uri "$ApiUrl/api/trading-partners?pageSize=50" -Method Get -UseBasicParsing
$partners = ConvertFrom-Json $partnersResponse.Content
$partnerId = $partners.data[0].id

Write-Host "Step 1: Creating purchase contract with external number..." -ForegroundColor Yellow
$externalNum = "TEST-EXTERNAL-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Host "  External number: $externalNum" -ForegroundColor Cyan

$contractPayload = @{
    externalContractNumber = $externalNum
    contractNumber = "PC-TEST-$(Get-Random -Minimum 1000 -Maximum 9999)"
    productId = $productId
    tradingPartnerId = $partnerId
    quantity = 1000
    quantityUnit = 0
    fixedPrice = 85.50
    deliveryTerms = 0
    settlementType = 0
    paymentTerms = "NET 30"
    laycanStart = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
    laycanEnd = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
} | ConvertTo-Json

try {
    $createResponse = Invoke-WebRequest -Uri "$ApiUrl/api/purchase-contracts" `
        -Method Post `
        -ContentType "application/json" `
        -Body $contractPayload `
        -UseBasicParsing

    $contract = ConvertFrom-Json $createResponse.Content
    $contractId = $contract.id
    Write-Host "  SUCCESS: Contract created with ID: $contractId" -ForegroundColor Green
    Write-Host "  Contract external number from API response: $($contract.externalContractNumber)" -ForegroundColor Cyan
}
catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Creating settlement from this contract..." -ForegroundColor Yellow

$settlementPayload = @{
    externalContractNumber = $externalNum
    documentNumber = "BL-$(Get-Random -Minimum 100000 -Maximum 999999)"
    documentType = 0
    documentDate = (Get-Date).ToString("yyyy-MM-dd")
} | ConvertTo-Json

try {
    $createResponse = Invoke-WebRequest -Uri "$ApiUrl/api/settlements/create-by-external-contract" `
        -Method Post `
        -ContentType "application/json" `
        -Body $settlementPayload `
        -UseBasicParsing

    $settlement = ConvertFrom-Json $createResponse.Content
    if ($settlement.isSuccessful) {
        $settlementId = $settlement.settlementId
        Write-Host "  SUCCESS: Settlement created with ID: $settlementId" -ForegroundColor Green
    }
    else {
        Write-Host "  FAILED: $($settlement.errorMessage)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Retrieving settlement by ID..." -ForegroundColor Yellow

try {
    $getResponse = Invoke-WebRequest -Uri "$ApiUrl/api/settlements/$settlementId" -Method Get -UseBasicParsing
    $retrieved = ConvertFrom-Json $getResponse.Content
    Write-Host "  SUCCESS: Retrieved settlement by ID" -ForegroundColor Green
    Write-Host "  Settlement ID: $($retrieved.id)" -ForegroundColor Cyan
    Write-Host "  Contract Number: $($retrieved.contractNumber)" -ForegroundColor Cyan
    Write-Host "  External Contract Number: $($retrieved.externalContractNumber)" -ForegroundColor Cyan
    Write-Host "  External Contract Number is: $(if ([string]::IsNullOrEmpty($retrieved.externalContractNumber)) {'EMPTY/NULL'} else {'POPULATED'})" -ForegroundColor Yellow
}
catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 4: CRITICAL TEST - Searching for settlement by external contract number..." -ForegroundColor Red

$searchUrl = "$ApiUrl/api/settlements?externalContractNumber=$externalNum"
Write-Host "  Search URL: $searchUrl" -ForegroundColor Cyan
Write-Host "  Searching for: $externalNum" -ForegroundColor Cyan

try {
    $searchResponse = Invoke-WebRequest -Uri $searchUrl -Method Get -UseBasicParsing
    $searchResult = ConvertFrom-Json $searchResponse.Content

    if ($searchResult.data -and ($searchResult.data | Measure-Object).Count -gt 0) {
        Write-Host "  SUCCESS: Found settlement(s)" -ForegroundColor Green
        Write-Host "  Count: $($searchResult.data.Count)" -ForegroundColor Green
        $searchResult.data | ForEach-Object {
            Write-Host "    - Settlement: $($_.id)" -ForegroundColor Green
            Write-Host "      External Contract: $($_.externalContractNumber)" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  FAILURE: NO SETTLEMENTS FOUND" -ForegroundColor Red
        Write-Host ""
        Write-Host "ANALYSIS:" -ForegroundColor Yellow
        Write-Host "  We can retrieve by ID: $settlementId" -ForegroundColor Yellow
        Write-Host "  But search by external: '$externalNum' returns ZERO results" -ForegroundColor Red
        Write-Host ""
        Write-Host "  Possible causes:" -ForegroundColor Yellow
        Write-Host "    1. ExternalContractNumber field is NULL in database (step 3 showed it)" -ForegroundColor Yellow
        Write-Host "    2. Settlement stored in wrong table (Settlement vs PurchaseSettlements)" -ForegroundColor Yellow
        Write-Host "    3. GetByExternalContractNumberAsync querying wrong table" -ForegroundColor Yellow
        Write-Host "    4. Repository not being used for search (using different repo/service)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  FAILED: $_" -ForegroundColor Red
}

Write-Host ""
