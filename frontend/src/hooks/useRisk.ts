import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { riskApi } from '@/services/riskApi';

// Risk calculation hook with automatic refresh
export const useRiskCalculation = () => {
  return useQuery({
    queryKey: ['riskCalculation'],
    queryFn: () => riskApi.calculateRisk(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refresh every 5 minutes
    retry: 3,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
  });
};

// Portfolio summary hook with shorter refresh interval
export const usePortfolioSummary = () => {
  return useQuery({
    queryKey: ['portfolioSummary'],
    queryFn: () => riskApi.getPortfolioSummary(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 3 * 60 * 1000, // Auto-refresh every 3 minutes
    retry: 3,
  });
};

// Product risk hook
export const useProductRisk = (productType: string) => {
  return useQuery({
    queryKey: ['productRisk', productType],
    queryFn: () => riskApi.getProductRisk(productType),
    enabled: !!productType,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Backtest hook
export const useBacktest = (days?: number) => {
  return useQuery({
    queryKey: ['backtest', days],
    queryFn: () => riskApi.runBacktest(days),
    staleTime: 10 * 60 * 1000, // 10 minutes - backtests are expensive
    retry: 1,
  });
};

// Manual risk recalculation
export const useRecalculateRisk = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: () => riskApi.calculateRisk(),
    onSuccess: (data) => {
      // Update all related queries with fresh data
      queryClient.setQueryData(['riskCalculation'], data);
      queryClient.invalidateQueries({ queryKey: ['portfolioSummary'] });
      queryClient.invalidateQueries({ queryKey: ['productRisk'] });
    },
  });
};

// Alias for consistency - commonly used hook for risk data
export const useRisk = useRiskCalculation;