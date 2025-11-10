// Export all settlement template components and utilities

export { TemplatePreview } from './TemplatePreview';
export { TemplateSelector } from './TemplateSelector';
export { TemplateForm } from './TemplateForm';

// Re-export types and services
export { templateApi } from '@/services/templateApi';
export type {
  SettlementTemplate,
  SettlementTemplateSummary,
  CreateTemplateRequest,
  UpdateTemplateRequest,
  SettlementTemplatePermission,
  ShareTemplateRequest,
  SettlementTemplateUsage,
  TemplateStatistics,
  BulkTemplateOperationRequest,
  BulkTemplateOperationResult,
} from '@/services/templateApi';

export type {
  SettlementTemplateConfig,
  DefaultChargeItem,
  TemplatePreviewData,
  TemplateQuickCreateOptions,
  TemplateManagementViewState,
} from '@/types/templates';

export {
  defaultTemplateConfig,
  permissionLevels,
} from '@/types/templates';
