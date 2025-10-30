/**
 * Contract Validation Utilities
 * Provides validation rules for contract-related operations
 */

/**
 * Validates an external contract number format
 * @param externalNumber - The external contract number to validate
 * @returns True if valid, false otherwise
 */
export const isValidExternalContractNumber = (externalNumber: string): boolean => {
  if (!externalNumber || typeof externalNumber !== 'string') {
    return false;
  }

  const trimmed = externalNumber.trim();

  // Must be between 1 and 100 characters
  if (trimmed.length === 0 || trimmed.length > 100) {
    return false;
  }

  // Allow alphanumeric, hyphens, underscores, dots
  const validPattern = /^[a-zA-Z0-9\-_.]+$/;
  return validPattern.test(trimmed);
};

/**
 * Validates a contract GUID format
 * @param guid - The GUID to validate
 * @returns True if valid GUID, false otherwise
 */
export const isValidContractGUID = (guid: string): boolean => {
  if (!guid || typeof guid !== 'string') {
    return false;
  }

  const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  return guidPattern.test(guid);
};

/**
 * Validates settlement document number format
 * @param documentNumber - The document number to validate
 * @returns True if valid, false otherwise
 */
export const isValidDocumentNumber = (documentNumber: string): boolean => {
  if (!documentNumber || typeof documentNumber !== 'string') {
    return false;
  }

  const trimmed = documentNumber.trim();

  // Must be between 1 and 50 characters
  if (trimmed.length === 0 || trimmed.length > 50) {
    return false;
  }

  // Allow alphanumeric, hyphens, underscores, slashes, dots
  const validPattern = /^[a-zA-Z0-9\-_/.]+$/;
  return validPattern.test(trimmed);
};

/**
 * Validates settlement quantity
 * @param quantity - The quantity to validate
 * @param minValue - Minimum acceptable value (default 0)
 * @returns True if valid, false otherwise
 */
export const isValidQuantity = (quantity: number | string, minValue: number = 0): boolean => {
  const num = typeof quantity === 'string' ? parseFloat(quantity) : quantity;

  if (isNaN(num)) {
    return false;
  }

  return num >= minValue;
};

/**
 * Validates trading partner selection
 * @param tradingPartnerId - The trading partner ID
 * @returns True if valid, false otherwise
 */
export const isValidTradingPartnerId = (tradingPartnerId: string | undefined): boolean => {
  if (!tradingPartnerId) {
    return false;
  }

  return isValidContractGUID(tradingPartnerId);
};

/**
 * Validates product selection
 * @param productId - The product ID
 * @returns True if valid, false otherwise
 */
export const isValidProductId = (productId: string | undefined): boolean => {
  if (!productId) {
    return false;
  }

  return isValidContractGUID(productId);
};

/**
 * Validates contract type
 * @param contractType - The contract type
 * @returns True if valid, false otherwise
 */
export const isValidContractType = (contractType: string | undefined): boolean => {
  if (!contractType) {
    return false;
  }

  return contractType === 'Purchase' || contractType === 'Sales';
};

/**
 * Gets a human-readable validation error message
 * @param fieldName - Name of the field that failed validation
 * @param reason - The reason for validation failure
 * @returns A formatted error message
 */
export const getValidationErrorMessage = (fieldName: string, reason?: string): string => {
  const messages: Record<string, string> = {
    externalContractNumber: 'External contract number must be 1-100 characters and contain only letters, numbers, hyphens, underscores, or dots',
    contractId: 'Please select a valid contract',
    documentNumber: 'Document number must be 1-50 characters and contain only letters, numbers, hyphens, underscores, slashes, or dots',
    actualQuantityMT: 'Actual quantity in MT must be a positive number',
    actualQuantityBBL: 'Actual quantity in BBL must be a positive number',
    tradingPartnerId: 'Please select a valid trading partner',
    productId: 'Please select a valid product',
    contractType: 'Please select either Purchase or Sales contract',
  };

  return messages[fieldName] || `Invalid ${fieldName}`;
};

/**
 * Validates an external contract creation DTO
 * @param dto - The DTO to validate
 * @returns An object with validation results and any errors
 */
export interface ValidationResult {
  isValid: boolean;
  errors: Record<string, string>;
}

export const validateSettlementByExternalContractDto = (dto: any): ValidationResult => {
  const errors: Record<string, string> = {};

  // Validate external contract number
  if (!isValidExternalContractNumber(dto.externalContractNumber)) {
    errors.externalContractNumber = getValidationErrorMessage('externalContractNumber');
  }

  // Validate document number
  if (dto.documentNumber && !isValidDocumentNumber(dto.documentNumber)) {
    errors.documentNumber = getValidationErrorMessage('documentNumber');
  }

  // Validate document date
  if (!dto.documentDate || !(dto.documentDate instanceof Date)) {
    errors.documentDate = 'Document date must be a valid date';
  }

  // Validate quantities
  if (!isValidQuantity(dto.actualQuantityMT)) {
    errors.actualQuantityMT = getValidationErrorMessage('actualQuantityMT');
  }

  if (!isValidQuantity(dto.actualQuantityBBL)) {
    errors.actualQuantityBBL = getValidationErrorMessage('actualQuantityBBL');
  }

  // Validate optional fields if provided
  if (dto.expectedContractType && !isValidContractType(dto.expectedContractType)) {
    errors.expectedContractType = getValidationErrorMessage('contractType');
  }

  if (dto.tradingPartnerId && !isValidTradingPartnerId(dto.tradingPartnerId)) {
    errors.tradingPartnerId = getValidationErrorMessage('tradingPartnerId');
  }

  if (dto.productId && !isValidProductId(dto.productId)) {
    errors.productId = getValidationErrorMessage('productId');
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
};

/**
 * Validates a shipping operation by external contract DTO
 * @param dto - The DTO to validate
 * @returns An object with validation results and any errors
 */
export const validateShippingOperationByExternalContractDto = (dto: any): ValidationResult => {
  const errors: Record<string, string> = {};

  // Validate external contract number
  if (!isValidExternalContractNumber(dto.externalContractNumber)) {
    errors.externalContractNumber = getValidationErrorMessage('externalContractNumber');
  }

  // Validate vessel name
  if (!dto.vesselName || typeof dto.vesselName !== 'string' || dto.vesselName.trim().length === 0) {
    errors.vesselName = 'Vessel name is required';
  } else if (dto.vesselName.length > 200) {
    errors.vesselName = 'Vessel name must not exceed 200 characters';
  }

  // Validate planned quantity
  if (!isValidQuantity(dto.plannedQuantity)) {
    errors.plannedQuantity = 'Planned quantity must be a positive number';
  }

  // Validate optional fields if provided
  if (dto.expectedContractType && !isValidContractType(dto.expectedContractType)) {
    errors.expectedContractType = getValidationErrorMessage('contractType');
  }

  if (dto.tradingPartnerId && !isValidTradingPartnerId(dto.tradingPartnerId)) {
    errors.tradingPartnerId = getValidationErrorMessage('tradingPartnerId');
  }

  if (dto.productId && !isValidProductId(dto.productId)) {
    errors.productId = getValidationErrorMessage('productId');
  }

  if (dto.vesselCapacity && !isValidQuantity(dto.vesselCapacity, 0)) {
    errors.vesselCapacity = 'Vessel capacity must be a positive number';
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
};

export default {
  isValidExternalContractNumber,
  isValidContractGUID,
  isValidDocumentNumber,
  isValidQuantity,
  isValidTradingPartnerId,
  isValidProductId,
  isValidContractType,
  getValidationErrorMessage,
  validateSettlementByExternalContractDto,
  validateShippingOperationByExternalContractDto,
};
