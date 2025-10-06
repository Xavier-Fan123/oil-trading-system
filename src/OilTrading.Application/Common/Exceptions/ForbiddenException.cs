namespace OilTrading.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user is authenticated but not authorized
/// </summary>
public class ForbiddenException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    public ForbiddenException()
        : base("You do not have permission to perform this action.")
    {
        ErrorCode = "FORBIDDEN";
    }

    public ForbiddenException(string message)
        : base(message)
    {
        ErrorCode = "FORBIDDEN";
    }

    public ForbiddenException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ForbiddenException(string errorCode, string message, object? details)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "FORBIDDEN";
    }

    public ForbiddenException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
