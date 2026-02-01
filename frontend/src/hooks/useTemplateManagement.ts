import { useState, useCallback, useEffect } from 'react';
import { templateApi, SettlementTemplate, CreateTemplateRequest, UpdateTemplateRequest } from '@/services/templateApi';
import { SettlementTemplateConfig } from '@/types/templates';

export interface UseTemplateManagementOptions {
  autoFetch?: boolean;
  pageSize?: number;
}

interface FetchOptions {
  searchTerm?: string;
  isPublic?: boolean;
  isActive?: boolean;
  pageNumber?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export const useTemplateManagement = (options: UseTemplateManagementOptions = {}) => {
  const { autoFetch = true, pageSize = 10 } = options;

  // State
  const [templates, setTemplates] = useState<SettlementTemplate[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<SettlementTemplate | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [_filters, setFilters] = useState<FetchOptions>({
    pageNumber: 1,
  });

  // Fetch templates
  const fetchTemplates = useCallback(
    async (fetchOptions: FetchOptions = {}) => {
      setLoading(true);
      setError(null);

      try {
        const params = {
          searchTerm: fetchOptions.searchTerm,
          isPublic: fetchOptions.isPublic,
          isActive: fetchOptions.isActive,
          pageNumber: fetchOptions.pageNumber || 1,
          pageSize,
          sortBy: fetchOptions.sortBy || 'createdAt',
          sortDescending: fetchOptions.sortDescending !== false,
        };

        const result = await templateApi.getTemplates(
          params.searchTerm,
          params.isPublic,
          params.isActive,
          params.pageNumber,
          params.pageSize,
          params.sortBy,
          params.sortDescending
        );

        // Convert SettlementTemplateSummary[] to SettlementTemplate[] for display
        setTemplates(result.data as unknown as SettlementTemplate[]);
        setTotalCount(result.totalCount);
        setCurrentPage(result.page);
        setFilters(params);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load templates';
        setError(message);
        setTemplates([]);
      } finally {
        setLoading(false);
      }
    },
    [pageSize]
  );

  // Get single template
  const getTemplate = useCallback(async (templateId: string) => {
    setLoading(true);
    setError(null);

    try {
      const template = await templateApi.getTemplateById(templateId);
      if (template) {
        setSelectedTemplate(template);
        return template;
      } else {
        setError('Template not found');
        return null;
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load template';
      setError(message);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  // Create template
  const createTemplate = useCallback(
    async (
      name: string,
      description: string,
      config: SettlementTemplateConfig,
      isPublic: boolean = false
    ) => {
      setLoading(true);
      setError(null);

      try {
        const request: CreateTemplateRequest = {
          name,
          description,
          templateConfiguration: JSON.stringify(config),
          isPublic,
        };

        const newTemplate = await templateApi.createTemplate(request);
        setTemplates([newTemplate, ...templates]);
        setSelectedTemplate(newTemplate);
        return newTemplate;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to create template';
        setError(message);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [templates]
  );

  // Update template
  const updateTemplate = useCallback(
    async (
      templateId: string,
      name: string,
      description: string,
      config: SettlementTemplateConfig,
      isPublic: boolean = false
    ) => {
      setLoading(true);
      setError(null);

      try {
        const request: UpdateTemplateRequest = {
          name,
          description,
          templateConfiguration: JSON.stringify(config),
          isPublic,
        };

        const updated = await templateApi.updateTemplate(templateId, request);

        // Update in list
        setTemplates(
          templates.map((t) => (t.id === templateId ? updated : t))
        );

        // Update selected if it's the one being edited
        if (selectedTemplate?.id === templateId) {
          setSelectedTemplate(updated);
        }

        return updated;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to update template';
        setError(message);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [templates, selectedTemplate]
  );

  // Delete template
  const deleteTemplate = useCallback(
    async (templateId: string) => {
      setLoading(true);
      setError(null);

      try {
        const success = await templateApi.deleteTemplate(templateId);

        if (success) {
          setTemplates(templates.filter((t) => t.id !== templateId));
          if (selectedTemplate?.id === templateId) {
            setSelectedTemplate(null);
          }
          return true;
        } else {
          setError('Failed to delete template');
          return false;
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to delete template';
        setError(message);
        return false;
      } finally {
        setLoading(false);
      }
    },
    [templates, selectedTemplate]
  );

  // Get accessible templates (public + personal + shared)
  const getAccessibleTemplates = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await templateApi.getAccessibleTemplates(1, pageSize);
      // Convert SettlementTemplateSummary[] to SettlementTemplate[]
      setTemplates(result.data as unknown as SettlementTemplate[]);
      setTotalCount(result.totalCount);
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load templates';
      setError(message);
      return null;
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  // Get public templates
  const getPublicTemplates = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await templateApi.getPublicTemplates(1, pageSize);
      // Convert SettlementTemplateSummary[] to SettlementTemplate[]
      setTemplates(result.data as unknown as SettlementTemplate[]);
      setTotalCount(result.totalCount);
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load templates';
      setError(message);
      return null;
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  // Get recently used templates
  const getRecentlyUsed = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await templateApi.getRecentlyUsedTemplates(5);
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load templates';
      setError(message);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  // Get most used templates
  const getMostUsed = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await templateApi.getMostUsedTemplates(5);
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load templates';
      setError(message);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  // Share template
  const shareTemplate = useCallback(
    async (templateId: string, userId: string, permissionLevel: number) => {
      setLoading(true);
      setError(null);

      try {
        const permission = await templateApi.shareTemplate(templateId, {
          userId,
          permissionLevel,
        });
        return permission;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to share template';
        setError(message);
        return null;
      } finally {
        setLoading(false);
      }
    },
    []
  );

  // Remove permission
  const removePermission = useCallback(async (templateId: string, userId: string) => {
    setLoading(true);
    setError(null);

    try {
      const success = await templateApi.removePermission(templateId, userId);
      return success;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to remove permission';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, []);

  // Bulk operations
  const bulkOperation = useCallback(
    async (templateIds: string[], operation: 'activate' | 'deactivate' | 'delete') => {
      setLoading(true);
      setError(null);

      try {
        const result = await templateApi.bulkTemplateOperation({
          templateIds,
          operation,
        });

        // Refresh templates after bulk operation
        if (operation === 'delete') {
          setTemplates(templates.filter((t) => !templateIds.includes(t.id)));
        }

        return result;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Bulk operation failed';
        setError(message);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [templates]
  );

  // Clear error
  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Clear selection
  const clearSelection = useCallback(() => {
    setSelectedTemplate(null);
  }, []);

  // Auto-fetch on mount
  useEffect(() => {
    if (autoFetch) {
      fetchTemplates();
    }
  }, [autoFetch, fetchTemplates]);

  return {
    // State
    templates,
    selectedTemplate,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,

    // Methods
    fetchTemplates,
    getTemplate,
    createTemplate,
    updateTemplate,
    deleteTemplate,
    getAccessibleTemplates,
    getPublicTemplates,
    getRecentlyUsed,
    getMostUsed,
    shareTemplate,
    removePermission,
    bulkOperation,
    clearError,
    clearSelection,

    // Helpers
    setSelectedTemplate,
    setCurrentPage,
  };
};

export type UseTemplateManagementReturn = ReturnType<typeof useTemplateManagement>;
