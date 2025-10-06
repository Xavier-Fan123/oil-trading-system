// Market Data Types

export interface MarketDataUploadResultDto {
  success: boolean;
  recordsProcessed: number;
  recordsInserted: number;
  recordsUpdated: number;
  errors: string[];
  warnings: string[];
  fileType: string;
  fileName: string;
  processingTimeMs: number;
}

export interface LatestPricesDto {
  lastUpdateDate: Date;
  spotPrices: ProductPriceDto[];
  futuresPrices: FuturesPriceDto[];
}

export interface ProductPriceDto {
  productCode: string;
  productName: string;
  price: number;
  previousPrice: number | null;
  change: number | null;
  changePercent: number | null;
  priceDate: Date;
}

export interface FuturesPriceDto {
  productType: string;
  contractMonth: string;
  settlementPrice: number;
  previousSettlement: number | null;
  change: number | null;
  settlementDate: Date;
}

export interface MarketPriceDto {
  productCode: string;
  productName: string;
  price: number;
  currency: string;
  unit: string;
  priceDate: Date;
  priceType: 'Spot' | 'Futures' | 'Forward';
  exchange: string;
  high: number | null;
  low: number | null;
  volume: number | null;
  change: number | null;
  changePercent: number | null;
}

export interface MarketDataUploadRequest {
  file: File;
  fileType: 'Spot' | 'Futures';
  overwriteExisting?: boolean;
}

export interface MarketDataFilters {
  productCode?: string;
  startDate?: Date;
  endDate?: Date;
  priceType?: 'Spot' | 'Futures' | 'Forward';
  exchange?: string;
}

export interface ImportResult {
  success: boolean;
  message: string;
  recordsProcessed: number;
  recordsImported: number;
  errors: string[];
  fileName: string;
}

export interface DataImportStatus {
  isImporting: boolean;
  progress: number;
  currentFile: string | null;
  totalFiles: number;
  completedFiles: number;
  errors: string[];
  lastImport: string | null;
}

export interface DeleteMarketDataResultDto {
  success: boolean;
  recordsDeleted: number;
  message: string;
  errors: string[];
}

// File type options for upload - simplified to only Spot and Futures
export const FILE_TYPES = [
  { value: 'Spot', label: 'Spot Prices', description: 'Physical market spot prices' },
  { value: 'Futures', label: 'Futures Prices', description: 'Futures contract prices' },
] as const;

export type FileType = typeof FILE_TYPES[number]['value'];

// Supported file formats
export const SUPPORTED_FORMATS = [
  '.xlsx', '.xls', '.csv'
];

export const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB