using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
/// Lists all project-type orchestration nodes, mapped to the ProjectConfiguration client contract.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListProjectsEndpoint : EndpointWithoutRequest<IReadOnlyList<ProjectConfiguration>>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<ListProjectsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListProjectsEndpoint"/> class.
    /// </summary>
    public ListProjectsEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<ListProjectsEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<ListProjectsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/projects");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:read");
#endif
        Summary(s =>
        {
            s.Summary = "List all ETL projects";
            s.Description = "Returns all project-type orchestration nodes mapped to ProjectConfiguration.";
        });
        Tags("Projects");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        ProjectLog.ListingProjects(_logger);

        var result = await _provider.Get(ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ProjectLog.ProjectsListFailed(_logger, result.CurrentMessage ?? "unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var projects = new List<ProjectConfiguration>();
        foreach (var node in (result.Value ?? []).Where(n => n.NodeTypeId == OrchestrationNodeTypes.Project.Id))
        {
            // List loads flat (depth=0) — Children is empty, Stages is populated as empty list.
            var mapped = OrchestrationNodeMapper.ToProject(node, _logger);
            if (mapped is null)
            {
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }
            projects.Add(mapped);
        }

        ProjectLog.ProjectsListed(_logger, projects.Count);
        await Send.OkAsync((IReadOnlyList<ProjectConfiguration>)projects, ct).ConfigureAwait(false);
    }
}
