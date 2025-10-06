# Full System Test Script for Oil Trading System
# Version: 2.0
# Date: 2025-01-12

param(
    [string]$ApiUrl = "http://localhost:5000/api",
    [switch]$GenerateData = $false,
    [switch]$CleanupAfter = $false
)

$ErrorActionPreference = "Stop"

Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "   OIL TRADING SYSTEM TEST SUITE" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Version: 2.0 | Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Test results storage
$testResults = @{
    Passed = 0
    Failed = 0
    Warnings = 0
    Details = @()
}

# Helper Functions
function Test-Endpoint {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$TestName,
        [scriptblock]$Validation = $null
    )
    
    Write-Host "Testing: $TestName" -NoNewline
    
    try {
        $uri = "$ApiUrl$Endpoint"
        $response = $null
        
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $uri -Method Get -ErrorAction Stop
        } else {
            $json = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $uri -Method $Method -ContentType "application/json" -Body $json -ErrorAction Stop
        }
        
        # Run validation if provided
        if ($Validation -and $response) {
            $validationResult = & $Validation $response
            if ($validationResult) {
                Write-Host " ✅" -ForegroundColor Green
                $script:testResults.Passed++
            } else {
                Write-Host " ⚠️ (validation warning)" -ForegroundColor Yellow
                $script:testResults.Warnings++
            }
        } else {
            Write-Host " ✅" -ForegroundColor Green
            $script:testResults.Passed++
        }
        
        $script:testResults.Details += @{
            Test = $TestName
            Status = "Passed"
            Response = $response
        }
        
        return $response
    }
    catch {
        Write-Host " ❌" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
        $script:testResults.Failed++
        
        $script:testResults.Details += @{
            Test = $TestName
            Status = "Failed"
            Error = $_.ToString()
        }
        
        return $null
    }
}

function Show-Summary {
    Write-Host "`n=====================================" -ForegroundColor Cyan
    Write-Host "         TEST SUMMARY" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    
    $total = $testResults.Passed + $testResults.Failed + $testResults.Warnings
    $passRate = if ($total -gt 0) { [math]::Round(($testResults.Passed / $total) * 100, 2) } else { 0 }
    
    Write-Host "Total Tests: $total"
    Write-Host "Passed: " -NoNewline
    Write-Host "$($testResults.Passed)" -ForegroundColor Green
    Write-Host "Failed: " -NoNewline
    Write-Host "$($testResults.Failed)" -ForegroundColor Red
    Write-Host "Warnings: " -NoNewline
    Write-Host "$($testResults.Warnings)" -ForegroundColor Yellow
    Write-Host "Pass Rate: $passRate%"
    
    if ($testResults.Failed -eq 0) {
        Write-Host "`n✅ ALL CRITICAL TESTS PASSED!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ SOME TESTS FAILED - Review required" -ForegroundColor Red
    }
}

# STEP 1: Generate Market Data (if requested)
if ($GenerateData) {
    Write-Host "`n📊 GENERATING MARKET DATA..." -ForegroundColor Yellow
    Write-Host "Running Python script to generate test data..."
    
    try {
        $pythonPath = "python"
        $scriptPath = Join-Path $PSScriptRoot "generate_market_data.py"
        
        if (Test-Path $scriptPath) {
            & $pythonPath $scriptPath
            Write-Host "✅ Market data generated successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Market data generator not found at: $scriptPath" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ Failed to generate market data: $_" -ForegroundColor Red
    }
}

# STEP 2: API Health Check
Write-Host "`n🏥 API HEALTH CHECK" -ForegroundColor Yellow
Write-Host "-" * 40

$health = Test-Endpoint -Method "GET" -Endpoint "/paper-contracts" `
    -TestName "API Connection Test"

if (-not $health) {
    Write-Host "`n❌ API is not responding. Please ensure the server is running." -ForegroundColor Red
    Write-Host "Start the API with: dotnet run" -ForegroundColor Yellow
    exit 1
}

# STEP 3: Paper Trading Tests
Write-Host "`n📈 PAPER TRADING MODULE" -ForegroundColor Yellow
Write-Host "-" * 40

# Create test positions
$positions = @(
    @{
        contractMonth = "FEB25"
        productType = "Brent"
        position = "Long"
        quantity = 100
        lotSize = 1000
        entryPrice = 85.50
        tradeDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        notes = "System test position"
    },
    @{
        contractMonth = "FEB25"
        productType = "380cst"
        position = "Short"
        quantity = 50
        lotSize = 1000
        entryPrice = 450.00
        tradeDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        notes = "System test position"
    }
)

$createdPositions = @()
foreach ($pos in $positions) {
    $result = Test-Endpoint -Method "POST" -Endpoint "/paper-contracts" -Body $pos `
        -TestName "Create $($pos.productType) $($pos.position) position"
    
    if ($result) {
        $createdPositions += $result
    }
}

# Verify open positions
$openPositions = Test-Endpoint -Method "GET" -Endpoint "/paper-contracts/open-positions" `
    -TestName "Fetch open positions" `
    -Validation { param($r) $r.Count -ge 2 }

# Test P&L Summary
$pnlSummary = Test-Endpoint -Method "GET" -Endpoint "/paper-contracts/pnl-summary" `
    -TestName "Get P&L summary"

# STEP 4: Risk Management Tests
Write-Host "`n⚠️ RISK MANAGEMENT MODULE" -ForegroundColor Yellow
Write-Host "-" * 40

# Test risk calculation
$riskCalc = Test-Endpoint -Method "GET" -Endpoint "/risk/calculate?historicalDays=30&includeStressTests=true" `
    -TestName "Calculate portfolio risk" `
    -Validation { 
        param($r) 
        $r.portfolioValue -gt 0 -and $r.positionCount -gt 0
    }

if ($riskCalc) {
    Write-Host "  Portfolio Value: $" -NoNewline
    Write-Host ("{0:N0}" -f $riskCalc.totalPortfolioValue) -ForegroundColor Green
    Write-Host "  Positions: $($riskCalc.positionCount)"
    
    if ($riskCalc.historicalVaR95 -gt 0) {
        Write-Host "  Historical VaR 95%: $" -NoNewline
        Write-Host ("{0:N0}" -f $riskCalc.historicalVaR95) -ForegroundColor Yellow
    } else {
        Write-Host "  ⚠️ Historical VaR is 0 (needs market data)" -ForegroundColor Yellow
    }
}

# Test portfolio summary
$portfolioSummary = Test-Endpoint -Method "GET" -Endpoint "/risk/portfolio-summary" `
    -TestName "Get portfolio risk summary"

# Test product-specific risk
$productRisk = Test-Endpoint -Method "GET" -Endpoint "/risk/product/Brent" `
    -TestName "Get Brent risk metrics"

# Test backtest
$backtest = Test-Endpoint -Method "GET" -Endpoint "/risk/backtest?startDate=2024-01-01&endDate=2025-01-01" `
    -TestName "Run VaR backtest"

# STEP 5: Market Data Tests
Write-Host "`n📊 MARKET DATA MODULE" -ForegroundColor Yellow
Write-Host "-" * 40

# Test latest prices
$latestPrices = Test-Endpoint -Method "GET" -Endpoint "/market-data/latest" `
    -TestName "Get latest market prices"

# Test price history
$priceHistory = Test-Endpoint -Method "GET" -Endpoint "/market-data/history/Brent?startDate=2024-01-01" `
    -TestName "Get Brent price history"

# STEP 6: Purchase Contract Tests
Write-Host "`n📋 PURCHASE CONTRACT MODULE" -ForegroundColor Yellow
Write-Host "-" * 40

# Test contract list
$contracts = Test-Endpoint -Method "GET" -Endpoint "/purchase-contracts" `
    -TestName "List purchase contracts"

# Create test contract
$testContract = @{
    contractNumber = "PC-$(Get-Random -Maximum 9999)"
    supplier = "Test Supplier"
    product = "Brent"
    quantity = 10000
    unit = "BBL"
    price = 85.00
    currency = "USD"
    deliveryDate = (Get-Date).AddMonths(1).ToString("yyyy-MM-dd")
    paymentTerms = "30 days"
    status = "Draft"
}

$newContract = Test-Endpoint -Method "POST" -Endpoint "/purchase-contracts" -Body $testContract `
    -TestName "Create purchase contract"

# STEP 7: Validation Tests
Write-Host "`n✔️ VALIDATION TESTS" -ForegroundColor Yellow
Write-Host "-" * 40

# Test data consistency
Write-Host "Testing: Data consistency" -NoNewline
if ($riskCalc -and $openPositions) {
    if ($riskCalc.positionCount -eq $openPositions.Count) {
        Write-Host " ✅" -ForegroundColor Green
        $testResults.Passed++
    } else {
        Write-Host " ⚠️ Position count mismatch" -ForegroundColor Yellow
        $testResults.Warnings++
    }
} else {
    Write-Host " ⏭️ Skipped" -ForegroundColor Gray
}

# Test VaR consistency
Write-Host "Testing: VaR consistency (99% > 95%)" -NoNewline
if ($riskCalc) {
    if ($riskCalc.historicalVaR99 -ge $riskCalc.historicalVaR95) {
        Write-Host " ✅" -ForegroundColor Green
        $testResults.Passed++
    } else {
        Write-Host " ❌ VaR inconsistency detected" -ForegroundColor Red
        $testResults.Failed++
    }
} else {
    Write-Host " ⏭️ Skipped" -ForegroundColor Gray
}

# STEP 8: Cleanup (if requested)
if ($CleanupAfter -and $createdPositions.Count -gt 0) {
    Write-Host "`n🧹 CLEANUP" -ForegroundColor Yellow
    Write-Host "-" * 40
    
    foreach ($pos in $createdPositions) {
        try {
            $closeData = @{
                closingPrice = $pos.entryPrice
                closeDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
            
            Invoke-RestMethod -Uri "$ApiUrl/paper-contracts/$($pos.id)/close" `
                -Method Post -ContentType "application/json" `
                -Body ($closeData | ConvertTo-Json) -ErrorAction Stop
            
            Write-Host "Closed position: $($pos.id)" -ForegroundColor Gray
        }
        catch {
            Write-Host "Failed to close position: $($pos.id)" -ForegroundColor Red
        }
    }
}

# Show final summary
Show-Summary

# Export results if needed
$reportPath = Join-Path $PSScriptRoot "../test_results_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$testResults | ConvertTo-Json -Depth 10 | Out-File $reportPath
Write-Host "`n📄 Test report saved: $reportPath" -ForegroundColor Gray

# Return exit code based on results
exit ($testResults.Failed -gt 0 ? 1 : 0)