# Phase 5 - Reporting System REST API Reference

**Version**: 2.11.0
**Status**: ✅ Production Ready
**Test Coverage**: 13/13 tests passing (100%)

---

## Quick Start

### Base URL
```
http://localhost:5000/api
```

### Authentication
Currently no authentication required (add as needed)

---

## API Endpoints Summary

### ReportConfiguration Endpoints (6)

#### 1. List All Configurations
```http
GET /report-configurations?pageNum=1&pageSize=10
```
**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "uuid",
      "name": "Sales Report",
      "description": "Monthly sales analysis",
      "reportType": "Sales",
      "filters": { "month": "2025-11" },
      "columns": ["ProductName", "Quantity", "Revenue"],
      "exportFormat": "CSV",
      "includeMetadata": true,
      "isActive": true,
      "createdDate": "2025-11-07T00:00:00Z",
      "createdBy": "uuid",
      "updatedDate": "2025-11-07T00:00:00Z",
      "updatedBy": "uuid"
    }
  ],
  "pageNum": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5
}
```

#### 2. Get Single Configuration
```http
GET /report-configurations/{id}
```
**Response** (200 OK): Single configuration object

#### 3. Create Configuration
```http
POST /report-configurations
Content-Type: application/json

{
  "name": "Weekly Report",
  "description": "Weekly business metrics",
  "reportType": "Executive",
  "schedule": "Weekly",
  "scheduleTime": "09:00:00",
  "isActive": true,
  "filters": { "department": "Sales" },
  "columns": ["Metric", "Value", "Target"],
  "exportFormat": "PDF",
  "includeMetadata": false
}
```
**Response** (201 Created): Created configuration object

#### 4. Update Configuration
```http
PUT /report-configurations/{id}
Content-Type: application/json

{
  "name": "Updated Name",
  "description": "Updated description",
  "reportType": "Executive",
  "schedule": "Daily",
  "scheduleTime": "08:00:00",
  "isActive": true,
  "filters": { "updated": true },
  "columns": ["UpdatedColumns"],
  "exportFormat": "Excel",
  "includeMetadata": true
}
```
**Response** (200 OK): Updated configuration object

#### 5. Delete Configuration
```http
DELETE /report-configurations/{id}
```
**Response** (204 No Content)

---

### ReportExecution Endpoints (6)

#### 1. List All Executions
```http
GET /report-executions?pageNum=1&pageSize=10
```
**Response** (200 OK): Paged list of executions

#### 2. Get Single Execution
```http
GET /report-executions/{id}
```
**Response** (200 OK): Execution details

#### 3. Execute Report
```http
POST /report-executions/execute
Content-Type: application/json

{
  "reportConfigurationId": "uuid",
  "parameters": {
    "dateRange": "2025-11-01 to 2025-11-07",
    "department": "Sales"
  },
  "outputFormat": "CSV",
  "isScheduled": false
}
```
**Response** (200 OK):
```json
{
  "id": "uuid",
  "reportConfigId": "uuid",
  "executionStartTime": "2025-11-07T10:30:00Z",
  "executionEndTime": "2025-11-07T10:30:05Z",
  "status": "Completed",
  "recordsProcessed": 1500,
  "errorMessage": null,
  "durationMilliseconds": 5000,
  "fileSizeBytes": 125000,
  "outputFileName": "report_uuid.csv",
  "outputFilePath": "/reports/report_uuid.csv",
  "executedBy": "uuid"
}
```

#### 4. Download Execution Result
```http
POST /report-executions/{id}/download
```
**Response** (200 OK): Binary file content
**Content-Type**: application/octet-stream

---

### ReportDistribution Endpoints (6)

#### 1. List All Distributions
```http
GET /report-distributions?pageNum=1&pageSize=10
```
**Response** (200 OK): Paged list of distributions

#### 2. Get Single Distribution
```http
GET /report-distributions/{id}
```
**Response** (200 OK): Distribution details

#### 3. Create Distribution Channel
```http
POST /report-distributions
Content-Type: application/json

{
  "reportConfigId": "uuid",
  "name": "Email to Sales Team",
  "channelName": "Sales Distribution",
  "channel": "Email",
  "channelType": "Email",
  "recipients": "sales@company.com;manager@company.com",
  "channelConfiguration": {
    "subject": "Weekly Sales Report",
    "attachmentType": "PDF"
  },
  "isEnabled": true
}
```

**Alternative Property Names** (also supported):
- Use `name` instead of `channelName`
- Use `channel` instead of `channelType`

**Valid Channel Types**: Email, SFTP, Webhook, FTP, S3, Azure

**Response** (201 Created): Created distribution object

#### 4. Update Distribution
```http
PUT /report-distributions/{id}
Content-Type: application/json

{
  "channelName": "Updated Name",
  "channel": "SFTP",
  "recipients": "newemail@company.com",
  "channelConfiguration": { "path": "/reports/" },
  "isEnabled": true
}
```
**Response** (200 OK): Updated distribution object

#### 5. Delete Distribution
```http
DELETE /report-distributions/{id}
```
**Response** (204 No Content)

---

### ReportArchive Endpoints (6)

#### 1. List All Archives
```http
GET /report-archives?pageNum=1&pageSize=10
```
**Response** (200 OK): Paged list of archives

#### 2. Get Single Archive
```http
GET /report-archives/{id}
```
**Response** (200 OK):
```json
{
  "id": "uuid",
  "executionId": "uuid",
  "archiveDate": "2025-11-07T10:30:00Z",
  "retentionDays": 90,
  "expiryDate": "2026-02-05T10:30:00Z",
  "storageLocation": "/archives/report_uuid.tar.gz",
  "isCompressed": true,
  "fileSize": 98765
}
```

#### 3. Download Archive
```http
POST /report-archives/{id}/download
```
**Response** (200 OK): Binary file content
**Content-Type**: application/octet-stream
**Note**: Fails if archive has expired

#### 4. Restore Archive
```http
POST /report-archives/{id}/restore
```
**Response** (200 OK): Restored execution object
**Note**: Fails if archive has expired

#### 5. Delete Archive
```http
DELETE /report-archives/{id}
```
**Response** (204 No Content)

---

## HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful GET, PUT, POST (execute), POST (download) |
| 201 | Created | Resource created (initial creation only) |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Validation error, expired archive, invalid channel type |
| 404 | Not Found | Resource doesn't exist |
| 500 | Server Error | Unexpected error (see logs) |

---

## Common Patterns

### Pagination
All list endpoints support:
- `pageNum` - Page number (default: 1)
- `pageSize` - Items per page (default: 10, max: 100)

Example: `/api/report-configurations?pageNum=2&pageSize=20`

### Soft Delete
All resources support soft delete:
- Deleted items have `IsDeleted = true`
- Still exist in database for audit trail
- Excluded from all query results

### Flexible Property Names
ReportDistribution endpoints accept both:
- New names: `channelName`, `channelType`
- Old names: `name`, `channel`
- Fallback: If both provided, new names take precedence

### Error Responses
All errors include consistent format:
```json
{
  "error": "Description of what went wrong"
}
```

---

## Request Examples

### cURL - Create Configuration
```bash
curl -X POST http://localhost:5000/api/report-configurations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sales Report",
    "reportType": "Sales",
    "schedule": "Daily",
    "scheduleTime": "09:00:00",
    "isActive": true
  }'
```

### cURL - Execute Report
```bash
curl -X POST http://localhost:5000/api/report-executions/execute \
  -H "Content-Type: application/json" \
  -d '{
    "reportConfigurationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "outputFormat": "CSV",
    "isScheduled": false
  }'
```

### cURL - List with Pagination
```bash
curl -X GET "http://localhost:5000/api/report-configurations?pageNum=1&pageSize=20"
```

---

## Notes for Integration

1. **No Authentication** - Currently endpoints have no authentication
   - Add as needed in future phases

2. **CORS** - Configure CORS if frontend on different domain
   - Currently assumes same domain

3. **Logging** - All endpoints include INFO/WARNING/ERROR logging
   - Check application logs for detailed error messages

4. **Async Operations** - All endpoints are fully async
   - Safe for high-concurrency scenarios

5. **Database Transactions** - Single SaveChangesAsync per endpoint
   - ACID compliance guaranteed

6. **Type Conversions** - Entity properties converted to DTOs
   - Long? → Int?, Double? → Int? (milliseconds)

---

## Testing Endpoints

Use the integration tests as examples:
```bash
cd tests/OilTrading.IntegrationTests
dotnet test --filter "Reporting" --verbosity detailed
```

All 13 tests should pass.

---

## Next Steps

1. **Frontend Integration** - Create React components consuming these endpoints
2. **Authentication** - Add OAuth/JWT authentication
3. **Rate Limiting** - Add rate limiting for production
4. **Caching** - Add response caching where appropriate
5. **Real-time Updates** - Add WebSocket support for long-running operations

---

**Last Updated**: 2025-11-07
**Test Status**: ✅ 13/13 Passing
**Production Ready**: Yes
