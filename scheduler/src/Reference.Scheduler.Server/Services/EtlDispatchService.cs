using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Resiliency.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Logging;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Default implementation of ETL dispatch service using FDW resiliency pipelines.
/// </summary>
public sealed class EtlDispatchService : IEtlDispatchService
{
    private readonly IPipelineJobClient _client;
    private readonly ILogger<EtlDispatchService> _logger;
    private readonly IOptions<EtlDispatchConfiguration> _options;
    private readonly IResiliencyPipelineFactory _pipelineFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlDispatchService"/> class.
    /// </summary>
    /// <param name="client">The pipeline job client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="pipelineFactory">The resiliency pipeline factory.</param>
    public EtlDispatchService(
        IPipelineJobClient client,
        ILogger<EtlDispatchService> logger,
        IOptions<EtlDispatchConfiguration> options,
        IResiliencyPipelineFactory pipelineFactory)
    {
        _client = client;
        _logger = logger;
        _options = options;
        _pipelineFactory = pipelineFactory;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Dispatch(
        string scheduleName,
        string pipelineName,
        string triggerSource,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var config = _options.Value;

        if (!config.Enabled)
        {
            SchedulerServerLog.DispatchDisabled(_logger, scheduleName);
            return GenericResult.Success();
        }

        SchedulerServerLog.DispatchStarted(_logger, scheduleName, pipelineName);

        var pipelineResult = _pipelineFactory.GetOrCreate("HttpClient", "EtlDispatch");
        if (!pipelineResult.IsSuccess)
        {
            return GenericResult.Failure(
                SchedulerServerLog.DispatchFailed(_logger,
                    new InvalidOperationException("Failed to create resiliency pipeline"),
                    scheduleName, pipelineName));
        }

        if (pipelineResult.Value is not { } pipeline)
        {
            return GenericResult.Failure(
                SchedulerServerLog.DispatchFailed(_logger,
                    new InvalidOperationException("Resiliency pipeline was null"),
                    scheduleName, pipelineName));
        }

        try
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                var result = await DispatchOnce(scheduleName, pipelineName, triggerSource, tenantId, ct)
                    .ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        SchedulerServerLog.GetError(result, _logger));
                }
            }, cancellationToken).ConfigureAwait(false);

            SchedulerServerLog.DispatchCompleted(_logger, scheduleName, pipelineName);
            return GenericResult.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SchedulerServerLog.DispatchFailed(_logger, ex, scheduleName, pipelineName));
        }
    }

    private async Task<IGenericResult> DispatchOnce(
        string scheduleName,
        string pipelineName,
        string triggerSource,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var request = new TriggerPipelineRequest
        {
            Name = pipelineName,
            TriggerSource = triggerSource,
            ScheduleName = scheduleName,
            TenantId = tenantId
        };

        var result = await _client.Trigger(request, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return GenericResult.Success();
        }

        return GenericResult.Failure(result.Messages.ToArray());
    }
}
