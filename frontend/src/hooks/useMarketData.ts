import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { marketDataApi } from '@/services/marketDataApi';
import type { FileType } from '@/types/marketData';

// Get available benchmarks for floating pricing contracts
export const useAvailableBenchmarks = (enabled = true) => {
  return useQuery({
    queryKey: ['market-data', 'available-benchmarks'],
    queryFn: () => marketDataApi.getAvailableBenchmarks(),
    enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Get latest prices
export const useLatestPrices = () => {
  return useQuery({
    queryKey: ['market-data', 'latest'],
    queryFn: () => marketDataApi.getLatestPrices(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 5 * 60 * 1000, // Refresh every 5 minutes
    retry: 2,
  });
};

// Get price history for a product
export const usePriceHistory = (
  productCode: string,
  startDate?: string,
  endDate?: string,
  priceType?: string,
  contractMonth?: string,
  region?: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ['market-data', 'history', productCode, startDate, endDate, priceType, contractMonth, region],
    queryFn: () => marketDataApi.getPriceHistory(productCode, startDate, endDate, priceType, contractMonth, region),
    enabled: enabled && !!productCode,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Upload market data file
export const useUploadMarketData = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ file, fileType, overwriteExisting }: { file: File; fileType: FileType; overwriteExisting?: boolean }) =>
      marketDataApi.uploadFile(file, fileType, overwriteExisting),
    onSuccess: () => {
      // Invalidate market data queries after successful upload
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Import spot prices
export const useImportSpotPrices = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ file, fileType }: { file: File; fileType: FileType }) =>
      marketDataApi.importSpotPrices(file, fileType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Import futures prices
export const useImportFuturesPrices = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ file, fileType }: { file: File; fileType: FileType }) =>
      marketDataApi.importFuturesPrices(file, fileType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Import local files (for testing)
export const useImportLocalFutures = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (filePath: string) => marketDataApi.importLocalFutures(filePath),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

export const useImportLocalSpot = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (filePath: string) => marketDataApi.importLocalSpot(filePath),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// ICE Settlement import
export const useImportIceSettlement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (filePath?: string) => marketDataApi.importIceSettlement(filePath),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Paper trading data import
export const useImportPaperTradingData = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (filePath?: string) => marketDataApi.importPaperTradingData(filePath),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Bulk import
export const useBulkImport = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => marketDataApi.bulkImport(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Get import status
export const useImportStatus = (enabled = true) => {
  return useQuery({
    queryKey: ['import-status'],
    queryFn: () => marketDataApi.getImportStatus(),
    enabled,
    refetchInterval: 2000, // Poll every 2 seconds when enabled
    retry: 1,
  });
};

// Delete all market data
export const useDeleteAllMarketData = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (reason?: string) => marketDataApi.deleteAllMarketData(reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Delete market data by date range
export const useDeleteMarketDataByDate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ startDate, endDate, reason }: {
      startDate: string;
      endDate: string;
      reason?: string
    }) => marketDataApi.deleteMarketDataByDate(startDate, endDate, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['market-data'] });
    },
  });
};

// Get available contract months for a product
export const useAvailableContractMonths = (
  productCode: string,
  priceType?: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ['market-data', 'contract-months', productCode, priceType],
    queryFn: () => marketDataApi.getAvailableContractMonths(productCode, priceType),
    enabled: enabled && !!productCode,
    staleTime: 15 * 60 * 1000, // 15 minutes (contract months don't change frequently)
    retry: 1,
  });
};

// Get basis analysis data (spot vs futures comparison)
// Returns both spot and futures prices for the same product over time
export const useBasisAnalysis = (
  productCode: string,
  contractMonth: string,
  startDate: string | Date,
  endDate: string | Date,
  enabled = true
) => {
  return useQuery({
    queryKey: ['basis-analysis', productCode, contractMonth, startDate, endDate],
    queryFn: () => marketDataApi.getBasisAnalysis(productCode, contractMonth, startDate, endDate),
    enabled: enabled && !!productCode && !!contractMonth && !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};