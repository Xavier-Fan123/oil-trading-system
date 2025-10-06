using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.Common.Models;
using OilTrading.Application.Common.Exceptions;
using System.Diagnostics;

namespace OilTrading.Api.Common.Utilities;

/// <summary>
/// Extension methods for standardized error handling
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Creates a standardized error response for API controllers
    /// </summary>
    public static ObjectResult CreateErrorResponse(this ControllerBase controller, string errorCode, string message, int statusCode, object? details = null)
    {
        var response = new StandardErrorResponse
        {
            Code = errorCode,
            Message = message,
            StatusCode = statusCode,
            Details = details,
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = statusCode };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ObjectResult CreateValidationErrorResponse(this ControllerBase controller, IDictionary<string, string[]> validationErrors, string? message = null)
    {
        var response = new StandardErrorResponse
        {
            Code = ErrorCodes.ValidationFailed,
            Message = message ?? ErrorMessages.ValidationFailed,
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            ValidationErrors = validationErrors,
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }

    /// <summary>
    /// Creates a not found error response
    /// </summary>
    public static ObjectResult CreateNotFoundResponse(this ControllerBase controller, string resourceName, object resourceId)
    {
        var response = new StandardErrorResponse
        {
            Code = ErrorCodes.ResourceNotFound,
            Message = $"Entity \"{resourceName}\" ({resourceId}) was not found.",
            StatusCode = StatusCodes.Status404NotFound,
            Details = new { EntityName = resourceName, EntityId = resourceId },
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = StatusCodes.Status404NotFound };
    }

    /// <summary>
    /// Creates a business rule violation error response
    /// </summary>
    public static ObjectResult CreateBusinessRuleErrorResponse(this ControllerBase controller, string errorCode, string message, object? details = null)
    {
        var response = new StandardErrorResponse
        {
            Code = errorCode,
            Message = message,
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            Details = details,
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }

    /// <summary>
    /// Creates an unauthorized error response
    /// </summary>
    public static ObjectResult CreateUnauthorizedResponse(this ControllerBase controller, string? message = null)
    {
        var response = new StandardErrorResponse
        {
            Code = ErrorCodes.Unauthorized,
            Message = message ?? ErrorMessages.Unauthorized,
            StatusCode = StatusCodes.Status401Unauthorized,
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = StatusCodes.Status401Unauthorized };
    }

    /// <summary>
    /// Creates a forbidden error response
    /// </summary>
    public static ObjectResult CreateForbiddenResponse(this ControllerBase controller, string? message = null)
    {
        var response = new StandardErrorResponse
        {
            Code = ErrorCodes.Forbidden,
            Message = message ?? ErrorMessages.Forbidden,
            StatusCode = StatusCodes.Status403Forbidden,
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = StatusCodes.Status403Forbidden };
    }

    /// <summary>
    /// Creates a conflict error response
    /// </summary>
    public static ObjectResult CreateConflictResponse(this ControllerBase controller, string errorCode, string message, object? details = null)
    {
        var response = new StandardErrorResponse
        {
            Code = errorCode,
            Message = message,
            StatusCode = StatusCodes.Status409Conflict,
            Details = details,
            TraceId = Activity.Current?.Id ?? controller.HttpContext.TraceIdentifier,
            Path = controller.HttpContext.Request.Path.Value
        };

        return new ObjectResult(response) { StatusCode = StatusCodes.Status409Conflict };
    }
}

/// <summary>
/// Result wrapper for operations that can fail
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string ErrorCode { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public object? ErrorDetails { get; private set; }

    private Result(bool isSuccess, T? value, string errorCode, string errorMessage, object? errorDetails)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty, string.Empty, null);
    
    public static Result<T> Failure(string errorCode, string errorMessage, object? errorDetails = null) => 
        new(false, default, errorCode, errorMessage, errorDetails);

    public ObjectResult ToActionResult(ControllerBase controller, int successStatusCode = StatusCodes.Status200OK)
    {
        if (IsSuccess)
        {
            return new ObjectResult(Value) { StatusCode = successStatusCode };
        }

        return controller.CreateErrorResponse(ErrorCode, ErrorMessage, GetStatusCodeFromErrorCode(ErrorCode), ErrorDetails);
    }

    private static int GetStatusCodeFromErrorCode(string errorCode)
    {
        return errorCode switch
        {
            ErrorCodes.NotFound or ErrorCodes.ResourceNotFound or ErrorCodes.ContractNotFound or ErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Unauthorized or ErrorCodes.InvalidCredentials or ErrorCodes.TokenExpired or ErrorCodes.TokenInvalid => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden or ErrorCodes.InsufficientPermissions or ErrorCodes.AccessDenied => StatusCodes.Status403Forbidden,
            ErrorCodes.ValidationFailed or ErrorCodes.BusinessRuleViolation or ErrorCodes.InvalidBusinessOperation => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.Conflict or ErrorCodes.ResourceConflict or ErrorCodes.DuplicateEntry => StatusCodes.Status409Conflict,
            ErrorCodes.InvalidInput or ErrorCodes.MissingRequiredField or ErrorCodes.InvalidFormat or ErrorCodes.ValueOutOfRange => StatusCodes.Status400BadRequest,
            ErrorCodes.RequestTimeout or ErrorCodes.OperationTimeout => StatusCodes.Status408RequestTimeout,
            ErrorCodes.RateLimitExceeded => StatusCodes.Status429TooManyRequests,
            ErrorCodes.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

/// <summary>
/// Result wrapper for operations that don't return a value
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string ErrorCode { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public object? ErrorDetails { get; private set; }

    private Result(bool isSuccess, string errorCode, string errorMessage, object? errorDetails)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    public static Result Success() => new(true, string.Empty, string.Empty, null);
    
    public static Result Failure(string errorCode, string errorMessage, object? errorDetails = null) => 
        new(false, errorCode, errorMessage, errorDetails);

    public IActionResult ToActionResult(ControllerBase controller, int successStatusCode = StatusCodes.Status204NoContent)
    {
        if (IsSuccess)
        {
            return new StatusCodeResult(successStatusCode);
        }

        return controller.CreateErrorResponse(ErrorCode, ErrorMessage, GetStatusCodeFromErrorCode(ErrorCode), ErrorDetails);
    }

    private static int GetStatusCodeFromErrorCode(string errorCode)
    {
        return errorCode switch
        {
            ErrorCodes.NotFound or ErrorCodes.ResourceNotFound or ErrorCodes.ContractNotFound or ErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Unauthorized or ErrorCodes.InvalidCredentials or ErrorCodes.TokenExpired or ErrorCodes.TokenInvalid => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden or ErrorCodes.InsufficientPermissions or ErrorCodes.AccessDenied => StatusCodes.Status403Forbidden,
            ErrorCodes.ValidationFailed or ErrorCodes.BusinessRuleViolation or ErrorCodes.InvalidBusinessOperation => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.Conflict or ErrorCodes.ResourceConflict or ErrorCodes.DuplicateEntry => StatusCodes.Status409Conflict,
            ErrorCodes.InvalidInput or ErrorCodes.MissingRequiredField or ErrorCodes.InvalidFormat or ErrorCodes.ValueOutOfRange => StatusCodes.Status400BadRequest,
            ErrorCodes.RequestTimeout or ErrorCodes.OperationTimeout => StatusCodes.Status408RequestTimeout,
            ErrorCodes.RateLimitExceeded => StatusCodes.Status429TooManyRequests,
            ErrorCodes.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
