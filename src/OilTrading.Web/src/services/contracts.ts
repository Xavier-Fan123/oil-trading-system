import { apiClient } from '@/lib/api'
import { PurchaseContract, SalesContract } from '@/types/contracts'

export const contractsService = {
  // Purchase Contracts
  async getPurchaseContracts(): Promise<PurchaseContract[]> {
    const response = await apiClient.get('/purchase-contracts')
    return response.data
  },

  async getPurchaseContract(id: string): Promise<PurchaseContract> {
    const response = await apiClient.get(`/purchase-contracts/${id}`)
    return response.data
  },

  async createPurchaseContract(contract: Omit<PurchaseContract, 'id' | 'createdAt' | 'updatedAt'>): Promise<PurchaseContract> {
    const response = await apiClient.post('/purchase-contracts', contract)
    return response.data
  },

  async updatePurchaseContract(id: string, contract: Partial<PurchaseContract>): Promise<PurchaseContract> {
    const response = await apiClient.put(`/purchase-contracts/${id}`, contract)
    return response.data
  },

  async deletePurchaseContract(id: string): Promise<void> {
    await apiClient.delete(`/purchase-contracts/${id}`)
  },

  // Sales Contracts
  async getSalesContracts(): Promise<SalesContract[]> {
    const response = await apiClient.get('/sales-contracts')
    return response.data
  },

  async getSalesContract(id: string): Promise<SalesContract> {
    const response = await apiClient.get(`/sales-contracts/${id}`)
    return response.data
  },

  async createSalesContract(contract: Omit<SalesContract, 'id' | 'createdAt' | 'updatedAt'>): Promise<SalesContract> {
    const response = await apiClient.post('/sales-contracts', contract)
    return response.data
  },

  async updateSalesContract(id: string, contract: Partial<SalesContract>): Promise<SalesContract> {
    const response = await apiClient.put(`/sales-contracts/${id}`, contract)
    return response.data
  },

  async deleteSalesContract(id: string): Promise<void> {
    await apiClient.delete(`/sales-contracts/${id}`)
  },
}