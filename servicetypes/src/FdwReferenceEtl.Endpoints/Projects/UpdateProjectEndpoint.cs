using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Writers;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceProjects.Endpoints.Logging;

namespace FdwReferenceProjects.Endpoints;

/// <summary>
/// Request to update an existing ETL project.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateProjectRequest
{
    /// <summary>Gets or sets the project identifier (from route).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the new name, or null to leave unchanged.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the new description, or null to leave unchanged.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new ordinal, or null to leave unchanged.</summary>
    public int? Ordinal { get; set; }

    /// <summary>Gets or sets the enabled state, or null to leave unchanged.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether a run of the project waits for approval (FDW-1740): true makes a run wait for someone who holds
    /// <c>pipelines:approve</c>, false states it needs none, null leaves it unchanged.
    /// </summary>
    public bool? RequireApprovalToRun { get; set; }
}

/// <summary>
/// Updates an existing project and returns the patched node as ProjectConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateProjectEndpoint : Endpoint<UpdateProjectRequest, ProjectConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly OrchestrationNodeConfigurationWriter _writer;
    private readonly ILogger<UpdateProjectEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProjectEndpoint"/> class.
    /// </summary>
    public UpdateProjectEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        OrchestrationNodeConfigurationWriter writer,
        ILogger<UpdateProjectEndpoint> logger)
    {
        _provider = provider;
        _writer = writer;
        _logger = logger ?? NullLogger<UpdateProjectEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/projects/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Update an ETL project";
            s.Description = "Patches fields on an existing project orchestration node.";
        });
        Tags("Projects");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateProjectRequest req, CancellationToken ct)
    {
        ProjectLog.UpdatingProject(_logger, req.Id);

        var getResult = await _provider.Get(req.Id, ct).ConfigureAwait(false);
        if (getResult.IsFailure)
        {
            ProjectLog.ProjectGetFailed(_logger, req.Id, getResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (getResult.Value is null)
        {
            ProjectLog.ProjectNotFoundForUpdate(_logger, req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var existing = getResult.Value;

        if (req.Name != null)
            existing.Name = req.Name;
        if (req.Description != null)
            existing.Description = req.Description;
        if (req.Ordinal.HasValue)
            existing.Ordinal = req.Ordinal.Value;
        if (req.IsEnabled.HasValue)
            existing.IsEnabled = req.IsEnabled.Value;
        if (req.RequireApprovalToRun.HasValue)
            existing.RequireApprovalToRun = req.RequireApprovalToRun.Value;

        if (existing is not OrchestrationNodeImplementationConfiguration node)
        {
            ProjectLog.ProjectUpdateFailed(_logger, req.Id, $"resolved to '{existing.GetType().Name}'");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Why the writer: it validates the node before anything is stored; the provider alone does not.
        var saveResult = await _writer.Save(node, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            ProjectLog.ProjectUpdateFailed(_logger, req.Id, saveResult.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(saveResult, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        // Why: Save returns the updated node with no Children inflated — Stages[] is empty
        // in the response, consistent with the flat update path. Deep read via GET /projects/{id}.
        var mapped = OrchestrationNodeMapper.ToProject(existing, _logger);
        if (mapped is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        ProjectLog.ProjectUpdated(_logger, req.Id);
        await Send.OkAsync(mapped, ct).ConfigureAwait(false);
    }
}
