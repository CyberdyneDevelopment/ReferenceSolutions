using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using FdwReferencePipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to list the pipeline runs in this host's queue: the runs in flight and the runs waiting, each waiting run with the
/// reason it cannot start now (FDW-3097). Inherits the behavior from the framework and adds Reference.Etl logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPipelineRunQueueEndpoint : ListPipelineRunQueueEndpointBase
{
    private readonly ILogger<ListPipelineRunQueueEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ListPipelineRunQueueEndpoint"/> class.</summary>
    public ListPipelineRunQueueEndpoint(PipelineRunScheduler scheduler, ITenantContext tenant, ILogger<ListPipelineRunQueueEndpoint> logger)
        : base(logger, scheduler, tenant)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }

    /// <inheritdoc />
    protected override void OnNoTenant() => PipelineLog.RunQueueRequestedWithoutTenant(_logger);

    /// <inheritdoc />
    protected override void OnQueueListed(Guid? tenantId, bool reachesAll, int running, int waiting) =>
        PipelineLog.RunQueueListed(_logger, reachesAll, running, waiting);
}
