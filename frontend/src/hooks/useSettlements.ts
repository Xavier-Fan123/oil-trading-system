import { useState, useEffect } from 'react';
import { 
  ContractSettlementDto,
  ContractSettlementListDto,
  SettlementSearchFilters
} from '@/types/settlement';
import { 
  settlementApi,
  searchSettlementsWithFallback,
  getSettlementWithFallback 
} from '@/services/settlementApi';

// Hook for searching settlements
export const useSettlementSearch = () => {
  const [searchResults, setSearchResults] = useState<ContractSettlementListDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const searchByExternalContract = async (externalContractNumber: string) => {
    if (!externalContractNumber.trim()) {
      setError('External contract number is required');
      return [];
    }

    setLoading(true);
    setError(null);

    try {
      const result = await searchSettlementsWithFallback(externalContractNumber.trim());
      setSearchResults(result.data);
      return result.data;
    } catch (err) {
      console.error('Settlement search error:', err);
      setError('Failed to search settlements');
      return [];
    } finally {
      setLoading(false);
    }
  };

  const searchWithFilters = async (filters: SettlementSearchFilters) => {
    setLoading(true);
    setError(null);

    try {
      const result = await settlementApi.getSettlements(filters);
      setSearchResults(result.data);
      return result.data;
    } catch (err) {
      console.error('Settlement search error:', err);
      // Fallback to mock data
      const fallbackResult = await searchSettlementsWithFallback('');
      setSearchResults(fallbackResult.data);
      return fallbackResult.data;
    } finally {
      setLoading(false);
    }
  };

  const clearResults = () => {
    setSearchResults([]);
    setError(null);
  };

  return {
    searchResults,
    loading,
    error,
    searchByExternalContract,
    searchWithFilters,
    clearResults
  };
};

// Hook for settlement details
export const useSettlementDetail = (settlementId?: string) => {
  const [settlement, setSettlement] = useState<ContractSettlementDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadSettlement = async (id?: string) => {
    const targetId = id || settlementId;
    if (!targetId) return;

    setLoading(true);
    setError(null);

    try {
      const data = await getSettlementWithFallback(targetId);
      setSettlement(data);
      return data;
    } catch (err) {
      console.error('Error loading settlement:', err);
      setError('Failed to load settlement details');
      return null;
    } finally {
      setLoading(false);
    }
  };

  const recalculateSettlement = async () => {
    if (!settlementId) return;

    setLoading(true);
    try {
      const updatedSettlement = await settlementApi.recalculateSettlement(settlementId);
      setSettlement(updatedSettlement);
      return updatedSettlement;
    } catch (err) {
      console.error('Error recalculating settlement:', err);
      setError('Failed to recalculate settlement');
      return null;
    } finally {
      setLoading(false);
    }
  };

  const finalizeSettlement = async () => {
    if (!settlementId) return;

    setLoading(true);
    try {
      const finalizedSettlement = await settlementApi.finalizeSettlement(settlementId);
      setSettlement(finalizedSettlement);
      return finalizedSettlement;
    } catch (err) {
      console.error('Error finalizing settlement:', err);
      setError('Failed to finalize settlement');
      return null;
    } finally {
      setLoading(false);
    }
  };

  const refreshSettlement = () => {
    if (settlementId) {
      loadSettlement(settlementId);
    }
  };

  useEffect(() => {
    if (settlementId) {
      loadSettlement(settlementId);
    }
  }, [settlementId]);

  return {
    settlement,
    loading,
    error,
    loadSettlement,
    recalculateSettlement,
    finalizeSettlement,
    refreshSettlement
  };
};

// Hook for settlement operations (create, update, delete)
export const useSettlementOperations = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createSettlement = async (data: any) => {
    setLoading(true);
    setError(null);

    try {
      const result = await settlementApi.createSettlement(data);
      return result;
    } catch (err) {
      console.error('Error creating settlement:', err);
      setError('Failed to create settlement');
      return null;
    } finally {
      setLoading(false);
    }
  };

  const updateSettlement = async (settlementId: string, data: any) => {
    setLoading(true);
    setError(null);

    try {
      const result = await settlementApi.updateSettlement(settlementId, data);
      return result;
    } catch (err) {
      console.error('Error updating settlement:', err);
      setError('Failed to update settlement');
      return null;
    } finally {
      setLoading(false);
    }
  };

  return {
    loading,
    error,
    createSettlement,
    updateSettlement
  };
};

// Hook for settlement statistics and summary data
export const useSettlementStats = () => {
  const [stats, setStats] = useState({
    totalSettlements: 0,
    finalizedCount: 0,
    draftCount: 0,
    totalValue: 0,
    currency: 'USD'
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadStats = async (filters?: Partial<SettlementSearchFilters>) => {
    setLoading(true);
    setError(null);

    try {
      // This would typically call a dedicated stats endpoint
      // For now, we'll use the search endpoint and calculate stats
      const searchFilters: SettlementSearchFilters = {
        pageNumber: 1,
        pageSize: 100,
        ...filters
      };

      const result = await settlementApi.getSettlements(searchFilters);
      
      const data = result?.data || [];
      const totalValue = data.reduce((sum, settlement) => sum + settlement.totalSettlementAmount, 0);
      const finalizedCount = data.filter(s => s.isFinalized).length;
      
      setStats({
        totalSettlements: result.totalCount,
        finalizedCount,
        draftCount: result.totalCount - finalizedCount,
        totalValue,
        currency: data[0]?.settlementCurrency || 'USD'
      });

      return stats;
    } catch (err) {
      console.error('Error loading settlement stats:', err);
      setError('Failed to load settlement statistics');
      
      // Return mock stats
      const mockStats = {
        totalSettlements: 2,
        finalizedCount: 1,
        draftCount: 1,
        totalValue: 3050000,
        currency: 'USD'
      };
      setStats(mockStats);
      return mockStats;
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadStats();
  }, []);

  return {
    stats,
    loading,
    error,
    loadStats,
    refreshStats: () => loadStats()
  };
};

// Hook for managing settlement charges
export const useSettlementCharges = (settlementId?: string) => {
  const [charges, setCharges] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadCharges = async () => {
    if (!settlementId) return;

    setLoading(true);
    setError(null);

    try {
      // This would load from the settlement detail
      const settlement = await getSettlementWithFallback(settlementId);
      if (settlement) {
        setCharges(settlement.charges);
      }
    } catch (err) {
      console.error('Error loading charges:', err);
      setError('Failed to load charges');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (settlementId) {
      loadCharges();
    }
  }, [settlementId]);

  return {
    charges,
    loading,
    error,
    loadCharges,
    refreshCharges: loadCharges
  };
};

export default {
  useSettlementSearch,
  useSettlementDetail,
  useSettlementOperations,
  useSettlementStats,
  useSettlementCharges
};