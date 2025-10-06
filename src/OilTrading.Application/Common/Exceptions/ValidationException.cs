using FluentValidation.Results;

namespace OilTrading.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public string ErrorCode { get; }
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        ErrorCode = "VALIDATION_FAILED";
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string errorCode)
        : base("One or more validation failures have occurred.")
    {
        ErrorCode = errorCode;
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(string errorCode, IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures have occurred.")
    {
        ErrorCode = errorCode;
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(string errorCode, string message, IDictionary<string, string[]> errors)
        : base(message)
    {
        ErrorCode = errorCode;
        Errors = errors;
    }
}