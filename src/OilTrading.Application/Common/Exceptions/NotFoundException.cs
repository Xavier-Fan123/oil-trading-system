namespace OilTrading.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    public NotFoundException()
        : base("The requested resource was not found.")
    {
        ErrorCode = "NOT_FOUND";
    }

    public NotFoundException(string message)
        : base(message)
    {
        ErrorCode = "NOT_FOUND";
    }

    public NotFoundException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "NOT_FOUND";
    }

    public NotFoundException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
        ErrorCode = "RESOURCE_NOT_FOUND";
        Details = new { EntityName = name, EntityKey = key };
    }

    public NotFoundException(string errorCode, string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
        ErrorCode = errorCode;
        Details = new { EntityName = name, EntityKey = key };
    }
}