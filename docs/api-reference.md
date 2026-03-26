# Complete API Reference - All 59+ Endpoints

**Version**: 2.0 Enterprise Grade
**Last Updated**: November 2025
**Scope**: Purchase Contracts, Sales Contracts, Settlements, and All Supporting Services
**Base URL**: `https://api.yourdomain.com/api` or `http://localhost:5000/api`

---

## Table of Contents

1. [API Conventions](#api-conventions)
2. [Authentication](#authentication)
3. [Purchase Contracts API](#purchase-contracts-api)
4. [Sales Contracts API](#sales-contracts-api)
5. [Settlement APIs](#settlement-apis)
6. [Dashboard API](#dashboard-api)
7. [Risk Management API](#risk-management-api)
8. [Shipping Operations API](#shipping-operations-api)
9. [Supporting APIs](#supporting-apis)
10. [Error Handling](#error-handling)

---

## API Conventions

### Request Format

All requests use JSON with UTF-8 encoding:

```
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>
```

### Response Format

All successful responses follow standard format:

```json
{
  "data": { /* response body */ },
  "timestamp": "2025-11-10T14:30:00Z",
  "traceId": "0HN1GJ7F3VG4K:00000001"
}
```

### Pagination

List endpoints support pagination:

```
GET /api/purchase-contracts?pageNum=1&pageSize=50&sortBy=CreatedAt&sortDescending=true
```

Response includes:
```json
{
  "data": [
    { /* contract 1 */ },
    { /* contract 2 */ }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalCount": 1500,
  "totalPages": 30
}
```

### Error Response Format

```json
{
  "error": "Contract not found",
  "details": "No purchase contract with ID '550e8400-e29b-41d4-a716-446655440000' exists",
  "traceId": "0HN1GJ7F3VG4K:00000001",
  "validationErrors": [
    {
      "field": "quantity",
      "message": "Quantity must be greater than 0"
    }
  ]
}
```

---

## Authentication

### Obtain JWT Token

**Endpoint**: `POST /api/identity/login`

**Request**:
```json
{
  "email": "trader@company.com",
  "password": "secure-password"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "email": "trader@company.com",
    "fullName": "John Trader",
    "role": "Trader"
  },
  "timestamp": "2025-11-10T14:30:00Z"
}
```

**Token Expiration**: 60 minutes
**Refresh Token**: Available for extending session

### Using Token in Requests

```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
     https://api.yourdomain.com/api/purchase-contracts
```

---

## Purchase Contracts API

### 1. Create Purchase Contract

**Endpoint**: `POST /api/purchase-contracts`

**Authentication**: Required (Trader, SeniorTrader, TradingManager)

**Request**:
```json
{
  "contractNumber": "PC-2025-001",
  "externalContractNumber": "SUPPLIER-INV-2025-001",
  "tradingPartnerId": "550e8400-e29b-41d4-a716-446655440002",
  "productId": "550e8400-e29b-41d4-a716-446655440003",
  "quantity": 1000,
  "quantityUnit": "MT",
  "contractValue": 85500.00,
  "pricingType": "Fixed",
  "fixedPrice": 85.50,
  "deliveryTerms": "FOB",
  "laycanStart": "2025-12-01T00:00:00Z",
  "laycanEnd": "2025-12-15T00:00:00Z",
  "loadPort": "Ras Tanura",
  "dischargePort": "Singapore",
  "settlementType": "TT",
  "creditPeriodDays": 30,
  "paymentTerms": "TT 30 days after B/L"
}
```

**Response** (201 Created):
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "contractNumber": "PC-2025-001",
    "externalContractNumber": "SUPPLIER-INV-2025-001",
    "status": "Draft",
    "createdAt": "2025-11-10T14:30:00Z",
    "createdBy": "550e8400-e29b-41d4-a716-446655440001"
  },
  "timestamp": "2025-11-10T14:30:00Z"
}
```

**Error Cases**:
- 400 Bad Request: Missing required fields
- 401 Unauthorized: Invalid or expired token
- 403 Forbidden: Insufficient permissions
- 409 Conflict: Contract number already exists

---

### 2. Get Purchase Contracts (List)

**Endpoint**: `GET /api/purchase-contracts`

**Query Parameters**:
```
pageNum=1                    (default: 1)
pageSize=50                  (default: 50, max: 1000)
sortBy=CreatedAt             (CreatedAt, ContractNumber, Status)
sortDescending=false         (true or false)
status=Active                (Draft, PendingApproval, Active, Completed)
tradingPartnerId=<guid>      (filter by supplier)
productId=<guid>             (filter by product)
```

**Example Request**:
```bash
GET /api/purchase-contracts?pageNum=1&pageSize=10&status=Active&sortBy=CreatedAt&sortDescending=true
```

**Response** (200 OK):
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440010",
      "contractNumber": "PC-2025-001",
      "status": "Active",
      "tradingPartner": {
        "id": "550e8400-e29b-41d4-a716-446655440002",
        "name": "UNION INTERNATIONAL TRADING PTE LTD"
      },
      "product": {
        "id": "550e8400-e29b-41d4-a716-446655440003",
        "code": "BRENT",
        "name": "Brent Crude"
      },
      "quantity": 1000,
      "quantityUnit": "MT",
      "fixedPrice": 85.50,
      "contractValue": 85500.00,
      "laycanStart": "2025-12-01T00:00:00Z",
      "laycanEnd": "2025-12-15T00:00:00Z",
      "createdAt": "2025-11-10T14:30:00Z",
      "activatedAt": "2025-11-10T15:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 125,
  "totalPages": 13,
  "timestamp": "2025-11-10T14:30:00Z"
}
```

---

### 3. Get Purchase Contract Details

**Endpoint**: `GET /api/purchase-contracts/{id}`

**Path Parameters**:
- `id`: Contract GUID (e.g., `550e8400-e29b-41d4-a716-446655440010`)

**Response** (200 OK):
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "contractNumber": "PC-2025-001",
    "externalContractNumber": "SUPPLIER-INV-2025-001",
    "status": "Active",
    "createdAt": "2025-11-10T14:30:00Z",
    "createdBy": "550e8400-e29b-41d4-a716-446655440001",
    "activatedAt": "2025-11-10T15:00:00Z",
    "activatedBy": "550e8400-e29b-41d4-a716-446655440001",
    "completedAt": null,
    "tradingPartner": {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "name": "UNION INTERNATIONAL TRADING PTE LTD",
      "country": "Singapore"
    },
    "product": {
      "id": "550e8400-e29b-41d4-a716-446655440003",
      "code": "BRENT",
      "name": "Brent Crude",
      "defaultUnit": "MT"
    },
    "quantity": 1000,
    "quantityUnit": "MT",
    "contractValue": 85500.00,
    "pricing": {
      "pricingType": "Fixed",
      "benchmarkPrice": 85.50,
      "benchmarkUnit": "USD/MT",
      "adjustmentPrice": 0.00,
      "adjustmentUnit": "USD/BBL"
    },
    "deliveryTerms": "FOB",
    "laycanStart": "2025-12-01T00:00:00Z",
    "laycanEnd": "2025-12-15T00:00:00Z",
    "loadPort": "Ras Tanura",
    "dischargePort": "Singapore",
    "settlementType": "TT",
    "creditPeriodDays": 30,
    "paymentTerms": "TT 30 days after B/L",
    "shippingOperations": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440020",
        "status": "Completed",
        "quantity": 1000,
        "shippedQuantity": 1000
      }
    ],
    "settlements": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440030",
        "status": "Finalized",
        "settlementAmount": 85500.00,
        "currency": "USD"
      }
    ]
  },
  "timestamp": "2025-11-10T14:30:00Z"
}
```

**Error Cases**:
- 404 Not Found: Contract with given ID doesn't exist

---

### 4. Update Purchase Contract

**Endpoint**: `PUT /api/purchase-contracts/{id}`

**Request** (partial update):
```json
{
  "quantity": 1050,
  "fixedPrice": 85.75,
  "paymentTerms": "TT 45 days after B/L"
}
```

**Response** (200 OK): Updated contract details

**Validation Rules**:
- Can only update Draft status contracts
- Cannot update after activation
- Quantity and price must be positive

---

### 5. Activate Purchase Contract

**Endpoint**: `POST /api/purchase-contracts/{id}/activate`

**Request Body**: (empty or omitted)

**Response** (200 OK):
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "status": "Active",
    "activatedAt": "2025-11-10T15:00:00Z",
    "activatedBy": "550e8400-e29b-41d4-a716-446655440001"
  },
  "timestamp": "2025-11-10T15:00:00Z"
}
```

**Business Rules**:
- Requires all mandatory fields populated
- Pricing formula must be defined
- Contract value must be calculated
- Only Draft contracts can be activated

---

## Sales Contracts API

### 1. Create Sales Contract

**Endpoint**: `POST /api/sales-contracts`

**Authentication**: Required (Trader, SeniorTrader, TradingManager)

**Request**:
```json
{
  "contractNumber": "SC-2025-001",
  "externalContractNumber": "CUSTOMER-ORDER-2025-001",
  "tradingPartnerId": "550e8400-e29b-41d4-a716-446655440040",
  "productId": "550e8400-e29b-41d4-a716-446655440003",
  "quantity": 1000,
  "quantityUnit": "MT",
  "contractValue": 86500.00,
  "pricingType": "Fixed",
  "fixedPrice": 86.50,
  "deliveryTerms": "FOB",
  "laycanStart": "2025-12-01T00:00:00Z",
  "laycanEnd": "2025-12-15T00:00:00Z",
  "dischargePort": "Singapore",
  "settlementType": "TT",
  "creditPeriodDays": 30,
  "prepaymentPercentage": 0,
  "paymentTerms": "TT 30 days after B/L"
}
```

**Response** (201 Created): Same structure as purchase contract

---

### 2. Approve Sales Contract

**Endpoint**: `POST /api/sales-contracts/{id}/approve`

**Authentication**: Required (Manager, SeniorTrader)

**Request Body**:
```json
{
  "approvalComments": "Approved - terms acceptable"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440050",
    "status": "Active",
    "approvalStatus": "Approved",
    "approvedAt": "2025-11-10T15:30:00Z",
    "approvedBy": "550e8400-e29b-41d4-a716-446655440001"
  },
  "timestamp": "2025-11-10T15:30:00Z"
}
```

---

### 3. Reject Sales Contract

**Endpoint**: `POST /api/sales-contracts/{id}/reject`

**Request Body**:
```json
{
  "rejectionReason": "Terms not acceptable to customer"
}
```

**Response** (200 OK): Contract status changes to "Rejected"

---

### 4. Other Sales Contract Endpoints

```
GET    /api/sales-contracts                    (List)
GET    /api/sales-contracts/{id}               (Detail)
PUT    /api/sales-contracts/{id}               (Update)
POST   /api/sales-contracts/{id}/activate      (Activate after approval)
DELETE /api/sales-contracts/{id}               (Delete - Draft only)
POST   /api/sales-contracts/link               (Link to purchase contract)
POST   /api/sales-contracts/unlink             (Unlink from purchase contract)
```

---

## Settlement APIs

### Purchase Settlement API (Type-Safe AP)

#### Create Purchase Settlement

**Endpoint**: `POST /api/purchase-settlements`

**Request**:
```json
{
  "supplierContractId": "550e8400-e29b-41d4-a716-446655440010",
  "settlementDate": "2025-12-20T00:00:00Z",
  "settlementAmount": {
    "amount": 85500.00,
    "currency": "USD"
  },
  "charges": [
    {
      "chargeType": "Demurrage",
      "amount": 1500.00,
      "description": "3 days demurrage @ 500/day"
    }
  ]
}
```

**Response** (201 Created):
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440060",
    "supplierContractId": "550e8400-e29b-41d4-a716-446655440010",
    "status": "Draft",
    "settlementAmount": {
      "amount": 85500.00,
      "currency": "USD"
    },
    "createdAt": "2025-11-10T14:30:00Z"
  }
}
```

#### Get Purchase Settlement by External Contract Number

**Endpoint**: `GET /api/purchase-settlements/by-external-contract/{externalNumber}`

**Purpose**: Retrieve settlement using supplier's contract number (no UUID needed)

**Example**:
```bash
GET /api/purchase-settlements/by-external-contract/SUPPLIER-INV-2025-001
```

**Response** (200 OK): Settlement details

**Error Cases**:
- 404 Not Found: No matching external contract

---

#### Calculate Settlement Amount

**Endpoint**: `POST /api/purchase-settlements/{id}/calculate`

**Request** (empty):
```json
{}
```

**Response** (200 OK):
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440060",
    "status": "Calculated",
    "settlementAmount": 87000.00,
    "charges": [
      { "type": "Demurrage", "amount": 1500.00 }
    ],
    "totalAmount": 87000.00
  }
}
```

---

#### Approve & Finalize Settlement

**Endpoint**: `POST /api/purchase-settlements/{id}/approve`
**Endpoint**: `POST /api/purchase-settlements/{id}/finalize`

**Responses**: Similar structure, status changes accordingly

---

#### Get Outstanding Supplier Payments (AP Aging)

**Endpoint**: `GET /api/purchase-settlements/pending-payments`

**Query Parameters**:
```
supplierId=<guid>        (filter by supplier)
overdueDays=30           (only overdue by N days)
status=Unpaid            (Unpaid, Partial, Paid)
```

**Response** (200 OK):
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440060",
      "supplierName": "UNION INTERNATIONAL TRADING PTE LTD",
      "contractNumber": "PC-2025-001",
      "dueDate": "2025-12-20T00:00:00Z",
      "settlementAmount": 87000.00,
      "currency": "USD",
      "daysPastDue": 5,
      "status": "Unpaid"
    }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalCount": 42,
  "totalPages": 1
}
```

---

### Sales Settlement API (Type-Safe AR)

Similar structure to Purchase Settlement, but:
- Uses `customerContractId` instead of `supplierContractId`
- AR-specific endpoints for collection management
- Outstanding receivables tracking

**Key Endpoints**:
```
POST   /api/sales-settlements                  (Create)
GET    /api/sales-settlements/{id}             (Detail)
POST   /api/sales-settlements/{id}/calculate   (Calculate)
POST   /api/sales-settlements/{id}/approve     (Approve)
POST   /api/sales-settlements/{id}/finalize    (Finalize)
GET    /api/sales-settlements/outstanding-receivables  (AR aging)
GET    /api/sales-settlements/credit-risk              (Credit exposure)
POST   /api/sales-settlements/{id}/send-reminder       (Collection)
```

---

### Generic Settlement API (Legacy)

For backward compatibility, generic settlement endpoint:

```
POST   /api/settlements                        (Create)
GET    /api/settlements/{id}                   (Detail)
GET    /api/settlements                        (List)
PUT    /api/settlements/{id}                   (Update)
POST   /api/settlements/{id}/calculate         (Calculate)
POST   /api/settlements/{id}/approve           (Approve)
POST   /api/settlements/{id}/finalize          (Finalize)
POST   /api/settlements/bulk-approve           (Batch approve)
POST   /api/settlements/bulk-finalize          (Batch finalize)
POST   /api/settlements/bulk-export            (Export to Excel)
```

---

## Dashboard API

### Overall Dashboard

**Endpoint**: `GET /api/dashboard`

**Query Parameters**:
```
period=Last30Days         (Last7Days, Last30Days, Last90Days, YearToDate)
tradingPartnerId=<guid>   (optional filter)
productId=<guid>          (optional filter)
```

**Response** (200 OK):
```json
{
  "data": {
    "summary": {
      "activeContracts": 45,
      "totalNotionalValue": 3850000.00,
      "currency": "USD",
      "activePurchases": 25,
      "activeSales": 20,
      "netPosition": 500,
      "netPositionValue": 42500.00
    },
    "metrics": {
      "totalContractsCreated": 120,
      "contractsActivated": 85,
      "contractsCompleted": 40,
      "settlementsPending": 12,
      "settlementsFinalized": 28
    },
    "topPartners": [
      {
        "partnerId": "550e8400-e29b-41d4-a716-446655440002",
        "partnerName": "UNION INTERNATIONAL TRADING PTE LTD",
        "contractCount": 15,
        "totalVolume": 5000.00,
        "totalValue": 420000.00
      }
    ],
    "productBreakdown": [
      {
        "productId": "550e8400-e29b-41d4-a716-446655440003",
        "productName": "Brent Crude",
        "contractCount": 30,
        "totalQuantity": 15000.00,
        "quantityUnit": "MT"
      }
    ]
  },
  "timestamp": "2025-11-10T14:30:00Z"
}
```

**Cache**: 5 minutes (Redis)

---

## Risk Management API

### Calculate Risk (VaR)

**Endpoint**: `POST /api/risk/calculate`

**Request**:
```json
{
  "confidenceLevel": 0.95,
  "method": "HistoricalSimulation",
  "periods": 250
}
```

**Response** (200 OK):
```json
{
  "data": {
    "portfolioVaR": 45000.00,
    "currency": "USD",
    "confidenceLevel": 0.95,
    "method": "HistoricalSimulation",
    "calculatedAt": "2025-11-10T14:30:00Z",
    "componentBreakdown": [
      {
        "product": "Brent",
        "individualVaR": 32000.00,
        "weight": 0.65
      },
      {
        "product": "WTI",
        "individualVaR": 18000.00,
        "weight": 0.35
      }
    ]
  }
}
```

---

### Get Portfolio Risk Summary

**Endpoint**: `GET /api/risk/portfolio-summary`

**Response** (200 OK):
```json
{
  "data": {
    "portfolioValue": 3850000.00,
    "currency": "USD",
    "vaR95": 45000.00,
    "vaR99": 72000.00,
    "concentrationRisk": {
      "topCustomer": {
        "name": "ABC Refinery",
        "exposure": 850000.00,
        "percentageOfPortfolio": 22.1
      },
      "topProduct": {
        "name": "Brent Crude",
        "exposure": 2150000.00,
        "percentageOfPortfolio": 55.8
      }
    },
    "limitStatus": {
      "counterpartyLimit": "85% of limit",
      "concentrationLimit": "92% of limit",
      "productLimit": "78% of limit"
    }
  }
}
```

---

## Shipping Operations API

### Create Shipping Operation

**Endpoint**: `POST /api/shipping-operations`

**Request**:
```json
{
  "contractId": "550e8400-e29b-41d4-a716-446655440010",
  "contractType": "Purchase",
  "vesselName": "M/T Seabird",
  "chaptererName": "Global Shipping Inc",
  "quantity": 1000,
  "deliverySchedule": {
    "start": "2025-12-01T00:00:00Z",
    "end": "2025-12-15T00:00:00Z"
  },
  "loadPort": "Ras Tanura",
  "dischargePort": "Singapore",
  "shippingAgent": "Global Logistics"
}
```

**Response** (201 Created): Shipping operation details

---

### Record Shipping Events

**Endpoint**: `POST /api/shipping-operations/{id}/start-loading`

```json
{
  "startDate": "2025-12-01T08:00:00Z",
  "plannedQuantity": 1000
}
```

**Other Event Endpoints**:
```
POST   /api/shipping-operations/{id}/complete-loading
POST   /api/shipping-operations/{id}/complete-discharge
POST   /api/shipping-operations/{id}/record-lifting
POST   /api/shipping-operations/{id}/cancel
```

---

## Supporting APIs

### Products API

```
GET    /api/products                           (List all products)
GET    /api/products/{id}                      (Product detail)
POST   /api/products                           (Create product)
PUT    /api/products/{id}                      (Update product)
```

**Response Example**:
```json
{
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440003",
    "code": "BRENT",
    "name": "Brent Crude",
    "description": "Light Sweet Crude, 35 API",
    "defaultUnit": "MT",
    "category": "Crude Oil"
  }
}
```

---

### Trading Partners API

```
GET    /api/trading-partners                   (List)
GET    /api/trading-partners/{id}              (Detail)
POST   /api/trading-partners                   (Create)
PUT    /api/trading-partners/{id}              (Update)
POST   /api/trading-partners/{id}/block        (Block partner)
POST   /api/trading-partners/{id}/unblock      (Unblock partner)
```

---

### Users API

```
GET    /api/users                              (List users)
GET    /api/users/{id}                         (User detail)
POST   /api/users                              (Create user)
PUT    /api/users/{id}                         (Update user)
POST   /api/users/{id}/change-password         (Change password)
```

---

### Inventory API

```
GET    /api/inventory/locations                (List locations)
GET    /api/inventory/positions                (Current inventory)
POST   /api/inventory/positions/transfer       (Transfer inventory)
POST   /api/inventory/positions/receive        (Receive shipment)
POST   /api/inventory/positions/dispatch       (Dispatch shipment)
GET    /api/inventory/availability/{locationId}/{productId}  (Check availability)
POST   /api/inventory/reservations             (Create reservation)
DELETE /api/inventory/reservations/{id}        (Release reservation)
```

---

### Trade Groups API

```
POST   /api/trade-groups                       (Create strategy)
GET    /api/trade-groups/{id}                  (Strategy detail)
POST   /api/trade-groups/{id}/add-leg          (Add contract to strategy)
DELETE /api/trade-groups/{id}/legs/{legId}     (Remove from strategy)
POST   /api/trade-groups/{id}/close            (Close strategy)
GET    /api/trade-groups/{id}/risk             (Aggregate risk)
GET    /api/trade-groups/monitoring            (All active strategies)
```

---

### Paper Contracts API

```
POST   /api/paper-contracts                    (Create futures position)
GET    /api/paper-contracts/{id}               (Position detail)
GET    /api/paper-contracts/open-positions     (All open positions)
POST   /api/paper-contracts/{id}/close         (Close position)
GET    /api/paper-contracts/{id}/pnl           (P&L calculation)
POST   /api/paper-contracts/update-marks       (Update mark-to-market)
```

---

### Reports API

```
GET    /api/contract-execution-reports/{contractId}  (Single report)
GET    /api/contract-execution-reports                (Report list)
POST   /api/contract-execution-reports/generate       (Generate batch)
GET    /api/contract-execution-reports/analytics      (Aggregate metrics)
GET    /api/contract-execution-reports/export         (Export to file)

GET    /api/settlement-analytics/analytics           (Analytics dashboard)
GET    /api/settlement-analytics/metrics             (KPI metrics)
GET    /api/settlement-analytics/daily-trends        (Trend data)
GET    /api/settlement-analytics/currency-breakdown  (Currency distribution)
GET    /api/settlement-analytics/status-distribution (Status distribution)

POST   /api/reports                           (Create custom report)
POST   /api/reports/{id}/execute              (Execute report)
POST   /api/reports/{id}/schedule             (Schedule report)
POST   /api/reports/{id}/distribute           (Distribute report)
```

---

### Contract Matching API

```
POST   /api/contract-matching                  (Create matching)
GET    /api/contract-matching/{id}             (Matching detail)
GET    /api/contract-matching                  (List matchings)
POST   /api/contract-matching/{id}/close       (Close matching)
GET    /api/contract-matching/net-position     (Calculate net position)
GET    /api/contracts/available-purchases      (Unmatched purchases)
GET    /api/contracts/unmatched-sales          (Unmatched sales)
```

---

### External Contract Resolution API

**Purpose**: Retrieve settlements/operations using supplier/customer contract numbers

```
GET    /api/contracts/resolve?externalNumber=<number>&type=<Purchase|Sales>
POST   /api/settlements/create-by-external-contract
POST   /api/shipping-operations/create-by-external-contract
```

**Benefit**: No need to know internal GUIDs - client's contract numbers work

---

## Error Handling

### HTTP Status Codes

| Code | Meaning | Typical Response |
|------|---------|------------------|
| **200** | OK | Request succeeded |
| **201** | Created | Resource created successfully |
| **204** | No Content | Request succeeded, no response body |
| **400** | Bad Request | Invalid input, validation failed |
| **401** | Unauthorized | Missing or invalid authentication |
| **403** | Forbidden | Insufficient permissions |
| **404** | Not Found | Resource doesn't exist |
| **409** | Conflict | Business rule violation (e.g., duplicate) |
| **429** | Too Many Requests | Rate limit exceeded |
| **500** | Internal Server Error | Unexpected server error |
| **503** | Service Unavailable | Database/Redis unavailable |

---

### Common Error Scenarios

**1. Validation Error (400)**
```json
{
  "error": "Validation failed",
  "validationErrors": [
    {
      "field": "quantity",
      "message": "Quantity must be greater than 0"
    },
    {
      "field": "fixedPrice",
      "message": "Price must be positive"
    }
  ]
}
```

**2. Unauthorized (401)**
```json
{
  "error": "Invalid or expired token",
  "details": "JWT token expired at 2025-11-10T15:30:00Z"
}
```

**3. Business Rule Violation (409)**
```json
{
  "error": "Contract cannot be activated",
  "details": "All required fields must be populated before activation. Missing: PaymentTerms",
  "code": "ACTIVATION_FAILED"
}
```

**4. Rate Limit (429)**
```json
{
  "error": "Rate limit exceeded",
  "details": "Maximum 10 requests per minute for /api/contracts/login",
  "retryAfter": 15
}
```

---

### Retry Strategy

**Idempotent Operations** (safe to retry):
- GET requests
- POST to idempotent endpoints with idempotency key

**Non-Idempotent Operations** (use caution):
- POST to create endpoints
- PUT to update endpoints
- DELETE endpoints

**Recommended Backoff**:
```
Attempt 1: Immediate
Attempt 2: Wait 1 second
Attempt 3: Wait 2 seconds (exponential backoff)
Attempt 4: Wait 4 seconds
Attempt 5: Wait 8 seconds
Max retries: 5
```

---

## Rate Limiting

All API endpoints are rate-limited:

| Endpoint Category | Limit | Period |
|------------------|-------|--------|
| **Login** | 10 | 1 minute |
| **Contracts** | 100 | 1 minute |
| **Settlements** | 100 | 1 minute |
| **Reports** | 50 | 1 minute |
| **Global** | 1,000 | 1 minute (per user) |

**Response Headers**:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 2025-11-10T14:31:00Z
```

---

## Webhook Endpoints (Optional)

For async event notifications:

```
POST /webhooks/contract-activated
POST /webhooks/settlement-finalized
POST /webhooks/shipping-completed
POST /webhooks/payment-received
```

**Webhook Format**:
```json
{
  "event": "contract.activated",
  "timestamp": "2025-11-10T14:30:00Z",
  "data": {
    "contractId": "550e8400-e29b-41d4-a716-446655440010",
    "contractNumber": "PC-2025-001",
    "status": "Active"
  }
}
```

---

## Summary

This API reference documents **59+ endpoints** across:

- **Purchase Contracts** (6 endpoints)
- **Sales Contracts** (8 endpoints)
- **Settlements** (15+ endpoints)
- **Dashboard** (4 endpoints)
- **Risk Management** (5+ endpoints)
- **Shipping** (8 endpoints)
- **Supporting Services** (8+ endpoints)
- **Special Features** (5+ endpoints)

**Total**: 59+ production endpoints with complete documentation

For system architecture, see [ARCHITECTURE_BLUEPRINT.md](./ARCHITECTURE_BLUEPRINT.md)
For deployment, see [PRODUCTION_DEPLOYMENT_GUIDE.md](./PRODUCTION_DEPLOYMENT_GUIDE.md)

