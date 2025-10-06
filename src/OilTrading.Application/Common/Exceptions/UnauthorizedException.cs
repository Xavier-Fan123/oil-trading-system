namespace OilTrading.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user is not authenticated
/// </summary>
public class UnauthorizedException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    public UnauthorizedException()
        : base("You are not authorized to access this resource.")
    {
        ErrorCode = "UNAUTHORIZED";
    }

    public UnauthorizedException(string message)
        : base(message)
    {
        ErrorCode = "UNAUTHORIZED";
    }

    public UnauthorizedException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public UnauthorizedException(string errorCode, string message, object? details)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "UNAUTHORIZED";
    }

    public UnauthorizedException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
