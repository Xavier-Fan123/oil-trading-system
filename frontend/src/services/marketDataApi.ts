import api from './api';
import type {
  MarketDataUploadResultDto,
  LatestPricesDto,
  MarketPriceDto,
  ImportResult,
  DataImportStatus,
  FileType,
  DeleteMarketDataResultDto
} from '@/types/marketData';
import { formatApiDate, parseApiDateFields } from '@/utils/dateUtils';

export const marketDataApi = {
  // Upload market data file
  uploadFile: async (file: File, fileType: FileType, overwriteExisting = false): Promise<MarketDataUploadResultDto> => {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('FileType', fileType);
    formData.append('OverwriteExisting', overwriteExisting.toString());

    const response = await api.post('/market-data/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 300000, // 5 minutes for large files
    });

    return response.data;
  },

  // Get latest prices
  getLatestPrices: async (): Promise<LatestPricesDto> => {
    const response = await api.get('/market-data/latest');
    return response.data;
  },

  // Get price history for a specific product
  getPriceHistory: async (
    productCode: string,
    startDate?: string | Date,
    endDate?: string | Date
  ): Promise<MarketPriceDto[]> => {
    const params = new URLSearchParams();
    if (startDate) {
      const formattedDate = typeof startDate === 'string' ? startDate : formatApiDate(startDate);
      if (formattedDate) params.append('startDate', formattedDate);
    }
    if (endDate) {
      const formattedDate = typeof endDate === 'string' ? endDate : formatApiDate(endDate);
      if (formattedDate) params.append('endDate', formattedDate);
    }
    
    const query = params.toString() ? `?${params.toString()}` : '';
    const response = await api.get(`/market-data/history/${productCode}${query}`);
    
    // Parse date fields in the response
    const result = response.data;
    if (Array.isArray(result)) {
      return result.map(item => parseApiDateFields(item, ['date', 'timestamp', 'updatedAt']));
    }
    
    return result;
  },

  // CSV Import endpoints
  importSpotPrices: async (file: File, fileType: FileType): Promise<ImportResult> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('fileType', fileType);

    const response = await api.post('/CsvImport/spot-prices', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 300000,
    });

    return response.data;
  },

  importFuturesPrices: async (file: File, fileType: FileType): Promise<ImportResult> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('fileType', fileType);

    const response = await api.post('/CsvImport/futures-prices', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 300000,
    });

    return response.data;
  },

  // Local file import (for testing/development)
  importLocalFutures: async (filePath: string): Promise<ImportResult> => {
    const response = await api.post('/CsvImport/import-local-futures', {
      filePath: filePath
    });
    return response.data;
  },

  importLocalSpot: async (filePath: string): Promise<ImportResult> => {
    const response = await api.post('/CsvImport/import-local-spot', {
      filePath: filePath
    });
    return response.data;
  },

  // Data import endpoints
  importIceSettlement: async (filePath?: string): Promise<void> => {
    const params = filePath ? `?filePath=${encodeURIComponent(filePath)}` : '';
    await api.post(`/DataImport/ice-settlement-prices${params}`);
  },

  importPaperTradingData: async (filePath?: string): Promise<void> => {
    const params = filePath ? `?filePath=${encodeURIComponent(filePath)}` : '';
    await api.post(`/DataImport/paper-trading-data${params}`);
  },

  bulkImport: async (): Promise<void> => {
    await api.post('/DataImport/bulk-import');
  },

  getImportStatus: async (): Promise<DataImportStatus> => {
    const response = await api.get('/DataImport/status');
    return response.data;
  },

  // Utility functions
  validateFile: (file: File): { isValid: boolean; error?: string } => {
    console.log('Validating file:', {
      name: file.name,
      type: file.type,
      size: file.size,
      hasName: !!file.name,
      fileName: file.name || 'undefined'
    });

    const maxSize = 10 * 1024 * 1024; // 10MB
    const allowedTypes = [
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', // .xlsx
      'application/vnd.ms-excel', // .xls
      'text/csv', // .csv
      'application/csv', // .csv (alternative MIME type)
      'text/plain', // .csv (sometimes detected as plain text)
      'application/octet-stream', // .csv (sometimes detected as binary)
      '', // Empty MIME type fallback
    ];

    if (file.size > maxSize) {
      return { isValid: false, error: 'File size must be less than 10MB' };
    }

    // Check file extension first (more reliable than MIME type for CSV)
    const fileName = file.name || '';
    const extensionMatch = fileName.toLowerCase().match(/\.(xlsx|xls|csv)$/);
    
    if (fileName && extensionMatch) {
      console.log('File validation passed - matched extension:', extensionMatch[1]);
      return { isValid: true };
    }

    // Fallback to MIME type check (more lenient for CSV files)
    if (allowedTypes.includes(file.type)) {
      console.log('File validation passed - matched MIME type:', file.type);
      return { isValid: true };
    }

    // Special case for CSV files with unknown/problematic MIME types
    if (fileName.toLowerCase().endsWith('.csv')) {
      console.log('File validation passed - CSV file with unknown MIME type');
      return { isValid: true };
    }

    console.log('File validation failed - no match for extension or MIME type');
    return { isValid: false, error: 'File must be in Excel (.xlsx, .xls) or CSV format' };
  },

  formatFileSize: (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  },

  // Delete all market data
  deleteAllMarketData: async (reason?: string): Promise<DeleteMarketDataResultDto> => {
    const params = new URLSearchParams();
    if (reason) params.append('reason', reason);
    
    const query = params.toString() ? `?${params.toString()}` : '';
    const response = await api.delete(`/market-data/all${query}`);
    return response.data;
  },

  // Delete market data by date range
  deleteMarketDataByDate: async (
    startDate: string | Date,
    endDate: string | Date,
    reason?: string
  ): Promise<DeleteMarketDataResultDto> => {
    const params = new URLSearchParams();
    
    const formattedStartDate = typeof startDate === 'string' ? startDate : formatApiDate(startDate);
    const formattedEndDate = typeof endDate === 'string' ? endDate : formatApiDate(endDate);
    
    if (formattedStartDate) params.append('startDate', formattedStartDate);
    if (formattedEndDate) params.append('endDate', formattedEndDate);
    if (reason) params.append('reason', reason);

    const response = await api.delete(`/market-data/by-date?${params.toString()}`);
    return response.data;
  },

  // Get count of all market data records
  getMarketDataCount: async (): Promise<{ count: number }> => {
    const response = await api.get('/market-data/count');
    return response.data;
  }
};