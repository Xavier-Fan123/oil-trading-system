using System.Text.Json.Serialization;

namespace OilTrading.Application.Common.Models;

/// <summary>
/// Standard error response format for all API endpoints
/// </summary>
public class StandardErrorResponse
{
    /// <summary>
    /// Unique error code identifying the specific error type
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (can be string or object)
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Unique trace ID for debugging and correlation
    /// </summary>
    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Request path that generated the error
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Validation errors (for validation failures)
    /// </summary>
    [JsonPropertyName("validationErrors")]
    public IDictionary<string, string[]>? ValidationErrors { get; set; }

    public StandardErrorResponse()
    {
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Standard error codes used throughout the application
/// </summary>
public static class ErrorCodes
{
    // Validation errors (400 range)
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidInput = "INVALID_INPUT";
    public const string MissingRequiredField = "MISSING_REQUIRED_FIELD";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string ValueOutOfRange = "VALUE_OUT_OF_RANGE";

    // Authentication errors (401)
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";

    // Authorization errors (403)
    public const string Forbidden = "FORBIDDEN";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    public const string AccessDenied = "ACCESS_DENIED";

    // Not found errors (404)
    public const string NotFound = "NOT_FOUND";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ContractNotFound = "CONTRACT_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";

    // Business logic errors (422)
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string InvalidBusinessOperation = "INVALID_BUSINESS_OPERATION";
    public const string ContractStateInvalid = "CONTRACT_STATE_INVALID";
    public const string InsufficientQuantity = "INSUFFICIENT_QUANTITY";
    public const string DuplicateEntry = "DUPLICATE_ENTRY";
    public const string ContractAlreadyMatched = "CONTRACT_ALREADY_MATCHED";
    public const string InvalidContractStatus = "INVALID_CONTRACT_STATUS";
    public const string PricingPeriodInvalid = "PRICING_PERIOD_INVALID";
    public const string LaycanPeriodInvalid = "LAYCAN_PERIOD_INVALID";

    // Server errors (500 range)
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
    public const string ConfigurationError = "CONFIGURATION_ERROR";

    // Rate limiting (429)
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // Conflict (409)
    public const string Conflict = "CONFLICT";
    public const string ResourceConflict = "RESOURCE_CONFLICT";

    // Timeout (408)
    public const string RequestTimeout = "REQUEST_TIMEOUT";
    public const string OperationTimeout = "OPERATION_TIMEOUT";
}

/// <summary>
/// Standard error messages for common error scenarios
/// </summary>
public static class ErrorMessages
{
    public const string ValidationFailed = "One or more validation errors occurred.";
    public const string Unauthorized = "You are not authorized to access this resource.";
    public const string Forbidden = "You do not have permission to perform this action.";
    public const string NotFound = "The requested resource was not found.";
    public const string InternalServerError = "An internal server error occurred while processing your request.";
    public const string ServiceUnavailable = "The service is temporarily unavailable. Please try again later.";
    public const string BusinessRuleViolation = "The operation violates one or more business rules.";
    public const string InvalidOperation = "The requested operation is not valid in the current state.";
    public const string DuplicateEntry = "A resource with the same identifier already exists.";
    public const string RateLimitExceeded = "Too many requests. Please try again later.";
    public const string RequestTimeout = "The request timed out. Please try again.";
    public const string DatabaseError = "A database error occurred while processing your request.";
    public const string ExternalServiceError = "An external service is temporarily unavailable.";
    public const string ConfigurationError = "A configuration error has been detected.";
}
