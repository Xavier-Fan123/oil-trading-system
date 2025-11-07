try {
    Write-Host "Testing API on localhost:5000..." -ForegroundColor Cyan
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/products" -Method Get -UseBasicParsing -ErrorAction Stop
    Write-Host "API is responding successfully!" -ForegroundColor Green
    $data = ConvertFrom-Json $response.Content
    Write-Host "Total products: $($data.data.Count)" -ForegroundColor Green
    if ($data.data.Count -gt 0) {
        Write-Host "First 2 products:" -ForegroundColor Yellow
        $data.data | Select-Object -First 2 | ForEach-Object {
            Write-Host "  - $($_.code): $($_.name)"
        }
    }
}
catch {
    Write-Host "API Error: $_" -ForegroundColor Red
    exit 1
}
