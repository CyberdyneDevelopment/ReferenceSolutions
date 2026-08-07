using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Logging;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Background job that pre-computes frequently-used calculations.
/// Queries cfg.CalculationUsage for stale, high-value calculations and
/// executes them via the API so results are cached for subsequent requests.
/// </summary>
public sealed class PreComputeCalculationsJob : IHostedService, IDisposable
{
    private readonly ILogger<PreComputeCalculationsJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly PreComputeOptions _options;
    private Timer? _timer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public PreComputeCalculationsJob(
        IOptions<PreComputeOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PreComputeCalculationsJob> logger)
    {
        _options = options.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            PreComputeLog.JobDisabled(_logger);
            return Task.CompletedTask;
        }

        PreComputeLog.JobStarted(_logger, _options.IntervalMinutes);
        PreComputeLog.JobFirstRun(_logger, _options.InitialDelayMinutes);

        _timer = new Timer(
            Execute,
            null,
            TimeSpan.FromMinutes(_options.InitialDelayMinutes),
            TimeSpan.FromMinutes(_options.IntervalMinutes));

        return Task.CompletedTask;
    }

    private void Execute(object? state)
    {
        _ = ExecuteCore();
    }

    private async Task ExecuteCore()
    {
        if (!await _semaphore.WaitAsync(0).ConfigureAwait(false))
        {
            PreComputeLog.JobSkippedStillRunning(_logger);
            return;
        }

        try
        {
            await ExecutePreCompute().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            PreComputeLog.JobStopped(_logger);
        }
        catch (Exception ex)
        {
            PreComputeLog.JobFailedUnexpectedError(_logger, ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ExecutePreCompute()
    {
        var stopwatch = Stopwatch.StartNew();

        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICalculationUsageRepository>();
        var apiClient = scope.ServiceProvider.GetRequiredService<ICalculationApiClient>();

        // 1. Query stale, high-value calculations
        var staleResult = await repository.GetStaleCalculations(
            _options.MinExecutionCountThreshold,
            _options.StalenessThresholdMinutes,
            _options.MaxCalculationsPerRun,
            CancellationToken.None).ConfigureAwait(false);

        if (!staleResult.IsSuccess || staleResult.Value == null)
        {
            PreComputeLog.FoundStaleCalculations(_logger, 0);
            PreComputeLog.JobCompleted(_logger, 0, 0);
            return;
        }

        var calculations = staleResult.Value;
        PreComputeLog.FoundStaleCalculations(_logger, calculations.Count);

        if (calculations.Count == 0)
        {
            PreComputeLog.JobCompleted(_logger, 0, 0);
            return;
        }

        // 2. Execute each calculation via the API
        var successCount = 0;
        var failureCount = 0;

        for (var i = 0; i < calculations.Count; i++)
        {
            var calc = calculations[i];

            var executeResult = await apiClient.Execute(
                calc.CalculationType,
                calc.CalculationHash,
                CancellationToken.None).ConfigureAwait(false);

            if (executeResult.IsSuccess)
            {
                PreComputeLog.CalculationPreComputed(_logger, calc.CalculationType, executeResult.Value);
                successCount++;

                // Update LastCachedAt so this calculation won't be picked up again until stale
                var updateResult = await repository.UpdateLastCachedAt(calc.CalculationHash, CancellationToken.None).ConfigureAwait(false);
                if (!updateResult.IsSuccess)
                {
                    PreComputeLog.FailedToUpdateCache(_logger, calc.CalculationHash, PreComputeLog.GetError(updateResult, _logger));
                }
            }
            else
            {
                failureCount++;
            }

            // 3. Throttle between calculations (skip delay after last one)
            if (i < calculations.Count - 1 && _options.DelayBetweenCalculationsMs > 0)
            {
                PreComputeLog.Throttling(_logger, _options.DelayBetweenCalculationsMs);
                await Task.Delay(_options.DelayBetweenCalculationsMs).ConfigureAwait(false);
            }
        }

        stopwatch.Stop();

        // 4. Record run stats in PreComputeSchedule
        var recordResult = await repository.RecordPreComputeRun(
            successCount,
            failureCount,
            calculations.Count,
            stopwatch.ElapsedMilliseconds,
            CancellationToken.None).ConfigureAwait(false);

        if (!recordResult.IsSuccess)
        {
            PreComputeLog.FailedToRecordRunStats(_logger, PreComputeLog.GetError(recordResult, _logger));
        }

        PreComputeLog.JobCompleted(_logger, successCount, failureCount);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        PreComputeLog.JobStopped(_logger);
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _timer?.Dispose();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}
