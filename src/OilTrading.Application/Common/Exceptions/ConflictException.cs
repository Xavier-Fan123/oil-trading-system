namespace OilTrading.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a resource conflict occurs
/// </summary>
public class ConflictException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    public ConflictException()
        : base("A conflict occurred with the current state of the resource.")
    {
        ErrorCode = "CONFLICT";
    }

    public ConflictException(string message)
        : base(message)
    {
        ErrorCode = "CONFLICT";
    }

    public ConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ConflictException(string errorCode, string message, object? details)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "CONFLICT";
    }

    public ConflictException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
