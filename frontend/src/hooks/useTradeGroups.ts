import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  TradeGroupDto,
  StrategyType,
  TradeGroupStatus,
  RiskLevel,
  PortfolioRiskWithTradeGroupsDto,
  UpdateTradeGroupDto,
  AssignContractToTradeGroupDto
} from '../types/tradeGroups';
import { tradeGroupApi } from '../services/tradeGroupApi';

// Mock data for development
const mockTradeGroups: TradeGroupDto[] = [
  {
    id: '1',
    groupName: 'Q1 2025 Brent Calendar Spread',
    strategyType: StrategyType.CalendarSpread,
    description: 'Calendar spread strategy for Q1 2025 Brent crude oil contracts',
    status: TradeGroupStatus.Active,
    expectedRiskLevel: RiskLevel.Medium,
    maxAllowedLoss: 500000,
    targetProfit: 250000,
    createdAt: '2025-01-15T09:00:00Z',
    createdBy: 'john.trader',
    updatedAt: '2025-01-16T14:30:00Z',
    updatedBy: 'john.trader'
  },
  {
    id: '2',
    groupName: 'North Sea Basis Hedge',
    strategyType: StrategyType.BasisHedge,
    description: 'Hedging North Sea physical crude against Brent futures',
    status: TradeGroupStatus.Active,
    expectedRiskLevel: RiskLevel.Low,
    maxAllowedLoss: 200000,
    targetProfit: 150000,
    createdAt: '2025-01-10T11:00:00Z',
    createdBy: 'sarah.analyst',
    updatedAt: '2025-01-15T16:45:00Z',
    updatedBy: 'sarah.analyst'
  }
];

const mockPortfolioRisk: PortfolioRiskWithTradeGroupsDto = {
  totalTradeGroups: 2,
  activeTradeGroups: 2,
  totalValue: 12500000,
  totalPnL: 125000,
  portfolioVaR95: -350000,
  portfolioVaR99: -485000,
  riskConcentration: [],
  correlationMatrix: [],
  tradeGroupSummaries: [
    {
      id: '1',
      groupName: 'Q1 2025 Brent Calendar Spread',
      strategyType: StrategyType.CalendarSpread,
      status: TradeGroupStatus.Active,
      netPnL: 85000,
      totalValue: 7500000,
      var95: -180000,
      contractCount: 5,
      riskLevel: RiskLevel.Medium,
      lastUpdated: '2025-01-18T10:30:00Z'
    },
    {
      id: '2',
      groupName: 'North Sea Basis Hedge',
      strategyType: StrategyType.BasisHedge,
      status: TradeGroupStatus.Active,
      netPnL: 40000,
      totalValue: 5000000,
      var95: -170000,
      contractCount: 3,
      riskLevel: RiskLevel.Low,
      lastUpdated: '2025-01-18T10:25:00Z'
    }
  ]
};

// Query hooks for TradeGroups
export const useTradeGroups = () => {
  return useQuery({
    queryKey: ['trade-groups'],
    queryFn: async () => {
      try {
        return await tradeGroupApi.getAllTradeGroups();
      } catch (error) {
        console.warn('TradeGroups API failed, using mock data:', error);
        return mockTradeGroups;
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useTradeGroup = (id: string) => {
  return useQuery({
    queryKey: ['trade-groups', id],
    queryFn: () => tradeGroupApi.getTradeGroup(id),
    enabled: !!id,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useTradeGroupRisk = (id: string) => {
  return useQuery({
    queryKey: ['trade-groups', id, 'risk'],
    queryFn: () => tradeGroupApi.getTradeGroupRisk(id),
    enabled: !!id,
    staleTime: 1 * 60 * 1000, // 1 minute (risk data should be fresh)
  });
};

export const usePortfolioRiskWithTradeGroups = () => {
  return useQuery({
    queryKey: ['portfolio-risk-trade-groups'],
    queryFn: async () => {
      try {
        return await tradeGroupApi.getPortfolioRiskWithTradeGroups();
      } catch (error) {
        console.warn('Portfolio Risk API failed, using mock data:', error);
        return mockPortfolioRisk;
      }
    },
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};

export const useTradeGroupTags = (tradeGroupId: string) => {
  return useQuery({
    queryKey: ['trade-groups', tradeGroupId, 'tags'],
    queryFn: () => tradeGroupApi.getTradeGroupTags(tradeGroupId),
    enabled: !!tradeGroupId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

// Mutation hooks for TradeGroups
export const useCreateTradeGroup = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: tradeGroupApi.createTradeGroup,
    onSuccess: () => {
      // Invalidate and refetch trade groups list
      queryClient.invalidateQueries({ queryKey: ['trade-groups'] });
      queryClient.invalidateQueries({ queryKey: ['portfolio-risk-trade-groups'] });
    },
  });
};

export const useUpdateTradeGroup = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateTradeGroupDto }) => 
      tradeGroupApi.updateTradeGroup(id, dto),
    onSuccess: (_data, variables) => {
      // Update specific trade group cache
      queryClient.invalidateQueries({ queryKey: ['trade-groups', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['trade-groups'] });
      queryClient.invalidateQueries({ queryKey: ['portfolio-risk-trade-groups'] });
    },
  });
};

export const useCloseTradeGroup = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: tradeGroupApi.closeTradeGroup,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trade-groups'] });
      queryClient.invalidateQueries({ queryKey: ['portfolio-risk-trade-groups'] });
    },
  });
};

export const useAssignContractToTradeGroup = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ tradeGroupId, dto }: { tradeGroupId: string; dto: AssignContractToTradeGroupDto }) =>
      tradeGroupApi.assignContractToTradeGroup(tradeGroupId, dto),
    onSuccess: (_data, variables) => {
      // Invalidate specific trade group and overall list
      queryClient.invalidateQueries({ queryKey: ['trade-groups', variables.tradeGroupId] });
      queryClient.invalidateQueries({ queryKey: ['trade-groups'] });
      queryClient.invalidateQueries({ queryKey: ['portfolio-risk-trade-groups'] });
      
      // Also invalidate contracts list since contract is now assigned
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: ['paper-contracts'] });
    },
  });
};

export const useAddTagToTradeGroup = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ tradeGroupId, tagId, notes }: { tradeGroupId: string; tagId: string; notes?: string }) =>
      tradeGroupApi.addTagToTradeGroup(tradeGroupId, tagId, notes),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['trade-groups', variables.tradeGroupId] });
      queryClient.invalidateQueries({ queryKey: ['trade-groups', variables.tradeGroupId, 'tags'] });
      queryClient.invalidateQueries({ queryKey: ['trade-groups'] });
    },
  });
};

export const useRemoveTagFromTradeGroup = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ tradeGroupId, tagId, reason }: { tradeGroupId: string; tagId: string; reason?: string }) =>
      tradeGroupApi.removeTagFromTradeGroup(tradeGroupId, tagId, reason),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['trade-groups', variables.tradeGroupId] });
      queryClient.invalidateQueries({ queryKey: ['trade-groups', variables.tradeGroupId, 'tags'] });
      queryClient.invalidateQueries({ queryKey: ['trade-groups'] });
    },
  });
};

// Combined hooks for complex operations
export const useTradeGroupWithRisk = (id: string) => {
  const tradeGroupQuery = useTradeGroup(id);
  const riskQuery = useTradeGroupRisk(id);
  
  return {
    tradeGroup: tradeGroupQuery.data,
    riskMetrics: riskQuery.data,
    isLoading: tradeGroupQuery.isLoading || riskQuery.isLoading,
    error: tradeGroupQuery.error || riskQuery.error,
    refetch: () => {
      tradeGroupQuery.refetch();
      riskQuery.refetch();
    }
  };
};

export const useTradeGroupManagement = (id: string) => {
  const tradeGroup = useTradeGroup(id);
  const tags = useTradeGroupTags(id);
  const updateMutation = useUpdateTradeGroup();
  const closeMutation = useCloseTradeGroup();
  const addTagMutation = useAddTagToTradeGroup();
  const removeTagMutation = useRemoveTagFromTradeGroup();
  
  return {
    // Data
    tradeGroup: tradeGroup.data,
    tags: tags.data,
    
    // Loading states
    isLoading: tradeGroup.isLoading || tags.isLoading,
    isUpdating: updateMutation.isPending,
    isClosing: closeMutation.isPending,
    isManagingTags: addTagMutation.isPending || removeTagMutation.isPending,
    
    // Error states
    error: tradeGroup.error || tags.error,
    updateError: updateMutation.error,
    closeError: closeMutation.error,
    tagError: addTagMutation.error || removeTagMutation.error,
    
    // Actions
    updateTradeGroup: (dto: UpdateTradeGroupDto) => updateMutation.mutate({ id, dto }),
    closeTradeGroup: () => closeMutation.mutate(id),
    addTag: (tagId: string, notes?: string) => addTagMutation.mutate({ tradeGroupId: id, tagId, notes }),
    removeTag: (tagId: string, reason?: string) => removeTagMutation.mutate({ tradeGroupId: id, tagId, reason }),
    
    // Refresh
    refetch: () => {
      tradeGroup.refetch();
      tags.refetch();
    }
  };
};