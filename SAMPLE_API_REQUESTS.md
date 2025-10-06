# Oil Trading API - Sample Requests for Testing

## 🧪 **Swagger UI Testing Guide**

### **Step 1: Open Swagger UI**
Navigate to: `https://localhost:5001/swagger`

### **Step 2: Test Health Check**
```
GET /health
```
Expected Response:
```json
{
  "status": "Healthy",
  "timestamp": "2024-11-08T10:30:00Z",
  "environment": "Development"
}
```

---

## 📋 **Purchase Contracts API Testing**

### **Test 1: Get Empty Contracts List**
```
GET /api/purchase-contracts
```

Expected Response:
```json
{
  "data": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### **Test 2: Create a Purchase Contract (Fixed Price)**
```
POST /api/purchase-contracts
Content-Type: application/json
```

Request Body (use seeded GUIDs):
```json
{
  "contractType": "CARGO",
  "supplierId": "12345678-1234-1234-1234-123456789012",
  "productId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
  "traderId": "22222222-2222-2222-2222-222222222222",
  "quantity": 25000,
  "quantityUnit": "MT",
  "tonBarrelRatio": 7.45,
  "pricingType": "Fixed",
  "fixedPrice": 450.00,
  "deliveryTerms": "FOB",
  "laycanStart": "2024-12-01T00:00:00Z",
  "laycanEnd": "2024-12-05T00:00:00Z",
  "loadPort": "Singapore",
  "dischargePort": "Houston",
  "settlementType": "LC",
  "creditPeriodDays": 30,
  "paymentTerms": "LC at Sight",
  "qualitySpecifications": "API Gravity: 30-35, Sulfur: Max 3.5%",
  "inspectionAgency": "SGS",
  "notes": "First test contract via API"
}
```

Expected Response:
```json
"c1111111-1111-1111-1111-111111111111"
```

### **Test 3: Create a Purchase Contract (Formula Price)**
```json
{
  "contractType": "CARGO",
  "supplierId": "23456789-2345-2345-2345-234567890123",
  "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "traderId": "33333333-3333-3333-3333-333333333333",
  "quantity": 50000,
  "quantityUnit": "MT",
  "tonBarrelRatio": 7.6,
  "pricingType": "IndexAverage",
  "pricingFormula": "AVG(MOPS FO 380) + 10 USD/MT",
  "pricingPeriodStart": "2024-11-15T00:00:00Z",
  "pricingPeriodEnd": "2024-11-25T00:00:00Z",
  "deliveryTerms": "CFR",
  "laycanStart": "2024-12-10T00:00:00Z",
  "laycanEnd": "2024-12-15T00:00:00Z",
  "loadPort": "Rotterdam",
  "dischargePort": "New York",
  "settlementType": "TT",
  "creditPeriodDays": 15,
  "paymentTerms": "TT 15 days after BL",
  "qualitySpecifications": "Marine Fuel Oil 380cSt",
  "inspectionAgency": "Bureau Veritas"
}
```

### **Test 4: Get Contract by ID**
```
GET /api/purchase-contracts/{id}
```
Use the ID returned from the POST request.

Expected Response: Full contract details with related entities.

### **Test 5: Update Contract**
```
PUT /api/purchase-contracts/{id}
```

Request Body:
```json
{
  "quantity": 30000,
  "loadPort": "Singapore Updated",
  "dischargePort": "Long Beach",
  "notes": "Updated via API"
}
```

Expected Response: `204 No Content`

### **Test 6: Activate Contract**
```
POST /api/purchase-contracts/{id}/activate
```

Expected Response: `204 No Content`

### **Test 7: Get Available Quantity**
```
GET /api/purchase-contracts/{id}/available-quantity
```

Expected Response:
```json
{
  "contractId": "c1111111-1111-1111-1111-111111111111",
  "totalQuantity": 30000,
  "allocatedQuantity": 0,
  "availableQuantity": 30000,
  "quantityUnit": "MT",
  "linkedSalesContractsCount": 0
}
```

---

## ❌ **Testing Validation Errors**

### **Test 8: Invalid Contract (Missing Required Fields)**
```json
{
  "contractType": "CARGO",
  "quantity": 0,
  "laycanStart": "2024-01-01T00:00:00Z",
  "laycanEnd": "2023-12-31T00:00:00Z"
}
```

Expected Response: `422 Unprocessable Entity`
```json
{
  "statusCode": 422,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred.",
  "errors": {
    "SupplierId": ["Supplier is required"],
    "Quantity": ["Quantity must be greater than 0"],
    "LaycanEnd": ["Laycan end must be after laycan start"]
  }
}
```

### **Test 9: Invalid Pricing Type**
```json
{
  "contractType": "INVALID_TYPE",
  "supplierId": "12345678-1234-1234-1234-123456789012",
  "productId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
  "traderId": "22222222-2222-2222-2222-222222222222",
  "quantity": 25000,
  "pricingType": "INVALID_PRICING"
}
```

Expected Response: `422 Unprocessable Entity` with validation errors.

---

## 📊 **Query Parameters Testing**

### **Test 10: Filtered and Sorted Contracts**
```
GET /api/purchase-contracts?page=1&pageSize=10&status=Active&sortBy=CreatedAt&sortDescending=true
```

### **Test 11: Date Range Filter**
```
GET /api/purchase-contracts?dateFrom=2024-01-01&dateTo=2024-12-31&supplierId=12345678-1234-1234-1234-123456789012
```

### **Test 12: Contract Number Search**
```
GET /api/purchase-contracts?contractNumber=ITGR-2024
```

---

## 🔧 **Troubleshooting Test Results**

### **If POST Returns 400 Bad Request:**
- Check that supplier, product, and trader IDs exist in database
- Verify all required fields are provided
- Check data types match expectations

### **If POST Returns 500 Internal Server Error:**
- Check database connection
- Look at application logs in `logs/` folder
- Verify migrations have been applied

### **If GET Returns Empty Results:**
- Check if database has been seeded
- Verify entity relationships are properly configured
- Check if filters are too restrictive

### **If 404 Not Found on Swagger:**
- Ensure you're in Development environment
- Try both HTTP and HTTPS URLs
- Check if API is actually running

---

## 📝 **Expected Seeded Data IDs**

Based on the migration scripts, these IDs should exist:

**Users:**
- Admin: `11111111-1111-1111-1111-111111111111`
- John Smith: `22222222-2222-2222-2222-222222222222`
- Mary Johnson: `33333333-3333-3333-3333-333333333333`

**Products:**
- Brent Crude: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`
- WTI Crude: `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb`
- Fuel Oil 380: `dddddddd-dddd-dddd-dddd-dddddddddddd`

**Trading Partners:**
- Shell: `12345678-1234-1234-1234-123456789012`
- BP: `23456789-2345-2345-2345-234567890123`
- ExxonMobil: `34567890-3456-3456-3456-345678901234`

Use these IDs in your test requests to ensure referential integrity.