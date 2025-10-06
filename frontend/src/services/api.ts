import axios, { AxiosResponse } from 'axios'
import type {
  DashboardOverview,
  TradingMetrics,
  PerformanceAnalytics,
  MarketInsights,
  OperationalStatus,
  ApiError,
  StandardApiError
} from '@/types'
import { parseApiDateFields, formatApiDate, COMMON_DATE_FIELDS } from '@/utils/dateUtils'

// Use environment variable or fall back to localhost for development
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor to format dates for API calls
api.interceptors.request.use(
  (config) => {
    // Handle URL parameters containing dates
    if (config.params) {
      const params = new URLSearchParams()
      for (const [key, value] of Object.entries(config.params)) {
        if (value instanceof Date) {
          params.append(key, formatApiDate(value) || '')
        } else if (value != null) {
          params.append(key, String(value))
        }
      }
      config.params = Object.fromEntries(params.entries())
    }

    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor to parse dates and handle errors
api.interceptors.response.use(
  (response: AxiosResponse) => {
    // Automatically parse date fields in response data
    if (response.data && typeof response.data === 'object') {
      if (Array.isArray(response.data)) {
        // Handle array responses
        response.data = response.data.map(item => 
          typeof item === 'object' ? parseApiDateFields(item, COMMON_DATE_FIELDS) : item
        )
      } else if (response.data.items && Array.isArray(response.data.items)) {
        // Handle paged result responses
        response.data.items = response.data.items.map((item: any) => 
          typeof item === 'object' ? parseApiDateFields(item, COMMON_DATE_FIELDS) : item
        )
      } else {
        // Handle single object responses
        response.data = parseApiDateFields(response.data, COMMON_DATE_FIELDS)
      }
    }
    return response
  },
  (error) => {
    if (error.response) {
      // Handle standardized API error responses
      const responseData = error.response.data
      
      if (responseData && typeof responseData === 'object' && responseData.code) {
        // New standardized error format
        const standardError: StandardApiError = {
          code: responseData.code,
          message: responseData.message || 'An error occurred',
          details: responseData.details,
          timestamp: responseData.timestamp || new Date().toISOString(),
          traceId: responseData.traceId || '',
          statusCode: error.response.status,
          path: responseData.path,
          validationErrors: responseData.validationErrors
        }
        return Promise.reject(standardError)
      } else {
        // Legacy error format or non-standard response
        const legacyError: ApiError = {
          message: responseData?.message || responseData?.title || responseData?.detail || 'An error occurred',
          statusCode: error.response.status,
          timestamp: new Date().toISOString(),
        }
        return Promise.reject(legacyError)
      }
    }
    
    // Network or other errors
    const networkError: StandardApiError = {
      code: 'NETWORK_ERROR',
      message: error.message || 'Network error occurred',
      timestamp: new Date().toISOString(),
      traceId: '',
      statusCode: 0,
      details: {
        type: 'NetworkError',
        originalMessage: error.message
      }
    }
    return Promise.reject(networkError)
  }
)

export const dashboardApi = {
  getOverview: (): Promise<DashboardOverview> =>
    api.get('/dashboard/overview').then(res => res.data),

  getTradingMetrics: (startDate?: string | Date, endDate?: string | Date): Promise<TradingMetrics> => {
    const params = new URLSearchParams();
    if (startDate) {
      const formattedDate = typeof startDate === 'string' ? startDate : formatApiDate(startDate);
      if (formattedDate) params.append('startDate', formattedDate);
    }
    if (endDate) {
      const formattedDate = typeof endDate === 'string' ? endDate : formatApiDate(endDate);
      if (formattedDate) params.append('endDate', formattedDate);
    }
    const query = params.toString() ? `?${params.toString()}` : '';
    return api.get(`/dashboard/trading-metrics${query}`).then(res => res.data);
  },

  getPerformanceAnalytics: (startDate?: string | Date, endDate?: string | Date): Promise<PerformanceAnalytics> => {
    const params = new URLSearchParams();
    if (startDate) {
      const formattedDate = typeof startDate === 'string' ? startDate : formatApiDate(startDate);
      if (formattedDate) params.append('startDate', formattedDate);
    }
    if (endDate) {
      const formattedDate = typeof endDate === 'string' ? endDate : formatApiDate(endDate);
      if (formattedDate) params.append('endDate', formattedDate);
    }
    const query = params.toString() ? `?${params.toString()}` : '';
    return api.get(`/dashboard/performance${query}`).then(res => res.data);
  },

  getMarketInsights: (): Promise<MarketInsights> =>
    api.get('/dashboard/market-insights').then(res => res.data),

  getOperationalStatus: (): Promise<OperationalStatus> =>
    api.get('/dashboard/operational-status').then(res => res.data),

  getAlerts: (): Promise<any[]> =>
    api.get('/dashboard/alerts').then(res => res.data),

  getKpis: (): Promise<any> =>
    api.get('/dashboard/kpis').then(res => res.data),
}

export { api }
export default api