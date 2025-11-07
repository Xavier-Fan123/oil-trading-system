# Advanced Reporting System - Integration Test Guide

**Phase 3, Task 3.5 - Integration Testing**

## Overview

This guide provides comprehensive testing scenarios for the Advanced Reporting System, covering all components from report creation through execution and distribution.

---

## Test Environment Setup

### Prerequisites

1. **Backend Services Running**:
   - API: `http://localhost:5000`
   - Database: SQLite with seed data
   - Redis: Optional (system works without it)

2. **Frontend Running**:
   - Development server: `http://localhost:3003` (or assigned port)
   - Vite dev server running without errors

3. **Test Data Available**:
   - Multiple purchase contracts
   - Multiple sales contracts
   - Various trading partners
   - Products with different units

---

## Test Scenarios

### Test Suite 1: Report Configuration Management

#### 1.1: Create Basic Report Configuration

**Objective**: Verify report creation with basic settings

**Steps**:
1. Navigate to Advanced Reporting page
2. Click "Create Report" button
3. Enter report name: "Test Contract Report"
4. Enter description: "Test reporting functionality"
5. Select Report Type: "Contract Execution"
6. Click Next

**Expected Results**:
- ✅ Form progresses to Step 1
- ✅ No validation errors
- ✅ Form data preserved on return to Step 0

---

#### 1.2: Configure Report Filters

**Objective**: Verify filter configuration in multi-step form

**Steps**:
1. Continue from 1.1, now on Step 1
2. Set date range:
   - Start Date: 30 days ago
   - End Date: Today
3. Select Contract Type: "Purchase"
4. Click Next

**Expected Results**:
- ✅ Filters applied and preserved
- ✅ Date picker works correctly
- ✅ Dropdown selections saved
- ✅ Progress to Step 2

---

#### 1.3: Configure Report Columns

**Objective**: Verify column selection and visibility

**Steps**:
1. Continue from 1.2, now on Step 2
2. Enable columns: ContractNumber, Quantity, Price, Status
3. Disable columns: InternalNotes, MarginAnalysis
4. Verify column list shows correct count
5. Click Next

**Expected Results**:
- ✅ Column toggles work correctly
- ✅ Column count updates dynamically
- ✅ Selection state persists
- ✅ Progress to Step 3

---

#### 1.4: Configure Export Format and Save

**Objective**: Verify format selection and report save

**Steps**:
1. Continue from 1.3, now on Step 3
2. Select Format: "Excel"
3. Check "Include Metadata"
4. Review summary information
5. Click "Save Report"

**Expected Results**:
- ✅ Summary displays correct configuration
- ✅ Format selection options all visible
- ✅ Metadata toggle works
- ✅ Report saved to backend
- ✅ Redirects to list view
- ✅ New report appears in list

---

#### 1.5: Edit Existing Report

**Objective**: Verify report editing functionality

**Steps**:
1. From list view, click context menu on created report
2. Select "Edit"
3. Modify name to "Test Contract Report Updated"
4. Modify date range
5. Save changes

**Expected Results**:
- ✅ Form pre-populates with existing values
- ✅ Changes are persisted
- ✅ List view updates with new name
- ✅ No data loss during edit

---

#### 1.6: Delete Report Configuration

**Objective**: Verify report deletion with confirmation

**Steps**:
1. From list view, click context menu on any report
2. Select "Delete"
3. Confirm deletion in dialog

**Expected Results**:
- ✅ Confirmation dialog appears
- ✅ Report removed from list after deletion
- ✅ No errors during deletion
- ✅ Clean state after deletion

---

### Test Suite 2: Report Scheduling

#### 2.1: Create Daily Schedule

**Objective**: Verify daily report scheduling

**Steps**:
1. From list view, click report name to open "Manage" view
2. Click "Scheduling" tab
3. Click "Add Schedule"
4. Set Frequency: "Daily"
5. Set Time: 09:00
6. Leave Timezone default
7. Click "Create Schedule"

**Expected Results**:
- ✅ Schedule dialog closes
- ✅ New schedule appears in list
- ✅ Frequency display shows "Every day at 09:00"
- ✅ Schedule is enabled by default

---

#### 2.2: Create Weekly Schedule

**Objective**: Verify weekly scheduling with day selection

**Steps**:
1. Click "Add Schedule"
2. Set Frequency: "Weekly"
3. Select Day: "Monday" and "Friday"
4. Set Time: 14:30
5. Click "Create Schedule"

**Expected Results**:
- ✅ Day selector appears when frequency is weekly
- ✅ Multiple day selection works
- ✅ Schedule shows "Every Monday and Friday at 14:30"
- ✅ Both days can be toggled independently

---

#### 2.3: Create Monthly Schedule

**Objective**: Verify monthly scheduling with date selection

**Steps**:
1. Click "Add Schedule"
2. Set Frequency: "Monthly"
3. Select Day: "15" (15th of month)
4. Set Time: 08:00
5. Click "Create Schedule"

**Expected Results**:
- ✅ Day of month picker appears
- ✅ Valid dates (1-31) selectable
- ✅ Schedule shows "On the 15th of each month at 08:00"

---

#### 2.4: Enable/Disable Schedule

**Objective**: Verify schedule toggle functionality

**Steps**:
1. From schedule list, toggle a schedule's enabled/disabled state
2. Verify it updates in real-time

**Expected Results**:
- ✅ Toggle switch responds immediately
- ✅ Backend updates successfully
- ✅ No duplicate schedules created
- ✅ Disabled schedules remain in list

---

#### 2.5: Edit Schedule

**Objective**: Verify schedule editing

**Steps**:
1. From schedule list, click "Edit" on a schedule
2. Change frequency from Daily to Weekly
3. Select different day
4. Save changes

**Expected Results**:
- ✅ Edit dialog pre-populates with current values
- ✅ Changes persisted to backend
- ✅ Display updates with new schedule info
- ✅ No data loss

---

#### 2.6: Delete Schedule

**Objective**: Verify schedule deletion

**Steps**:
1. From schedule list, click menu and select "Delete"
2. Confirm deletion

**Expected Results**:
- ✅ Schedule removed from list
- ✅ No errors during deletion
- ✅ Other schedules unaffected

---

### Test Suite 3: Report Execution & History

#### 3.1: Manual Report Execution

**Objective**: Verify manual report execution

**Steps**:
1. From report list view, click "Run" icon on any report
2. Wait for execution to complete

**Expected Results**:
- ✅ Loading indicator displays
- ✅ Execution completes without errors
- ✅ Execution appears in history
- ✅ Status shows as "Completed" or "Running"

---

#### 3.2: View Execution History

**Objective**: Verify execution history display

**Steps**:
1. Open report "Manage" view
2. Click "Execution History" tab
3. Review execution records

**Expected Results**:
- ✅ History table displays all past executions
- ✅ Shows: Execution Date, Status, Records Processed, File Size, Duration
- ✅ Pagination works if many executions
- ✅ Status badges show correct colors:
  - ✅ Green for Completed
  - ⏳ Blue for Running
  - ❌ Red for Failed

---

#### 3.3: Download Report

**Objective**: Verify report file download

**Steps**:
1. In execution history, click "Download" on a completed execution
2. Check downloads folder for file

**Expected Results**:
- ✅ Download progress bar displays
- ✅ File downloads with correct name
- ✅ File format matches configuration (Excel, CSV, etc.)
- ✅ File size displays correctly
- ✅ No errors during download

---

#### 3.4: View Execution Details

**Objective**: Verify detailed execution information

**Steps**:
1. In execution history, click info icon on any execution
2. Review details dialog

**Expected Results**:
- ✅ Dialog displays:
  - Execution ID (GUID)
  - Status
  - Execution time
  - Completion time
  - Records processed
  - Duration
  - File size
  - Created by
- ✅ Dialog can be closed without issues

---

#### 3.5: Retry Failed Execution

**Objective**: Verify retry functionality for failed reports

**Steps**:
1. In execution history, find a failed execution
2. Click "Retry" button

**Expected Results**:
- ✅ Retry button only appears for failed executions
- ✅ Execution retries with same configuration
- ✅ New execution appears in history
- ✅ Previous failed execution remains in history

---

#### 3.6: Delete Execution Record

**Objective**: Verify execution record deletion

**Steps**:
1. In execution history, click menu on any execution
2. Select "Delete"

**Expected Results**:
- ✅ Execution removed from history
- ✅ Other executions unaffected
- ✅ No errors during deletion

---

### Test Suite 4: Report Distribution Configuration

#### 4.1: Add Email Distribution Channel

**Objective**: Verify email channel creation

**Steps**:
1. Open report "Manage" view
2. Click "Distribution" tab
3. Click "Add Channel"
4. Set Channel Name: "Operations Team"
5. Channel Type: "Email"
6. Recipients: "ops@company.com, admin@company.com"
7. Subject: "Daily Report: {reportName}"
8. Body: "Please review the attached report"
9. Retry Attempts: 3
10. Retry Delay: 5 minutes
11. Click "Create"

**Expected Results**:
- ✅ Dialog closes
- ✅ New channel appears in list
- ✅ Displays email icon
- ✅ Status shows "Enabled"
- ✅ Configuration shows recipients

---

#### 4.2: Add SFTP Distribution Channel

**Objective**: Verify SFTP channel creation

**Steps**:
1. Click "Add Channel"
2. Set Channel Name: "Archive Server"
3. Channel Type: "SFTP"
4. SFTP Host: "sftp.example.com"
5. Port: 22
6. Username: "reportuser"
7. Password: "••••••••"
8. Remote Path: "/reports/daily/"
9. Click "Create"

**Expected Results**:
- ✅ Dialog closes
- ✅ New channel appears with SFTP icon
- ✅ Configuration shows host and port
- ✅ Password field is masked on display

---

#### 4.3: Add Webhook Distribution Channel

**Objective**: Verify webhook channel creation

**Steps**:
1. Click "Add Channel"
2. Set Channel Name: "Notification Service"
3. Channel Type: "Webhook"
4. Webhook URL: "https://api.example.com/reports"
5. Custom Headers: `{"Authorization": "Bearer token123"}`
6. Click "Create"

**Expected Results**:
- ✅ Dialog closes
- ✅ New channel appears with webhook icon
- ✅ Configuration shows URL
- ✅ Headers properly stored

---

#### 4.4: Edit Distribution Channel

**Objective**: Verify channel editing

**Steps**:
1. Click menu on any distribution channel
2. Select "Edit"
3. Modify recipient list
4. Save changes

**Expected Results**:
- ✅ Edit dialog pre-populates with current values
- ✅ Changes saved successfully
- ✅ Display updates immediately

---

#### 4.5: Test Distribution Channel

**Objective**: Verify test functionality

**Steps**:
1. Click menu on any distribution channel
2. Select "Test Channel"
3. Wait for result

**Expected Results**:
- ✅ Test runs without errors
- ✅ Success/failure alert displays
- ✅ Test message provides feedback
- ✅ No actual report generated during test

---

#### 4.6: Toggle Channel Enable/Disable

**Objective**: Verify channel activation toggle

**Steps**:
1. Toggle enable/disable switch on any channel
2. Observe status change

**Expected Results**:
- ✅ Chip changes from "Enabled" to "Disabled"
- ✅ Change persists on page reload
- ✅ Disabled channels still visible in list

---

#### 4.7: Delete Distribution Channel

**Objective**: Verify channel deletion

**Steps**:
1. Click menu on any channel
2. Select "Delete"
3. Confirm deletion

**Expected Results**:
- ✅ Confirmation dialog appears
- ✅ Channel removed from list
- ✅ Other channels unaffected

---

### Test Suite 5: Multi-Step Report Workflow

#### 5.1: Complete End-to-End Report Workflow

**Objective**: Verify full workflow from creation to distribution

**Steps**:
1. Create new report:
   - Name: "End-to-End Test Report"
   - Type: Settlement Summary
   - Filters: Date range + Trading Partner
   - Columns: All key columns selected
   - Format: PDF
   - Save

2. Configure schedule:
   - Navigate to Manage
   - Add daily schedule at 10:00

3. Configure distribution:
   - Add email channel to operations team
   - Add SFTP channel to archive server

4. Execute report:
   - Click Run button
   - Wait for completion

5. Verify results:
   - Check execution history
   - Download generated file
   - Verify file contents

**Expected Results**:
- ✅ All steps complete without errors
- ✅ Report appears in list
- ✅ Schedule displays in scheduling tab
- ✅ Channels display in distribution tab
- ✅ Execution succeeds
- ✅ History shows completed execution
- ✅ Downloaded file is valid PDF

---

### Test Suite 6: Error Handling & Edge Cases

#### 6.1: Invalid Report Configuration

**Objective**: Verify validation of required fields

**Steps**:
1. Try to create report without name
2. Try to create without selecting report type
3. Try to save without selecting any columns

**Expected Results**:
- ✅ Validation error messages appear
- ✅ Form does not submit
- ✅ Error messages are helpful and specific

---

#### 6.2: Invalid Schedule Configuration

**Objective**: Verify schedule validation

**Steps**:
1. Try to create schedule without selecting frequency
2. Try to create weekly schedule without selecting day
3. Try invalid time format

**Expected Results**:
- ✅ Validation errors prevent submission
- ✅ Error messages guide user to correct input
- ✅ Form state preserved after validation error

---

#### 6.3: Invalid Distribution Channel Configuration

**Objective**: Verify distribution validation

**Steps**:
1. Try to create email channel without recipients
2. Try to create SFTP channel without host/username
3. Try to create webhook without URL

**Expected Results**:
- ✅ Validation errors prevent submission
- ✅ Required fields clearly marked
- ✅ Error messages specific to missing field

---

#### 6.4: API Error Handling

**Objective**: Verify graceful error handling

**Steps**:
1. Stop backend API
2. Try to create new report
3. Try to edit existing report
4. Try to execute report

**Expected Results**:
- ✅ User-friendly error messages display
- ✅ No console errors or warnings
- ✅ UI remains responsive
- ✅ Can retry after API recovery

---

#### 6.5: Network Timeout Handling

**Objective**: Verify timeout handling for slow operations

**Steps**:
1. Execute a report (may take time)
2. Simulate slow network (if possible)
3. Verify timeout handling

**Expected Results**:
- ✅ Long operations don't freeze UI
- ✅ Progress indicators display
- ✅ Can cancel operations
- ✅ Graceful timeout messages

---

### Test Suite 7: Performance & Load Testing

#### 7.1: Large Report List

**Objective**: Verify performance with many reports

**Steps**:
1. Create 20+ report configurations via API
2. Navigate to Advanced Reporting page
3. Measure load time
4. Scroll through list

**Expected Results**:
- ✅ Page loads in <2 seconds
- ✅ Pagination works smoothly
- ✅ No UI lag or freezing
- ✅ Search/filter responsive

---

#### 7.2: Large Execution History

**Objective**: Verify performance with large history

**Steps**:
1. Create many report executions
2. View execution history tab
3. Verify pagination

**Expected Results**:
- ✅ History tab loads quickly
- ✅ Pagination works correctly
- ✅ Downloads are responsive
- ✅ No memory leaks

---

#### 7.3: Many Schedules Per Report

**Objective**: Verify performance with multiple schedules

**Steps**:
1. Add 10+ schedules to a single report
2. View scheduling tab
3. Edit and delete schedules

**Expected Results**:
- ✅ All schedules display correctly
- ✅ Operations remain responsive
- ✅ No UI rendering issues

---

#### 7.4: Many Distribution Channels

**Objective**: Verify performance with many channels

**Steps**:
1. Add 15+ distribution channels to a report
2. View distribution tab
3. Test and edit channels

**Expected Results**:
- ✅ All channels display in table
- ✅ Operations responsive
- ✅ No performance degradation

---

### Test Suite 8: UI/UX Verification

#### 8.1: Responsive Design

**Objective**: Verify responsive layout at different screen sizes

**Steps**:
1. Resize browser to mobile (375px)
2. Navigate through all views
3. Resize to tablet (768px)
4. Resize to desktop (1920px)

**Expected Results**:
- ✅ All content visible at all sizes
- ✅ Forms remain usable on mobile
- ✅ Tables use horizontal scroll if needed
- ✅ No layout breaking

---

#### 8.2: Accessibility

**Objective**: Verify keyboard navigation and screen reader support

**Steps**:
1. Navigate entire page with Tab key
2. Verify form labels associated with inputs
3. Check button labels are descriptive
4. Verify focus indicators visible

**Expected Results**:
- ✅ All interactive elements keyboard accessible
- ✅ Focus visible at all times
- ✅ Form labels properly associated
- ✅ No keyboard traps

---

#### 8.3: Dark Mode Support

**Objective**: Verify appearance in dark mode

**Steps**:
1. Switch system to dark mode (if supported)
2. Verify all text readable
3. Verify all colors contrast properly

**Expected Results**:
- ✅ Text remains readable
- ✅ Color contrast meets standards
- ✅ All icons visible
- ✅ No inverted images

---

#### 8.4: Loading States

**Objective**: Verify loading indicators

**Steps**:
1. Create a report
2. Execute a report
3. Download a file

**Expected Results**:
- ✅ Loading spinners display
- ✅ Progress bars show progress
- ✅ Buttons disabled during operations
- ✅ Messages inform user of status

---

### Test Suite 9: Data Persistence

#### 9.1: Form Data Persistence

**Objective**: Verify form state preserved on navigation

**Steps**:
1. Start creating report, fill first step
2. Click back/forward buttons
3. Verify data preserved

**Expected Results**:
- ✅ Form data remains intact
- ✅ No data loss on navigation
- ✅ Multi-step form state preserved

---

#### 9.2: Page Refresh

**Objective**: Verify state after page refresh

**Steps**:
1. Open report manage view
2. Press F5 to refresh
3. Verify data reloads correctly

**Expected Results**:
- ✅ Page reloads without errors
- ✅ Data refetched from API
- ✅ Current tab remembered
- ✅ All sections load correctly

---

#### 9.3: Browser Back Button

**Objective**: Verify browser back button behavior

**Steps**:
1. Navigate from list → manage view
2. Click browser back button
3. Verify return to correct list state

**Expected Results**:
- ✅ Returns to list view
- ✅ Correct report may still be selected
- ✅ No data loss
- ✅ Page state correct

---

### Test Suite 10: Concurrent Operations

#### 10.1: Concurrent Report Creation

**Objective**: Verify handling of simultaneous operations

**Steps**:
1. In browser tab 1: Create report A
2. In browser tab 2: Create report B
3. Refresh both tabs

**Expected Results**:
- ✅ Both reports created successfully
- ✅ No data conflicts
- ✅ Both visible in list after refresh
- ✅ IDs are unique

---

#### 10.2: Concurrent Schedule Updates

**Objective**: Verify concurrent schedule modifications

**Steps**:
1. Open schedule edit in two tabs
2. Modify schedule in tab 1
3. Modify schedule in tab 2
4. Check final state

**Expected Results**:
- ✅ Last operation wins (expected behavior)
- ✅ No errors during concurrent modifications
- ✅ Data integrity maintained

---

## Automated Test Scenarios

### Unit Test Coverage Goals

```
Component Test Coverage:
- ReportBuilder: 90%+
- ReportScheduler: 90%+
- ReportHistory: 85%+
- ReportDistribution: 85%+
- advancedReportingApi: 95%+

Store/Hook Test Coverage:
- Hook functions: 90%+
- API service functions: 95%+
- Error handling: 100%
```

### Integration Test Template

```typescript
describe('Advanced Reporting - Integration Tests', () => {
  beforeEach(() => {
    // Setup test data
    // Clear any previous state
  });

  describe('Report Configuration Workflow', () => {
    it('should create, edit, and delete a report', async () => {
      // Create
      // Verify creation
      // Edit
      // Verify edit
      // Delete
      // Verify deletion
    });
  });

  describe('Report Scheduling Workflow', () => {
    it('should create schedules and verify execution', async () => {
      // Create schedule
      // Execute
      // Verify execution in history
    });
  });

  describe('Report Distribution Workflow', () => {
    it('should configure and test distribution channels', async () => {
      // Create channels
      // Test each channel
      // Verify test results
    });
  });

  describe('Error Handling', () => {
    it('should handle API errors gracefully', async () => {
      // Simulate API error
      // Verify error message
      // Verify UI recovery
    });
  });
});
```

---

## Test Report Template

### Test Execution Summary

| Component | Tests | Passed | Failed | Pass Rate |
|-----------|-------|--------|--------|-----------|
| Report Builder | 10 | 10 | 0 | 100% |
| Report Scheduler | 8 | 8 | 0 | 100% |
| Report History | 6 | 6 | 0 | 100% |
| Report Distribution | 9 | 9 | 0 | 100% |
| End-to-End Workflow | 1 | 1 | 0 | 100% |
| Error Handling | 5 | 5 | 0 | 100% |
| Performance | 4 | 4 | 0 | 100% |
| **TOTAL** | **43** | **43** | **0** | **100%** |

---

## Known Issues & Limitations

### Phase 1 (MVP)

1. **Email Distribution**: Requires SMTP server configuration (backend implementation)
2. **SFTP Distribution**: Requires SFTP server setup (backend implementation)
3. **Webhook Distribution**: Requires webhook implementation (backend implementation)
4. **Report Templates**: Template management UI ready, template API pending
5. **Archive Management**: Archive display ready, long-term retention policies pending

### Future Enhancements (Phase 2+)

1. Report analytics dashboard
2. Advanced filtering with custom date ranges
3. Report comparison capabilities
4. Scheduled report consolidation
5. Report versioning and rollback
6. Advanced Excel formatting options
7. Power BI / Tableau integration

---

## Regression Test Checklist

- [ ] Report creation flow works end-to-end
- [ ] Report editing preserves all settings
- [ ] Report deletion works with confirmation
- [ ] Schedule creation with all frequency types
- [ ] Schedule enable/disable toggle
- [ ] Schedule edit functionality
- [ ] Schedule deletion
- [ ] Manual report execution
- [ ] Execution history display
- [ ] Report download functionality
- [ ] Distribution channel creation (all types)
- [ ] Distribution channel testing
- [ ] Distribution channel editing
- [ ] Distribution channel deletion
- [ ] Navigation between tabs
- [ ] Error messages display correctly
- [ ] Loading states show appropriately
- [ ] API errors handled gracefully
- [ ] Form validation works
- [ ] Data persists across navigation

---

## Sign-Off Criteria

✅ All 43+ test scenarios pass
✅ No TypeScript compilation errors
✅ No console errors or warnings
✅ Performance metrics acceptable (<2s load time)
✅ All components render correctly
✅ API integration complete
✅ Error handling verified
✅ No accessibility issues
✅ Responsive design verified
✅ Browser compatibility confirmed (Chrome, Firefox, Safari, Edge)

---

**Test Report Generated**: November 7, 2025
**Testing Phase**: Phase 3, Task 3.5 - Integration Testing
**Status**: Ready for Execution

