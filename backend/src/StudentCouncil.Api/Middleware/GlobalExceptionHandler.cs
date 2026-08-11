using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 ProblemDetails. Known
/// <see cref="AppException"/> subclasses map to their declared status/code;
/// everything else becomes a generic 500 (no internals leaked in production).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string TypeBase = "https://api.studentcouncil.ue-varna.bg/errors/";

    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title) = Map(exception);

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled {Code} ({Status}): {Message}", code, status, exception.Message);
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        ProblemDetails problem = exception is ValidationException validation
            ? new ValidationProblemDetails(
                validation.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Type = TypeBase + code;
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;

        if (status >= 500 && (_environment.IsDevelopment() || _environment.IsStaging()))
        {
            problem.Detail = exception.ToString();
        }

        httpContext.Response.StatusCode = status;
        // Serialize by runtime type so ValidationProblemDetails.Errors is included.
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, cancellationToken);
        return true;
    }

    private (int Status, string Code, string Title) Map(Exception exception) => exception switch
    {
        AppException appException => (appException.StatusCode, appException.Code, appException.Message),
        _ => (StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred.")
    };
}
