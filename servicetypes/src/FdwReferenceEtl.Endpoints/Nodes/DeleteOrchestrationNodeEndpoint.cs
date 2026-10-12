using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceEtl.Endpoints.Nodes.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>
/// Request to delete a node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteOrchestrationNodeRequest
{
    /// <summary>Gets or sets the node identifier.</summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Soft-deletes an orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteOrchestrationNodeEndpoint : Endpoint<DeleteOrchestrationNodeRequest>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<DeleteOrchestrationNodeEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOrchestrationNodeEndpoint"/> class.
    /// </summary>
    public DeleteOrchestrationNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<DeleteOrchestrationNodeEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<DeleteOrchestrationNodeEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/nodes/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Delete an orchestration node";
            s.Description = "Soft-deletes an orchestration node by ID.";
        });
        Tags("Nodes");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DeleteOrchestrationNodeRequest req, CancellationToken ct)
    {
        NodeLog.DeletingNode(_logger, req.Id);

        var result = await _provider.Delete(req.Id, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            if (result.CurrentMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) is true)
            {
                NodeLog.NodeNotFoundForDelete(_logger, req.Id);
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            NodeLog.NodeDeleteFailed(_logger, req.Id, result.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        NodeLog.NodeDeleted(_logger, req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
