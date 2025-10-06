import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { contractsService } from '@/services/contracts'
import { PurchaseContract, SalesContract } from '@/types/contracts'

// Purchase Contracts Hooks
export function usePurchaseContracts() {
  return useQuery({
    queryKey: ['purchase-contracts'],
    queryFn: contractsService.getPurchaseContracts,
  })
}

export function usePurchaseContract(id: string) {
  return useQuery({
    queryKey: ['purchase-contracts', id],
    queryFn: () => contractsService.getPurchaseContract(id),
    enabled: !!id,
  })
}

export function useCreatePurchaseContract() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: contractsService.createPurchaseContract,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchase-contracts'] })
    },
  })
}

export function useUpdatePurchaseContract() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ id, contract }: { id: string; contract: Partial<PurchaseContract> }) =>
      contractsService.updatePurchaseContract(id, contract),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['purchase-contracts'] })
      queryClient.invalidateQueries({ queryKey: ['purchase-contracts', id] })
    },
  })
}

export function useDeletePurchaseContract() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: contractsService.deletePurchaseContract,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchase-contracts'] })
    },
  })
}

// Sales Contracts Hooks
export function useSalesContracts() {
  return useQuery({
    queryKey: ['sales-contracts'],
    queryFn: contractsService.getSalesContracts,
  })
}

export function useSalesContract(id: string) {
  return useQuery({
    queryKey: ['sales-contracts', id],
    queryFn: () => contractsService.getSalesContract(id),
    enabled: !!id,
  })
}

export function useCreateSalesContract() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: contractsService.createSalesContract,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales-contracts'] })
    },
  })
}

export function useUpdateSalesContract() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ id, contract }: { id: string; contract: Partial<SalesContract> }) =>
      contractsService.updateSalesContract(id, contract),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['sales-contracts'] })
      queryClient.invalidateQueries({ queryKey: ['sales-contracts', id] })
    },
  })
}

export function useDeleteSalesContract() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: contractsService.deleteSalesContract,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sales-contracts'] })
    },
  })
}