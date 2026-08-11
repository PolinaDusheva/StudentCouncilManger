using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Base for the periodic jobs (decision #6): the timer loop lives here, the actual work lives in
/// <see cref="ExecuteTickAsync"/>. Each tick runs in its own DI scope, and a failed tick is logged but
/// never tears down the host. <see cref="ExecuteTickAsync"/> is public so tests can drive a single
/// deterministic tick against a fresh scope, with the timer disabled.
/// </summary>
public abstract class PeriodicBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected PeriodicBackgroundService(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>How often the timer fires.</summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>Stable name used for logging and <c>job_runs</c> idempotency.</summary>
    protected abstract string JobName { get; }

    /// <summary>The job's work for a single tick. Resolves its dependencies from <paramref name="services"/>.</summary>
    public abstract Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await SafeWaitAsync(timer, stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await ExecuteTickAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job {JobName} tick failed.", JobName);
            }
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
