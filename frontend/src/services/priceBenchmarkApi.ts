import api from './api';
import type { PriceBenchmark } from '@/types/contracts';

export const priceBenchmarkApi = {
  /**
   * Get all active price benchmarks
   */
  getActiveBenchmarks: (): Promise<PriceBenchmark[]> =>
    api.get('/price-benchmarks').then(res => res.data),

  /**
   * Get benchmark by ID
   */
  getBenchmarkById: (id: string): Promise<PriceBenchmark> =>
    api.get(`/price-benchmarks/${id}`).then(res => res.data),

  /**
   * Get benchmarks by category
   */
  getBenchmarksByCategory: (category: string): Promise<PriceBenchmark[]> =>
    api.get(`/price-benchmarks/by-category/${category}`).then(res => res.data),
};

export default priceBenchmarkApi;