import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  purchaseContractsApi,
  tradingPartnersApi,
  productsApi
} from '@/services/contractsApi';
import { priceBenchmarkApi } from '@/services/priceBenchmarkApi';
import { userService } from '@/services/userService';
import {
  CreatePurchaseContractDto,
  UpdatePurchaseContractDto,
  ContractFilters
} from '@/types/contracts';

// Purchase Contracts Hooks
export const usePurchaseContracts = (filters?: ContractFilters) => {
  return useQuery({
    queryKey: ['purchaseContracts', filters],
    queryFn: () => purchaseContractsApi.getAll(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const usePurchaseContract = (id: string) => {
  return useQuery({
    queryKey: ['purchaseContract', id],
    queryFn: () => purchaseContractsApi.getById(id),
    enabled: !!id,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useCreatePurchaseContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (contract: CreatePurchaseContractDto) => 
      purchaseContractsApi.create(contract),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchaseContracts'] });
    },
  });
};

export const useUpdatePurchaseContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, contract }: { id: string; contract: UpdatePurchaseContractDto }) => 
      purchaseContractsApi.update(id, contract),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['purchaseContracts'] });
      queryClient.invalidateQueries({ queryKey: ['purchaseContract', id] });
    },
  });
};

export const useActivatePurchaseContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => purchaseContractsApi.activate(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['purchaseContracts'] });
      queryClient.invalidateQueries({ queryKey: ['purchaseContract', id] });
    },
  });
};

export const usePurchaseContractAvailableQuantity = (id: string) => {
  return useQuery({
    queryKey: ['purchaseContractAvailableQuantity', id],
    queryFn: () => purchaseContractsApi.getAvailableQuantity(id),
    enabled: !!id,
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};

// Note: Sales Contract hooks are now in useSalesContracts.ts
// These are kept here for backward compatibility but consider using useSalesContracts.ts directly

// Trading Partners Hook
export const useTradingPartners = () => {
  return useQuery({
    queryKey: ['tradingPartners'],
    queryFn: () => tradingPartnersApi.getAll(),
    staleTime: 10 * 60 * 1000, // 10 minutes - trading partners don't change often
  });
};

// Products Hook
export const useProducts = () => {
  return useQuery({
    queryKey: ['products'],
    queryFn: () => productsApi.getAll(),
    staleTime: 10 * 60 * 1000, // 10 minutes - products don't change often
  });
};

// Price Benchmarks Hook
export const usePriceBenchmarks = () => {
  return useQuery({
    queryKey: ['priceBenchmarks'],
    queryFn: () => priceBenchmarkApi.getActiveBenchmarks(),
    staleTime: 10 * 60 * 1000, // 10 minutes - benchmarks don't change often
  });
};

export const usePriceBenchmarksByCategory = (category: string) => {
  return useQuery({
    queryKey: ['priceBenchmarks', 'category', category],
    queryFn: () => priceBenchmarkApi.getBenchmarksByCategory(category),
    enabled: !!category,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
};

// Users/Traders Hooks
export const useUsers = () => {
  return useQuery({
    queryKey: ['users', 'traders'],
    queryFn: () => userService.getTraders(),
    staleTime: 15 * 60 * 1000, // 15 minutes
  });
};