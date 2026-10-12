using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Proxies the execution list to the ETL server.</summary>
/// <remarks>
/// <c>Fdw.Operations.Endpoints.Executions.ListExecutionsEndpointBase</c> already carries a
/// <c>[TypeOption]</c> and is live on the ETL host at bare <c>GET /executions</c> -- nothing routed
/// it through reference-api. Same raw-forward shape as <see cref="GetEtlExecutionStatusProxyEndpoint"/>
/// (its sibling on the same resource): no typed client exists for this route, so this talks to
/// <see cref="EtlOrchestrationProxy.ClientName"/> directly and forwards the response body/status
/// verbatim rather than round-tripping through a duplicate set of DTOs.
/// </remarks>
public sealed class ListExecutionsProxyEndpoint : Endpoint<ListExecutionsProxyRequest>
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ListExecutionsProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ListExecutionsProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public ListExecutionsProxyEndpoint(IHttpClientFactory factory, ILogger<ListExecutionsProxyEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/executions");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "List executions (proxy)";
            s.Description = "Forwards to the ETL server, which owns execution tracking.";
        });
        Tags("EtlOrchestration");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListExecutionsProxyRequest req, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>();
        if (req.Page.HasValue) query["page"] = req.Page.Value.ToString(CultureInfo.InvariantCulture);
        if (req.PageSize.HasValue) query["pageSize"] = req.PageSize.Value.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(req.ItemType)) query["itemType"] = req.ItemType;
        if (!string.IsNullOrWhiteSpace(req.State)) query["state"] = req.State;
        if (!string.IsNullOrWhiteSpace(req.CorrelationId)) query["correlationId"] = req.CorrelationId;
        if (req.RootId.HasValue) query["rootId"] = req.RootId.Value.ToString();
        var path = QueryHelpers.AddQueryString("executions", query);

        ProxyLog.ProxyRequest(_logger, "GET", "Etl", path);

        var client = _factory.CreateClient(EtlOrchestrationProxy.ClientName);
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(path, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            ProxyLog.ProxyFailed(_logger, "Etl", ex.Message);
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                ProxyLog.ProxyFailed(_logger, "Etl", $"status {(int)response.StatusCode}");

            await UpstreamPassThrough.Relay(HttpContext, response, body, ct).ConfigureAwait(false);
        }
    }
}
