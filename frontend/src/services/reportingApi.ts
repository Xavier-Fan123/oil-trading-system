import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 60000,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Report Configuration DTO
 */
export interface ReportConfiguration {
  id?: string;
  name: string;
  description?: string;
  reportType: string;
  filters?: Record<string, unknown>;
  columns?: string[];
  exportFormat: string;
  includeMetadata: boolean;
  isActive: boolean;
  createdDate?: string;
  createdBy?: string;
  updatedDate?: string;
  updatedBy?: string;
}

/**
 * Report Execution DTO
 */
export interface ReportExecution {
  id?: string;
  reportConfigId: string;
  executionStartTime?: string;
  executionEndTime?: string;
  status: string;
  recordsProcessed?: number;
  errorMessage?: string;
  durationMilliseconds?: number;
  fileSizeBytes?: number;
  outputFileName?: string;
  outputFilePath?: string;
  executedBy?: string;
}

/**
 * Report Distribution DTO
 */
export interface ReportDistribution {
  id?: string;
  reportConfigId: string;
  channelName: string;
  channelType: string;
  channelConfiguration?: Record<string, unknown>;
  isEnabled: boolean;
  lastTestedDate?: string;
  lastTestStatus?: string;
  createdDate?: string;
}

/**
 * Report Archive DTO
 */
export interface ReportArchive {
  id?: string;
  executionId: string;
  archiveDate?: string;
  retentionDays: number;
  expiryDate?: string;
  storageLocation?: string;
  isCompressed: boolean;
  fileSize?: number;
}

/**
 * Paged Result DTO
 */
export interface PagedResult<T> {
  items: T[];
  pageNum: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/**
 * Reporting API Service
 * Handles report configuration, execution, distribution, and archival
 */
export const reportingApi = {
  // ========== Report Configuration ==========

  /**
   * List all report configurations with pagination
   */
  listConfigurations: async (
    pageNum: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ReportConfiguration>> => {
    const response = await api.get('/report-configurations', {
      params: { pageNum, pageSize },
    });
    return response.data;
  },

  /**
   * Get a specific report configuration by ID
   */
  getConfiguration: async (id: string): Promise<ReportConfiguration> => {
    const response = await api.get(`/report-configurations/${id}`);
    return response.data;
  },

  /**
   * Create a new report configuration
   */
  createConfiguration: async (config: ReportConfiguration): Promise<ReportConfiguration> => {
    const response = await api.post('/report-configurations', config);
    return response.data;
  },

  /**
   * Update an existing report configuration
   */
  updateConfiguration: async (
    id: string,
    config: Partial<ReportConfiguration>
  ): Promise<ReportConfiguration> => {
    const response = await api.put(`/report-configurations/${id}`, config);
    return response.data;
  },

  /**
   * Delete a report configuration
   */
  deleteConfiguration: async (id: string): Promise<void> => {
    await api.delete(`/report-configurations/${id}`);
  },

  // ========== Report Execution ==========

  /**
   * List all report executions with pagination
   */
  listExecutions: async (
    pageNum: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ReportExecution>> => {
    const response = await api.get('/report-executions', {
      params: { pageNum, pageSize },
    });
    return response.data;
  },

  /**
   * Get a specific report execution by ID
   */
  getExecution: async (id: string): Promise<ReportExecution> => {
    const response = await api.get(`/report-executions/${id}`);
    return response.data;
  },

  /**
   * Execute a report based on configuration
   */
  executeReport: async (request: {
    reportConfigurationId: string;
    parameters?: Record<string, unknown>;
    outputFormat?: string;
    isScheduled?: boolean;
  }): Promise<ReportExecution> => {
    const response = await api.post('/report-executions/execute', request);
    return response.data;
  },

  /**
   * Download the result file of a report execution
   */
  downloadExecution: async (id: string): Promise<Blob> => {
    const response = await api.post(
      `/report-executions/${id}/download`,
      {},
      { responseType: 'blob' }
    );
    return response.data;
  },

  // ========== Report Distribution ==========

  /**
   * List all distribution configurations with pagination
   */
  listDistributions: async (
    pageNum: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ReportDistribution>> => {
    const response = await api.get('/report-distributions', {
      params: { pageNum, pageSize },
    });
    return response.data;
  },

  /**
   * Get a specific distribution configuration by ID
   */
  getDistribution: async (id: string): Promise<ReportDistribution> => {
    const response = await api.get(`/report-distributions/${id}`);
    return response.data;
  },

  /**
   * Create a new distribution channel configuration
   */
  createDistribution: async (distribution: ReportDistribution): Promise<ReportDistribution> => {
    const response = await api.post('/report-distributions', distribution);
    return response.data;
  },

  /**
   * Update an existing distribution configuration
   */
  updateDistribution: async (
    id: string,
    distribution: Partial<ReportDistribution>
  ): Promise<ReportDistribution> => {
    const response = await api.put(`/report-distributions/${id}`, distribution);
    return response.data;
  },

  /**
   * Delete a distribution configuration
   */
  deleteDistribution: async (id: string): Promise<void> => {
    await api.delete(`/report-distributions/${id}`);
  },

  // ========== Report Archive ==========

  /**
   * List all archived reports with pagination
   */
  listArchives: async (
    pageNum: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ReportArchive>> => {
    const response = await api.get('/report-archives', {
      params: { pageNum, pageSize },
    });
    return response.data;
  },

  /**
   * Get a specific archived report by ID
   */
  getArchive: async (id: string): Promise<ReportArchive> => {
    const response = await api.get(`/report-archives/${id}`);
    return response.data;
  },

  /**
   * Download an archived report file
   */
  downloadArchive: async (id: string): Promise<Blob> => {
    const response = await api.post(
      `/report-archives/${id}/download`,
      {},
      { responseType: 'blob' }
    );
    return response.data;
  },

  /**
   * Restore an archived report
   */
  restoreArchive: async (id: string): Promise<ReportExecution> => {
    const response = await api.post(`/report-archives/${id}/restore`, {});
    return response.data;
  },

  /**
   * Delete an archived report permanently
   */
  deleteArchive: async (id: string): Promise<void> => {
    await api.delete(`/report-archives/${id}`);
  },
};

export default reportingApi;
