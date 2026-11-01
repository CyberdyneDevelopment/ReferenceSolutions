using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// The address an outside system calls to start a pipeline through a webhook trigger (FDW-1884): this host is the one reachable from
/// outside, so it forwards the call, the body and the <c>X-Webhook-Secret</c> header, to the ETL server, which checks the secret and
/// answers; the answer is relayed unchanged.
/// </summary>
/// <remarks>
/// Why it is anonymous here too: the caller is another system with no account, and the ETL server's route, not this one, proves the call.
/// This host adds no check of its own and keeps no copy of any secret.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class WebhookCallProxyEndpoint : EndpointWithoutRequest
{
    private const string SecretHeader = "X-Webhook-Secret";
    private const long LargestBody = 1_048_576;

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<WebhookCallProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="WebhookCallProxyEndpoint"/> class.</summary>
    /// <param name="factory">Supplies the named client that reaches the ETL server.</param>
    /// <param name="logger">The logger.</param>
    public WebhookCallProxyEndpoint(IHttpClientFactory factory, ILogger<WebhookCallProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/etl/webhooks/{name}");
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
        if (HttpContext.Request.ContentLength > LargestBody)
        {
            await Send.ResponseAsync(new { error = "The body is larger than 1 MB." }, StatusCodes.Status413PayloadTooLarge, ct).ConfigureAwait(false);
            return;
        }

        // Why "!": the route template is "/etl/webhooks/{name}", so a request that reaches this handler always carries the segment.
        var name = Route<string>("name")!;

        // Why the full name: inside a FastEndpoints endpoint a bare HttpMethod is the endpoint's own Http member, not System.Net.Http.HttpMethod.
        var relayed = await EtlForwarder.Forward(
            HttpContext, _factory, _logger, System.Net.Http.HttpMethod.Post, $"etl/webhooks/{Uri.EscapeDataString(name)}", [SecretHeader], ct).ConfigureAwait(false);
        if (relayed)
        {
            return;
        }

        AddError("Failed to proxy request to EtlServer");
        await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
    }
}
