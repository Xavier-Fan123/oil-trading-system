import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { shippingApi } from '@/services/shippingApi';
import type {
  CreateShippingOperationDto,
  UpdateShippingOperationDto,
  RecordLiftingOperationDto,
  ShippingFilters,
} from '@/types/shipping';

// Get all shipping operations
export const useShippingOperations = (filters?: ShippingFilters) => {
  return useQuery({
    queryKey: ['shipping-operations', filters],
    queryFn: () => shippingApi.getAll(filters),
    staleTime: 2 * 60 * 1000, // 2 minutes
    retry: 2,
  });
};

// Get single shipping operation by ID
export const useShippingOperation = (id: string, enabled = true) => {
  return useQuery({
    queryKey: ['shipping-operation', id],
    queryFn: () => shippingApi.getById(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000, // 2 minutes
    retry: 2,
  });
};

// Get shipping operations by contract ID
export const useShippingOperationsByContract = (contractId: string, enabled = true) => {
  return useQuery({
    queryKey: ['shipping-operations-by-contract', contractId],
    queryFn: () => shippingApi.getByContractId(contractId),
    enabled: enabled && !!contractId,
    staleTime: 2 * 60 * 1000, // 2 minutes
    retry: 2,
  });
};

// Create shipping operation
export const useCreateShippingOperation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (operation: CreateShippingOperationDto) => 
      shippingApi.create(operation),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
    },
  });
};

// Update shipping operation
export const useUpdateShippingOperation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, operation }: { id: string; operation: UpdateShippingOperationDto }) => 
      shippingApi.update(id, operation),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
      queryClient.invalidateQueries({ queryKey: ['shipping-operation', variables.id] });
    },
  });
};

// Delete shipping operation
export const useDeleteShippingOperation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => shippingApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
    },
  });
};

// Record lifting operation
export const useRecordLifting = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (liftingData: RecordLiftingOperationDto) => 
      shippingApi.recordLifting(liftingData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
    },
  });
};

// Start loading
export const useStartLoading = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (operationId: string) => shippingApi.startLoading(operationId),
    onSuccess: (_, operationId) => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
      queryClient.invalidateQueries({ queryKey: ['shipping-operation', operationId] });
    },
  });
};

// Complete loading
export const useCompleteLoading = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (operationId: string) => shippingApi.completeLoading(operationId),
    onSuccess: (_, operationId) => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
      queryClient.invalidateQueries({ queryKey: ['shipping-operation', operationId] });
    },
  });
};

// Complete discharge
export const useCompleteDischarge = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (operationId: string) => shippingApi.completeDischarge(operationId),
    onSuccess: (_, operationId) => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
      queryClient.invalidateQueries({ queryKey: ['shipping-operation', operationId] });
    },
  });
};

// Cancel shipping operation
export const useCancelShippingOperation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ operationId, reason }: { operationId: string; reason: string }) => 
      shippingApi.cancel(operationId, reason),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['shipping-operations'] });
      queryClient.invalidateQueries({ queryKey: ['shipping-operation', variables.operationId] });
    },
  });
};