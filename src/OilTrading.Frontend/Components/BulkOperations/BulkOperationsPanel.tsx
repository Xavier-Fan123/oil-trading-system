import React, { useState, useCallback, useMemo } from 'react';
import { 
  CheckIcon, 
  XMarkIcon, 
  DocumentArrowDownIcon, 
  TrashIcon,
  PencilSquareIcon,
  ClipboardDocumentCheckIcon,
  ExclamationTriangleIcon
} from '@heroicons/react/24/outline';

// 批量操作类型
export type BulkOperationType = 
  | 'delete' 
  | 'update_status' 
  | 'export' 
  | 'archive' 
  | 'approve' 
  | 'reject'
  | 'assign'
  | 'tag'
  | 'copy'
  | 'move';

// 操作状态
export type OperationStatus = 'idle' | 'running' | 'completed' | 'failed';

// 批量操作配置
export interface BulkOperation {
  id: string;
  type: BulkOperationType;
  label: string;
  icon: React.ComponentType<any>;
  description: string;
  requiresConfirmation: boolean;
  dangerLevel: 'low' | 'medium' | 'high';
  supportedEntityTypes: string[];
  parameters?: BulkOperationParameter[];
}

// 操作参数
export interface BulkOperationParameter {
  name: string;
  label: string;
  type: 'text' | 'select' | 'multiselect' | 'date' | 'number' | 'boolean';
  required: boolean;
  options?: { value: string; label: string }[];
  defaultValue?: any;
  validation?: (value: any) => string | null;
}

// 选中项接口
export interface SelectedItem {
  id: string;
  type: string;
  data: any;
}

// 操作结果
export interface BulkOperationResult {
  operationId: string;
  totalItems: number;
  successCount: number;
  failureCount: number;
  errors: Array<{
    itemId: string;
    error: string;
  }>;
  warnings: Array<{
    itemId: string;
    warning: string;
  }>;
  duration: number;
}

// 组件属性
export interface BulkOperationsPanelProps {
  selectedItems: SelectedItem[];
  availableOperations: BulkOperation[];
  onOperationExecute: (
    operation: BulkOperation, 
    items: SelectedItem[], 
    parameters: Record<string, any>
  ) => Promise<BulkOperationResult>;
  onSelectionClear: () => void;
  maxSelectionSize?: number;
  className?: string;
}

// 预定义操作配置
export const DEFAULT_BULK_OPERATIONS: BulkOperation[] = [
  {
    id: 'delete',
    type: 'delete',
    label: 'Delete Selected',
    icon: TrashIcon,
    description: 'Permanently delete selected items',
    requiresConfirmation: true,
    dangerLevel: 'high',
    supportedEntityTypes: ['contract', 'product', 'partner'],
    parameters: [
      {
        name: 'reason',
        label: 'Deletion Reason',
        type: 'text',
        required: true,
        validation: (value) => value?.length < 10 ? 'Reason must be at least 10 characters' : null
      }
    ]
  },
  {
    id: 'update_status',
    type: 'update_status',
    label: 'Update Status',
    icon: PencilSquareIcon,
    description: 'Change status of selected items',
    requiresConfirmation: true,
    dangerLevel: 'medium',
    supportedEntityTypes: ['contract', 'shipment'],
    parameters: [
      {
        name: 'newStatus',
        label: 'New Status',
        type: 'select',
        required: true,
        options: [
          { value: 'active', label: 'Active' },
          { value: 'inactive', label: 'Inactive' },
          { value: 'pending', label: 'Pending' },
          { value: 'cancelled', label: 'Cancelled' }
        ]
      },
      {
        name: 'comment',
        label: 'Comment',
        type: 'text',
        required: false
      }
    ]
  },
  {
    id: 'export',
    type: 'export',
    label: 'Export Data',
    icon: DocumentArrowDownIcon,
    description: 'Export selected items to file',
    requiresConfirmation: false,
    dangerLevel: 'low',
    supportedEntityTypes: ['contract', 'product', 'partner', 'shipment'],
    parameters: [
      {
        name: 'format',
        label: 'Export Format',
        type: 'select',
        required: true,
        defaultValue: 'excel',
        options: [
          { value: 'excel', label: 'Excel (.xlsx)' },
          { value: 'csv', label: 'CSV (.csv)' },
          { value: 'pdf', label: 'PDF (.pdf)' },
          { value: 'json', label: 'JSON (.json)' }
        ]
      },
      {
        name: 'includeDetails',
        label: 'Include Detailed Information',
        type: 'boolean',
        required: false,
        defaultValue: true
      }
    ]
  },
  {
    id: 'approve',
    type: 'approve',
    label: 'Approve Selected',
    icon: CheckIcon,
    description: 'Approve selected items',
    requiresConfirmation: true,
    dangerLevel: 'medium',
    supportedEntityTypes: ['contract'],
    parameters: [
      {
        name: 'approvalComment',
        label: 'Approval Comment',
        type: 'text',
        required: false
      }
    ]
  },
  {
    id: 'assign',
    type: 'assign',
    label: 'Assign To User',
    icon: ClipboardDocumentCheckIcon,
    description: 'Assign selected items to a user',
    requiresConfirmation: true,
    dangerLevel: 'medium',
    supportedEntityTypes: ['contract', 'shipment'],
    parameters: [
      {
        name: 'assigneeId',
        label: 'Assign To',
        type: 'select',
        required: true,
        options: [
          { value: 'user1', label: 'John Trader' },
          { value: 'user2', label: 'Jane Manager' },
          { value: 'user3', label: 'Bob Analyst' }
        ]
      },
      {
        name: 'priority',
        label: 'Priority',
        type: 'select',
        required: false,
        defaultValue: 'normal',
        options: [
          { value: 'low', label: 'Low' },
          { value: 'normal', label: 'Normal' },
          { value: 'high', label: 'High' },
          { value: 'urgent', label: 'Urgent' }
        ]
      }
    ]
  }
];

// 参数输入组件
const ParameterInput: React.FC<{
  parameter: BulkOperationParameter;
  value: any;
  onChange: (value: any) => void;
  error?: string;
}> = ({ parameter, value, onChange, error }) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const newValue = parameter.type === 'boolean' 
      ? (e.target as HTMLInputElement).checked
      : parameter.type === 'number'
      ? parseFloat(e.target.value) || 0
      : e.target.value;
    
    onChange(newValue);
  };

  const inputClasses = `w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
    error ? 'border-red-500' : 'border-gray-300'
  }`;

  switch (parameter.type) {
    case 'select':
      return (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            {parameter.label}
            {parameter.required && <span className="text-red-500 ml-1">*</span>}
          </label>
          <select
            value={value || parameter.defaultValue || ''}
            onChange={handleChange}
            className={inputClasses}
            required={parameter.required}
          >
            <option value="">Select an option...</option>
            {parameter.options?.map(option => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
        </div>
      );

    case 'boolean':
      return (
        <div className="flex items-center">
          <input
            type="checkbox"
            checked={value !== undefined ? value : parameter.defaultValue}
            onChange={handleChange}
            className="mr-2 h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
          />
          <label className="text-sm font-medium text-gray-700">
            {parameter.label}
          </label>
        </div>
      );

    case 'number':
      return (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            {parameter.label}
            {parameter.required && <span className="text-red-500 ml-1">*</span>}
          </label>
          <input
            type="number"
            value={value !== undefined ? value : parameter.defaultValue || ''}
            onChange={handleChange}
            className={inputClasses}
            required={parameter.required}
          />
          {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
        </div>
      );

    case 'date':
      return (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            {parameter.label}
            {parameter.required && <span className="text-red-500 ml-1">*</span>}
          </label>
          <input
            type="date"
            value={value || parameter.defaultValue || ''}
            onChange={handleChange}
            className={inputClasses}
            required={parameter.required}
          />
          {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
        </div>
      );

    default: // text
      return (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            {parameter.label}
            {parameter.required && <span className="text-red-500 ml-1">*</span>}
          </label>
          <textarea
            value={value || parameter.defaultValue || ''}
            onChange={handleChange}
            className={inputClasses}
            required={parameter.required}
            rows={3}
            placeholder={`Enter ${parameter.label.toLowerCase()}...`}
          />
          {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
        </div>
      );
  }
};

// 确认对话框组件
const ConfirmationDialog: React.FC<{
  isOpen: boolean;
  operation: BulkOperation;
  itemCount: number;
  onConfirm: () => void;
  onCancel: () => void;
}> = ({ isOpen, operation, itemCount, onConfirm, onCancel }) => {
  if (!isOpen) return null;

  const getDangerColor = (level: string) => {
    switch (level) {
      case 'high': return 'text-red-600';
      case 'medium': return 'text-yellow-600';
      default: return 'text-blue-600';
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
        <div className="flex items-center mb-4">
          {operation.dangerLevel === 'high' && (
            <ExclamationTriangleIcon className="h-6 w-6 text-red-500 mr-2" />
          )}
          <h3 className="text-lg font-semibold text-gray-900">
            Confirm {operation.label}
          </h3>
        </div>
        
        <p className="text-gray-600 mb-4">
          Are you sure you want to {operation.label.toLowerCase()} {itemCount} item{itemCount !== 1 ? 's' : ''}?
        </p>
        
        <p className={`text-sm mb-6 ${getDangerColor(operation.dangerLevel)}`}>
          {operation.description}
        </p>
        
        <div className="flex space-x-3">
          <button
            onClick={onConfirm}
            className={`flex-1 px-4 py-2 rounded-md text-white font-medium ${
              operation.dangerLevel === 'high'
                ? 'bg-red-600 hover:bg-red-700'
                : operation.dangerLevel === 'medium'
                ? 'bg-yellow-600 hover:bg-yellow-700'
                : 'bg-blue-600 hover:bg-blue-700'
            }`}
          >
            Confirm
          </button>
          <button
            onClick={onCancel}
            className="flex-1 px-4 py-2 border border-gray-300 rounded-md text-gray-700 font-medium hover:bg-gray-50"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
};

// 操作结果对话框
const ResultDialog: React.FC<{
  isOpen: boolean;
  result: BulkOperationResult | null;
  onClose: () => void;
}> = ({ isOpen, result, onClose }) => {
  if (!isOpen || !result) return null;

  const successRate = (result.successCount / result.totalItems) * 100;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 max-w-lg w-full mx-4 max-h-96 overflow-y-auto">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Operation Results
        </h3>
        
        <div className="grid grid-cols-2 gap-4 mb-4">
          <div className="bg-gray-50 p-3 rounded">
            <p className="text-sm text-gray-600">Total Items</p>
            <p className="text-2xl font-bold text-gray-900">{result.totalItems}</p>
          </div>
          <div className="bg-green-50 p-3 rounded">
            <p className="text-sm text-green-600">Successful</p>
            <p className="text-2xl font-bold text-green-900">{result.successCount}</p>
          </div>
          {result.failureCount > 0 && (
            <div className="bg-red-50 p-3 rounded">
              <p className="text-sm text-red-600">Failed</p>
              <p className="text-2xl font-bold text-red-900">{result.failureCount}</p>
            </div>
          )}
          <div className="bg-blue-50 p-3 rounded">
            <p className="text-sm text-blue-600">Success Rate</p>
            <p className="text-2xl font-bold text-blue-900">{successRate.toFixed(1)}%</p>
          </div>
        </div>

        {result.errors.length > 0 && (
          <div className="mb-4">
            <h4 className="font-medium text-red-900 mb-2">Errors:</h4>
            <div className="max-h-32 overflow-y-auto">
              {result.errors.map((error, index) => (
                <div key={index} className="text-sm text-red-700 bg-red-50 p-2 rounded mb-1">
                  <strong>Item {error.itemId}:</strong> {error.error}
                </div>
              ))}
            </div>
          </div>
        )}

        {result.warnings.length > 0 && (
          <div className="mb-4">
            <h4 className="font-medium text-yellow-900 mb-2">Warnings:</h4>
            <div className="max-h-32 overflow-y-auto">
              {result.warnings.map((warning, index) => (
                <div key={index} className="text-sm text-yellow-700 bg-yellow-50 p-2 rounded mb-1">
                  <strong>Item {warning.itemId}:</strong> {warning.warning}
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

// 主要批量操作面板组件
export const BulkOperationsPanel: React.FC<BulkOperationsPanelProps> = ({
  selectedItems,
  availableOperations,
  onOperationExecute,
  onSelectionClear,
  maxSelectionSize = 1000,
  className = ''
}) => {
  const [selectedOperation, setSelectedOperation] = useState<BulkOperation | null>(null);
  const [operationParameters, setOperationParameters] = useState<Record<string, any>>({});
  const [parameterErrors, setParameterErrors] = useState<Record<string, string>>({});
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [operationStatus, setOperationStatus] = useState<OperationStatus>('idle');
  const [operationResult, setOperationResult] = useState<BulkOperationResult | null>(null);
  const [showResult, setShowResult] = useState(false);

  // 获取适用的操作
  const applicableOperations = useMemo(() => {
    if (selectedItems.length === 0) return [];
    
    const entityTypes = [...new Set(selectedItems.map(item => item.type))];
    
    return availableOperations.filter(operation => 
      entityTypes.every(type => operation.supportedEntityTypes.includes(type))
    );
  }, [selectedItems, availableOperations]);

  // 重置状态
  const resetOperationState = useCallback(() => {
    setSelectedOperation(null);
    setOperationParameters({});
    setParameterErrors({});
    setShowConfirmation(false);
    setOperationStatus('idle');
  }, []);

  // 处理操作选择
  const handleOperationSelect = useCallback((operation: BulkOperation) => {
    setSelectedOperation(operation);
    
    // 设置默认参数值
    const defaultParams: Record<string, any> = {};
    operation.parameters?.forEach(param => {
      if (param.defaultValue !== undefined) {
        defaultParams[param.name] = param.defaultValue;
      }
    });
    setOperationParameters(defaultParams);
    setParameterErrors({});
  }, []);

  // 处理参数变化
  const handleParameterChange = useCallback((paramName: string, value: any) => {
    setOperationParameters(prev => ({
      ...prev,
      [paramName]: value
    }));
    
    // 清除该参数的错误
    if (parameterErrors[paramName]) {
      setParameterErrors(prev => ({
        ...prev,
        [paramName]: ''
      }));
    }
  }, [parameterErrors]);

  // 验证参数
  const validateParameters = useCallback(() => {
    if (!selectedOperation) return false;
    
    const errors: Record<string, string> = {};
    let isValid = true;

    selectedOperation.parameters?.forEach(param => {
      const value = operationParameters[param.name];
      
      if (param.required && (value === undefined || value === null || value === '')) {
        errors[param.name] = `${param.label} is required`;
        isValid = false;
      } else if (value && param.validation) {
        const validationError = param.validation(value);
        if (validationError) {
          errors[param.name] = validationError;
          isValid = false;
        }
      }
    });

    setParameterErrors(errors);
    return isValid;
  }, [selectedOperation, operationParameters]);

  // 处理操作执行
  const handleExecuteOperation = useCallback(async () => {
    if (!selectedOperation) return;

    if (!validateParameters()) return;

    if (selectedOperation.requiresConfirmation) {
      setShowConfirmation(true);
      return;
    }

    await executeOperation();
  }, [selectedOperation, validateParameters]);

  // 执行操作
  const executeOperation = useCallback(async () => {
    if (!selectedOperation) return;

    setOperationStatus('running');
    setShowConfirmation(false);

    try {
      const result = await onOperationExecute(
        selectedOperation,
        selectedItems,
        operationParameters
      );
      
      setOperationResult(result);
      setOperationStatus('completed');
      setShowResult(true);
      
      // 如果操作成功，清除选择
      if (result.failureCount === 0) {
        onSelectionClear();
      }
    } catch (error) {
      setOperationStatus('failed');
      console.error('Bulk operation failed:', error);
    }
  }, [selectedOperation, selectedItems, operationParameters, onOperationExecute, onSelectionClear]);

  // 如果没有选中项，不显示面板
  if (selectedItems.length === 0) {
    return null;
  }

  return (
    <>
      <div className={`bg-white border border-gray-300 rounded-lg shadow-lg p-4 ${className}`}>
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">
              Bulk Operations
            </h3>
            <p className="text-sm text-gray-600">
              {selectedItems.length} item{selectedItems.length !== 1 ? 's' : ''} selected
              {selectedItems.length >= maxSelectionSize && (
                <span className="text-yellow-600 ml-2">
                  (Maximum selection limit reached)
                </span>
              )}
            </p>
          </div>
          <button
            onClick={onSelectionClear}
            className="text-gray-500 hover:text-gray-700"
          >
            <XMarkIcon className="h-5 w-5" />
          </button>
        </div>

        {/* 操作选择 */}
        {!selectedOperation && (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
            {applicableOperations.map(operation => (
              <button
                key={operation.id}
                onClick={() => handleOperationSelect(operation)}
                disabled={operationStatus === 'running'}
                className="p-3 border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <operation.icon className="h-6 w-6 mx-auto mb-2 text-gray-600" />
                <p className="text-sm font-medium text-gray-900">{operation.label}</p>
                <p className="text-xs text-gray-500 mt-1">{operation.description}</p>
              </button>
            ))}
          </div>
        )}

        {/* 参数配置 */}
        {selectedOperation && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h4 className="text-md font-medium text-gray-900">
                {selectedOperation.label} Configuration
              </h4>
              <button
                onClick={resetOperationState}
                className="text-gray-500 hover:text-gray-700"
              >
                <XMarkIcon className="h-5 w-5" />
              </button>
            </div>

            {selectedOperation.parameters && selectedOperation.parameters.length > 0 && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {selectedOperation.parameters.map(param => (
                  <ParameterInput
                    key={param.name}
                    parameter={param}
                    value={operationParameters[param.name]}
                    onChange={(value) => handleParameterChange(param.name, value)}
                    error={parameterErrors[param.name]}
                  />
                ))}
              </div>
            )}

            <div className="flex space-x-3">
              <button
                onClick={handleExecuteOperation}
                disabled={operationStatus === 'running'}
                className={`flex-1 px-4 py-2 rounded-md text-white font-medium ${
                  operationStatus === 'running'
                    ? 'bg-gray-400 cursor-not-allowed'
                    : selectedOperation.dangerLevel === 'high'
                    ? 'bg-red-600 hover:bg-red-700'
                    : selectedOperation.dangerLevel === 'medium'
                    ? 'bg-yellow-600 hover:bg-yellow-700'
                    : 'bg-blue-600 hover:bg-blue-700'
                }`}
              >
                {operationStatus === 'running' ? 'Processing...' : `Execute ${selectedOperation.label}`}
              </button>
              <button
                onClick={resetOperationState}
                className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 font-medium hover:bg-gray-50"
              >
                Cancel
              </button>
            </div>
          </div>
        )}
      </div>

      {/* 确认对话框 */}
      {selectedOperation && (
        <ConfirmationDialog
          isOpen={showConfirmation}
          operation={selectedOperation}
          itemCount={selectedItems.length}
          onConfirm={executeOperation}
          onCancel={() => setShowConfirmation(false)}
        />
      )}

      {/* 结果对话框 */}
      <ResultDialog
        isOpen={showResult}
        result={operationResult}
        onClose={() => {
          setShowResult(false);
          resetOperationState();
        }}
      />
    </>
  );
};

export default BulkOperationsPanel;