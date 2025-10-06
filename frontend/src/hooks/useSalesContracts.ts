import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { salesContractsApi } from '@/services/salesContractsApi';
import {
  CreateSalesContractDto,
  UpdateSalesContractDto,
  SalesContractFilters
} from '@/types/salesContracts';

// Get all sales contracts
export const useSalesContracts = (filters?: SalesContractFilters) => {
  return useQuery({
    queryKey: ['salesContracts', filters],
    queryFn: () => salesContractsApi.getAll(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Get single sales contract by ID
export const useSalesContract = (id: string, options?: { enabled?: boolean }) => {
  return useQuery({
    queryKey: ['salesContract', id],
    queryFn: () => salesContractsApi.getById(id),
    enabled: options?.enabled !== undefined ? options.enabled && !!id : !!id,
    staleTime: 2 * 60 * 1000, // 2 minutes
    retry: 2,
  });
};

// Get sales contracts summary
export const useSalesContractsSummary = () => {
  return useQuery({
    queryKey: ['salesContractsSummary'],
    queryFn: () => salesContractsApi.getSummary(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

// Create sales contract
export const useCreateSalesContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (contract: CreateSalesContractDto) => 
      salesContractsApi.create(contract),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['salesContracts'] });
      queryClient.invalidateQueries({ queryKey: ['salesContractsSummary'] });
    },
  });
};

// Update sales contract
export const useUpdateSalesContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, contract }: { id: string; contract: UpdateSalesContractDto }) => 
      salesContractsApi.update(id, contract),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['salesContracts'] });
      queryClient.invalidateQueries({ queryKey: ['salesContract', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['salesContractsSummary'] });
    },
  });
};

// Delete sales contract
export const useDeleteSalesContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => salesContractsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['salesContracts'] });
      queryClient.invalidateQueries({ queryKey: ['salesContractsSummary'] });
    },
  });
};

// Approve sales contract
export const useApproveSalesContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => salesContractsApi.approve(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['salesContracts'] });
      queryClient.invalidateQueries({ queryKey: ['salesContract', id] });
      queryClient.invalidateQueries({ queryKey: ['salesContractsSummary'] });
    },
  });
};

// Reject sales contract
export const useRejectSalesContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => 
      salesContractsApi.reject(id, reason),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['salesContracts'] });
      queryClient.invalidateQueries({ queryKey: ['salesContract', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['salesContractsSummary'] });
    },
  });
};