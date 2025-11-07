param(
    [string]$ApiBaseUrl = "http://localhost:5000/api"
)

Write-Host "Oil Trading System - DAXIN MARINE Contract Import Script" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# Define contract data from spreadsheet
$contractsData = @(
    @{ contractId = "IGR-2025-CAG-S0253-1/2"; product = "GASOLINE USD"; quantity = 44.250; quantityUnit = "BBL"; price = 4000.200; priceCurrency = "USD"; laycanStart = "2025/11/8"; laycanEnd = "2025/11/24"; },
    @{ contractId = "IGR-2025-CAG-S0254-H/2"; product = "GASOLINE USD"; quantity = 31.800; quantityUnit = "BBL"; price = 2556.720; priceCurrency = "USD"; laycanStart = "2025/11/9"; laycanEnd = "2025/11/25"; },
    @{ contractId = "IGR-2025-CAG-S0254-3/4"; product = "GASOLINE USD"; quantity = 56.500; quantityUnit = "BBL"; price = 4542.600; priceCurrency = "USD"; laycanStart = "2025/11/13"; laycanEnd = "2025/11/29"; },
    @{ contractId = "IGR-2025-CAG-S0253-3/4"; product = "GASOLINE USD"; quantity = 54.500; quantityUnit = "BBL"; price = 5830.800; priceCurrency = "USD"; laycanStart = "2025/11/15"; laycanEnd = "2025/12/1"; },
    @{ contractId = "IGR-2025-CAG-S0264"; product = "GASOLINE USD"; quantity = 44.000; quantityUnit = "BBL"; price = 3537.600; priceCurrency = "USD"; laycanStart = "2025/11/26"; laycanEnd = "2025/12/12"; },
    @{ contractId = "IGR-2025-CAG-S0263"; product = "Low Sulphur Diesel"; quantity = 56.250; quantityUnit = "BBL"; price = 5085.000; priceCurrency = "USD"; laycanStart = "2025/11/24"; laycanEnd = "2025/12/10"; },
    @{ contractId = "IGR-2025-CAG-S0266"; product = "GASOLINE USD"; quantity = 67.000; quantityUnit = "BBL"; price = 5386.800; priceCurrency = "USD"; laycanStart = "2025/12/8"; laycanEnd = "2025/12/24"; },
    @{ contractId = "IGR-2025-CAG-S0267"; product = "Low Sulphur Diesel"; quantity = 103.500; quantityUnit = "BBL"; price = 9356.400; priceCurrency = "USD"; laycanStart = "2025/12/9"; laycanEnd = "2025/12/25"; },
    @{ contractId = "IGR-2025-CAG-S0271"; product = "GASOLINE USD"; quantity = 27.500; quantityUnit = "BBL"; price = 1917.000; priceCurrency = "USD"; laycanStart = "2025/12/12"; laycanEnd = "2025/12/28"; },
    @{ contractId = "IGR-2025-CAG-S0272"; product = "GASOLINE USD"; quantity = 37.500; quantityUnit = "BBL"; price = 3202.500; priceCurrency = "USD"; laycanStart = "2025/12/9"; laycanEnd = "2025/12/25"; },
    @{ contractId = "IGR-2025-CAG-S0276"; product = "GASOLINE USD"; quantity = 100.000; quantityUnit = "BBL"; price = 8030.000; priceCurrency = "USD"; laycanStart = "2026/1/14"; laycanEnd = "2026/1/30"; },
    @{ contractId = "IGR-2025-CAG-S0273"; product = "GASOLINE USD"; quantity = 103.000; quantityUnit = "BBL"; price = 8281.200; priceCurrency = "USD"; laycanStart = "2025/12/10"; laycanEnd = "2025/12/26"; },
    @{ contractId = "IGR-2025-CAG-S0274"; product = "GASOLINE USD"; quantity = 150.000; quantityUnit = "BBL"; price = 12045.000; priceCurrency = "USD"; laycanStart = "2026/1/14"; laycanEnd = "2026/1/30"; },
    @{ contractId = "IGR-2025-CAG-S0280"; product = "GASOLINE USD"; quantity = 30.000; quantityUnit = "BBL"; price = 2409.000; priceCurrency = "USD"; laycanStart = "2025/12/12"; laycanEnd = "2025/12/28"; },
    @{ contractId = "IGR-2025-CAG-S0281"; product = "Low Sulphur Diesel"; quantity = 22.500; quantityUnit = "BBL"; price = 1921.500; priceCurrency = "USD"; laycanStart = "2025/12/17"; laycanEnd = "2026/1/2"; },
    @{ contractId = "IGR-2025-CAG-S0282"; product = "Low Sulphur Diesel"; quantity = 36.000; quantityUnit = "BBL"; price = 3074.400; priceCurrency = "USD"; laycanStart = "2025/12/23"; laycanEnd = "2026/1/8"; },
)

function Test-ApiConnection {
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/health" -Method Get -ErrorAction Stop
        Write-Host "API Connection: SUCCESS" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "API Connection: FAILED" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Get-Trading-Partner {
    param([string]$partnerCode)
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/trading-partners" -Method Get -ErrorAction Stop
        $partner = $response.data | Where-Object { $_.code -eq $partnerCode } | Select-Object -First 1
        return $partner
    } catch {
        Write-Host "Error fetching trading partners: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Create-Trading-Partner {
    try {
        $body = @{
            code = "DAXIN"
            name = "DAXIN MARINE PTE LTD"
            companyCode = "DAXIN"
            companyName = "DAXIN MARINE PTE LTD"
            type = 1
            partnerType = 1
            creditLimit = 10000000
            country = "Singapore"
            contactEmail = "contact@daxinmarine.com"
            contactPhone = "+65-6000-0000"
            address = "Singapore"
            creditLimitValidUntil = (Get-Date).AddYears(1).ToString("yyyy-MM-ddTHH:mm:ss")
            isActive = $true
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/trading-partners" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
        Write-Host "Created trading partner: DAXIN MARINE PTE LTD (ID: $($response.id))" -ForegroundColor Green
        return $response.id
    } catch {
        Write-Host "Error creating trading partner: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Get-Product {
    param([string]$productCode)
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/products" -Method Get -ErrorAction Stop
        $product = $response.data | Where-Object {
            $_.code -eq $productCode -or $_.name -like "*$productCode*"
        } | Select-Object -First 1
        return $product
    } catch {
        Write-Host "Error fetching products: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Create-Product {
    param(
        [string]$code,
        [string]$name,
        [string]$type
    )
    try {
        $productTypeMap = @{
            "GASOLINE USD" = 4
            "Low Sulphur Diesel" = 4
        }
        $productType = $productTypeMap[$name]

        $body = @{
            code = $code
            name = $name
            productName = $name
            productCode = $code
            type = $productType
            productType = $productType
            description = "$name for trading"
            grade = "Standard"
            specification = "ISO 8217 compliant"
            unitOfMeasure = "BBL"
            density = 0.85
            origin = "Various"
            isActive = $true
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/products" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
        Write-Host "Created product: $name (ID: $($response.id))" -ForegroundColor Green
        return $response.id
    } catch {
        Write-Host "Error creating product: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Get-First-Trader {
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/users" -Method Get -ErrorAction Stop
        $trader = $response | Where-Object { $_.role -eq 1 } | Select-Object -First 1
        if (-not $trader) {
            $trader = $response | Select-Object -First 1
        }
        return $trader
    } catch {
        Write-Host "Error fetching users: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Create-Sales-Contract {
    param(
        [string]$externalContractNumber,
        [Guid]$tradingPartnerId,
        [Guid]$productId,
        [Guid]$traderId,
        [decimal]$quantity,
        [decimal]$price,
        [DateTime]$laycanStart,
        [DateTime]$laycanEnd
    )
    try {
        $contractNumber = "SC-" + (Get-Random -Minimum 100000 -Maximum 999999)

        $body = @{
            contractNumber = $contractNumber
            contractType = 0
            tradingPartnerId = $tradingPartnerId
            productId = $productId
            traderId = $traderId
            contractQuantity = @{
                value = $quantity
                unit = 1
            }
            tonBarrelRatio = 7.6
            externalContractNumber = $externalContractNumber
            paymentTerms = "NET 30"
            deliveryTerms = 4
            settlementType = 1
            estimatedPaymentDate = $laycanEnd.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss")
        } | ConvertTo-Json -Depth 3

        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/sales-contracts" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop

        # Update contract details
        $contractId = $response.id

        # Update Laycan dates
        $laycanBody = @{
            laycanStart = $laycanStart.ToString("yyyy-MM-ddTHH:mm:ss")
            laycanEnd = $laycanEnd.ToString("yyyy-MM-ddTHH:mm:ss")
        } | ConvertTo-Json

        Invoke-RestMethod -Uri "$ApiBaseUrl/sales-contracts/$contractId/laycan" -Method Put -Body $laycanBody -ContentType "application/json" -ErrorAction Stop | Out-Null

        # Update ports
        $portsBody = @{
            loadPort = "Singapore"
            dischargePort = "Singapore"
        } | ConvertTo-Json

        Invoke-RestMethod -Uri "$ApiBaseUrl/sales-contracts/$contractId/ports" -Method Put -Body $portsBody -ContentType "application/json" -ErrorAction Stop | Out-Null

        Write-Host "Created sales contract: $externalContractNumber (Contract ID: $contractNumber)" -ForegroundColor Green
        return $contractId
    } catch {
        Write-Host "Error creating sales contract: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Main execution
Write-Host ""
Write-Host "Step 1: Testing API Connection..." -ForegroundColor Yellow
if (-not (Test-ApiConnection)) {
    Write-Host "Aborting: API server not responding" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Setting up Trading Partner..." -ForegroundColor Yellow
$daxinPartner = Get-Trading-Partner -partnerCode "DAXIN"
if ($daxinPartner) {
    Write-Host "Trading partner DAXIN already exists (ID: $($daxinPartner.id))" -ForegroundColor Cyan
    $daxinId = $daxinPartner.id
} else {
    $daxinId = Create-Trading-Partner
    if (-not $daxinId) {
        Write-Host "Failed to create trading partner. Aborting." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Step 3: Verifying Products..." -ForegroundColor Yellow
$gasolineProduct = Get-Product -productCode "GASOLINE"
if (-not $gasolineProduct) {
    Write-Host "Creating GASOLINE USD product..." -ForegroundColor Yellow
    $gasolineProduct = @{ id = (Create-Product -code "GASOLINE" -name "GASOLINE USD" -type "RefinedProducts") }
} else {
    Write-Host "GASOLINE USD product already exists (ID: $($gasolineProduct.id))" -ForegroundColor Cyan
}

$dieselProduct = Get-Product -productCode "DIESEL"
if (-not $dieselProduct) {
    Write-Host "Creating Low Sulphur Diesel product..." -ForegroundColor Yellow
    $dieselProduct = @{ id = (Create-Product -code "DIESEL" -name "Low Sulphur Diesel" -type "RefinedProducts") }
} else {
    Write-Host "Low Sulphur Diesel product already exists (ID: $($dieselProduct.id))" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Step 4: Getting Trader User..." -ForegroundColor Yellow
$trader = Get-First-Trader
if ($trader) {
    Write-Host "Using trader: $($trader.name) (ID: $($trader.id))" -ForegroundColor Green
} else {
    Write-Host "No traders found. Please create a trader user first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 5: Importing Sales Contracts..." -ForegroundColor Yellow
Write-Host "Total contracts to import: $($contractsData.Count)" -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$failureCount = 0

foreach ($contract in $contractsData) {
    $productId = if ($contract.product -like "*GASOLINE*") { $gasolineProduct.id } else { $dieselProduct.id }

    $laycanStart = [DateTime]::ParseExact($contract.laycanStart, "yyyy/MM/dd", $null)
    $laycanEnd = [DateTime]::ParseExact($contract.laycanEnd, "yyyy/MM/dd", $null)

    $result = Create-Sales-Contract `
        -externalContractNumber $contract.contractId `
        -tradingPartnerId $daxinId `
        -productId $productId `
        -traderId $trader.id `
        -quantity $contract.quantity `
        -price $contract.price `
        -laycanStart $laycanStart `
        -laycanEnd $laycanEnd

    if ($result) {
        $successCount++
    } else {
        $failureCount++
    }

    Start-Sleep -Milliseconds 100
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Import Summary:" -ForegroundColor Cyan
Write-Host "Total Contracts Processed: $($contractsData.Count)" -ForegroundColor White
Write-Host "Successfully Created: $successCount" -ForegroundColor Green
Write-Host "Failed: $failureCount" -ForegroundColor $(if ($failureCount -gt 0) { "Red" } else { "Green" })
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

if ($failureCount -eq 0) {
    Write-Host "All contracts imported successfully!" -ForegroundColor Green
} else {
    Write-Host "Some contracts failed. Please review the errors above." -ForegroundColor Yellow
}
