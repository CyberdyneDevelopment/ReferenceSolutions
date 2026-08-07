using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Nodes;

/// <summary>
/// Request to get a node by ID, optionally inflating children to a given depth.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetNodeRequest
{
    /// <summary>Gets or sets the node identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the depth of children to inflate (0 = no children, int.MaxValue = all).</summary>
    public int Depth { get; set; }
}

/// <summary>
/// Gets a single orchestration node by ID (shallow — no children).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetNodeEndpoint : Endpoint<GetNodeRequest, OrchestrationNodeConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<GetNodeEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNodeEndpoint"/> class.
    /// </summary>
    public GetNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<GetNodeEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<GetNodeEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/nodes/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get an orchestration node";
            s.Description = "Returns a single orchestration node by ID (no children loaded).";
        });
        Tags("Nodes");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(GetNodeRequest req, CancellationToken ct)
    {
        if (req.Depth > 0)
            NodeLog.GettingNodeDeep(_logger, req.Id, req.Depth);
        else
            NodeLog.GettingNode(_logger, req.Id);

        var result = req.Depth > 0
            ? await _provider.Get(req.Id, req.Depth, ct).ConfigureAwait(false)
            : await _provider.Get(req.Id, ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            NodeLog.NodeGetFailed(_logger, req.Id, result.CurrentMessage ?? "unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            NodeLog.NodeNotFound(_logger, req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        NodeLog.NodeRetrieved(_logger, req.Id);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
