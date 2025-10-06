# Risk Calculation System Testing Guide

## 🚀 Quick Start Testing

### Step 1: Start the API Server
```bash
cd src/OilTrading.Api
dotnet run
```
The API will start at `http://localhost:5000`

### Step 2: Install Python Dependencies (for risk engine)
```bash
pip install numpy pandas scipy arch
```

## 📋 Test Scenarios

### 1. Basic API Health Check
Open Swagger UI: `http://localhost:5000/swagger`

### 2. Create Test Data (Paper Trading Positions)

#### Create Long Position (Brent)
```bash
curl -X POST http://localhost:5000/api/paper-contracts \
  -H "Content-Type: application/json" \
  -d '{
    "contractMonth": "FEB25",
    "productType": "Brent",
    "position": "Long",
    "quantity": 100,
    "lotSize": 1000,
    "entryPrice": 85.50,
    "tradeDate": "2025-01-12T00:00:00Z",
    "tradeReference": "TEST-001",
    "notes": "Test long position"
  }'
```

#### Create Short Position (380cst)
```bash
curl -X POST http://localhost:5000/api/paper-contracts \
  -H "Content-Type: application/json" \
  -d '{
    "contractMonth": "FEB25",
    "productType": "380cst",
    "position": "Short",
    "quantity": 50,
    "lotSize": 1000,
    "entryPrice": 450.00,
    "tradeDate": "2025-01-12T00:00:00Z",
    "tradeReference": "TEST-002",
    "notes": "Test short position"
  }'
```

#### Create More Positions for Portfolio
```bash
# Marine Fuel 0.5% Long
curl -X POST http://localhost:5000/api/paper-contracts \
  -H "Content-Type: application/json" \
  -d '{
    "contractMonth": "MAR25",
    "productType": "0.5%",
    "position": "Long",
    "quantity": 75,
    "lotSize": 1000,
    "entryPrice": 520.00,
    "tradeDate": "2025-01-12T00:00:00Z"
  }'

# Gasoil Short
curl -X POST http://localhost:5000/api/paper-contracts \
  -H "Content-Type: application/json" \
  -d '{
    "contractMonth": "MAR25",
    "productType": "Gasoil",
    "position": "Short",
    "quantity": 25,
    "lotSize": 1000,
    "entryPrice": 680.00,
    "tradeDate": "2025-01-12T00:00:00Z"
  }'
```

### 3. Upload Market Data
First, create sample Excel files with historical prices, then upload:

```bash
# Upload daily prices
curl -X POST http://localhost:5000/api/market-data/upload \
  -F "file=@daily_prices.xlsx" \
  -F "fileType=DailyPrices"
```

### 4. Test Risk Calculation Endpoints

#### Calculate Portfolio Risk (Main Test)
```bash
curl -X GET "http://localhost:5000/api/risk/calculate?historicalDays=252&includeStressTests=true"
```

Expected Response:
```json
{
  "calculationDate": "2025-01-12T00:00:00",
  "totalPortfolioValue": 15250000,
  "positionCount": 4,
  "historicalVaR95": 125000,
  "historicalVaR99": 187500,
  "garchVaR95": 135000,
  "garchVaR99": 195000,
  "mcVaR95": 130000,
  "mcVaR99": 190000,
  "expectedShortfall95": 145000,
  "expectedShortfall99": 210000,
  "portfolioVolatility": 0.0245,
  "maxDrawdown": 0.0850,
  "stressTests": [
    {
      "scenario": "-10% Shock",
      "pnlImpact": -1525000,
      "percentageChange": -10.00,
      "description": "10% decline in all oil and fuel prices"
    },
    {
      "scenario": "+10% Shock",
      "pnlImpact": 1525000,
      "percentageChange": 10.00,
      "description": "10% increase in all oil and fuel prices"
    },
    {
      "scenario": "Historical Worst",
      "pnlImpact": -2287500,
      "percentageChange": -15.00,
      "description": "Repeat of historical worst daily oil price decline (15%)"
    }
  ],
  "productExposures": [
    {
      "productType": "Brent",
      "netExposure": 8550000,
      "grossExposure": 8550000,
      "longPositions": 1,
      "shortPositions": 0,
      "vaR95": 140625,
      "vaR99": 198900
    }
  ]
}
```

#### Get Portfolio Risk Summary
```bash
curl -X GET http://localhost:5000/api/risk/portfolio-summary
```

#### Get Product-Specific Risk
```bash
curl -X GET http://localhost:5000/api/risk/product/Brent
```

#### Run VaR Backtest
```bash
curl -X GET "http://localhost:5000/api/risk/backtest?startDate=2024-01-01&endDate=2025-01-01&lookbackDays=252"
```

## 🧪 Testing with PowerShell (Windows)

```powershell
# Create test position
$body = @{
    contractMonth = "FEB25"
    productType = "Brent"
    position = "Long"
    quantity = 100
    lotSize = 1000
    entryPrice = 85.50
    tradeDate = "2025-01-12T00:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/paper-contracts" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

# Calculate risk
Invoke-RestMethod -Uri "http://localhost:5000/api/risk/calculate" -Method Get | ConvertTo-Json -Depth 10
```

## 🔍 Test Validation Checklist

### ✅ Functional Tests
- [ ] API starts without errors
- [ ] Can create paper trading positions
- [ ] Risk calculation endpoint returns data
- [ ] All three VaR methods return values
- [ ] Stress tests return 5 scenarios
- [ ] VaR values are rounded to nearest USD
- [ ] Portfolio summary shows risk limits

### ✅ Data Validation
- [ ] Historical VaR < GARCH VaR < MC VaR (usually)
- [ ] VaR 99% > VaR 95% (always)
- [ ] Expected Shortfall > VaR (always)
- [ ] Stress test impacts are proportional to shock size
- [ ] Portfolio volatility is between 0 and 1

### ✅ Edge Cases
- [ ] System handles empty portfolio gracefully
- [ ] System handles single position
- [ ] System handles missing market data
- [ ] System handles extreme price movements

## 📊 Sample Test Data Generation

### Create Python Script for Test Data
```python
# generate_test_data.py
import pandas as pd
import numpy as np
from datetime import datetime, timedelta

# Generate sample price data
np.random.seed(42)
dates = pd.date_range(end=datetime.now(), periods=365)

products = ['Brent', '380cst', '0.5%', 'Gasoil']
base_prices = [85, 450, 520, 680]

data = []
for product, base_price in zip(products, base_prices):
    returns = np.random.normal(0, 0.02, len(dates))  # 2% daily volatility
    prices = base_price * np.exp(np.cumsum(returns))
    
    for date, price in zip(dates, prices):
        data.append({
            'Date': date,
            'Product': product,
            'Price': round(price, 2)
        })

df = pd.DataFrame(data)
df.to_excel('test_market_data.xlsx', index=False)
print("Test data generated: test_market_data.xlsx")
```

## 🐛 Troubleshooting

### Common Issues and Solutions

#### 1. Python Script Not Found
**Error**: "Python script not found at path"
**Solution**: 
```bash
# Ensure the script exists
ls src/OilTrading.Api/Scripts/risk_engine.py

# Test Python script directly
echo '{"method":"garch","positions":[],"returns":{},"seed":42}' | python src/OilTrading.Api/Scripts/risk_engine.py
```

#### 2. No Open Positions
**Error**: "No open positions found"
**Solution**: Create test positions using the curl commands above

#### 3. Missing Market Data
**Error**: "No historical prices available"
**Solution**: Upload market data or use in-memory test data

#### 4. GARCH Library Not Installed
**Warning**: "arch library not available"
**Solution**: 
```bash
pip install arch
```

## 🎯 Performance Testing

### Load Test with Multiple Positions
```bash
# Create 100 positions
for i in {1..100}
do
  curl -X POST http://localhost:5000/api/paper-contracts \
    -H "Content-Type: application/json" \
    -d "{
      \"contractMonth\": \"FEB25\",
      \"productType\": \"Brent\",
      \"position\": \"Long\",
      \"quantity\": $((RANDOM % 100 + 1)),
      \"lotSize\": 1000,
      \"entryPrice\": $((RANDOM % 20 + 80)).00,
      \"tradeDate\": \"2025-01-12T00:00:00Z\"
    }"
done

# Measure response time
time curl -X GET "http://localhost:5000/api/risk/calculate"
```

### Expected Performance Metrics
- Historical VaR: < 100ms
- GARCH VaR: < 500ms
- Monte Carlo VaR (100k simulations): < 2000ms
- Full risk calculation: < 3000ms

## 📈 Monitoring Risk Metrics

### Create Dashboard HTML
```html
<!DOCTYPE html>
<html>
<head>
    <title>Risk Dashboard</title>
    <script src="https://cdn.jsdelivr.net/npm/axios/dist/axios.min.js"></script>
</head>
<body>
    <h1>Portfolio Risk Dashboard</h1>
    <button onclick="calculateRisk()">Calculate Risk</button>
    <div id="results"></div>
    
    <script>
        async function calculateRisk() {
            try {
                const response = await axios.get('http://localhost:5000/api/risk/calculate');
                const data = response.data;
                
                document.getElementById('results').innerHTML = `
                    <h2>VaR Metrics</h2>
                    <p>Historical VaR 95%: $${data.historicalVaR95.toLocaleString()}</p>
                    <p>GARCH VaR 95%: $${data.garchVaR95.toLocaleString()}</p>
                    <p>Monte Carlo VaR 95%: $${data.mcVaR95.toLocaleString()}</p>
                    
                    <h2>Stress Tests</h2>
                    ${data.stressTests.map(s => 
                        `<p>${s.scenario}: $${s.pnlImpact.toLocaleString()}</p>`
                    ).join('')}
                `;
            } catch (error) {
                console.error('Error:', error);
                alert('Failed to calculate risk');
            }
        }
    </script>
</body>
</html>
```

## 🔐 Security Testing

### Test Authorization (when implemented)
```bash
# Test without auth token (should fail when auth is enabled)
curl -X GET http://localhost:5000/api/risk/calculate

# Test with invalid token (should fail)
curl -X GET http://localhost:5000/api/risk/calculate \
  -H "Authorization: Bearer invalid_token"
```

## 📝 Test Report Template

```markdown
# Risk System Test Report
Date: [DATE]
Tester: [NAME]

## Test Environment
- .NET Version: 9.0
- Python Version: [VERSION]
- OS: Windows 11

## Test Results
| Test Case | Status | Notes |
|-----------|--------|-------|
| API Startup | ✅ Pass | |
| Create Positions | ✅ Pass | |
| Calculate Risk | ✅ Pass | |
| Stress Tests | ✅ Pass | |
| Performance | ✅ Pass | < 3s |

## Issues Found
1. [Issue description]
2. [Issue description]

## Recommendations
1. [Recommendation]
2. [Recommendation]
```

## 🎉 Success Criteria

Your risk system is working correctly if:
1. ✅ All API endpoints return 200 OK
2. ✅ VaR values are consistent (99% > 95%)
3. ✅ Stress tests show reasonable P&L impacts
4. ✅ Response times are under 3 seconds
5. ✅ Results are reproducible (same seed = same results)

---
**Last Updated**: January 2025
**Support**: Check CLAUDE.md for system documentation