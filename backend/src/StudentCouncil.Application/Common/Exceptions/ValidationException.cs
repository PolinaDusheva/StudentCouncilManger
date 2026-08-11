using FluentValidation.Results;

namespace StudentCouncil.Application.Common.Exceptions;

/// <summary>Thrown when input fails validation. Maps to 400 with a per-field error dictionary.</summary>
public sealed class ValidationException : AppException
{
    public override string Code => "validation_error";
    public override int StatusCode => 400;

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.Distinct().ToArray());
    }
}
