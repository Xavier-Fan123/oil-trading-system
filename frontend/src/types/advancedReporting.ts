/**
 * Advanced Reporting and Export System Types
 * Phase 3, Task 3: Advanced Export/Reporting
 */

// Report Configuration Types
export enum ReportType {
  ContractExecution = 'contract-execution',
  SettlementSummary = 'settlement-summary',
  PaymentStatus = 'payment-status',
  RiskAnalysis = 'risk-analysis',
  CustomReport = 'custom-report',
}

export enum ReportFormat {
  CSV = 'csv',
  Excel = 'excel',
  PDF = 'pdf',
  JSON = 'json',
}

export enum ScheduleFrequency {
  Once = 'once',
  Daily = 'daily',
  Weekly = 'weekly',
  Monthly = 'monthly',
  Quarterly = 'quarterly',
  Annually = 'annually',
}

export enum ReportStatus {
  Draft = 'draft',
  Scheduled = 'scheduled',
  Running = 'running',
  Completed = 'completed',
  Failed = 'failed',
  Archived = 'archived',
}

// Report Configuration
export interface ReportConfiguration {
  id?: string;
  name: string;
  description?: string;
  reportType: ReportType;
  filters: ReportFilter;
  columns: ReportColumn[];
  format: ReportFormat;
  includeMetadata: boolean;
  createdDate?: Date;
  lastModifiedDate?: Date;
}

export interface ReportFilter {
  dateRange?: {
    startDate: Date;
    endDate: Date;
  };
  contractType?: 'Purchase' | 'Sales';
  executionStatus?: string;
  tradingPartnerId?: string;
  productId?: string;
  customFilters?: Record<string, any>;
}

export interface ReportColumn {
  id: string;
  name: string;
  displayName: string;
  dataType: 'string' | 'number' | 'date' | 'currency' | 'percentage';
  visible: boolean;
  sortable: boolean;
  filterable: boolean;
  format?: string; // e.g., currency format, date format
}

// Report Scheduling
export interface ReportSchedule {
  id?: string;
  reportConfigId: string;
  enabled: boolean;
  frequency: ScheduleFrequency;
  dayOfWeek?: number; // 0-6 for weekly (Sunday = 0)
  dayOfMonth?: number; // 1-31 for monthly
  time: string; // HH:mm format
  timezone?: string;
  nextRunDate?: Date;
  lastRunDate?: Date;
  createdDate?: Date;
}

// Report Distribution
export interface ReportDistribution {
  id: string;
  reportConfigId: string;
  channelName: string;
  channelType: 'Email' | 'SFTP' | 'Webhook';
  channelConfiguration: string; // JSON string with channel-specific config
  isEnabled: boolean;
  lastTestedDate?: Date;
  lastTestStatus?: 'Success' | 'Failed';
  lastTestMessage?: string;
  createdDate?: Date;
  createdBy?: string;
  updatedDate?: Date;
  updatedBy?: string;
}

export interface DistributionChannel {
  type: 'Email' | 'SFTP' | 'Webhook';
  enabled: boolean;
  configuration: Record<string, any>;
}

export interface ReportRecipient {
  email?: string;
  name?: string;
  department?: string;
  receiveAlert?: boolean; // Send alert if report fails
}

// Report Execution & History
export interface ReportExecutionHistory {
  id: string;
  reportConfigId: string;
  scheduleId?: string;
  executionDate: Date;
  completionDate?: Date;
  status: ReportStatus;
  recordsProcessed: number;
  downloadUrl?: string;
  errorMessage?: string;
  executionDurationMs: number;
  createdBy: string;
  fileSize?: number;
  fileName?: string;
}

export interface ReportTemplate {
  id: string;
  name: string;
  description: string;
  category: string; // 'Standard', 'Financial', 'Operations', 'Risk', 'Custom'
  configuration: ReportConfiguration;
  isPublic: boolean;
  usageCount: number;
  createdBy: string;
  createdDate: Date;
  previewUrl?: string; // URL to template preview image
}

// Report Archive
export interface ReportArchive {
  id: string;
  reportConfigId: string;
  executionId: string;
  archiveDate: Date;
  retentionDays: number;
  expiryDate: Date;
  storageLocation: string;
  isCompressed: boolean;
  fileSize: number;
  accessLog?: ArchiveAccessLog[];
}

export interface ArchiveAccessLog {
  accessDate: Date;
  accessedBy: string;
  action: 'view' | 'download' | 'delete';
  ipAddress?: string;
}

// Report Analytics
export interface ReportAnalytics {
  totalReportsGenerated: number;
  totalDataPoints: number;
  averageGenerationTime: number; // in ms
  mostUsedReports: ReportUsageStats[];
  formatDistribution: {
    [key in ReportFormat]?: number;
  };
  frequencyDistribution: {
    [key in ScheduleFrequency]?: number;
  };
  failureRate: number;
  averageFileSize: number;
}

export interface ReportUsageStats {
  reportId: string;
  reportName: string;
  usageCount: number;
  lastUsed: Date;
  averageDownloadTime: number;
}

// Report Builder State
export interface ReportBuilderState {
  step: number; // 0-4 for multi-step form
  configuration: ReportConfiguration;
  schedule?: ReportSchedule;
  distribution?: ReportDistribution;
  preview?: ReportPreview;
  errors: Record<string, string>;
  isLoading: boolean;
}

export interface ReportPreview {
  totalRecords: number;
  sampleData: Record<string, any>[];
  columns: ReportColumn[];
  generatedDate: Date;
}

// Export Options
export interface AdvancedExportOptions {
  format: ReportFormat;
  includeHeaders: boolean;
  includeFilters: boolean;
  includeMetadata: boolean;
  compression?: 'none' | 'gzip' | 'zip';
  encoding?: string; // 'utf-8', 'iso-8859-1', etc.
  delimiter?: string; // for CSV
  pageSize?: number; // for pagination
  fileName?: string;
  customProperties?: Record<string, any>;
}

// Report Query Result
export interface ReportQueryResult<T = any> {
  data: T[];
  totalCount: number;
  generationTime: number; // in ms
  filters: ReportFilter;
  columns: ReportColumn[];
  metadata: {
    generatedDate: Date;
    generatedBy: string;
    dataSource: string;
    version: string;
  };
}

// Paged result for reports
export interface PagedReportResult<T = any> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Constants
export const REPORT_TYPE_LABELS: Record<ReportType, string> = {
  [ReportType.ContractExecution]: 'Contract Execution Report',
  [ReportType.SettlementSummary]: 'Settlement Summary',
  [ReportType.PaymentStatus]: 'Payment Status Report',
  [ReportType.RiskAnalysis]: 'Risk Analysis Report',
  [ReportType.CustomReport]: 'Custom Report',
};

export const REPORT_FORMAT_LABELS: Record<ReportFormat, string> = {
  [ReportFormat.CSV]: 'CSV File',
  [ReportFormat.Excel]: 'Excel Workbook',
  [ReportFormat.PDF]: 'PDF Document',
  [ReportFormat.JSON]: 'JSON Data',
};

export const SCHEDULE_FREQUENCY_LABELS: Record<ScheduleFrequency, string> = {
  [ScheduleFrequency.Once]: 'Once',
  [ScheduleFrequency.Daily]: 'Every Day',
  [ScheduleFrequency.Weekly]: 'Every Week',
  [ScheduleFrequency.Monthly]: 'Every Month',
  [ScheduleFrequency.Quarterly]: 'Every Quarter',
  [ScheduleFrequency.Annually]: 'Every Year',
};

export const REPORT_STATUS_LABELS: Record<ReportStatus, string> = {
  [ReportStatus.Draft]: 'Draft',
  [ReportStatus.Scheduled]: 'Scheduled',
  [ReportStatus.Running]: 'Running',
  [ReportStatus.Completed]: 'Completed',
  [ReportStatus.Failed]: 'Failed',
  [ReportStatus.Archived]: 'Archived',
};

// Sample report templates
export const SAMPLE_REPORT_TEMPLATES: ReportTemplate[] = [
  {
    id: 'template-1',
    name: 'Daily Contract Status',
    description: 'Overview of all contracts and their execution status',
    category: 'Standard',
    configuration: {
      name: 'Daily Contract Status',
      reportType: ReportType.ContractExecution,
      filters: {
        dateRange: {
          startDate: new Date(),
          endDate: new Date(),
        },
      },
      columns: [
        {
          id: 'contractNumber',
          name: 'contractNumber',
          displayName: 'Contract Number',
          dataType: 'string',
          visible: true,
          sortable: true,
          filterable: true,
        },
        {
          id: 'contractStatus',
          name: 'contractStatus',
          displayName: 'Status',
          dataType: 'string',
          visible: true,
          sortable: true,
          filterable: true,
        },
        {
          id: 'executionPercentage',
          name: 'executionPercentage',
          displayName: 'Execution %',
          dataType: 'percentage',
          visible: true,
          sortable: true,
          filterable: true,
        },
      ],
      format: ReportFormat.Excel,
      includeMetadata: true,
    },
    isPublic: true,
    usageCount: 156,
    createdBy: 'System',
    createdDate: new Date('2025-01-01'),
  },
];
