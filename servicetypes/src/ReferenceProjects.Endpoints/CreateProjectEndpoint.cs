using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceProjects.Endpoints.Logging;

namespace ReferenceProjects.Endpoints;

/// <summary>
/// Request to create a new ETL project.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateProjectRequest
{
    /// <summary>Gets or sets the project name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the display ordinal among sibling projects.</summary>
    public int Ordinal { get; set; }
}

/// <summary>
/// Creates a new project orchestration node (root-level) and returns it as ProjectConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateProjectEndpoint : Endpoint<CreateProjectRequest, ProjectConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<CreateProjectEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectEndpoint"/> class.
    /// </summary>
    public CreateProjectEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<CreateProjectEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<CreateProjectEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/projects");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Create an ETL project";
            s.Description = "Creates a new root-level project orchestration node.";
        });
        Tags("Projects");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        ProjectLog.CreatingProject(_logger, req.Name);

        var config = new OrchestrationNodeConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            NodeTypeId = OrchestrationNodeTypes.Project.Id,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            IsEnabled = true,
        };

        var result = await _provider.Save(config, ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
        {
            ProjectLog.ProjectCreateFailed(_logger, req.Name, result.CurrentMessage ?? "unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Why: Newly saved node has no Children (flat save) — Stages[] is empty, which is correct
        // for a freshly created project. Stages are added via POST /nodes.
        var mapped = OrchestrationNodeMapper.ToProject(result.Value, _logger);
        if (mapped is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        ProjectLog.ProjectCreated(_logger, req.Name, mapped.Id);
        await Send.CreatedAtAsync<GetProjectEndpoint>(
            new { id = mapped.Id },
            mapped,
            cancellation: ct).ConfigureAwait(false);
    }
}
