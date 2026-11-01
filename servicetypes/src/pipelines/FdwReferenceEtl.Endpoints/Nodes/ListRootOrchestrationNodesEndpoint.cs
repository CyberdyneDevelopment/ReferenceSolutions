using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceEtl.Endpoints.Nodes.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>
/// Lists all root orchestration nodes (nodes with no parent).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListRootOrchestrationNodesEndpoint : EndpointWithoutRequest<IReadOnlyList<IOrchestrationNodeImplementationConfiguration>>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<ListRootOrchestrationNodesEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListRootOrchestrationNodesEndpoint"/> class.
    /// </summary>
    public ListRootOrchestrationNodesEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<ListRootOrchestrationNodesEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<ListRootOrchestrationNodesEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/nodes");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:read");
#endif
        Summary(s =>
        {
            s.Summary = "List root orchestration nodes";
            s.Description = "Returns all root-level orchestration nodes (no parent).";
        });
        Tags("Nodes");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        NodeLog.ListingRootNodes(_logger);

        var result = await _provider
            .Find<IOrchestrationNodeImplementationConfiguration>(n => n.ParentId is null, ct)
            .ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
        {
            NodeLog.RootNodesListFailed(_logger, result.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var nodes = result.Value;
        NodeLog.RootNodesListed(_logger, nodes.Count);
        await Send.OkAsync(nodes, ct).ConfigureAwait(false);
    }
}
