using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OilTrading.Api.Middleware;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Common.Models;
using OilTrading.Core.Common;
using Xunit;

namespace OilTrading.Tests.Middleware;

/// <summary>
/// Comprehensive unit tests for GlobalExceptionMiddleware
/// Verifies correct handling of all exception types with proper HTTP status codes and error responses
/// </summary>
public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger;
    private readonly GlobalExceptionMiddleware _middleware;

    public GlobalExceptionMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _middleware = new GlobalExceptionMiddleware(
            next: (innerHttpContext) => throw new InvalidOperationException("This should be replaced by test setup"),
            logger: _mockLogger.Object
        );
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test HTTP context with a memory stream for response capture
    /// </summary>
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// Reads the JSON response from the HTTP context
    /// </summary>
    private static async Task<StandardErrorResponse?> GetResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(responseText))
            return null;

        return JsonSerializer.Deserialize<StandardErrorResponse>(responseText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Creates middleware with a custom request delegate
    /// </summary>
    private GlobalExceptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new GlobalExceptionMiddleware(next, _mockLogger.Object);
    }

    #endregion

    #region NotFoundException Tests (404)

    [Fact]
    public async Task NotFoundException_Returns404WithStandardErrorResponse()
    {
        // Arrange
        var exception = new NotFoundException("CONTRACT_NOT_FOUND", "Purchase contract PC-2024-001 was not found.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("CONTRACT_NOT_FOUND", response.Code);
        Assert.Equal("Purchase contract PC-2024-001 was not found.", response.Message);
        Assert.Equal("/api/test", response.Path);
        Assert.NotEmpty(response.TraceId);
        Assert.InRange(response.Timestamp, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task NotFoundException_WithEntityDetails_ReturnsDetailsInResponse()
    {
        // Arrange
        var exception = new NotFoundException("PurchaseContract", "PC-2024-001");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        // NotFoundException includes entity id in the message
        Assert.Contains("PC-2024-001", response.Message);
    }

    #endregion

    #region ValidationException Tests (422)

    [Fact]
    public async Task ValidationException_Returns422WithValidationErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]>
        {
            { "ContractNumber", new[] { "Contract number is required." } },
            { "Quantity", new[] { "Quantity must be greater than zero.", "Quantity cannot exceed 1,000,000 BBL." } }
        };
        var exception = new OilTrading.Application.Common.Exceptions.ValidationException(
            "VALIDATION_FAILED",
            "One or more validation errors occurred.",
            validationErrors
        );
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.UnprocessableEntity, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("VALIDATION_FAILED", response.Code);
        Assert.NotNull(response.ValidationErrors);
        Assert.Equal(2, response.ValidationErrors.Count);
        Assert.Contains("ContractNumber", response.ValidationErrors.Keys);
        Assert.Contains("Quantity", response.ValidationErrors.Keys);
        Assert.Equal(2, response.ValidationErrors["Quantity"].Length);
    }

    [Fact]
    public async Task FluentValidationException_Returns422WithProperlyFormattedErrors()
    {
        // Arrange
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Price", "Price must be greater than zero"),
            new FluentValidation.Results.ValidationFailure("Price", "Price format is invalid"),
            new FluentValidation.Results.ValidationFailure("DeliveryDate", "Delivery date must be in the future")
        };
        var exception = new FluentValidation.ValidationException(failures);
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.UnprocessableEntity, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("VALIDATION_FAILED", response.Code);
        Assert.NotNull(response.ValidationErrors);
        Assert.Contains("Price", response.ValidationErrors.Keys);
        Assert.Contains("DeliveryDate", response.ValidationErrors.Keys);
        Assert.Equal(2, response.ValidationErrors["Price"].Length);
    }

    #endregion

    #region BusinessRuleException Tests (422)

    [Fact]
    public async Task BusinessRuleException_Returns422WithBusinessRuleDetails()
    {
        // Arrange
        var exception = new BusinessRuleException(
            "LAYCAN_PERIOD_INVALID",
            "Laycan period must be at least 3 days.",
            new
            {
                RuleName = "LaycanPeriodValidation",
                EntityType = "PurchaseContract",
                EntityId = "PC-2024-001",
                MinimumDays = 3,
                ActualDays = 1
            }
        );
        exception.RuleName = "LaycanPeriodValidation";
        exception.EntityType = "PurchaseContract";
        exception.EntityId = "PC-2024-001";

        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.UnprocessableEntity, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("LAYCAN_PERIOD_INVALID", response.Code);
        Assert.Equal("Laycan period must be at least 3 days.", response.Message);
        Assert.NotNull(response.Details);
    }

    #endregion

    #region UnauthorizedException Tests (401)

    [Fact]
    public async Task UnauthorizedException_Returns401WithProperErrorResponse()
    {
        // Arrange
        var exception = new UnauthorizedException(
            "TOKEN_EXPIRED",
            "Your authentication token has expired. Please log in again."
        );
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("TOKEN_EXPIRED", response.Code);
        Assert.Equal("Your authentication token has expired. Please log in again.", response.Message);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns401WithStandardMessage()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("UNAUTHORIZED", response.Code);
        Assert.Equal("You are not authorized to access this resource.", response.Message);
    }

    #endregion

    #region ForbiddenException Tests (403)

    [Fact]
    public async Task ForbiddenException_Returns403WithProperErrorResponse()
    {
        // Arrange
        var exception = new ForbiddenException(
            "INSUFFICIENT_PERMISSIONS",
            "You do not have permission to approve contracts.",
            new { RequiredRole = "ContractApprover", CurrentRole = "Trader" }
        );
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("INSUFFICIENT_PERMISSIONS", response.Code);
        Assert.Equal("You do not have permission to approve contracts.", response.Message);
        Assert.NotNull(response.Details);
    }

    #endregion

    #region ConflictException Tests (409)

    [Fact]
    public async Task ConflictException_Returns409WithConflictDetails()
    {
        // Arrange
        var exception = new ConflictException(
            "CONTRACT_ALREADY_MATCHED",
            "This purchase contract has already been matched to a sales contract.",
            new
            {
                ContractId = "PC-2024-001",
                ExistingMatchId = "M-2024-100",
                AttemptedMatchId = "M-2024-101"
            }
        );
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("CONTRACT_ALREADY_MATCHED", response.Code);
        Assert.Equal("This purchase contract has already been matched to a sales contract.", response.Message);
        Assert.NotNull(response.Details);
    }

    #endregion

    #region DomainException Tests (400)

    [Fact]
    public async Task DomainException_Returns400WithErrorMessage()
    {
        // Arrange
        var exception = new DomainException("Invalid contract state transition from Active to Draft.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("BUSINESS_RULE_VIOLATION", response.Code);
        Assert.Equal("Invalid contract state transition from Active to Draft.", response.Message);
    }

    #endregion

    #region ArgumentException Tests (400)

    [Fact]
    public async Task ArgumentException_Returns400WithInvalidInputCode()
    {
        // Arrange
        var exception = new ArgumentException("Contract ID cannot be null or empty.", "contractId");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("INVALID_INPUT", response.Code);
        Assert.Contains("Contract ID cannot be null or empty.", response.Message);
    }

    #endregion

    #region InvalidOperationException Tests (400)

    [Fact]
    public async Task InvalidOperationException_Returns400WithInvalidOperationCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Cannot delete a contract that has associated settlements.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("INVALID_BUSINESS_OPERATION", response.Code);
        Assert.Equal("Cannot delete a contract that has associated settlements.", response.Message);
    }

    #endregion

    #region TimeoutException Tests (408)

    [Fact]
    public async Task TimeoutException_Returns408WithTimeoutMessage()
    {
        // Arrange
        var exception = new TimeoutException("Database query exceeded timeout limit of 30 seconds.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.RequestTimeout, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("REQUEST_TIMEOUT", response.Code);
        Assert.Equal("The request timed out. Please try again.", response.Message);
        Assert.NotNull(response.Details);
    }

    [Fact]
    public async Task TaskCanceledException_WithTimeoutInnerException_Returns408()
    {
        // Arrange
        var innerException = new TimeoutException("Operation timed out");
        var exception = new TaskCanceledException("Task was canceled", innerException);
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.RequestTimeout, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("OPERATION_TIMEOUT", response.Code);
    }

    [Fact]
    public async Task OperationCanceledException_Returns408WithTimeoutMessage()
    {
        // Arrange
        var exception = new OperationCanceledException("The operation was canceled by the user.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.RequestTimeout, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("OPERATION_TIMEOUT", response.Code);
    }

    #endregion

    #region Generic Exception Tests (500)

    [Fact]
    public async Task GenericException_Returns500WithInternalServerError()
    {
        // Arrange
        var exception = new Exception("Unexpected error occurred in the system.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("INTERNAL_SERVER_ERROR", response.Code);
        Assert.Equal("An internal server error occurred while processing your request.", response.Message);
        // Details is a JsonElement, convert to string for comparison
        var detailsStr = response.Details?.ToString();
        Assert.Equal("Please contact support if the problem persists.", detailsStr);
    }

    [Fact]
    public async Task NullReferenceException_Returns500WithInternalServerError()
    {
        // Arrange
        var exception = new NullReferenceException("Object reference not set to an instance of an object.");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal("INTERNAL_SERVER_ERROR", response.Code);
    }

    #endregion

    #region TraceId and Logging Tests

    [Fact]
    public async Task ExceptionHandling_LogsErrorWithTraceId()
    {
        // Arrange
        var exception = new NotFoundException("Resource not found");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task ExceptionHandling_IncludesTraceIdInResponse()
    {
        // Arrange
        var exception = new NotFoundException("Resource not found");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.NotEmpty(response.TraceId);
        Assert.True(response.TraceId == context.TraceIdentifier || !string.IsNullOrEmpty(response.TraceId));
    }

    #endregion

    #region Success Path Tests

    [Fact]
    public async Task NoException_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task ErrorResponse_HasCorrectJsonFormat()
    {
        // Arrange
        var exception = new NotFoundException("CONTRACT_NOT_FOUND", "Contract not found");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        Assert.Contains("\"code\":", json);
        Assert.Contains("\"message\":", json);
        Assert.Contains("\"timestamp\":", json);
        Assert.Contains("\"traceId\":", json);
        Assert.Contains("\"statusCode\":", json);
        Assert.Contains("\"path\":", json);
    }

    [Fact]
    public async Task ErrorResponse_UsesCamelCaseNaming()
    {
        // Arrange
        var exception = new NotFoundException("NOT_FOUND", "Resource not found");
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        // Verify camelCase is used (not PascalCase or snake_case)
        Assert.Contains("\"statusCode\":", json);
        Assert.DoesNotContain("\"StatusCode\":", json);
        Assert.DoesNotContain("\"status_code\":", json);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Exception_WithNullMessage_HandledGracefully()
    {
        // Arrange
        var exception = new NotFoundException();
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.NotEmpty(response.Message);
    }

    [Fact]
    public async Task Exception_WithVeryLongMessage_HandledCorrectly()
    {
        // Arrange
        var longMessage = new string('x', 5000);
        var exception = new DomainException(longMessage);
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var response = await GetResponseAsync(context);
        Assert.NotNull(response);
        Assert.Equal(longMessage, response.Message);
    }

    #endregion
}
