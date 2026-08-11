using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceProjects.Endpoints.Logging;

namespace ReferenceProjects.Endpoints;

/// <summary>
/// Request for getting a project by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetProjectRequest
{
    /// <summary>Gets or sets the project identifier.</summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Gets a project by ID with full Stages → Steps tree, mapped to ProjectConfiguration.
/// Uses depth=int.MaxValue to inflate the complete hierarchy.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetProjectEndpoint : Endpoint<GetProjectRequest, ProjectConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<GetProjectEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectEndpoint"/> class.
    /// </summary>
    public GetProjectEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<GetProjectEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<GetProjectEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/projects/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get an ETL project by ID";
            s.Description = "Returns the full project tree (Stages → Steps → Pipelines) mapped to ProjectConfiguration.";
        });
        Tags("Projects");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(GetProjectRequest req, CancellationToken ct)
    {
        ProjectLog.GettingProject(_logger, req.Id);

        // Why: int.MaxValue inflates the complete Stage→Step tree so the response matches
        // ProjectConfiguration.Stages[].Steps[] as the client contract expects.
        var result = await _provider.Get(req.Id, int.MaxValue, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ProjectLog.ProjectGetFailed(_logger, req.Id, result.CurrentMessage ?? "unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            ProjectLog.ProjectNotFound(_logger, req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var mapped = OrchestrationNodeMapper.ToProject(result.Value, _logger);
        if (mapped is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        ProjectLog.ProjectRetrieved(_logger, req.Id);
        await Send.OkAsync(mapped, ct).ConfigureAwait(false);
    }
}
