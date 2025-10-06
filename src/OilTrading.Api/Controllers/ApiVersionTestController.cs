using Microsoft.AspNetCore.Mvc;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/version-test")]
[ApiVersion("2.0")]
public class ApiVersionTestController : ControllerBase
{
    private readonly ILogger<ApiVersionTestController> _logger;

    public ApiVersionTestController(ILogger<ApiVersionTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to verify API versioning is working correctly
    /// </summary>
    /// <returns>Version information and test data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(VersionTestResponseDto), StatusCodes.Status200OK)]
    public IActionResult GetVersionInfo()
    {
        var response = new VersionTestResponseDto
        {
            ApiVersion = "2.0",
            Message = "API Versioning is configured successfully",
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            SupportedVersions = new[] { "2.0" },
            VersioningStrategy = "URL Segment"
        };

        _logger.LogInformation(
            "API version test endpoint called. Version: {Version}, Environment: {Environment}",
            response.ApiVersion, response.EnvironmentName);

        return Ok(response);
    }

    /// <summary>
    /// Test endpoint to verify API versioning with parameters
    /// </summary>
    /// <param name="testValue">Test parameter</param>
    /// <returns>Echo response with version information</returns>
    [HttpGet("echo/{testValue}")]
    [ProducesResponseType(typeof(VersionEchoResponseDto), StatusCodes.Status200OK)]
    public IActionResult EchoTest(string testValue)
    {
        var response = new VersionEchoResponseDto
        {
            ApiVersion = "2.0",
            EchoValue = testValue,
            Timestamp = DateTime.UtcNow,
            RequestPath = HttpContext.Request.Path.ToString(),
            QueryString = HttpContext.Request.QueryString.ToString()
        };

        return Ok(response);
    }

    /// <summary>
    /// Test endpoint to verify API versioning with query parameters
    /// </summary>
    /// <param name="name">Name parameter</param>
    /// <param name="value">Value parameter</param>
    /// <returns>Query parameter test response</returns>
    [HttpGet("query-test")]
    [ProducesResponseType(typeof(QueryTestResponseDto), StatusCodes.Status200OK)]
    public IActionResult QueryTest([FromQuery] string? name, [FromQuery] int? value)
    {
        var response = new QueryTestResponseDto
        {
            ApiVersion = "2.0",
            Name = name ?? "No name provided",
            Value = value ?? 0,
            Timestamp = DateTime.UtcNow,
            Headers = HttpContext.Request.Headers
                .Where(h => h.Key.StartsWith("api-", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString())
        };

        return Ok(response);
    }
}

/// <summary>
/// Version test response DTO
/// </summary>
public class VersionTestResponseDto
{
    public string ApiVersion { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public string[] SupportedVersions { get; set; } = Array.Empty<string>();
    public string VersioningStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Version echo response DTO
/// </summary>
public class VersionEchoResponseDto
{
    public string ApiVersion { get; set; } = string.Empty;
    public string EchoValue { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string RequestPath { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
}

/// <summary>
/// Query test response DTO
/// </summary>
public class QueryTestResponseDto
{
    public string ApiVersion { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}
