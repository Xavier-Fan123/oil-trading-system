import axios from 'axios'

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Legacy client for backwards compatibility
export const apiClient = api

// Request interceptor for auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('auth_token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// Market Data APIs
export const marketDataApi = {
  uploadFile: async (file: File, fileType: 'DailyPrices' | 'ICESettlement') => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('fileType', fileType)
    
    const response = await api.post('/market-data/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 120000, // 2 minutes for file upload
    })
    return response.data
  },
  
  getLatestPrices: async () => {
    const response = await api.get('/market-data/latest')
    return response.data
  },
  
  getPriceHistory: async (productCode: string, startDate?: Date, endDate?: Date) => {
    const params = new URLSearchParams()
    if (startDate) params.append('startDate', startDate.toISOString())
    if (endDate) params.append('endDate', endDate.toISOString())
    
    const response = await api.get(`/market-data/history/${productCode}?${params}`)
    return response.data
  },
}

// Paper Contracts APIs
export const paperContractsApi = {
  getAll: async () => {
    const response = await api.get('/paper-contracts')
    return response.data
  },
  
  getOpenPositions: async () => {
    const response = await api.get('/paper-contracts/open-positions')
    return response.data
  },
  
  create: async (data: any) => {
    const response = await api.post('/paper-contracts', data)
    return response.data
  },
  
  updateMTM: async (data: any) => {
    const response = await api.post('/paper-contracts/update-mtm', data)
    return response.data
  },
  
  closePosition: async (id: string, data: any) => {
    const response = await api.post(`/paper-contracts/${id}/close`, data)
    return response.data
  },
  
  getPnLSummary: async () => {
    const response = await api.get('/paper-contracts/pnl-summary')
    return response.data
  },
}

export default api