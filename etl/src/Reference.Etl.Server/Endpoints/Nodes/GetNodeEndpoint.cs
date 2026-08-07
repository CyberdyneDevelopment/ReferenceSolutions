using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Etl.Server.Logging;

namespace Reference.Etl.Server.Endpoints.Nodes;

/// <summary>
/// Request for getting an orchestration node (with optional deep load).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetNodeRequest
{
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth of children to load recursively.
    /// Null = unlimited.
    /// </summary>
    public int? Depth { get; set; }
}

/// <summary>
/// <c>GET /etl/nodes/{id}</c> — get a node by its logical identifier.
/// Supports recursive load via optional <c>depth</c> query parameter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetNodeEndpoint : Endpoint<GetNodeRequest, NodeDto>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<GetNodeEndpoint> _logger;

    public GetNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<GetNodeEndpoint> logger)
    {
        _provider = provider;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<GetNodeEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("etl/nodes/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get an orchestration node";
            s.Description = "Retrieves an orchestration node by id. " +
                             "Supply ?depth=N to load children recursively up to N levels.";
        });
    }

    public override async Task HandleAsync(GetNodeRequest req, CancellationToken ct)
    {
        EtlServerLog.GetNodeRequestReceived(_logger, req.Id);

        // Why: use deep load when depth is specified so the caller gets the full subtree in one
        // round trip; use shallow load otherwise to avoid unnecessary DB reads.
        var result = req.Depth.HasValue
            ? await _provider.Get(req.Id, req.Depth.Value, ct).ConfigureAwait(false)
            : await _provider.Get(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            EtlServerLog.NodeNotFound(_logger, req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(CreateNodeEndpoint.MapToDto(result.Value), ct).ConfigureAwait(false);
    }
}
