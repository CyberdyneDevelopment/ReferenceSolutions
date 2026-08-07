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
/// Request for deleting an orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteNodeRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>DELETE /etl/nodes/{id}</c> — soft-delete an orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteNodeEndpoint : Endpoint<DeleteNodeRequest>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<DeleteNodeEndpoint> _logger;

    public DeleteNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<DeleteNodeEndpoint> logger)
    {
        _provider = provider;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<DeleteNodeEndpoint>.Instance;
    }

    public override void Configure()
    {
        Delete("etl/nodes/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:write");
#endif
        Summary(s =>
        {
            s.Summary = "Delete an orchestration node";
            s.Description = "Soft-deletes an orchestration node (sets IsDeleted=true). " +
                             "Cannot delete ctrl (system) nodes.";
        });
    }

    public override async Task HandleAsync(DeleteNodeRequest req, CancellationToken ct)
    {
        EtlServerLog.DeleteNodeRequestReceived(_logger, req.Id);

        // Why: verify existence before delete so DELETE on a non-existent node returns 404,
        // not 204. The provider.Delete() soft-deletes silently (no-op on missing records).
        var existing = await _provider.Get(req.Id, ct).ConfigureAwait(false);
        if (!existing.IsSuccess || existing.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var result = await _provider.Delete(req.Id, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            AddError(EtlServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        EtlServerLog.NodeDeleted(_logger, req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
