import axios from 'axios';
import {
  InventoryLocation,
  InventoryPosition,
  InventoryMovement,
  CreateInventoryLocationRequest,
  UpdateInventoryLocationRequest,
  CreateInventoryPositionRequest,
  UpdateInventoryPositionRequest,
  CreateInventoryMovementRequest,
  UpdateInventoryMovementRequest,
  InventorySummary,
  LocationSummary,
  InventoryLocationType
} from '@/types/inventory';

const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Inventory Locations API
export const inventoryLocationsApi = {
  getAll: async (): Promise<InventoryLocation[]> => {
    const response = await api.get('/inventory/locations');
    return response.data;
  },

  getById: async (id: string): Promise<InventoryLocation> => {
    const response = await api.get(`/inventory/locations/${id}`);
    return response.data;
  },

  getByType: async (type: InventoryLocationType): Promise<InventoryLocation[]> => {
    const response = await api.get(`/inventory/locations/by-type/${type}`);
    return response.data;
  },

  getByCountry: async (country: string): Promise<InventoryLocation[]> => {
    const response = await api.get(`/inventory/locations/by-country/${country}`);
    return response.data;
  },

  create: async (location: CreateInventoryLocationRequest): Promise<InventoryLocation> => {
    const response = await api.post('/inventory/locations', location);
    return response.data;
  },

  update: async (id: string, location: UpdateInventoryLocationRequest): Promise<InventoryLocation> => {
    const response = await api.put(`/inventory/locations/${id}`, location);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/inventory/locations/${id}`);
  },

  getSummary: async (id: string): Promise<LocationSummary> => {
    const response = await api.get(`/inventory/locations/${id}/summary`);
    return response.data;
  }
};

// Inventory Positions API
export const inventoryPositionsApi = {
  getAll: async (): Promise<InventoryPosition[]> => {
    const response = await api.get('/inventory/positions');
    return response.data;
  },

  getById: async (id: string): Promise<InventoryPosition> => {
    const response = await api.get(`/inventory/positions/${id}`);
    return response.data;
  },

  getByLocation: async (locationId: string): Promise<InventoryPosition[]> => {
    const response = await api.get(`/inventory/positions/by-location/${locationId}`);
    return response.data;
  },

  getByProduct: async (productId: string): Promise<InventoryPosition[]> => {
    const response = await api.get(`/inventory/positions/by-product/${productId}`);
    return response.data;
  },

  create: async (position: CreateInventoryPositionRequest): Promise<InventoryPosition> => {
    const response = await api.post('/inventory/positions', position);
    return response.data;
  },

  update: async (id: string, position: UpdateInventoryPositionRequest): Promise<InventoryPosition> => {
    const response = await api.put(`/inventory/positions/${id}`, position);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/inventory/positions/${id}`);
  }
};

// Inventory Movements API
export const inventoryMovementsApi = {
  getAll: async (): Promise<InventoryMovement[]> => {
    const response = await api.get('/inventory/movements');
    return response.data;
  },

  getById: async (id: string): Promise<InventoryMovement> => {
    const response = await api.get(`/inventory/movements/${id}`);
    return response.data;
  },

  getPending: async (): Promise<InventoryMovement[]> => {
    const response = await api.get('/inventory/movements/pending');
    return response.data;
  },

  create: async (movement: CreateInventoryMovementRequest): Promise<InventoryMovement> => {
    const response = await api.post('/inventory/movements', movement);
    return response.data;
  },

  update: async (id: string, movement: UpdateInventoryMovementRequest): Promise<InventoryMovement> => {
    const response = await api.put(`/inventory/movements/${id}`, movement);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/inventory/movements/${id}`);
  }
};

// Inventory Summary API
export const inventorySummaryApi = {
  getOverview: async (): Promise<InventorySummary> => {
    const response = await api.get('/inventory/summary');
    return response.data;
  }
};

// Combined inventory API
export const inventoryApi = {
  locations: inventoryLocationsApi,
  positions: inventoryPositionsApi,
  movements: inventoryMovementsApi,
  summary: inventorySummaryApi
};

export default inventoryApi;