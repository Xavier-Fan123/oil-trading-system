import axios from 'axios';
import { ContractExecutionReportDto } from '@/types/reports';
import { PagedResult } from '@/types/common';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const contractExecutionReportApi = {
  /**
   * Get a single contract execution report by contract ID
   */
  async getContractReport(
    contractId: string,
    isPurchaseContract: boolean = true
  ): Promise<ContractExecutionReportDto | null> {
    try {
      const response = await api.get<ContractExecutionReportDto>(
        `/contract-execution-reports/${contractId}`,
        {
          params: {
            isPurchaseContract,
          },
        }
      );
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Get paginated list of contract execution reports with filtering
   */
  async getContractReports(
    pageNumber: number = 1,
    pageSize: number = 10,
    contractType?: string,
    executionStatus?: string,
    fromDate?: Date,
    toDate?: Date,
    tradingPartnerId?: string,
    productId?: string,
    sortBy: string = 'ReportGeneratedDate',
    sortDescending: boolean = true
  ): Promise<PagedResult<ContractExecutionReportDto>> {
    const params: any = {
      pageNumber,
      pageSize,
      sortBy,
      sortDescending,
    };

    if (contractType) params.contractType = contractType;
    if (executionStatus) params.executionStatus = executionStatus;
    if (fromDate) params.fromDate = fromDate.toISOString();
    if (toDate) params.toDate = toDate.toISOString();
    if (tradingPartnerId) params.tradingPartnerId = tradingPartnerId;
    if (productId) params.productId = productId;

    const response = await api.get<PagedResult<ContractExecutionReportDto>>(
      '/contract-execution-reports',
      { params }
    );
    return response.data;
  },

  /**
   * Get execution reports for a specific trading partner
   */
  async getTradingPartnerReports(
    tradingPartnerId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ContractExecutionReportDto>> {
    const response = await api.get<PagedResult<ContractExecutionReportDto>>(
      `/contract-execution-reports/trading-partner/${tradingPartnerId}`,
      {
        params: { pageNumber, pageSize },
      }
    );
    return response.data;
  },

  /**
   * Get execution reports for a specific product
   */
  async getProductReports(
    productId: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ContractExecutionReportDto>> {
    const response = await api.get<PagedResult<ContractExecutionReportDto>>(
      `/contract-execution-reports/product/${productId}`,
      {
        params: { pageNumber, pageSize },
      }
    );
    return response.data;
  },

  /**
   * Get execution reports filtered by execution status
   */
  async getReportsByStatus(
    executionStatus: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ContractExecutionReportDto>> {
    const response = await api.get<PagedResult<ContractExecutionReportDto>>(
      `/contract-execution-reports/status/${executionStatus}`,
      {
        params: { pageNumber, pageSize },
      }
    );
    return response.data;
  },

  /**
   * Get execution reports for a date range
   */
  async getReportsByDateRange(
    fromDate: Date,
    toDate: Date,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Promise<PagedResult<ContractExecutionReportDto>> {
    const response = await api.get<PagedResult<ContractExecutionReportDto>>(
      '/contract-execution-reports/date-range',
      {
        params: {
          fromDate: fromDate.toISOString(),
          toDate: toDate.toISOString(),
          pageNumber,
          pageSize,
        },
      }
    );
    return response.data;
  },

  /**
   * Export reports to CSV format
   */
  async exportReportsToCsv(
    contractType?: string,
    executionStatus?: string,
    fromDate?: Date,
    toDate?: Date,
    tradingPartnerId?: string,
    productId?: string
  ): Promise<Blob> {
    const params: any = {};

    if (contractType) params.contractType = contractType;
    if (executionStatus) params.executionStatus = executionStatus;
    if (fromDate) params.fromDate = fromDate.toISOString();
    if (toDate) params.toDate = toDate.toISOString();
    if (tradingPartnerId) params.tradingPartnerId = tradingPartnerId;
    if (productId) params.productId = productId;

    const response = await api.get('/contract-execution-reports/export/csv', {
      params,
      responseType: 'blob',
    });
    return response.data;
  },

  /**
   * Export reports to Excel format
   */
  async exportReportsToExcel(
    contractType?: string,
    executionStatus?: string,
    fromDate?: Date,
    toDate?: Date,
    tradingPartnerId?: string,
    productId?: string
  ): Promise<Blob> {
    const params: any = {};

    if (contractType) params.contractType = contractType;
    if (executionStatus) params.executionStatus = executionStatus;
    if (fromDate) params.fromDate = fromDate.toISOString();
    if (toDate) params.toDate = toDate.toISOString();
    if (tradingPartnerId) params.tradingPartnerId = tradingPartnerId;
    if (productId) params.productId = productId;

    const response = await api.get('/contract-execution-reports/export/excel', {
      params,
      responseType: 'blob',
    });
    return response.data;
  },

  /**
   * Export reports to PDF format
   */
  async exportReportsToPdf(
    contractType?: string,
    executionStatus?: string,
    fromDate?: Date,
    toDate?: Date,
    tradingPartnerId?: string,
    productId?: string
  ): Promise<Blob> {
    const params: any = {};

    if (contractType) params.contractType = contractType;
    if (executionStatus) params.executionStatus = executionStatus;
    if (fromDate) params.fromDate = fromDate.toISOString();
    if (toDate) params.toDate = toDate.toISOString();
    if (tradingPartnerId) params.tradingPartnerId = tradingPartnerId;
    if (productId) params.productId = productId;

    const response = await api.get('/contract-execution-reports/export/pdf', {
      params,
      responseType: 'blob',
    });
    return response.data;
  },
};
