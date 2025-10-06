import axios from 'axios';
import {
  Product,
  CreateProductRequest,
  UpdateProductRequest,
  ProductFilters
} from '@/types/products';

const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Products API
export const productsApi = {
  getAll: async (filters?: ProductFilters): Promise<Product[]> => {
    const params = new URLSearchParams();
    if (filters?.type !== undefined) params.append('type', filters.type.toString());
    if (filters?.code) params.append('code', filters.code);
    if (filters?.name) params.append('name', filters.name);
    if (filters?.isActive !== undefined) params.append('isActive', filters.isActive.toString());
    if (filters?.pageNumber) params.append('pageNumber', filters.pageNumber.toString());
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

    const url = params.toString() ? `/products?${params.toString()}` : '/products';
    const response = await api.get(url);
    return response.data;
  },

  getById: async (id: string): Promise<Product> => {
    const response = await api.get(`/products/${id}`);
    return response.data;
  },

  getByCode: async (code: string): Promise<Product> => {
    const response = await api.get(`/products/by-code/${code}`);
    return response.data;
  },

  create: async (product: CreateProductRequest): Promise<Product> => {
    const response = await api.post('/products', product);
    return response.data;
  },

  update: async (id: string, product: UpdateProductRequest): Promise<void> => {
    await api.put(`/products/${id}`, product);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/products/${id}`);
  }
};

export default productsApi;