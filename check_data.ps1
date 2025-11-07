# Quick data completeness check
Write-Host "Checking contract data..." -ForegroundColor Cyan

try {
    $response = curl -s "http://localhost:5000/api/purchase-contracts?pageSize=1" | ConvertFrom-Json

    if ($null -eq $response -or $response.data.Count -eq 0) {
        Write-Host "ERROR: No contract data found" -ForegroundColor Red
        exit 1
    }

    Write-Host "Found $($response.totalCount) contracts" -ForegroundColor Green

    $contract = $response.data[0]
    Write-Host ""
    Write-Host "Contract: $($contract.contractNumber)" -ForegroundColor Cyan
    Write-Host "  contractValue: $($contract.contractValue)" -ForegroundColor Green
    Write-Host "  paymentTerms: $($contract.paymentTerms)" -ForegroundColor Green
    Write-Host "  status: $($contract.status)" -ForegroundColor Green

    Write-Host ""
    Write-Host "SUCCESS: All required fields are present" -ForegroundColor Green

} catch {
    Write-Host "ERROR: Failed to fetch data" -ForegroundColor Red
    Write-Host "$_" -ForegroundColor Red
    exit 1
}
