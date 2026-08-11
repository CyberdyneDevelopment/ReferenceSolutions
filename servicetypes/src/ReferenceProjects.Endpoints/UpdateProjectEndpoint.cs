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
}

/// <summary>
/// Updates an existing project and returns the patched node as ProjectConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateProjectEndpoint : Endpoint<UpdateProjectRequest, ProjectConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<UpdateProjectEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProjectEndpoint"/> class.
    /// </summary>
    public UpdateProjectEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<UpdateProjectEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<UpdateProjectEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/projects/{id}");
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
            ProjectLog.ProjectGetFailed(_logger, req.Id, getResult.CurrentMessage ?? "unknown error");
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

        var saveResult = await _provider.Save(existing, ct).ConfigureAwait(false);
        if (saveResult.IsFailure || saveResult.Value is null)
        {
            ProjectLog.ProjectUpdateFailed(_logger, req.Id, saveResult.CurrentMessage ?? "unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Why: Save returns the updated node with no Children inflated — Stages[] is empty
        // in the response, consistent with the flat update path. Deep read via GET /projects/{id}.
        var mapped = OrchestrationNodeMapper.ToProject(saveResult.Value, _logger);
        if (mapped is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        ProjectLog.ProjectUpdated(_logger, req.Id);
        await Send.OkAsync(mapped, ct).ConfigureAwait(false);
    }
}
