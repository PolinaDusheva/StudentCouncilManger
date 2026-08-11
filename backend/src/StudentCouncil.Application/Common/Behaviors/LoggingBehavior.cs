using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Common.Behaviors;

/// <summary>Structured request/duration logging around every MediatR handler.</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUser.Id;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next();
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} for user {UserId} in {ElapsedMs} ms",
                requestName, userId, stopwatch.ElapsedMilliseconds);
        }
    }
}
