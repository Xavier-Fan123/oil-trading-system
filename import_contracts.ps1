param(
    [string]$ApiBaseUrl = "http://localhost:5000/api"
)

Write-Host "Oil Trading System - Contract Import" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Test API connection
Write-Host "Testing API connection..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$ApiBaseUrl/health" -Method Get -ErrorAction Stop
    Write-Host "API Status: OK" -ForegroundColor Green
} catch {
    Write-Host "API Connection Failed" -ForegroundColor Red
    exit 1
}

# Check for trading partner DAXIN
Write-Host ""
Write-Host "Checking trading partners..." -ForegroundColor Yellow
try {
    $partners = Invoke-RestMethod -Uri "$ApiBaseUrl/trading-partners" -Method Get -ErrorAction Stop
    $daxinPartner = $partners.data | Where-Object { $_.code -eq "DAXIN" } | Select-Object -First 1

    if ($daxinPartner) {
        Write-Host "Found existing DAXIN partner (ID: $($daxinPartner.id))" -ForegroundColor Green
        $daxinId = $daxinPartner.id
    } else {
        Write-Host "DAXIN partner not found, creating..." -ForegroundColor Yellow

        $partnerBody = @{
            companyName = "DAXIN MARINE PTE LTD"
            partnerType = "Customer"
            contactEmail = "contact@daxinmarine.com"
            contactPhone = "+65-6000-0000"
            address = "Singapore"
            creditLimit = 10000000
            creditLimitValidUntil = (Get-Date).AddYears(1).ToString("yyyy-MM-ddTHH:mm:ss")
            paymentTermDays = 30
        } | ConvertTo-Json

        $newPartner = Invoke-RestMethod -Uri "$ApiBaseUrl/trading-partners" -Method Post -Body $partnerBody -ContentType "application/json" -ErrorAction Stop
        $daxinId = $newPartner.id
        Write-Host "Created DAXIN partner (ID: $daxinId)" -ForegroundColor Green
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Check for products - Use WTI for GASOLINE and MGO for Diesel
Write-Host ""
Write-Host "Checking products..." -ForegroundColor Yellow
try {
    $products = Invoke-RestMethod -Uri "$ApiBaseUrl/products" -Method Get -ErrorAction Stop

    $gasProduct = $products | Where-Object { $_.code -eq "WTI" } | Select-Object -First 1
    if (-not $gasProduct) {
        Write-Host "WTI product not found!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Found WTI product for GASOLINE (ID: $($gasProduct.id))" -ForegroundColor Green

    $dieselProduct = $products | Where-Object { $_.code -eq "MGO" } | Select-Object -First 1
    if (-not $dieselProduct) {
        Write-Host "MGO product not found!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Found MGO product for Low Sulphur Diesel (ID: $($dieselProduct.id))" -ForegroundColor Green

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Get trader user
Write-Host ""
Write-Host "Getting trader user..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/users" -Method Get -ErrorAction Stop
    $trader = $usersResponse.items | Where-Object { $_.role -eq "Trader" } | Select-Object -First 1
    if (-not $trader) {
        $trader = $usersResponse.items | Select-Object -First 1
    }
    Write-Host "Using trader: $($trader.fullName) (ID: $($trader.id))" -ForegroundColor Green
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Contract data array
$contractsList = @(
    @{ext = "IGR-2025-CAG-S0253-1_2"; prod = "GASOLINE"; qty = 44.250; price = 4000.200; start = "2025-11-08"; end = "2025-11-24"},
    @{ext = "IGR-2025-CAG-S0254-H_2"; prod = "GASOLINE"; qty = 31.800; price = 2556.720; start = "2025-11-09"; end = "2025-11-25"},
    @{ext = "IGR-2025-CAG-S0254-3_4"; prod = "GASOLINE"; qty = 56.500; price = 4542.600; start = "2025-11-13"; end = "2025-11-29"},
    @{ext = "IGR-2025-CAG-S0253-3_4"; prod = "GASOLINE"; qty = 54.500; price = 5830.800; start = "2025-11-15"; end = "2025-12-01"},
    @{ext = "IGR-2025-CAG-S0264"; prod = "GASOLINE"; qty = 44.000; price = 3537.600; start = "2025-11-26"; end = "2025-12-12"},
    @{ext = "IGR-2025-CAG-S0263"; prod = "DIESEL"; qty = 56.250; price = 5085.000; start = "2025-11-24"; end = "2025-12-10"},
    @{ext = "IGR-2025-CAG-S0266"; prod = "GASOLINE"; qty = 67.000; price = 5386.800; start = "2025-12-08"; end = "2025-12-24"},
    @{ext = "IGR-2025-CAG-S0267"; prod = "DIESEL"; qty = 103.500; price = 9356.400; start = "2025-12-09"; end = "2025-12-25"},
    @{ext = "IGR-2025-CAG-S0271"; prod = "GASOLINE"; qty = 27.500; price = 1917.000; start = "2025-12-12"; end = "2025-12-28"},
    @{ext = "IGR-2025-CAG-S0272"; prod = "GASOLINE"; qty = 37.500; price = 3202.500; start = "2025-12-09"; end = "2025-12-25"},
    @{ext = "IGR-2025-CAG-S0276"; prod = "GASOLINE"; qty = 100.000; price = 8030.000; start = "2026-01-14"; end = "2026-01-30"},
    @{ext = "IGR-2025-CAG-S0273"; prod = "GASOLINE"; qty = 103.000; price = 8281.200; start = "2025-12-10"; end = "2025-12-26"},
    @{ext = "IGR-2025-CAG-S0274"; prod = "GASOLINE"; qty = 150.000; price = 12045.000; start = "2026-01-14"; end = "2026-01-30"},
    @{ext = "IGR-2025-CAG-S0280"; prod = "GASOLINE"; qty = 30.000; price = 2409.000; start = "2025-12-12"; end = "2025-12-28"},
    @{ext = "IGR-2025-CAG-S0281"; prod = "DIESEL"; qty = 22.500; price = 1921.500; start = "2025-12-17"; end = "2026-01-02"},
    @{ext = "IGR-2025-CAG-S0282"; prod = "DIESEL"; qty = 36.000; price = 3074.400; start = "2025-12-23"; end = "2026-01-08"}
)

# Import contracts
Write-Host ""
Write-Host "Importing $($contractsList.Count) sales contracts..." -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$failureCount = 0

foreach ($contract in $contractsList) {
    try {
        $productId = if ($contract.prod -eq "GASOLINE") { $gasProduct.id } else { $dieselProduct.id }

        $contractNumber = "SC-" + (Get-Random -Minimum 100000 -Maximum 999999)

        $startDate = [DateTime]::ParseExact($contract.start, "yyyy-MM-dd", $null)
        $endDate = [DateTime]::ParseExact($contract.end, "yyyy-MM-dd", $null)

        $contractBody = @{
            externalContractNumber = $contract.ext
            contractType = "CARGO"
            customerId = $daxinId
            productId = $productId
            traderId = $trader.id
            quantity = $contract.qty
            quantityUnit = "BBL"
            tonBarrelRatio = 7.6
            pricingType = "FixedPrice"
            fixedPrice = $contract.price
            deliveryTerms = "DES"
            laycanStart = $startDate.ToString("yyyy-MM-ddTHH:mm:ss")
            laycanEnd = $endDate.ToString("yyyy-MM-ddTHH:mm:ss")
            loadPort = "Singapore"
            dischargePort = "Singapore"
            settlementType = "TT"
            creditPeriodDays = 30
            paymentTerms = "NET 30"
        } | ConvertTo-Json -Depth 3

        $newContract = Invoke-RestMethod -Uri "$ApiBaseUrl/sales-contracts" -Method Post -Body $contractBody -ContentType "application/json" -ErrorAction Stop

        Write-Host "[OK] $($contract.ext) - $($contract.qty) BBL - $($contract.prod) - Price: $($contract.price)" -ForegroundColor Green
        $successCount++

    } catch {
        Write-Host "[FAIL] $($contract.ext) - Error: $($_.Exception.Message)" -ForegroundColor Red
        $failureCount++
    }

    Start-Sleep -Milliseconds 50
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Import Complete" -ForegroundColor Cyan
Write-Host "Total: $($contractsList.Count), Success: $successCount, Failed: $failureCount" -ForegroundColor White
Write-Host "====================================" -ForegroundColor Cyan
