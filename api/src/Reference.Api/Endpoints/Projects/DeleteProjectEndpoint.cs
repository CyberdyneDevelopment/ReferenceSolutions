using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Projects;

/// <summary>
/// Request to delete a project.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteProjectRequest
{
    /// <summary>Gets or sets the project identifier.</summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Soft-deletes a project orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteProjectEndpoint : Endpoint<DeleteProjectRequest>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<DeleteProjectEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProjectEndpoint"/> class.
    /// </summary>
    public DeleteProjectEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<DeleteProjectEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<DeleteProjectEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/projects/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Delete an ETL project";
            s.Description = "Soft-deletes a project orchestration node by ID.";
        });
        Tags("Projects");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DeleteProjectRequest req, CancellationToken ct)
    {
        ProjectLog.DeletingProject(_logger, req.Id);

        var result = await _provider.Delete(req.Id, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            var msg = result.CurrentMessage ?? "unknown error";
            if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                ProjectLog.ProjectNotFoundForDelete(_logger, req.Id);
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            ProjectLog.ProjectDeleteFailed(_logger, req.Id, msg);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        ProjectLog.ProjectDeleted(_logger, req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
