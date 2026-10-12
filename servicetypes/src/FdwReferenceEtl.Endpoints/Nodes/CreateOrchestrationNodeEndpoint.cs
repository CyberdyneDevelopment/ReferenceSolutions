using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceEtl.Endpoints.Nodes.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes;

/// <summary>
/// Request to create a new orchestration node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateOrchestrationNodeRequest
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
public sealed class CreateOrchestrationNodeEndpoint : Endpoint<CreateOrchestrationNodeRequest, OrchestrationNodeImplementationConfiguration>
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly ILogger<CreateOrchestrationNodeEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrchestrationNodeEndpoint"/> class.
    /// </summary>
    public CreateOrchestrationNodeEndpoint(
        IOrchestrationNodeConfigurationProvider provider,
        ILogger<CreateOrchestrationNodeEndpoint> logger)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<CreateOrchestrationNodeEndpoint>.Instance;
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
    public override async Task HandleAsync(CreateOrchestrationNodeRequest req, CancellationToken ct)
    {
        NodeLog.CreatingNode(_logger, req.Name);

        var config = new OrchestrationNodeImplementationConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            // The implementation the domain provider is registered under -- OrchestrationNode has one
            // (DefaultOrchestrationServiceType.Register("OrchestrationNode", ...)), not one per node type.
            // Save looks the name up before writing; "" is registered for nothing. (FDW-790)
            Implementation = "OrchestrationNode",
            NodeTypeId = req.NodeTypeId,
            Name = req.Name,
            Description = req.Description,
            Ordinal = req.Ordinal,
            // The parent node is cited by its durable Id; the table stores it as it is.
            ParentId = req.ParentId,
            IsEnabled = true,
        };

        var result = await _provider.Save(config, "OrchestrationNode", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            NodeLog.NodeCreateFailed(_logger, req.Name, result.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        NodeLog.NodeCreated(_logger, req.Name, config.Id);
        await Send.CreatedAtAsync<GetOrchestrationNodeEndpoint>(
            new { id = config.Id },
            config,
            cancellation: ct).ConfigureAwait(false);
    }
}
