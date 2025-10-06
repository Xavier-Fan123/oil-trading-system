import { 
  Tag, 
  TagSummary, 
  CreateTagDto, 
  UpdateTagDto, 
  AddContractTagDto,
  TagValidationResult,
  TagUsageStatistics,
  PredefinedTagInfo,
  TagCategory 
} from '../types/contracts';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

// Tag Management API
export const tagApi = {
  // Get all tags
  getTags: async (): Promise<TagSummary[]> => {
    const response = await fetch(`${API_BASE_URL}/tags`);
    if (!response.ok) {
      throw new Error('Failed to fetch tags');
    }
    return response.json();
  },

  // Get tag details by ID
  getTag: async (id: string): Promise<Tag> => {
    const response = await fetch(`${API_BASE_URL}/tags/${id}`);
    if (!response.ok) {
      throw new Error('Failed to fetch tag');
    }
    return response.json();
  },

  // Get tags by category
  getTagsByCategory: async (category: TagCategory): Promise<TagSummary[]> => {
    const response = await fetch(`${API_BASE_URL}/tags/category/${category}`);
    if (!response.ok) {
      throw new Error('Failed to fetch tags by category');
    }
    return response.json();
  },

  // Create tag
  createTag: async (dto: CreateTagDto): Promise<Tag> => {
    const response = await fetch(`${API_BASE_URL}/tags`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(dto),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to create tag');
    }
    return response.json();
  },

  // Update tag
  updateTag: async (id: string, dto: UpdateTagDto): Promise<Tag> => {
    const response = await fetch(`${API_BASE_URL}/tags/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(dto),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to update tag');
    }
    return response.json();
  },

  // Delete tag
  deleteTag: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/tags/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to delete tag');
    }
  },

  // Add tag to contract
  addTagToContract: async (
    contractId: string, 
    contractType: string, 
    dto: AddContractTagDto
  ): Promise<void> => {
    const response = await fetch(
      `${API_BASE_URL}/tags/contracts/${contractId}/tags?contractType=${contractType}`, 
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(dto),
      }
    );
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to add tag to contract');
    }
  },

  // Remove tag from contract
  removeTagFromContract: async (
    contractId: string, 
    contractType: string, 
    tagId: string
  ): Promise<void> => {
    const response = await fetch(
      `${API_BASE_URL}/tags/contracts/${contractId}/tags/${tagId}?contractType=${contractType}`, 
      {
        method: 'DELETE',
      }
    );
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to remove tag from contract');
    }
  },

  // Get all tags for contract
  getContractTags: async (contractId: string, contractType: string): Promise<TagSummary[]> => {
    const response = await fetch(
      `${API_BASE_URL}/tags/contracts/${contractId}/tags?contractType=${contractType}`
    );
    if (!response.ok) {
      throw new Error('Failed to fetch contract tags');
    }
    return response.json();
  },

  // Validate if tag can be applied to contract
  validateTagForContract: async (
    tagId: string, 
    contractId: string, 
    contractType: string
  ): Promise<TagValidationResult> => {
    const response = await fetch(`${API_BASE_URL}/tags/validate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        tagId,
        contractId,
        contractType,
      }),
    });
    if (!response.ok) {
      throw new Error('Failed to validate tag');
    }
    return response.json();
  },

  // Get predefined tag information
  getPredefinedTagInfo: async (): Promise<PredefinedTagInfo[]> => {
    const response = await fetch(`${API_BASE_URL}/tags/predefined`);
    if (!response.ok) {
      throw new Error('Failed to fetch predefined tag info');
    }
    return response.json();
  },

  // Create predefined tags
  createPredefinedTags: async (): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/tags/predefined/create`, {
      method: 'POST',
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to create predefined tags');
    }
  },

  // Get tag usage statistics
  getTagUsageStatistics: async (): Promise<TagUsageStatistics> => {
    const response = await fetch(`${API_BASE_URL}/tags/statistics`);
    if (!response.ok) {
      throw new Error('Failed to fetch tag usage statistics');
    }
    return response.json();
  },

  // Synchronize tag usage counts
  synchronizeTagUsageCounts: async (): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/tags/sync-usage-counts`, {
      method: 'POST',
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to synchronize tag usage counts');
    }
  },
};

// Tag Category helper functions
export const tagCategoryHelpers = {
  getCategoryDisplayName: (category: TagCategory): string => {
    switch (category) {
      case TagCategory.RiskLevel: return 'Risk Level';
      case TagCategory.TradingStrategy: return 'Trading Strategy';
      case TagCategory.PositionManagement: return 'Position Management';
      case TagCategory.RiskControl: return 'Risk Control';
      case TagCategory.Compliance: return 'Compliance';
      case TagCategory.MarketCondition: return 'Market Condition';
      case TagCategory.ProductClass: return 'Product Classification';
      case TagCategory.Region: return 'Geographic Region';
      case TagCategory.Priority: return 'Business Priority';
      case TagCategory.Customer: return 'Customer Classification';
      case TagCategory.Custom: return 'Custom';
      default: return 'Unknown';
    }
  },

  getCategoryColor: (category: TagCategory): string => {
    switch (category) {
      case TagCategory.RiskLevel: return '#EF4444'; // Red - Risk Alert
      case TagCategory.TradingStrategy: return '#8B5CF6'; // Purple - Strategy ID
      case TagCategory.PositionManagement: return '#10B981'; // Green - Position Status
      case TagCategory.RiskControl: return '#DC2626'; // Dark Red - Risk Control
      case TagCategory.Compliance: return '#059669'; // Dark Green - Compliance Safe
      case TagCategory.MarketCondition: return '#0891B2'; // Dark Cyan - Market Status
      case TagCategory.ProductClass: return '#EA580C'; // Dark Orange - Product Class
      case TagCategory.Region: return '#2563EB'; // Blue - Geographic Region
      case TagCategory.Priority: return '#D97706'; // Amber - Priority Level
      case TagCategory.Customer: return '#DB2777'; // Pink - Customer Relation
      case TagCategory.Custom: return '#6B7280'; // Gray - Custom Tag
      default: return '#6B7280';
    }
  },

  getAllCategories: (): { value: TagCategory; label: string; color: string }[] => {
    return [
      { value: TagCategory.RiskLevel, label: 'Risk Level', color: '#EF4444' },
      { value: TagCategory.TradingStrategy, label: 'Trading Strategy', color: '#8B5CF6' },
      { value: TagCategory.PositionManagement, label: 'Position Management', color: '#10B981' },
      { value: TagCategory.RiskControl, label: 'Risk Control', color: '#DC2626' },
      { value: TagCategory.Compliance, label: 'Compliance', color: '#059669' },
      { value: TagCategory.MarketCondition, label: 'Market Condition', color: '#0891B2' },
      { value: TagCategory.ProductClass, label: 'Product Classification', color: '#EA580C' },
      { value: TagCategory.Region, label: 'Geographic Region', color: '#2563EB' },
      { value: TagCategory.Priority, label: 'Business Priority', color: '#D97706' },
      { value: TagCategory.Customer, label: 'Customer Classification', color: '#DB2777' },
      { value: TagCategory.Custom, label: 'Custom', color: '#6B7280' },
    ];
  },

  // Check if category is trading strategy related
  isTradingRelated: (category: TagCategory): boolean => {
    return category === TagCategory.TradingStrategy ||
           category === TagCategory.PositionManagement ||
           category === TagCategory.RiskControl ||
           category === TagCategory.MarketCondition;
  },

  // Check if category is risk management related
  isRiskRelated: (category: TagCategory): boolean => {
    return category === TagCategory.RiskLevel ||
           category === TagCategory.RiskControl ||
           category === TagCategory.Compliance;
  },

  // Get category description
  getCategoryDescription: (category: TagCategory): string => {
    switch (category) {
      case TagCategory.RiskLevel: return 'Trading position risk level classification';
      case TagCategory.TradingStrategy: return 'Trading strategy type identification, corresponding to TradeGroup strategies';
      case TagCategory.PositionManagement: return 'Position management status identification';
      case TagCategory.RiskControl: return 'Risk control measures and limit management';
      case TagCategory.Compliance: return 'Compliance checking and regulatory requirements identification';
      case TagCategory.MarketCondition: return 'Market condition and price structure identification';
      case TagCategory.ProductClass: return 'Oil product classification and quality grades';
      case TagCategory.Region: return 'Geographic region and trading market classification';
      case TagCategory.Priority: return 'Business processing priority level';
      case TagCategory.Customer: return 'Customer classification and credit rating';
      case TagCategory.Custom: return 'User-defined classification';
      default: return 'Unknown classification';
    }
  },
};