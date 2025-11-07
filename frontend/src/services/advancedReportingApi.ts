import axios, { AxiosProgressEvent } from 'axios';
import {
  ReportConfiguration,
  ReportSchedule,
  ReportDistribution,
  ReportExecutionHistory,
  ReportTemplate,
  ReportArchive,
  AdvancedExportOptions,
  PagedReportResult,
  ReportAnalytics,
  ScheduleFrequency,
  ReportFormat,
  ReportStatus,
} from '@/types/advancedReporting';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 60000, // 60s for large reports
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Advanced Reporting API Service
 * Handles report configuration, scheduling, distribution, and archival
 */
export const advancedReportingApi = {
  // ========== Report Configuration ==========

  /**
   * Create a new report configuration
   */
  createReportConfig: async (config: ReportConfiguration): Promise<ReportConfiguration> => {
    const response = await api.post('/advanced-reports/configurations', config);
    return response.data;
  },

  /**
   * Get report configuration by ID
   */
  getReportConfig: async (configId: string): Promise<ReportConfiguration> => {
    const response = await api.get(`/advanced-reports/configurations/${configId}`);
    return response.data;
  },

  /**
   * List all report configurations with paging
   */
  listReportConfigs: async (
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Promise<PagedReportResult<ReportConfiguration>> => {
    const response = await api.get('/advanced-reports/configurations', {
      params: {
        pageNumber,
        pageSize,
        searchTerm,
      },
    });
    return response.data;
  },

  /**
   * Update report configuration
   */
  updateReportConfig: async (
    configId: string,
    config: ReportConfiguration
  ): Promise<ReportConfiguration> => {
    const response = await api.put(`/advanced-reports/configurations/${configId}`, config);
    return response.data;
  },

  /**
   * Delete report configuration
   */
  deleteReportConfig: async (configId: string): Promise<boolean> => {
    const response = await api.delete(`/advanced-reports/configurations/${configId}`);
    return response.status === 200 || response.status === 204;
  },

  /**
   * Clone/duplicate a report configuration
   */
  cloneReportConfig: async (configId: string, newName: string): Promise<ReportConfiguration> => {
    const response = await api.post(`/advanced-reports/configurations/${configId}/clone`, {
      newName,
    });
    return response.data;
  },

  /**
   * Preview report data without saving
   */
  previewReport: async (config: ReportConfiguration): Promise<any> => {
    const response = await api.post('/advanced-reports/preview', config);
    return response.data;
  },

  // ========== Report Scheduling ==========

  /**
   * Create report schedule
   */
  createReportSchedule: async (schedule: ReportSchedule): Promise<ReportSchedule> => {
    const response = await api.post('/advanced-reports/schedules', schedule);
    return response.data;
  },

  /**
   * Get report schedule
   */
  getReportSchedule: async (scheduleId: string): Promise<ReportSchedule> => {
    const response = await api.get(`/advanced-reports/schedules/${scheduleId}`);
    return response.data;
  },

  /**
   * List schedules for a report configuration
   */
  listReportSchedules: async (configId: string): Promise<ReportSchedule[]> => {
    const response = await api.get('/advanced-reports/schedules', {
      params: { configId },
    });
    return response.data;
  },

  /**
   * Update report schedule
   */
  updateReportSchedule: async (
    scheduleId: string,
    schedule: ReportSchedule
  ): Promise<ReportSchedule> => {
    const response = await api.put(`/advanced-reports/schedules/${scheduleId}`, schedule);
    return response.data;
  },

  /**
   * Delete report schedule
   */
  deleteReportSchedule: async (scheduleId: string): Promise<boolean> => {
    const response = await api.delete(`/advanced-reports/schedules/${scheduleId}`);
    return response.status === 200 || response.status === 204;
  },

  /**
   * Enable/disable schedule
   */
  toggleReportSchedule: async (scheduleId: string, enabled: boolean): Promise<ReportSchedule> => {
    const response = await api.patch(`/advanced-reports/schedules/${scheduleId}`, { enabled });
    return response.data;
  },

  // ========== Report Distribution ==========

  /**
   * Create distribution configuration
   */
  createDistribution: async (
    distribution: ReportDistribution
  ): Promise<ReportDistribution> => {
    const response = await api.post('/advanced-reports/distributions', distribution);
    return response.data;
  },

  /**
   * Get distribution configuration
   */
  getDistribution: async (distributionId: string): Promise<ReportDistribution> => {
    const response = await api.get(`/advanced-reports/distributions/${distributionId}`);
    return response.data;
  },

  /**
   * Update distribution configuration
   */
  updateDistribution: async (
    distributionId: string,
    distribution: ReportDistribution
  ): Promise<ReportDistribution> => {
    const response = await api.put(`/advanced-reports/distributions/${distributionId}`, distribution);
    return response.data;
  },

  /**
   * Delete distribution configuration
   */
  deleteDistribution: async (distributionId: string): Promise<boolean> => {
    const response = await api.delete(`/advanced-reports/distributions/${distributionId}`);
    return response.status === 200 || response.status === 204;
  },

  /**
   * Test email delivery
   */
  testEmailDistribution: async (
    distributionId: string,
    testEmail: string
  ): Promise<{ success: boolean; message: string }> => {
    const response = await api.post(
      `/advanced-reports/distributions/${distributionId}/test-email`,
      { testEmail }
    );
    return response.data;
  },

  // ========== Report Execution & History ==========

  /**
   * Execute report manually
   */
  executeReport: async (
    configId: string,
    onProgress?: (event: AxiosProgressEvent) => void
  ): Promise<ReportExecutionHistory> => {
    const response = await api.post(`/advanced-reports/execute`, { configId }, {
      onDownloadProgress: onProgress,
    });
    return response.data;
  },

  /**
   * Get execution history
   */
  getExecutionHistory: async (configId: string): Promise<ReportExecutionHistory[]> => {
    const response = await api.get('/advanced-reports/execution-history', {
      params: { configId },
    });
    return response.data;
  },

  /**
   * Get execution history with paging
   */
  getExecutionHistoryPaged: async (
    configId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedReportResult<ReportExecutionHistory>> => {
    const response = await api.get('/advanced-reports/execution-history/paged', {
      params: { configId, pageNumber, pageSize },
    });
    return response.data;
  },

  /**
   * Get single execution record
   */
  getExecution: async (executionId: string): Promise<ReportExecutionHistory> => {
    const response = await api.get(`/advanced-reports/executions/${executionId}`);
    return response.data;
  },

  /**
   * Download report file
   */
  downloadReport: async (
    executionId: string,
    onProgress?: (event: AxiosProgressEvent) => void
  ): Promise<Blob> => {
    const response = await api.get(
      `/advanced-reports/executions/${executionId}/download`,
      {
        responseType: 'blob',
        onDownloadProgress: onProgress,
      }
    );
    return response.data;
  },

  /**
   * Retry failed report execution
   */
  retryExecution: async (executionId: string): Promise<ReportExecutionHistory> => {
    const response = await api.post(`/advanced-reports/executions/${executionId}/retry`);
    return response.data;
  },

  /**
   * Get report statistics/analytics
   */
  getReportAnalytics: async (): Promise<ReportAnalytics> => {
    const response = await api.get('/advanced-reports/analytics');
    return response.data;
  },

  // ========== Report Templates ==========

  /**
   * Get available report templates
   */
  getReportTemplates: async (
    category?: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedReportResult<ReportTemplate>> => {
    const response = await api.get('/advanced-reports/templates', {
      params: { category, pageNumber, pageSize },
    });
    return response.data;
  },

  /**
   * Get single template
   */
  getReportTemplate: async (templateId: string): Promise<ReportTemplate> => {
    const response = await api.get(`/advanced-reports/templates/${templateId}`);
    return response.data;
  },

  /**
   * Create report from template
   */
  createReportFromTemplate: async (templateId: string, overrides?: Partial<ReportConfiguration>): Promise<ReportConfiguration> => {
    const response = await api.post(
      `/advanced-reports/templates/${templateId}/create-report`,
      overrides || {}
    );
    return response.data;
  },

  /**
   * Save custom template
   */
  saveAsTemplate: async (
    config: ReportConfiguration,
    category: string,
    isPublic: boolean
  ): Promise<ReportTemplate> => {
    const response = await api.post('/advanced-reports/templates', {
      config,
      category,
      isPublic,
    });
    return response.data;
  },

  // ========== Report Archive ==========

  /**
   * Get archived reports
   */
  getArchivedReports: async (
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedReportResult<ReportArchive>> => {
    const response = await api.get('/advanced-reports/archive', {
      params: { pageNumber, pageSize },
    });
    return response.data;
  },

  /**
   * Get archive record
   */
  getArchiveRecord: async (archiveId: string): Promise<ReportArchive> => {
    const response = await api.get(`/advanced-reports/archive/${archiveId}`);
    return response.data;
  },

  /**
   * Retrieve archived report
   */
  retrieveArchivedReport: async (archiveId: string): Promise<Blob> => {
    const response = await api.get(`/advanced-reports/archive/${archiveId}/retrieve`, {
      responseType: 'blob',
    });
    return response.data;
  },

  /**
   * Configure archival policy
   */
  configureArchivePolicy: async (configId: string, retentionDays: number): Promise<any> => {
    const response = await api.post(
      `/advanced-reports/configurations/${configId}/archive-policy`,
      { retentionDays }
    );
    return response.data;
  },

  /**
   * Get archive access log
   */
  getArchiveAccessLog: async (archiveId: string): Promise<any[]> => {
    const response = await api.get(`/advanced-reports/archive/${archiveId}/access-log`);
    return response.data;
  },

  /**
   * Delete archived report
   */
  deleteArchivedReport: async (archiveId: string): Promise<boolean> => {
    const response = await api.delete(`/advanced-reports/archive/${archiveId}`);
    return response.status === 200 || response.status === 204;
  },

  // ========== Distribution Configuration ==========

  /**
   * Get distribution channels for a report
   */
  getDistributions: async (configId: string): Promise<ReportDistribution[]> => {
    const response = await api.get(`/advanced-reports/configurations/${configId}/distributions`);
    return response.data;
  },

  /**
   * Create a distribution channel
   */
  createDistribution: async (
    configId: string,
    distribution: {
      channelName: string;
      channelType: string;
      channelConfiguration: string;
      isEnabled: boolean;
    }
  ): Promise<ReportDistribution> => {
    const response = await api.post(
      `/advanced-reports/configurations/${configId}/distributions`,
      distribution
    );
    return response.data;
  },

  /**
   * Update a distribution channel
   */
  updateDistribution: async (
    configId: string,
    channelId: string,
    distribution: {
      channelName: string;
      channelType: string;
      channelConfiguration: string;
      isEnabled: boolean;
    }
  ): Promise<ReportDistribution> => {
    const response = await api.put(
      `/advanced-reports/configurations/${configId}/distributions/${channelId}`,
      distribution
    );
    return response.data;
  },

  /**
   * Delete a distribution channel
   */
  deleteDistribution: async (configId: string, channelId: string): Promise<void> => {
    await api.delete(`/advanced-reports/configurations/${configId}/distributions/${channelId}`);
  },

  /**
   * Test a distribution channel
   */
  testDistribution: async (configId: string, channelId: string): Promise<any> => {
    const response = await api.post(
      `/advanced-reports/configurations/${configId}/distributions/${channelId}/test`
    );
    return response.data;
  },

  // ========== Bulk & Advanced Operations ==========

  /**
   * Bulk schedule reports
   */
  bulkScheduleReports: async (configIds: string[], schedule: ReportSchedule): Promise<any> => {
    const response = await api.post('/advanced-reports/bulk-schedule', {
      configIds,
      schedule,
    });
    return response.data;
  },

  /**
   * Bulk export reports
   */
  bulkExportReports: async (
    executionIds: string[],
    format: ReportFormat,
    options?: AdvancedExportOptions
  ): Promise<Blob> => {
    const response = await api.post(
      '/advanced-reports/bulk-export',
      { executionIds, format, options },
      { responseType: 'blob' }
    );
    return response.data;
  },

  /**
   * Delete old executions (cleanup)
   */
  cleanupOldExecutions: async (configId: string, retentionDays: number): Promise<any> => {
    const response = await api.post(
      `/advanced-reports/configurations/${configId}/cleanup`,
      { retentionDays }
    );
    return response.data;
  },
};

export default advancedReportingApi;
