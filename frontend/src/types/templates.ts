// Settlement Template Types

export interface SettlementTemplateConfig {
  // Core settings
  defaultCurrency: string;
  defaultPaymentTerms?: string;
  defaultPaymentMethod?: string;

  // Pricing defaults
  benchmarkPriceFormula?: string;
  autoCalculatePrices?: boolean;

  // Charges defaults
  defaultCharges: DefaultChargeItem[];

  // Calculation settings
  calculationMode?: string;
  quantityOverride?: {
    overrideMT?: number;
    overrideBBL?: number;
  };

  // Notes and metadata
  notes?: string;
  tags?: string[];

  // Custom fields
  customFields?: Record<string, any>;
}

export interface DefaultChargeItem {
  chargeType: number; // ChargeType enum
  chargeTypeLabel: string;
  description: string;
  amount: number;
  currency: string;
  isFixed: boolean; // true = fixed amount, false = percentage-based
  includeByDefault: boolean;
}

export interface TemplatePreviewData {
  templateId: string;
  name: string;
  description: string;
  config: SettlementTemplateConfig;
  timesUsed: number;
  lastUsedAt?: Date;
  createdByUserName: string;
}

export interface TemplateQuickCreateOptions {
  templateId: string;
  contractId: string;
  valueOverrides?: Record<string, any>;
}

export interface TemplateManagementViewState {
  selectedTemplate?: string;
  isEditMode: boolean;
  isDeleteConfirmOpen: boolean;
  isShareDialogOpen: boolean;
  filters: {
    searchTerm: string;
    isPublic?: boolean;
    isActive?: boolean;
  };
  pagination: {
    pageNumber: number;
    pageSize: number;
  };
  sorting: {
    sortBy: 'name' | 'createdAt' | 'lastUsedAt' | 'timesUsed';
    sortDescending: boolean;
  };
}

export const defaultTemplateConfig: SettlementTemplateConfig = {
  defaultCurrency: 'USD',
  autoCalculatePrices: true,
  calculationMode: 'UseActualQuantities',
  defaultCharges: [],
  customFields: {},
};

export const permissionLevels = {
  0: { label: 'View', description: 'Can view template only' },
  1: { label: 'Use', description: 'Can use template to create settlements' },
  2: { label: 'Edit', description: 'Can edit template and use' },
  3: { label: 'Admin', description: 'Full control including sharing' },
};
