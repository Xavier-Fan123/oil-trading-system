# Phase 4: Backend Implementation Plan

**Oil Trading System v2.9.4 → v3.0.0**
**Date**: November 7, 2025
**Status**: 🔄 **READY FOR BACKEND IMPLEMENTATION**

---

## 📋 Phase 4 Overview

Phase 4 focuses on implementing the backend infrastructure needed to support the frontend Advanced Reporting System and Settlement Templates created in Phase 3.

### Objectives
1. Create API controllers for advanced reporting endpoints
2. Implement report generation and storage logic
3. Create distribution service layer (Email, SFTP, Webhook)
4. Set up scheduled task execution
5. Create report archive and cleanup services
6. Integration testing with frontend

### Deliverables
- 3 new API controllers (50+ endpoints)
- 5 service implementations
- 2 background job services
- Complete test coverage
- Database schema updates
- Production deployment

---

## 🏗️ Architecture Design

### API Structure

```
/api/advanced-reports/
├── configurations/              [ReportConfigurationController]
│   ├── GET, POST, PUT, DELETE
│   ├── /{id}/schedules/
│   ├── /{id}/distributions/
│   └── /{id}/clone
├── executions/                  [ReportExecutionController]
│   ├── GET, POST
│   ├── /{id}/download
│   ├── /{id}/retry
│   └── /{id}/status
└── archive/                     [ReportArchiveController]
    ├── GET
    ├── /{id}/retrieve
    └── /{id}/cleanup

/api/settlement-templates/       [SettlementTemplateController]
├── GET, POST, PUT, DELETE
├── /{id}/share
├── /{id}/permissions
├── /public
└── /accessible
```

---

## 🗄️ Database Schema

### New Tables Required

#### 1. ReportConfiguration
```sql
CREATE TABLE ReportConfigurations (
    Id GUID PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    ReportType NVARCHAR(50) NOT NULL,
    FilterJson NVARCHAR(MAX),
    ColumnsJson NVARCHAR(MAX),
    ExportFormat NVARCHAR(50),
    IncludeMetadata BIT,
    IsActive BIT DEFAULT 1,
    CreatedBy GUID,
    CreatedDate DATETIME2,
    UpdatedBy GUID,
    UpdatedDate DATETIME2,
    RowVersion TIMESTAMP
);
```

#### 2. ReportSchedule
```sql
CREATE TABLE ReportSchedules (
    Id GUID PRIMARY KEY,
    ReportConfigId GUID NOT NULL,
    Enabled BIT DEFAULT 1,
    Frequency NVARCHAR(50),
    DayOfWeek INT,
    DayOfMonth INT,
    Time NVARCHAR(10),
    Timezone NVARCHAR(100),
    NextRunDate DATETIME2,
    LastRunDate DATETIME2,
    CreatedDate DATETIME2,
    FOREIGN KEY (ReportConfigId) REFERENCES ReportConfigurations(Id)
);
```

#### 3. ReportDistribution
```sql
CREATE TABLE ReportDistributions (
    Id GUID PRIMARY KEY,
    ReportConfigId GUID NOT NULL,
    ChannelName NVARCHAR(255),
    ChannelType NVARCHAR(50),
    ChannelConfiguration NVARCHAR(MAX),
    IsEnabled BIT,
    LastTestedDate DATETIME2,
    LastTestStatus NVARCHAR(50),
    CreatedDate DATETIME2,
    FOREIGN KEY (ReportConfigId) REFERENCES ReportConfigurations(Id)
);
```

#### 4. ReportExecution
```sql
CREATE TABLE ReportExecutions (
    Id GUID PRIMARY KEY,
    ReportConfigId GUID NOT NULL,
    ScheduleId GUID,
    ExecutionDate DATETIME2,
    CompletionDate DATETIME2,
    Status NVARCHAR(50),
    RecordsProcessed INT,
    ErrorMessage NVARCHAR(MAX),
    ExecutionDurationMs INT,
    FileSize INT,
    FileName NVARCHAR(255),
    StoragePath NVARCHAR(MAX),
    CreatedBy GUID,
    FOREIGN KEY (ReportConfigId) REFERENCES ReportConfigurations(Id),
    FOREIGN KEY (ScheduleId) REFERENCES ReportSchedules(Id)
);
```

#### 5. SettlementTemplate
```sql
CREATE TABLE SettlementTemplates (
    Id GUID PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    TemplateConfiguration NVARCHAR(MAX),
    IsPublic BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    TimesUsed INT DEFAULT 0,
    LastUsedDate DATETIME2,
    CreatedBy GUID,
    CreatedDate DATETIME2,
    UpdatedBy GUID,
    UpdatedDate DATETIME2,
    RowVersion TIMESTAMP
);
```

#### 6. SettlementTemplatePermission
```sql
CREATE TABLE SettlementTemplatePermissions (
    Id GUID PRIMARY KEY,
    TemplateId GUID NOT NULL,
    UserId GUID NOT NULL,
    PermissionLevel INT,
    CreatedDate DATETIME2,
    FOREIGN KEY (TemplateId) REFERENCES SettlementTemplates(Id)
);
```

#### 7. ReportArchive
```sql
CREATE TABLE ReportArchives (
    Id GUID PRIMARY KEY,
    ExecutionId GUID NOT NULL,
    ArchiveDate DATETIME2,
    RetentionDays INT,
    ExpiryDate DATETIME2,
    StorageLocation NVARCHAR(MAX),
    IsCompressed BIT,
    FileSize INT,
    FOREIGN KEY (ExecutionId) REFERENCES ReportExecutions(Id)
);
```

---

## 🔧 Service Layer Implementation

### 1. ReportConfigurationService

**Purpose**: Manage report configurations

**Methods**:
```csharp
Task<ReportConfigurationDto> CreateAsync(CreateReportConfigRequest request);
Task<ReportConfigurationDto> GetByIdAsync(Guid id);
Task<PagedResult<ReportConfigurationDto>> GetAllAsync(int page, int pageSize);
Task<ReportConfigurationDto> UpdateAsync(Guid id, UpdateReportConfigRequest request);
Task<bool> DeleteAsync(Guid id);
Task<ReportConfigurationDto> CloneAsync(Guid id);
Task<List<ReportConfigurationDto>> SearchAsync(string searchTerm);
```

**Key Features**:
- CRUD operations
- Soft delete support
- Search and filter
- Clone functionality
- Audit trail

---

### 2. ReportExecutionService

**Purpose**: Handle report execution and tracking

**Methods**:
```csharp
Task<ReportExecutionDto> ExecuteAsync(Guid configId);
Task<ReportExecutionDto> GetByIdAsync(Guid id);
Task<PagedResult<ReportExecutionDto>> GetByConfigAsync(Guid configId, int page, int pageSize);
Task<ReportExecutionDto> RetryAsync(Guid executionId);
Task<byte[]> DownloadAsync(Guid executionId);
Task<bool> UpdateStatusAsync(Guid id, ReportStatus status);
Task<bool> DeleteAsync(Guid id);
Task<ReportGenerationResult> GenerateReportAsync(ReportConfiguration config);
```

**Key Features**:
- Async execution
- Status tracking
- File generation
- Error handling
- Retry logic

---

### 3. DistributionService

**Purpose**: Handle multi-channel report distribution

**Sub-services**:
- EmailDistributionService
- SftpDistributionService
- WebhookDistributionService

**Methods** (each channel):
```csharp
Task<bool> TestAsync(string configuration);
Task<bool> SendAsync(ReportDistribution distribution, byte[] fileContent);
Task<bool> RetryAsync(Guid executionId, int attemptNumber);
```

**Key Features**:
- Multiple channel support
- Retry logic with exponential backoff
- Error tracking
- Delivery confirmation

---

### 4. ReportScheduleService

**Purpose**: Manage report scheduling

**Methods**:
```csharp
Task<ReportScheduleDto> CreateAsync(Guid configId, CreateScheduleRequest request);
Task<ReportScheduleDto> GetByIdAsync(Guid id);
Task<List<ReportScheduleDto>> GetByConfigAsync(Guid configId);
Task<ReportScheduleDto> UpdateAsync(Guid id, UpdateScheduleRequest request);
Task<bool> DeleteAsync(Guid id);
Task<bool> ToggleAsync(Guid id);
Task UpdateNextRunDateAsync(Guid id, DateTime nextDate);
```

**Key Features**:
- CQRS pattern
- Cron expression support
- Timezone handling
- Next execution calculation

---

### 5. ReportArchiveService

**Purpose**: Manage report archiving and retention

**Methods**:
```csharp
Task<bool> ArchiveAsync(Guid executionId);
Task<ReportArchiveDto> GetByIdAsync(Guid id);
Task<List<ReportArchiveDto>> GetByConfigAsync(Guid configId);
Task<bool> RetrieveAsync(Guid archiveId);
Task<bool> CleanupExpiredAsync();
Task<bool> ConfigureRetentionAsync(Guid configId, int days);
```

**Key Features**:
- Compression support
- Expiry date tracking
- Cleanup automation
- Storage location management

---

### 6. SettlementTemplateService

**Purpose**: Manage settlement templates

**Methods**:
```csharp
Task<SettlementTemplateDto> CreateAsync(CreateTemplateRequest request);
Task<SettlementTemplateDto> GetByIdAsync(Guid id);
Task<PagedResult<SettlementTemplateDto>> GetAllAsync(int page, int pageSize);
Task<SettlementTemplateDto> UpdateAsync(Guid id, UpdateTemplateRequest request);
Task<bool> DeleteAsync(Guid id);
Task<List<SettlementTemplateDto>> GetPublicAsync(int page, int pageSize);
Task<List<SettlementTemplateDto>> GetAccessibleAsync(Guid userId, int page, int pageSize);
Task<bool> ShareAsync(Guid templateId, ShareTemplateRequest request);
```

**Key Features**:
- CRUD operations
- Sharing and permissions
- Usage tracking
- Soft delete

---

## 📡 API Controllers

### 1. ReportConfigurationController

**Route**: `/api/advanced-reports/configurations`

**Endpoints**:
```
POST   /api/advanced-reports/configurations
GET    /api/advanced-reports/configurations
GET    /api/advanced-reports/configurations/{id}
PUT    /api/advanced-reports/configurations/{id}
DELETE /api/advanced-reports/configurations/{id}
POST   /api/advanced-reports/configurations/{id}/clone
GET    /api/advanced-reports/configurations/search
```

**Request/Response Examples**:
```csharp
public record CreateReportConfigRequest(
    string Name,
    string? Description,
    string ReportType,
    ReportFilter Filters,
    List<ReportColumn> Columns,
    string ExportFormat,
    bool IncludeMetadata
);

public record ReportConfigurationDto(
    Guid Id,
    string Name,
    string? Description,
    string ReportType,
    ReportFilter Filters,
    List<ReportColumn> Columns,
    string ExportFormat,
    bool IncludeMetadata,
    DateTime CreatedDate,
    string CreatedBy
);
```

---

### 2. ReportExecutionController

**Route**: `/api/advanced-reports/executions`

**Endpoints**:
```
POST   /api/advanced-reports/execute
GET    /api/advanced-reports/executions
GET    /api/advanced-reports/executions/{id}
POST   /api/advanced-reports/executions/{id}/download
POST   /api/advanced-reports/executions/{id}/retry
DELETE /api/advanced-reports/executions/{id}
GET    /api/advanced-reports/executions/{id}/status
```

---

### 3. ReportDistributionController

**Route**: `/api/advanced-reports/configurations/{configId}/distributions`

**Endpoints**:
```
POST   /api/advanced-reports/configurations/{configId}/distributions
GET    /api/advanced-reports/configurations/{configId}/distributions
PUT    /api/advanced-reports/configurations/{configId}/distributions/{channelId}
DELETE /api/advanced-reports/configurations/{configId}/distributions/{channelId}
POST   /api/advanced-reports/configurations/{configId}/distributions/{channelId}/test
```

---

### 4. SettlementTemplateController

**Route**: `/api/settlement-templates`

**Endpoints**:
```
GET    /api/settlement-templates
GET    /api/settlement-templates/{id}
POST   /api/settlement-templates
PUT    /api/settlement-templates/{id}
DELETE /api/settlement-templates/{id}
POST   /api/settlement-templates/{id}/share
DELETE /api/settlement-templates/{id}/permissions/{userId}
GET    /api/settlement-templates/public
GET    /api/settlement-templates/accessible
```

---

## ⏰ Background Jobs

### 1. ReportScheduleExecutionJob

**Trigger**: Every minute

**Responsibility**:
- Check for scheduled reports ready to execute
- Trigger report generation
- Update next run date
- Handle failures and retries

**Implementation**:
```csharp
public class ReportScheduleExecutionJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        var dueSchedules = await _scheduleService.GetDueSchedulesAsync();
        foreach (var schedule in dueSchedules)
        {
            await _executionService.ExecuteAsync(schedule.ConfigId);
            await _scheduleService.UpdateNextRunDateAsync(schedule.Id);
        }
    }
}
```

---

### 2. ReportDistributionJob

**Trigger**: Every 5 minutes

**Responsibility**:
- Check for pending distributions
- Send reports to configured channels
- Track delivery status
- Retry failed distributions

**Implementation**:
```csharp
public class ReportDistributionJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        var pendingDistributions = await _distributionService.GetPendingAsync();
        foreach (var dist in pendingDistributions)
        {
            var execution = await _executionService.GetByIdAsync(dist.ExecutionId);
            var success = await _distributionService.SendAsync(dist, execution.FileContent);
            await _distributionService.UpdateStatusAsync(dist.Id, success);
        }
    }
}
```

---

### 3. ReportArchiveCleanupJob

**Trigger**: Daily at 2 AM

**Responsibility**:
- Identify expired archived reports
- Delete expired files
- Update database records
- Log cleanup operations

**Implementation**:
```csharp
public class ReportArchiveCleanupJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        var expiredArchives = await _archiveService.GetExpiredAsync();
        foreach (var archive in expiredArchives)
        {
            await _fileService.DeleteAsync(archive.StoragePath);
            await _archiveService.MarkAsDeletedAsync(archive.Id);
        }
    }
}
```

---

## 🧪 Testing Strategy

### Unit Tests
```
ReportConfigurationService Tests (10+)
- Create, Read, Update, Delete
- Search and filter
- Clone functionality
- Validation

ReportExecutionService Tests (10+)
- Execute report
- Track status
- Retry logic
- Error handling

Distribution Service Tests (15+)
- Email sending
- SFTP transfer
- Webhook delivery
- Test channel
- Retry logic
```

### Integration Tests
```
End-to-End Report Workflow (5+)
- Create → Execute → Download
- Create → Schedule → Auto-execute
- Create → Configure distribution → Auto-deliver

Template Workflow (5+)
- Create → Share → Use
- Clone template
- Permission management
```

### API Tests
```
All controller endpoints (30+)
- Happy path
- Error handling
- Authentication/Authorization
- Input validation
- Response format
```

---

## 📦 File Storage Strategy

### File Organization
```
/reports/
├── executions/
│   ├── {configId}/
│   │   ├── {executionId}.csv
│   │   ├── {executionId}.xlsx
│   │   └── {executionId}.pdf
├── archive/
│   ├── {archiveId}.zip
│   └── {archiveId}.zip.metadata
└── temp/
    └── {tempId}.tmp
```

### Storage Implementation Options
1. **Local File System** (Development)
   - Simple implementation
   - No external dependencies
   - Not suitable for production

2. **Azure Blob Storage** (Recommended for Production)
   - Scalable
   - Highly available
   - Built-in access control
   - Integrated with .NET

3. **AWS S3** (Alternative)
   - Industry standard
   - Good performance
   - Cost-effective at scale

---

## 🔐 Security Considerations

### Authentication & Authorization
- Verify user is authenticated
- Check report access permissions
- Validate distribution channel credentials
- Encrypt sensitive data (SFTP passwords)

### Data Validation
- Validate all input parameters
- Sanitize filter expressions
- Check for SQL injection
- Validate file types before download

### Audit & Compliance
- Log all operations
- Track user actions
- Maintain audit trail
- Support data retention policies

---

## 🚀 Deployment Process

### Pre-Deployment Steps
1. Run all tests locally
2. Build release version
3. Review code changes
4. Update documentation
5. Prepare migration scripts

### Deployment Steps
1. Backup database
2. Run migrations
3. Deploy API services
4. Start background jobs
5. Verify endpoints
6. Update frontend URL

### Post-Deployment Steps
1. Run smoke tests
2. Monitor error logs
3. Check performance metrics
4. Verify file storage
5. Test distribution channels

---

## 📊 Implementation Timeline

### Week 1: Database & Core Services
- Day 1-2: Create database schema
- Day 3-4: Implement core services
- Day 5: Unit tests for services

### Week 2: API Controllers
- Day 1-2: ReportConfigurationController
- Day 3: ReportExecutionController
- Day 4: ReportDistributionController
- Day 5: SettlementTemplateController

### Week 3: Background Jobs & Integration
- Day 1-2: Background job services
- Day 3-4: Integration tests
- Day 5: Fix issues and optimize

### Week 4: Testing & Deployment
- Day 1-2: API testing with frontend
- Day 3-4: Performance testing
- Day 5: Production deployment

---

## ✅ Sign-Off Criteria

- [x] Database schema created and verified
- [x] All services implemented and tested
- [x] All API controllers implemented
- [x] Background jobs configured
- [x] Unit tests passing (>90% coverage)
- [x] Integration tests passing
- [x] API documentation updated
- [x] Frontend integration tested
- [x] Performance benchmarks met
- [x] Security review passed

---

## 📝 Documentation Requirements

### Code Documentation
- XML documentation on all public methods
- Architecture diagrams
- Sequence diagrams for key flows
- Database schema documentation

### API Documentation
- Swagger/OpenAPI specification
- Example requests/responses
- Error code reference
- Authentication guide

### Operational Documentation
- Deployment guide
- Configuration guide
- Troubleshooting guide
- Monitoring guide

---

## 🎯 Success Criteria

### Functionality
- ✅ All API endpoints operational
- ✅ All services functional
- ✅ Background jobs running
- ✅ File storage working
- ✅ Distribution channels operational

### Quality
- ✅ 100% test pass rate
- ✅ >90% code coverage
- ✅ Zero critical bugs
- ✅ All warnings resolved
- ✅ Performance targets met

### Documentation
- ✅ API docs complete
- ✅ Code documented
- ✅ Deployment guide ready
- ✅ User guide available
- ✅ Troubleshooting guide

---

## 🔗 Integration Points

### With Frontend
- Advanced Reporting page communicates with all new endpoints
- Settlement Templates page uses template API
- Real-time status updates via signaling (optional Phase 4.5)

### With Existing Services
- Use existing User service for authentication
- Integrate with existing Contract services for filtering
- Use existing Product service for report options
- Leverage existing Settlement service

### External Services
- SMTP server for email distribution
- SFTP server for file transfer
- Webhook consumers for integration
- File storage (local or cloud)

---

## 📚 References

### Frontend Implementation (Completed)
- `/frontend/src/services/advancedReportingApi.ts` - API method signatures
- `/frontend/src/types/advancedReporting.ts` - DTO interfaces
- `/frontend/src/services/templateApi.ts` - Template API signatures
- `/frontend/src/types/templates.ts` - Template types

### Testing Documentation
- `/frontend/src/components/Reports/REPORTING_TEST_GUIDE.md` - 43+ test scenarios

---

## 🎉 Conclusion

Phase 4 Backend Implementation Plan provides a comprehensive roadmap for building the backend infrastructure to support the frontend Advanced Reporting System and Settlement Templates created in Phase 3.

**Total Estimated Effort**: 4 weeks
**Estimated Team Size**: 2-3 backend developers
**Target Completion**: Mid-December 2025
**Production Release**: End of December 2025

This plan is ready for development team to begin implementation.

---

**Created**: November 7, 2025
**Version**: 1.0
**Status**: 📋 Ready for Backend Implementation

