using System;

namespace OilTrading.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }
    public string? RuleName { get; set; }
    public string? EntityType { get; set; }
    public object? EntityId { get; set; }

    public BusinessRuleException() : base()
    {
        ErrorCode = "BUSINESS_RULE_VIOLATION";
    }

    public BusinessRuleException(string message) : base(message)
    {
        ErrorCode = "BUSINESS_RULE_VIOLATION";
    }

    public BusinessRuleException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "BUSINESS_RULE_VIOLATION";
    }

    public BusinessRuleException(string errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string errorCode, string message, object? details) : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public BusinessRuleException(string errorCode, string message, object? details, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}