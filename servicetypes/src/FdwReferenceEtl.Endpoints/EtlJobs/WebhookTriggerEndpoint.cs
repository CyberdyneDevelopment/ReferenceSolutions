using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.Web.RestEndpoints.Security;
using FdwReferenceEtlJobs.Endpoints.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;

namespace FdwReferenceEtlJobs.Endpoints;

/// <summary>
/// An outside system starts a pipeline by calling a webhook trigger's address with its secret (FDW-1884):
/// <c>POST /etl/webhooks/{name}</c>, the secret in the <c>X-Webhook-Secret</c> header, a JSON body whose values the trigger's mappings
/// give to the pipeline's parameters. Answers 202 with the run's id.
/// </summary>
/// <remarks>
/// <para>
/// Why it is anonymous to the platform's sign-in: the caller is another system with no account. The secret is the proof, checked here
/// before anything of the body is read. An unknown name, a trigger that is off, a missing secret and a wrong secret are all answered 401
/// with no body, so a caller learns nothing about which names exist; the reason is logged.
/// </para>
/// <para>
/// Why the lookup runs as the system and the run as the trigger's tenant: an anonymous request has no ambient identity, and what the
/// configuration store returns is filtered by one. The trigger is found as the system (its row carries the tenant it belongs to), and
/// the run is started with that tenant relayed, exactly as a schedule's firing is, so its run roles are judged for what the pipeline
/// reaches the same way (<c>AuthorizeScheduled</c>).
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class WebhookTriggerEndpoint : EndpointWithoutRequest
{
    private const string SecretHeader = "X-Webhook-Secret";
    private const string TriggerSourceName = "Webhook";
    private const long LargestBody = 1_048_576;

    private readonly WebhookTriggerConfigurationProvider _webhooks;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly PipelineTrigger _pipelineTrigger;
    private readonly ILogger<WebhookTriggerEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="WebhookTriggerEndpoint"/> class.</summary>
    public WebhookTriggerEndpoint(
        WebhookTriggerConfigurationProvider webhooks,
        IExecutionTracker executionTracker,
        IDataGatewayProvider dataGateways,
        IEtlPipelineProvider pipelineProvider,
        IPipelineConfigurationProvider pipelineConfigurations,
        IAuthenticationContextAccessor accessor,
        IPipelineExecutionQueue pipelineQueue,
        IPipelineRunHolds holds,
        IPipelineStatusBroadcaster broadcaster,
        IRoleGrants grants,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        IDataStoreProvider dataStores,
        ILogger<WebhookTriggerEndpoint> logger)
    {
        _webhooks = webhooks;
        _accessor = accessor;
        _logger = logger;
        _pipelineTrigger = new PipelineTrigger(
            executionTracker, dataGateways, pipelineProvider, pipelineConfigurations, accessor, pipelineQueue, holds, broadcaster,
            new PipelineRunAuthority(grants, dataSets, connections, dataStores), logger);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("etl/webhooks/{name}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitPolicyNames.Standard));
        Tags("Webhooks");
        Summary(s =>
        {
            s.Summary = "Start a pipeline through a webhook trigger";
            s.Description = "Starts the trigger's pipeline. Send the trigger's secret in the X-Webhook-Secret header and a JSON body whose values the trigger's mappings give to the pipeline's parameters. Answers 202 with the run's id; 401, with no body, for an unknown or switched-off trigger or a wrong or missing secret.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        // Why "!": the route template is "etl/webhooks/{name}", so a request that reaches this handler always carries the segment.
        var name = Route<string>("name")!;
        var sent = HttpContext.Request.Headers[SecretHeader].ToString();
        var proven = await Authenticate(name, sent, ct).ConfigureAwait(false);
        if (proven is null)
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var body = await ReadBody(ct).ConfigureAwait(false);
        if (body is null)
        {
            await Send.ResponseAsync(new { error = "The body is larger than 1 MB." }, StatusCodes.Status413PayloadTooLarge, ct).ConfigureAwait(false);
            return;
        }

        var values = WebhookBodyMapper.Map(body, proven.Trigger.ParameterMaps);
        if (values.IsFailure || values.Value is not { } supplied)
        {
            var reason = string.Concat(values.CurrentMessage);
            WebhookCallLog.BodyRefused(_logger, name, reason);
            AddError(reason);
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        await Start(proven, supplied, ct).ConfigureAwait(false);
    }

    // The trigger the call is for, when the secret it sent proves it; null for every other case, whose reason is logged and never answered.
    private async Task<ProvenCall?> Authenticate(string name, string sent, CancellationToken ct)
    {
        IGenericResult<IWebhookTriggerImplementationConfiguration> read;
        using (new SystemAuthenticationContextScope(_accessor))
        {
            read = await _webhooks.Get(name, ct).ConfigureAwait(false);
        }

        if (read.IsFailure)
        {
            WebhookCallLog.Unreadable(_logger, name, string.Concat(read.CurrentMessage));
            return null;
        }

        if (read.Value is not WebhookTriggerConfiguration trigger)
        {
            WebhookCallLog.Refused(_logger, name, "no webhook trigger has that name");
            return null;
        }

        if (!trigger.IsEnabled)
        {
            WebhookCallLog.Refused(_logger, name, "the trigger is switched off");
            return null;
        }

        if (!WebhookSecret.Matches(sent, trigger.SecretHash))
        {
            WebhookCallLog.Refused(_logger, name, "the secret is missing or wrong");
            return null;
        }

        // Nothing stands in for a tenant: a trigger that names none cannot start a run.
        if (trigger.TenantId is not { } tenantId)
        {
            WebhookCallLog.Unreadable(_logger, name, "the trigger names no tenant");
            return null;
        }

        return new ProvenCall(trigger, tenantId);
    }

    // The body as text, or null when it is larger than the limit. Nothing larger is read.
    private async Task<string?> ReadBody(CancellationToken ct)
    {
        if (HttpContext.Request.ContentLength > LargestBody)
        {
            return null;
        }

        var limit = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
        {
            limit.MaxRequestBodySize = LargestBody;
        }

        using var reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    private async Task Start(ProvenCall proven, IReadOnlyList<KeyValuePair<string, string>> supplied, CancellationToken ct)
    {
        var trigger = proven.Trigger;
        WebhookCallLog.Accepted(_logger, trigger.Name, trigger.PipelineName);
        var request = new UnifiedTriggerRequest
        {
            Id = trigger.PipelineId,
            TriggerSource = TriggerSourceName,
            TenantId = proven.TenantId,
            Parameters = [.. supplied.Select(pair => new PipelineParameterValue { Name = pair.Key, Value = pair.Value })],
            RunRoles = [.. trigger.RunRoleNames],
        };

        IGenericResult<UnifiedTriggerResponse> result;
        using (new SystemAuthenticationContextScope(_accessor, proven.TenantId))
        {
            result = await _pipelineTrigger.Trigger(request, HttpContext.User, trigger.Name, ct).ConfigureAwait(false);
        }

        if (result.IsFailure || result.Value is not { } started)
        {
            AddError(EtlServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var status = string.Equals(started.Status, "QueueFull", StringComparison.Ordinal) ? 503 : 202;
        await Send.ResponseAsync(started, status, ct).ConfigureAwait(false);
    }

    // A call whose secret was proved: the trigger, and the tenant its run acts in.
    private sealed record ProvenCall(WebhookTriggerConfiguration Trigger, Guid TenantId);
}
