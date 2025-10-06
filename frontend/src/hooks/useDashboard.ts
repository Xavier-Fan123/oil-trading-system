import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '@/services/dashboardApi'

export const useDashboardOverview = () => {
  return useQuery({
    queryKey: ['dashboard', 'overview'],
    queryFn: dashboardApi.getOverview,
    refetchInterval: 15000,
    staleTime: 10000,
  })
}

export const useTradingMetrics = (startDate?: string, endDate?: string) => {
  return useQuery({
    queryKey: ['dashboard', 'trading-metrics', startDate, endDate],
    queryFn: () => dashboardApi.getTradingMetrics(startDate, endDate),
    refetchInterval: 30000,
    staleTime: 20000,
  })
}

export const usePerformanceAnalytics = (startDate?: string, endDate?: string) => {
  return useQuery({
    queryKey: ['dashboard', 'performance-analytics', startDate, endDate],
    queryFn: () => dashboardApi.getPerformanceAnalytics(startDate, endDate),
    refetchInterval: 60000,
    staleTime: 45000,
  })
}

export const useMarketInsights = () => {
  return useQuery({
    queryKey: ['dashboard', 'market-insights'],
    queryFn: dashboardApi.getMarketInsights,
    refetchInterval: 20000,
    staleTime: 15000,
  })
}

export const useOperationalStatus = () => {
  return useQuery({
    queryKey: ['dashboard', 'operational-status'],
    queryFn: dashboardApi.getOperationalStatus,
    refetchInterval: 15000,
    staleTime: 10000,
  })
}

export const useAlerts = () => {
  return useQuery({
    queryKey: ['dashboard', 'alerts'],
    queryFn: dashboardApi.getAlerts,
    refetchInterval: 60000, // 1 minute
    staleTime: 30000,
  })
}

export const useKpis = () => {
  return useQuery({
    queryKey: ['dashboard', 'kpis'],
    queryFn: dashboardApi.getKpis,
    refetchInterval: 300000, // 5 minutes
    staleTime: 120000, // 2 minutes
  })
}