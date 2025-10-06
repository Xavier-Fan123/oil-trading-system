using System.Net;
using System.Text.Json;
using System.Diagnostics;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Common.Models;
using OilTrading.Core.Common;
using FluentValidation;

namespace OilTrading.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            _logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", traceId);
            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
    {
        context.Response.ContentType = "application/json";

        var response = new StandardErrorResponse
        {
            TraceId = traceId,
            Path = context.Request.Path.Value
        };

        switch (exception)
        {
            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Code = notFoundEx.ErrorCode;
                response.Message = notFoundEx.Message;
                response.Details = notFoundEx.Details;
                break;

            case Application.Common.Exceptions.ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                response.Code = validationEx.ErrorCode;
                response.Message = validationEx.Message;
                response.ValidationErrors = validationEx.Errors;
                break;

            case BusinessRuleException businessEx:
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                response.Code = businessEx.ErrorCode;
                response.Message = businessEx.Message;
                response.Details = businessEx.Details ?? new 
                { 
                    RuleName = businessEx.RuleName,
                    EntityType = businessEx.EntityType,
                    EntityId = businessEx.EntityId
                };
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Code = unauthorizedEx.ErrorCode;
                response.Message = unauthorizedEx.Message;
                response.Details = unauthorizedEx.Details;
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Code = forbiddenEx.ErrorCode;
                response.Message = forbiddenEx.Message;
                response.Details = forbiddenEx.Details;
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Code = conflictEx.ErrorCode;
                response.Message = conflictEx.Message;
                response.Details = conflictEx.Details;
                break;

            case DomainException domainEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ErrorCodes.BusinessRuleViolation;
                response.Message = domainEx.Message;
                break;

            case FluentValidation.ValidationException fluentValidationEx:
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                response.Code = ErrorCodes.ValidationFailed;
                response.Message = ErrorMessages.ValidationFailed;
                response.ValidationErrors = fluentValidationEx.Errors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Code = ErrorCodes.Unauthorized;
                response.Message = ErrorMessages.Unauthorized;
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ErrorCodes.InvalidInput;
                response.Message = argEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ErrorCodes.InvalidBusinessOperation;
                response.Message = invalidOpEx.Message;
                break;

            case TimeoutException timeoutEx:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Code = ErrorCodes.RequestTimeout;
                response.Message = ErrorMessages.RequestTimeout;
                response.Details = timeoutEx.Message;
                break;

            case TaskCanceledException when exception.InnerException is TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Code = ErrorCodes.OperationTimeout;
                response.Message = ErrorMessages.RequestTimeout;
                break;

            case OperationCanceledException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Code = ErrorCodes.OperationTimeout;
                response.Message = ErrorMessages.RequestTimeout;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Code = ErrorCodes.InternalServerError;
                response.Message = ErrorMessages.InternalServerError;
                response.Details = "Please contact support if the problem persists.";
                break;
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

// Legacy error response for backward compatibility (deprecated)
[Obsolete("Use StandardErrorResponse instead")]
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}