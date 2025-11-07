import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Settlement Template DTOs
export interface SettlementTemplate {
  id: string;
  name: string;
  description: string;
  version: number;
  isActive: boolean;
  isPublic: boolean;
  templateConfiguration: string; // JSON string of template config
  timesUsed: number;
  lastUsedAt?: Date;
  sharedWithCount?: number;
  canEdit: boolean;
  canDelete: boolean;
  createdByUserName: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface SettlementTemplateSummary {
  id: string;
  name: string;
  description: string;
  timesUsed: number;
  lastUsedAt?: Date;
  createdByUserName: string;
}

export interface CreateTemplateRequest {
  name: string;
  description: string;
  templateConfiguration: string; // JSON string
  isPublic: boolean;
}

export interface UpdateTemplateRequest {
  name: string;
  description: string;
  templateConfiguration: string; // JSON string
  isPublic: boolean;
}

export interface SettlementTemplatePermission {
  userId: string;
  userNa: string;
  permissionLevel: number; // 0: View, 1: Use, 2: Edit, 3: Admin
  permissionLevelName: string;
  grantedAt: Date;
  grantedBy: string;
}

export interface ShareTemplateRequest {
  userId: string;
  permissionLevel: number;
}

export interface SettlementTemplateUsage {
  templateId: string;
  settlementId: string;
  appliedBy: string;
  appliedAt: Date;
}

export interface TemplateStatistics {
  totalUsages: number;
  lastUsedAt?: Date;
  sharedWithCount: number;
  usageTrend: UsageTrendItem[];
}

export interface UsageTrendItem {
  weekStart: Date;
  count: number;
}

export interface BulkTemplateOperationRequest {
  templateIds: string[];
  operation: 'activate' | 'deactivate' | 'delete' | 'publish' | 'unpublish';
}

export interface BulkTemplateOperationResult {
  successCount: number;
  failureCount: number;
  details: BulkTemplateOperationDetail[];
}

export interface BulkTemplateOperationDetail {
  templateId: string;
  status: 'success' | 'failure';
  message?: string;
}

export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetTemplatesQuery {
  searchTerm?: string;
  isPublic?: boolean;
  isActive?: boolean;
  pageNumber: number;
  pageSize: number;
  sortBy?: 'name' | 'createdAt' | 'lastUsedAt' | 'timesUsed';
  sortDescending?: boolean;
}

// Settlement Template API service
export const templateApi = {
  // Get all templates (with paging and filtering)
  getTemplates: async (
    searchTerm?: string,
    isPublic?: boolean,
    isActive?: boolean,
    pageNumber: number = 1,
    pageSize: number = 10,
    sortBy: string = 'createdAt',
    sortDescending: boolean = true
  ): Promise<PagedResult<SettlementTemplateSummary>> => {
    const response = await api.get('/settlement-templates', {
      params: {
        searchTerm,
        isPublic,
        isActive,
        pageNumber,
        pageSize,
        sortBy,
        sortDescending,
      },
    });
    return response.data;
  },

  // Get single template by ID
  getTemplateById: async (templateId: string): Promise<SettlementTemplate | null> => {
    try {
      const response = await api.get(`/settlement-templates/${templateId}`);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  // Create new template
  createTemplate: async (request: CreateTemplateRequest): Promise<SettlementTemplate> => {
    const response = await api.post('/settlement-templates', request);
    return response.data;
  },

  // Update existing template
  updateTemplate: async (templateId: string, request: UpdateTemplateRequest): Promise<SettlementTemplate> => {
    const response = await api.put(`/settlement-templates/${templateId}`, request);
    return response.data;
  },

  // Delete template
  deleteTemplate: async (templateId: string): Promise<boolean> => {
    const response = await api.delete(`/settlement-templates/${templateId}`);
    return response.status === 200 || response.status === 204;
  },

  // Get public templates (for loading into settlements)
  getPublicTemplates: async (pageNumber: number = 1, pageSize: number = 10): Promise<PagedResult<SettlementTemplateSummary>> => {
    const response = await api.get('/settlement-templates', {
      params: {
        isPublic: true,
        isActive: true,
        pageNumber,
        pageSize,
        sortBy: 'timesUsed',
        sortDescending: true,
      },
    });
    return response.data;
  },

  // Get accessible templates (public + personal + shared)
  getAccessibleTemplates: async (pageNumber: number = 1, pageSize: number = 10): Promise<PagedResult<SettlementTemplateSummary>> => {
    const response = await api.get('/settlement-templates/accessible', {
      params: {
        pageNumber,
        pageSize,
        sortBy: 'createdAt',
        sortDescending: true,
      },
    });
    return response.data;
  },

  // Get recently used templates
  getRecentlyUsedTemplates: async (pageSize: number = 5): Promise<SettlementTemplateSummary[]> => {
    const response = await api.get('/settlement-templates/recently-used', {
      params: { pageSize },
    });
    return response.data;
  },

  // Get most used templates
  getMostUsedTemplates: async (pageSize: number = 5): Promise<SettlementTemplateSummary[]> => {
    const response = await api.get('/settlement-templates/most-used', {
      params: { pageSize },
    });
    return response.data;
  },

  // Share template with user
  shareTemplate: async (templateId: string, request: ShareTemplateRequest): Promise<SettlementTemplatePermission> => {
    const response = await api.post(`/settlement-templates/${templateId}/share`, request);
    return response.data;
  },

  // Get template permissions
  getTemplatePermissions: async (templateId: string): Promise<SettlementTemplatePermission[]> => {
    const response = await api.get(`/settlement-templates/${templateId}/permissions`);
    return response.data;
  },

  // Remove permission
  removePermission: async (templateId: string, userId: string): Promise<boolean> => {
    const response = await api.delete(`/settlement-templates/${templateId}/permissions/${userId}`);
    return response.status === 200 || response.status === 204;
  },

  // Get template usage history
  getTemplateUsages: async (templateId: string): Promise<SettlementTemplateUsage[]> => {
    const response = await api.get(`/settlement-templates/${templateId}/usages`);
    return response.data;
  },

  // Get template statistics
  getTemplateStatistics: async (templateId: string): Promise<TemplateStatistics> => {
    const response = await api.get(`/settlement-templates/${templateId}/statistics`);
    return response.data;
  },

  // Bulk operations
  bulkTemplateOperation: async (request: BulkTemplateOperationRequest): Promise<BulkTemplateOperationResult> => {
    const response = await api.post('/settlement-templates/bulk', request);
    return response.data;
  },

  // Search templates
  searchTemplates: async (query: string, pageNumber: number = 1, pageSize: number = 10): Promise<PagedResult<SettlementTemplateSummary>> => {
    const response = await api.get('/settlement-templates/search', {
      params: {
        query,
        pageNumber,
        pageSize,
      },
    });
    return response.data;
  },

  // Quick create settlement from template
  quickCreateFromTemplate: async (templateId: string, valueOverrides?: Record<string, any>): Promise<{ settlementId: string }> => {
    const response = await api.post('/settlement-templates/quick-create', {
      templateId,
      valueOverrides,
    });
    return response.data;
  },
};

export default templateApi;
