# Oil Trading System - Standardized Error Handling Implementation

## Overview

This document summarizes the comprehensive implementation of standardized error handling across the Oil Trading System, providing consistent error management between frontend and backend components.

## Implementation Summary

### ✅ Backend Error Handling

#### 1. Standard Error Response Models
- **File**: `C:\Users\itg\Desktop\X\src\OilTrading.Application\Common\Models\StandardErrorResponse.cs`
- **Features**:
  - Standardized error response format with error codes, messages, details, timestamps, and trace IDs
  - Comprehensive error code constants for different scenarios
  - User-friendly error messages for common error types
  - Support for validation errors and structured error details

#### 2. Enhanced Exception Classes
- **Updated Files**:
  - `BusinessRuleException.cs` - Enhanced with error codes and details
  - `NotFoundException.cs` - Added error codes and structured details
  - `ValidationException.cs` - Improved with error code support
  - `UnauthorizedException.cs` - New exception for authentication errors
  - `ForbiddenException.cs` - New exception for authorization errors
  - `ConflictException.cs` - New exception for resource conflicts

#### 3. Global Exception Middleware
- **File**: `C:\Users\itg\Desktop\X\src\OilTrading.Api\Middleware\GlobalExceptionMiddleware.cs`
- **Features**:
  - Centralized exception handling with trace ID generation
  - Automatic mapping of exception types to standardized error responses
  - Comprehensive logging with correlation IDs
  - Support for timeout, validation, business rule, and system errors

#### 4. Error Handling Utilities
- **File**: `C:\Users\itg\Desktop\X\src\OilTrading.Application\Common\Utilities\ErrorHandlingExtensions.cs`
- **Features**:
  - Extension methods for controllers to create standardized error responses
  - Result wrapper classes for operation success/failure handling
  - Automatic status code mapping from error codes
  - Utility methods for common error scenarios

#### 5. Updated Controller Implementation
- **Example**: `C:\Users\itg\Desktop\X\src\OilTrading.Api\Controllers\ContractMatchingController.cs`
- **Features**:
  - Comprehensive error handling with specific error codes
  - Detailed validation error responses
  - Business rule violation handling
  - Proper HTTP status code mapping
  - Enhanced error details for debugging

### ✅ Frontend Error Handling

#### 1. Standardized Error Types
- **File**: `C:\Users\itg\Desktop\X\frontend\src\types\index.ts`
- **Features**:
  - `StandardApiError` interface matching backend format
  - Error severity levels (Low, Medium, High, Critical)
  - Comprehensive error code constants
  - Error context tracking for debugging
  - Enhanced `AppError` interface with user-friendly messages

#### 2. Enhanced API Interceptor
- **File**: `C:\Users\itg\Desktop\X\frontend\src\services\api.ts`
- **Features**:
  - Automatic detection of standardized vs legacy error formats
  - Network error normalization
  - Trace ID preservation for error correlation
  - Support for both new and legacy error formats

#### 3. Error Handling Utilities
- **File**: `C:\Users\itg\Desktop\X\frontend\src\utils\errorUtils.ts`
- **Features**:
  - Error normalization and classification
  - Severity determination based on error codes
  - Recoverable error detection for retry logic
  - User-friendly message generation
  - Validation error extraction
  - Automatic retry mechanism for recoverable errors
  - Error logging with structured context

#### 4. Error Display Components
- **File**: `C:\Users\itg\Desktop\X\frontend\src\components\Common\ErrorDisplay.tsx`
- **Features**:
  - Comprehensive error display with severity-based styling
  - Expandable error details for debugging
  - Retry and support contact actions
  - Validation error display component
  - Network error display component
  - Configurable display options based on environment

#### 5. Error Handling Hooks
- **File**: `C:\Users\itg\Desktop\X\frontend\src\hooks\useErrorHandler.ts`
- **Features**:
  - `useErrorHandler` - Centralized error handling with logging
  - `useFormErrorHandler` - Form validation error management
  - `useAsyncOperation` - Async operation error handling
  - `useApiCall` - API call error handling with auto-retry
  - Error context tracking and reporting

#### 6. Error Boundary Component
- **File**: `C:\Users\itg\Desktop\X\frontend\src\components\Common\ErrorBoundary.tsx`
- **Features**:
  - React error boundary with different display levels
  - Automatic error reporting and logging
  - Recovery mechanisms (retry, reload, navigate home)
  - Development vs production error details
  - Higher-order component wrapper

#### 7. Updated Service Implementation
- **Example**: `C:\Users\itg\Desktop\X\frontend\src\services\contractMatchingApi.ts`
- **Features**:
  - ApiResult wrapper for consistent success/failure handling
  - Client-side validation before API calls
  - Structured error logging with context
  - Error normalization and user-friendly message generation

#### 8. Component Implementation Example
- **File**: `C:\Users\itg\Desktop\X\frontend\src\components\ContractMatching\ContractMatchingForm.tsx`
- **Features**:
  - Comprehensive form validation with error display
  - Loading state management with error handling
  - User-friendly error messages and retry mechanisms
  - Validation error highlighting at field level
  - Context-aware error logging

#### 9. Test Coverage
- **File**: `C:\Users\itg\Desktop\X\frontend\src\utils\errorUtils.test.ts`
- **Features**:
  - Comprehensive unit tests for error utility functions
  - Error normalization testing
  - Severity classification testing
  - Retry logic testing
  - Edge case handling validation

## Error Categories Handled

### 1. Validation Errors (400-422)
- **Backend**: FluentValidation integration with structured error responses
- **Frontend**: Field-level validation display with user guidance
- **Error Codes**: `VALIDATION_FAILED`, `INVALID_INPUT`, `MISSING_REQUIRED_FIELD`

### 2. Authentication Errors (401)
- **Backend**: Comprehensive authentication error handling
- **Frontend**: Automatic redirect to login, token refresh handling
- **Error Codes**: `UNAUTHORIZED`, `TOKEN_EXPIRED`, `INVALID_CREDENTIALS`

### 3. Authorization Errors (403)
- **Backend**: Role-based access control error responses
- **Frontend**: User-friendly permission denied messages
- **Error Codes**: `FORBIDDEN`, `INSUFFICIENT_PERMISSIONS`, `ACCESS_DENIED`

### 4. Not Found Errors (404)
- **Backend**: Entity-specific not found responses with context
- **Frontend**: Resource-specific error messages with navigation options
- **Error Codes**: `NOT_FOUND`, `RESOURCE_NOT_FOUND`, `CONTRACT_NOT_FOUND`

### 5. Business Logic Errors (422)
- **Backend**: Domain rule violation handling with detailed explanations
- **Frontend**: Business rule error display with corrective guidance
- **Error Codes**: `BUSINESS_RULE_VIOLATION`, `INSUFFICIENT_QUANTITY`, `INVALID_CONTRACT_STATUS`

### 6. Server Errors (500+)
- **Backend**: Comprehensive server error logging with trace IDs
- **Frontend**: User-friendly server error messages with support contact
- **Error Codes**: `INTERNAL_SERVER_ERROR`, `SERVICE_UNAVAILABLE`, `DATABASE_ERROR`

### 7. Network Errors
- **Frontend**: Network connectivity error handling with retry mechanisms
- **Error Codes**: `NETWORK_ERROR`, `CONNECTION_ERROR`, `REQUEST_TIMEOUT`

## Key Features Implemented

### 1. Trace ID System
- **Backend**: Automatic trace ID generation using Activity.Current
- **Frontend**: Client-side trace ID generation for debugging
- **Correlation**: Full request-response error correlation

### 2. Error Severity Classification
- **Low**: Minor validation issues, user input errors
- **Medium**: Business rule violations, not found errors
- **High**: Authentication/authorization failures, critical business errors
- **Critical**: System failures, database errors, service unavailable

### 3. Recoverable Error Detection
- **Automatic Retry**: Network errors, timeouts, temporary service issues
- **User Action Required**: Validation errors, authentication issues
- **System Issues**: Server errors requiring support intervention

### 4. Context-Aware Error Handling
- **User Context**: User ID, session information for personalized errors
- **Component Context**: Component name, action performed for debugging
- **System Context**: URL, user agent, timestamp for comprehensive logging

### 5. Environment-Specific Error Display
- **Development**: Full error details, stack traces, technical information
- **Production**: User-friendly messages, support contact information
- **Debugging**: Trace IDs, error codes, detailed context for troubleshooting

## Benefits Achieved

### 1. Consistency
- Uniform error response format across all API endpoints
- Consistent error handling patterns in frontend components
- Standardized error codes and messages

### 2. Developer Experience
- Comprehensive error utilities and helper functions
- Type-safe error handling with TypeScript interfaces
- Reusable error components and hooks
- Extensive test coverage for error scenarios

### 3. User Experience
- User-friendly error messages with clear guidance
- Automatic retry for recoverable errors
- Progressive error disclosure (basic message + detailed info)
- Contextual help and support options

### 4. Debugging and Monitoring
- Comprehensive error logging with trace IDs
- Structured error context for troubleshooting
- Error correlation between frontend and backend
- Performance impact monitoring for error handling

### 5. Maintainability
- Centralized error handling logic
- Easy addition of new error types and codes
- Consistent error handling patterns across the application
- Automated error testing and validation

## Usage Examples

### Backend Controller Error Handling
```csharp
[HttpPost]
public async Task<ActionResult> CreateContract([FromBody] CreateContractDto dto)
{
    try
    {
        // Validation
        if (string.IsNullOrEmpty(dto.ContractNumber))
        {
            return this.CreateValidationErrorResponse(new Dictionary<string, string[]>
            {
                [nameof(dto.ContractNumber)] = new[] { "Contract number is required" }
            });
        }

        // Business logic
        var result = await _contractService.CreateAsync(dto);
        return Ok(result);
    }
    catch (BusinessRuleException ex)
    {
        return this.CreateBusinessRuleErrorResponse(ex.ErrorCode, ex.Message, ex.Details);
    }
    catch (Exception ex)
    {
        return this.CreateErrorResponse(
            ErrorCodes.InternalServerError,
            "Failed to create contract",
            StatusCodes.Status500InternalServerError,
            ex.Message
        );
    }
}
```

### Frontend Error Handling with Hooks
```typescript
const MyComponent: React.FC = () => {
  const { error, isLoading, executeWithErrorHandling } = useErrorHandler();
  const { validationErrors, handleValidationError } = useFormErrorHandler();
  
  const handleSubmit = async (data: FormData) => {
    const result = await executeWithErrorHandling(
      () => contractApi.create(data),
      'Failed to create contract'
    );
    
    if (result) {
      // Success handling
    }
  };
  
  return (
    <div>
      {error && <ErrorDisplay error={error} onRetry={handleSubmit} />}
      {Object.keys(validationErrors).length > 0 && (
        <ValidationErrorDisplay errors={validationErrors} />
      )}
      {/* Form content */}
    </div>
  );
};
```

## Files Created/Modified

### Backend Files
1. `StandardErrorResponse.cs` - **NEW** - Standard error response model
2. `BusinessRuleException.cs` - **UPDATED** - Enhanced with error codes
3. `NotFoundException.cs` - **UPDATED** - Added error codes and context
4. `ValidationException.cs` - **UPDATED** - Enhanced error code support
5. `UnauthorizedException.cs` - **NEW** - Authentication error handling
6. `ForbiddenException.cs` - **NEW** - Authorization error handling
7. `ConflictException.cs` - **NEW** - Resource conflict handling
8. `GlobalExceptionMiddleware.cs` - **UPDATED** - Comprehensive error handling
9. `ErrorHandlingExtensions.cs` - **NEW** - Error handling utilities
10. `ContractMatchingController.cs` - **UPDATED** - Example implementation

### Frontend Files
1. `types/index.ts` - **UPDATED** - Error type definitions
2. `services/api.ts` - **UPDATED** - Enhanced error interceptor
3. `utils/errorUtils.ts` - **NEW** - Comprehensive error utilities
4. `components/Common/ErrorDisplay.tsx` - **NEW** - Error display components
5. `components/Common/ErrorBoundary.tsx` - **NEW** - React error boundary
6. `hooks/useErrorHandler.ts` - **NEW** - Error handling hooks
7. `services/contractMatchingApi.ts` - **UPDATED** - Example service implementation
8. `components/ContractMatching/ContractMatchingForm.tsx` - **NEW** - Example component
9. `utils/errorUtils.test.ts` - **NEW** - Comprehensive test coverage

## Next Steps

1. **Gradual Migration**: Update remaining controllers and services to use new error handling
2. **Error Monitoring**: Integrate with error tracking services (Sentry, Application Insights)
3. **Performance Monitoring**: Monitor error handling performance impact
4. **User Testing**: Validate user-friendly error messages with actual users
5. **Documentation**: Create developer guidelines for consistent error handling
6. **Training**: Educate development team on new error handling patterns

This implementation provides a robust, consistent, and user-friendly error handling system that improves both developer experience and user experience while maintaining high code quality and debugging capabilities.
