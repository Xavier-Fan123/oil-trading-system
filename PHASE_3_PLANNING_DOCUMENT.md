# Phase 3 - Settlement Enhancement Module - DETAILED PLANNING DOCUMENT

**Status**: PLANNING
**Estimated Duration**: 5-7 hours
**Complexity**: Medium-High
**Dependencies**: Phase 2 (✅ SATISFIED)

---

## Phase 3 Overview

Phase 3 focuses on **bulk operations**, **template management**, and **advanced reporting** - enabling power users to perform batch operations and create reusable settlement configurations.

### Phase 3 Objectives

1. **Task 1: Bulk Actions System** (2-2.5 hours)
   - Batch approval workflow
   - Batch finalization
   - Multi-format export (Excel, CSV, PDF)
   - Checkbox selection in tables

2. **Task 2: Settlement Templates** (2-2.5 hours)
   - Template creation and management
   - Template preview and loading
   - Quick-create from templates
   - Template versioning and sharing

3. **Task 3: Advanced Export/Reporting** (1.5-2 hours)
   - Custom report builder
   - Report scheduling
   - Email distribution
   - Historical report archive

---

## Task 1: Bulk Actions System (Approve, Finalize, Export)

### Business Context

**Problem**: Settlement operators must approve/finalize settlements individually, which is time-consuming for high-volume operations.

**Solution**: Implement batch operations allowing selection and processing of multiple settlements at once.

### Scope & Features

#### 1.1 Checkbox Selection Enhancement
**Location**: Settlement tables (PendingSettlements, SettlementsList, SettlementDetail)

**Features**:
- [ ] Add checkbox column to settlement tables
- [ ] Select/deselect individual settlements
- [ ] Select all / Deselect all functionality
- [ ] Show count of selected items (e.g., "3 of 15 selected")
- [ ] Only enable bulk actions when 1+ selected

**Implementation Plan**:
```typescript
// Add to table state
const [selectedSettlements, setSelectedSettlements] = useState<Set<string>>(new Set());

// Selection handlers
const handleSelectSettlement = (settlementId: string) => {
  setSelectedSettlements(prev => {
    const newSet = new Set(prev);
    if (newSet.has(settlementId)) {
      newSet.delete(settlementId);
    } else {
      newSet.add(settlementId);
    }
    return newSet;
  });
};

const handleSelectAll = (allSettlementIds: string[]) => {
  if (selectedSettlements.size === allSettlementIds.length) {
    setSelectedSettlements(new Set()); // Deselect all
  } else {
    setSelectedSettlements(new Set(allSettlementIds)); // Select all
  }
};
```

**Files to Create**:
- `useSettlementSelection.ts` - Custom hook for selection logic

**Files to Modify**:
- `SettlementsList.tsx`
- `PendingSettlements.tsx`
- SettlementTable component

#### 1.2 Batch Approval Workflow
**Endpoint**: `POST /api/settlements/bulk-approve`

**Request**:
```typescript
{
  settlementIds: string[],  // Array of settlement IDs
  approverNotes: string,     // Optional approval notes
  forceApprove: boolean      // Force approve even with warnings
}
```

**Response**:
```typescript
{
  success: boolean,
  processedCount: number,
  failedCount: number,
  failures: Array<{
    settlementId: string,
    reason: string
  }>,
  message: string
}
```

**Validation Rules**:
- [ ] All settlements must be in "DataEntered" or "Calculated" status
- [ ] No validation errors
- [ ] User has approval permission
- [ ] Check for any pending changes

**Files to Create**:
- Backend controller method in `SettlementController`
- Backend query/command: `BulkApproveSettlementsCommand`
- Frontend service: `settlementApi.bulkApprove()`

**Files to Modify**:
- `SettlementController.cs`
- `settlementApi.ts`

#### 1.3 Batch Finalization Workflow
**Endpoint**: `POST /api/settlements/bulk-finalize`

**Request**:
```typescript
{
  settlementIds: string[],
  finalizeNotes: string,
  updatePaymentStatus: boolean  // Auto-update payment status
}
```

**Response**:
```typescript
{
  success: boolean,
  finalizedCount: number,
  failedCount: number,
  message: string,
  transactionIds: string[]  // Created payment transactions
}
```

**Workflow**:
1. Validate all settlements in "Approved" status
2. Create payment transactions for each
3. Update settlement status to "Finalized"
4. Log transaction for audit trail
5. Return results summary

**Files to Create**:
- Backend command: `BulkFinalizeSettlementsCommand`
- Backend handler with transaction management
- Frontend service method

**Files to Modify**:
- `SettlementController.cs`
- `settlementApi.ts`

#### 1.4 Multi-Format Export
**Endpoint**: `POST /api/settlements/bulk-export`

**Formats Supported**:
- [ ] Excel (.xlsx) - Full formatting with headers and totals
- [ ] CSV (.csv) - Clean data export
- [ ] PDF (.pdf) - Professional report format

**Request**:
```typescript
{
  settlementIds: string[],
  format: 'excel' | 'csv' | 'pdf',
  includeCharges: boolean,
  includePricingDetails: boolean,
  groupBy?: 'tradingPartner' | 'contract' | 'none'
}
```

**Excel Columns**:
```
Settlement ID | Contract | Type | Amount | Currency | Status |
Payment Terms | Due Date | Charges | Submitted Date | Approved By
```

**PDF Report**:
```
Settlement Export Report
- Generated Date/Time
- Settlement Details Table
- Summary Statistics
- Footer with company info
```

**Files to Create**:
- Backend service: `SettlementExportService`
- Export handlers for each format
- Excel: Using EPPlus or similar
- PDF: Using iText or similar

**Files to Modify**:
- `SettlementController.cs`
- `settlementApi.ts`

#### 1.5 UI Components for Bulk Actions

**New Component: BulkActionsToolbar**
```typescript
// frontend/src/components/Settlements/BulkActionsToolbar.tsx
interface BulkActionsToolbarProps {
  selectedCount: number;
  onApprove: () => void;
  onFinalize: () => void;
  onExport: (format: 'excel' | 'csv' | 'pdf') => void;
  disabled?: boolean;
}
```

**Features**:
- Shows "X settlements selected" counter
- Action buttons: Approve, Finalize, Export dropdown
- Confirmation dialogs for destructive actions
- Loading states during processing
- Success/error notifications

**New Component: BulkExportDialog**
```typescript
// frontend/src/components/Settlements/BulkExportDialog.tsx
interface BulkExportDialogProps {
  open: boolean;
  selectedSettlementIds: string[];
  onExport: (options: ExportOptions) => Promise<void>;
  onClose: () => void;
}
```

**Features**:
- Format selection (Excel, CSV, PDF)
- Options panel (include charges, pricing details)
- Group by option
- File name preview
- Export button with loading state

**Files to Create**:
- `BulkActionsToolbar.tsx`
- `BulkExportDialog.tsx`
- `useSettlementBulkActions.ts` - Custom hook

### Implementation Timeline

1. **Phase 3.1.1**: Settlement selection UI (30 min)
2. **Phase 3.1.2**: Bulk approve backend (45 min)
3. **Phase 3.1.3**: Bulk finalize backend (45 min)
4. **Phase 3.1.4**: Bulk export (60 min)
5. **Phase 3.1.5**: UI components and integration (30 min)
6. **Phase 3.1.6**: Testing and verification (15 min)

**Total**: ~3 hours

---

## Task 2: Settlement Templates System

### Business Context

**Problem**: Users frequently create similar settlements with same payment terms, charges, and settings.

**Solution**: Allow saving settlement configurations as reusable templates for quick-create functionality.

### Scope & Features

#### 2.1 Template Data Model

**SettlementTemplate Entity**:
```csharp
public class SettlementTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }           // e.g., "Standard TT NET30"
    public string Description { get; set; }    // e.g., "Telegraphic transfer with 30-day credit"
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; }           // For versioning
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }         // Shared across team?

    // Template configuration (JSON stored)
    public string TemplateConfiguration { get; set; }

    // Configuration includes:
    // {
    //   "documentType": "BillOfLading",
    //   "paymentTerms": "NET 30",
    //   "creditPeriodDays": 30,
    //   "settlementType": "TT",
    //   "prepaymentPercentage": 0,
    //   "defaultCharges": [
    //     { "type": "Demurrage", "amount": 500, "currency": "USD" },
    //     { "type": "PortCharges", "amount": 1000, "currency": "USD" }
    //   ]
    // }

    public List<SettlementTemplateUsage> Usages { get; set; }
}

public class SettlementTemplateUsage
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid SettlementId { get; set; }
    public DateTime UsedAt { get; set; }
}
```

**Files to Create**:
- `SettlementTemplate.cs` (entity)
- `SettlementTemplateConfiguration.cs` (EF mapping)
- Database migration

#### 2.2 API Endpoints

**Template Management**:
- `GET /api/settlement-templates` - List templates
- `GET /api/settlement-templates/{id}` - Get template details
- `POST /api/settlement-templates` - Create new template
- `PUT /api/settlement-templates/{id}` - Update template
- `DELETE /api/settlement-templates/{id}` - Delete template
- `POST /api/settlement-templates/{id}/version` - Create version

**Template Usage**:
- `POST /api/settlement-templates/{id}/apply` - Create settlement from template
- `GET /api/settlement-templates/{id}/usage-stats` - Usage statistics

**Files to Create**:
- `SettlementTemplateController.cs`
- Template queries and commands (CQRS)
- `SettlementTemplateService.cs`

#### 2.3 Template Preview & Loading

**Template Preview Component**:
```typescript
// frontend/src/components/Settlements/TemplatePreview.tsx
interface TemplatePreviewProps {
  template: SettlementTemplate;
  onApply: (template: SettlementTemplate) => void;
}
```

**Features**:
- Display template configuration in readable format
- Show payment terms, default charges
- Show last used date and usage count
- Preview how settlement would look
- Apply button to use template

**Quick-Create Flow**:
1. User opens settlement form
2. "Load from Template" button shows template list
3. User selects template
4. Form pre-populated with template values
5. User can override values as needed
6. Submit to create settlement

**Files to Create**:
- `TemplatePreview.tsx`
- `TemplateSelector.tsx`
- `useTemplateManagement.ts` - Custom hook

#### 2.4 Template Management UI

**New Page: Settlement Templates**
```typescript
// frontend/src/pages/SettlementTemplatesPage.tsx
```

**Features**:
- List all templates (with filtering)
- Create new template from scratch
- Create template from existing settlement
- Edit template details
- Mark as public/private
- Usage statistics
- Delete template (with confirmation)
- Template versioning history

**Components**:
- `TemplatesTable.tsx` - Table with all templates
- `TemplateForm.tsx` - Form to create/edit
- `TemplateUsageChart.tsx` - Usage statistics
- `TemplateVersionHistory.tsx` - Version timeline

#### 2.5 Template Sharing & Versioning

**Sharing Features**:
- Public templates available to all users
- Private templates only for creator
- Option to share with specific teams/users
- Permissions system (view, use, edit, delete)

**Versioning System**:
- Auto-increment version on each save
- Keep history of all versions
- Ability to revert to previous version
- Changelog for modifications

**Files to Create**:
- `SettlementTemplatePermission.cs` (entity)
- `ISettlementTemplateRepository.cs` (interface)
- `SettlementTemplateRepository.cs` (implementation)

### Implementation Timeline

1. **Phase 3.2.1**: Database model and migrations (30 min)
2. **Phase 3.2.2**: API endpoints and services (60 min)
3. **Phase 3.2.3**: Frontend UI components (60 min)
4. **Phase 3.2.4**: Template management page (45 min)
5. **Phase 3.2.5**: Testing and verification (15 min)

**Total**: ~3 hours

---

## Task 3: Advanced Export/Reporting

### Business Context

**Problem**: Users need flexible reporting with custom columns, filtering, and scheduling.

**Solution**: Build advanced report builder with multi-format export and scheduling.

### Scope & Features

#### 3.1 Custom Report Builder

**Report Configuration**:
```typescript
interface ReportConfiguration {
  id?: string;
  name: string;
  description: string;
  columns: ReportColumn[];
  filters: ReportFilter[];
  grouping?: string;
  sorting?: ReportSort[];
  format: 'excel' | 'csv' | 'pdf';
  includeCharts: boolean;
  includeStatistics: boolean;
}

interface ReportColumn {
  field: string;          // e.g., "settlementId", "amount", "dueDate"
  label: string;          // e.g., "Settlement ID", "Amount", "Due Date"
  width?: number;
  format?: string;        // e.g., "currency", "date", "number"
  hidden?: boolean;
}

interface ReportFilter {
  field: string;
  operator: 'equals' | 'contains' | 'greaterThan' | 'lessThan' | 'between' | 'in';
  value: any;
}
```

**Available Columns**:
- Settlement ID
- Contract Number
- Trading Partner
- Settlement Type (AP/AR)
- Amount
- Currency
- Status
- Payment Terms
- Due Date
- Submitted Date
- Approved Date
- Finalized Date
- Payment Date
- Charges Total
- Created By
- Approved By
- Notes

**Available Filters**:
- By date range
- By status
- By trading partner
- By amount range
- By payment terms
- By created user
- By approved user

#### 3.2 Report Templates

**Pre-built Report Templates**:
- [ ] **Daily Pending Settlements** - All pending approvals
- [ ] **Payment Due Report** - Settlements due within 7 days
- [ ] **Partner Exposure Report** - Total exposure by partner
- [ ] **Revenue Report** - Sales settlements with amounts
- [ ] **Approval Audit Trail** - Who approved what and when
- [ ] **Aging Report** - Settlements by age bracket

#### 3.3 Report Scheduling

**Scheduling Features**:
```typescript
interface ScheduledReport {
  id: string;
  name: string;
  configuration: ReportConfiguration;
  schedule: {
    frequency: 'daily' | 'weekly' | 'monthly';
    daysOfWeek?: number[];        // For weekly
    dayOfMonth?: number;           // For monthly
    timeOfDay: string;             // HH:mm format
    timezone?: string;
  };
  recipients: string[];            // Email addresses
  isActive: boolean;
  lastRun?: DateTime;
  nextRun?: DateTime;
}
```

**Features**:
- Schedule reports to generate automatically
- Email delivery to recipients
- Archive generated reports
- Pause/resume scheduling
- Notification on generation

#### 3.4 Email Distribution

**Email Template**:
```
Subject: Settlement Report - {{reportName}} for {{date}}

Dear {{recipientName}},

Please find attached the {{reportName}} generated on {{generatedDate}}.

Report Summary:
- Total Settlements: {{count}}
- Total Amount: {{totalAmount}}
- Report Period: {{period}}

This report is for internal use only.

Best regards,
Oil Trading System
```

**Features**:
- Personalized emails with name
- Attachment in requested format
- Schedule customization per recipient
- Delivery confirmation tracking

#### 3.5 Report History & Archive

**Report Archive Features**:
- Store generated reports for historical reference
- Search/filter archived reports
- Download previous reports
- Retention policy (keep for N days/months)
- Audit trail of who accessed what

**Files to Create**:
- `ReportArchive.cs` (entity)
- `ReportSchedule.cs` (entity)
- `ReportBuilder.tsx` (component)
- `ReportScheduler.tsx` (component)
- `ReportService.cs` (backend)
- `reportApi.ts` (frontend)

### Implementation Timeline

1. **Phase 3.3.1**: Report builder UI (60 min)
2. **Phase 3.3.2**: Report generation engine (45 min)
3. **Phase 3.3.3**: Scheduling system (45 min)
4. **Phase 3.3.4**: Email distribution (30 min)
5. **Phase 3.3.5**: Testing and verification (15 min)

**Total**: ~2.5 hours

---

## Consolidated Phase 3 Timeline

### Total Estimated Duration: 5-7 hours

| Task | Duration | Complexity | Risk |
|------|----------|------------|------|
| Task 1: Bulk Actions | 2-2.5h | Medium | Low |
| Task 2: Templates | 2-2.5h | Medium | Low |
| Task 3: Reporting | 1.5-2h | Medium-High | Medium |
| Testing & QA | 1h | Medium | Low |
| **Total** | **5.5-7h** | **Medium** | **Low-Medium** |

### Recommended Schedule

**Session 1** (2.5-3 hours):
- Task 1: Bulk Actions (complete)

**Session 2** (2.5-3 hours):
- Task 2: Settlement Templates (complete)

**Session 3** (1.5-2 hours):
- Task 3: Advanced Reporting (complete)
- Integration testing and refinement

---

## Technical Considerations

### Database Changes
- [ ] Add SettlementTemplate table
- [ ] Add SettlementTemplatePermission table
- [ ] Add ReportSchedule table
- [ ] Add ReportArchive table
- [ ] Create indices on frequently queried columns
- [ ] Total: ~4 new tables

### Backend Components
- [ ] 3 new controllers
- [ ] 10+ new CQRS commands/queries
- [ ] 3 new services
- [ ] 4 new entities
- [ ] Database migrations
- [ ] Email service integration

### Frontend Components
- [ ] 6 new React components
- [ ] 3 new custom hooks
- [ ] 2 new API service modules
- [ ] 4 new pages/sections
- [ ] ~2,000 lines of TypeScript code

### External Dependencies (Possible)
- **Excel Export**: EPPlus (C#) or ExcelJS (JS)
- **PDF Export**: iText (C#) or PDFKit (JS)
- **Email Service**: SMTP integration or SendGrid
- **Scheduling**: Hangfire or similar job scheduler
- **Storage**: File system or cloud storage for archives

---

## Risks & Mitigation

### Risk 1: Complex Report Builder UI
- **Risk**: Report builder UI could become bloated and confusing
- **Mitigation**: Start simple with preset templates, add custom builder later
- **Contingency**: Use drag-and-drop library (e.g., React Beautiful DnD)

### Risk 2: Email Delivery Issues
- **Risk**: Emails might not deliver reliably
- **Mitigation**: Implement retry logic with exponential backoff
- **Contingency**: Use reliable email service (SendGrid, Mailgun)

### Risk 3: Template Compatibility
- **Risk**: Templates might break if settlement schema changes
- **Mitigation**: Version templates, add migration logic
- **Contingency**: Provide template upgrade utility

### Risk 4: Performance with Large Datasets
- **Risk**: Report generation slow with 10,000+ settlements
- **Mitigation**: Implement pagination, async processing
- **Contingency**: Queue long-running reports with background jobs

---

## Success Criteria

### Phase 3 Success Metrics

**Functionality**:
- [ ] All 3 tasks completed and tested
- [ ] Zero compilation errors
- [ ] 100% test pass rate
- [ ] All 4+ new endpoints working

**Quality**:
- [ ] No breaking changes to existing APIs
- [ ] TypeScript strict mode compliance
- [ ] Proper error handling throughout
- [ ] Comprehensive logging

**Performance**:
- [ ] Bulk operations complete in <5 seconds for 100 items
- [ ] Report generation in <10 seconds
- [ ] Email delivery within 1 minute

**User Experience**:
- [ ] Clear UI for all new features
- [ ] Helpful error messages
- [ ] Loading indicators for async operations
- [ ] Success/failure notifications

---

## Dependencies & Prerequisites

### Phase 2 Completion ✅
- All Phase 2 tasks completed successfully
- Current codebase compiling without errors
- Database fully migrated

### External Tools (Install if needed)
- [ ] EPPlus (NuGet) - For Excel export
- [ ] iText (NuGet) - For PDF export
- [ ] Hangfire (NuGet) - For job scheduling (optional)

### Knowledge/Skills
- [ ] CQRS pattern (already used in codebase)
- [ ] Entity Framework Core
- [ ] React hooks
- [ ] Material-UI components
- [ ] Async/await patterns

---

## Sign-Off & Approval

**Prepared By**: Claude Code AI
**Date**: November 6, 2025
**Status**: READY FOR IMPLEMENTATION

### Next Steps

1. **Review** this planning document
2. **Approve** task scope and timeline
3. **Begin** Task 1: Bulk Actions System
4. **Follow** implementation sequence
5. **Report** progress in session summaries

---

## Appendix A: Code Structure Examples

### Example: Bulk Approve Command
```csharp
public class BulkApproveSettlementsCommand : IRequest<BulkApproveSettlementsResult>
{
    public List<Guid> SettlementIds { get; set; }
    public string ApproverNotes { get; set; }
    public bool ForceApprove { get; set; }
}

public class BulkApproveSettlementsCommandHandler
    : IRequestHandler<BulkApproveSettlementsCommand, BulkApproveSettlementsResult>
{
    public async Task<BulkApproveSettlementsResult> Handle(
        BulkApproveSettlementsCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation
        // 1. Load all settlements
        // 2. Validate each one
        // 3. Approve those valid
        // 4. Return summary
    }
}
```

### Example: Template Application
```typescript
const applyTemplate = async (templateId: string, overrides?: Partial<SettlementFormData>) => {
  const template = await settlementApi.getTemplate(templateId);
  const formData = {
    ...template.configuration,
    ...overrides
  };
  setFormData(formData);
};
```

---

*End of Phase 3 Planning Document*
*Revision: 1.0*
*Last Updated: November 6, 2025*
