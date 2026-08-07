using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Nodes;

/// <summary>
/// Request to create a new orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNodeRequest
{
    /// <summary>Gets or sets the node name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the node type ID (references OrchestrationNodeTypes).</summary>
    public int NodeTypeId { get; set; }

    /// <summary>Gets or sets the optional parent's durable logical Id. The physical RowId is DB-managed
    /// and invisible to the application, so the parent is addressed by its durable Id only.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the display ordinal among siblings.</summary>
    public int Ordinal { get; set; }
}

/// <summary>
/// Creates a new orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateNodeEndpoint : Endpoint<CreateNodeRequest, OrchestrationNodeConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<CreateNodeEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNodeEndpoint"/> class.
    /// </summary>
    public CreateNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<CreateNodeEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<CreateNodeEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/nodes");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("projects:write");
#endif
        Summary(s =>
        {
            s.Summary = "Create an orchestration node";
            s.Description = "Creates a new orchestration node of any type.";
        });
        Tags("Nodes");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateNodeRequest req, CancellationToken ct)
    {
        NodeLog.CreatingNode(_logger, req.Name);

        var config = new OrchestrationNodeConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            NodeTypeId = req.NodeTypeId,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            // Why: only the durable ParentId is set — RowId is DB-managed and invisible; the save
            // translator resolves the physical ParentRowId by subquery on this ParentId.
            ParentId = req.ParentId,
            IsEnabled = true,
        };

        var result = await _provider.Save(config, ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
        {
            NodeLog.NodeCreateFailed(_logger, req.Name, result.CurrentMessage ?? "unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        NodeLog.NodeCreated(_logger, req.Name, result.Value.Id);
        await Send.CreatedAtAsync<GetNodeEndpoint>(
            new { id = result.Value.Id },
            result.Value,
            cancellation: ct).ConfigureAwait(false);
    }
}
