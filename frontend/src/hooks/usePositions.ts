import { useQuery } from '@tanstack/react-query';
import { positionsApi } from '@/services/positionsApi';
import { PositionFilters } from '@/types/positions';

// Get current positions
export const useCurrentPositions = () => {
  return useQuery({
    queryKey: ['currentPositions'],
    queryFn: () => positionsApi.getCurrentPositions(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 30 * 1000, // Refresh every 30 seconds
    retry: 3,
  });
};

// Get position summary
export const usePositionSummary = () => {
  return useQuery({
    queryKey: ['positionSummary'],
    queryFn: () => positionsApi.getPositionSummary(),
    staleTime: 1 * 60 * 1000, // 1 minute
    refetchInterval: 30 * 1000, // Refresh every 30 seconds
    retry: 3,
  });
};

// Alias for consistency
export const usePositionsSummary = usePositionSummary;

// Get position analytics
export const usePositionAnalytics = () => {
  return useQuery({
    queryKey: ['positionAnalytics'],
    queryFn: () => positionsApi.getPositionAnalytics(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 2 * 60 * 1000, // Refresh every 2 minutes
    retry: 2,
  });
};

// Filter positions hook
export const useFilteredPositions = (filters?: PositionFilters) => {
  const { data: positions = [], isLoading, error } = useCurrentPositions();
  
  const filteredPositions = filters 
    ? positionsApi.filterPositions(positions, filters)
    : positions;

  return {
    positions: filteredPositions,
    isLoading,
    error,
    totalCount: positions.length,
    filteredCount: filteredPositions.length
  };
};