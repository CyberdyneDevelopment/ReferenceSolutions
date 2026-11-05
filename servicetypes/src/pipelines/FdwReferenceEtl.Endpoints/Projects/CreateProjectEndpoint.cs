using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Writers;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceProjects.Endpoints.Logging;

namespace FdwReferenceProjects.Endpoints;

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

    /// <summary>
    /// Gets or sets whether a run of the project waits for someone who holds <c>pipelines:approve</c> to approve it (FDW-1740). True makes a
    /// run wait; false states it needs no approval; null (left out) inherits the server's default.
    /// </summary>
    public bool? RequireApprovalToRun { get; set; }

    /// <summary>Whether the project runs. Required: the caller's choice, never assumed (FDW-3931).</summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Creates a new project orchestration node (root-level) and returns it as ProjectConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateProjectEndpoint : Endpoint<CreateProjectRequest, ProjectConfiguration>
{
    private readonly OrchestrationNodeConfigurationWriter _writer;
    private readonly ILogger<CreateProjectEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectEndpoint"/> class.
    /// </summary>
    public CreateProjectEndpoint(
        OrchestrationNodeConfigurationWriter writer,
        ILogger<CreateProjectEndpoint> logger)
    {
        _writer = writer;
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

        // Whether the project runs is the caller's choice; a request that does not say is refused, never given an answer (FDW-3931).
        if (req.IsEnabled is not { } isEnabled)
        {
            var unstated = GenericResult.Failure(ProjectLog.ProjectEnablementNotStated(_logger, req.Name));
            ProjectLog.ProjectCreateFailed(_logger, req.Name, unstated.CurrentMessage);
            var (unstatedStatus, unstatedProblem) = ResultHttpStatusMapper.Map(unstated, HttpContext);
            await Send.ResultAsync(TypedResults.Json(unstatedProblem, statusCode: unstatedStatus)).ConfigureAwait(false);
            return;
        }

        var config = new OrchestrationNodeImplementationConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            // The implementation the domain provider is registered under -- OrchestrationNode has one
            // (DefaultOrchestrationServiceType.Register("OrchestrationNode", ...)), not one per node type.
            // Save looks the name up before writing; "" is registered for nothing. (FDW-790)
            Implementation = "OrchestrationNode",
            NodeTypeId = OrchestrationNodeTypes.Project.Id,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            IsEnabled = isEnabled,
            RequireApprovalToRun = req.RequireApprovalToRun,
        };

        // Why the writer: it validates the node before anything is stored; the provider alone does not.
        var result = await _writer.Save(config, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ProjectLog.ProjectCreateFailed(_logger, req.Name, result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        // Why: Newly saved node has no Children (flat save) — Stages[] is empty, which is correct
        // for a freshly created project. Stages are added via POST /nodes.
        var mapped = OrchestrationNodeMapper.ToProject(config, _logger);
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
