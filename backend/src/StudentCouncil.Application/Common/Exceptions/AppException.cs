namespace StudentCouncil.Application.Common.Exceptions;

/// <summary>
/// Base type for expected application errors. The global exception handler maps
/// <see cref="StatusCode"/> and <see cref="Code"/> into an RFC 7807 ProblemDetails.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>Machine-readable error code (e.g. <c>not_found</c>).</summary>
    public abstract string Code { get; }

    /// <summary>HTTP status code this exception maps to.</summary>
    public abstract int StatusCode { get; }

    protected AppException(string message) : base(message)
    {
    }

    protected AppException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
