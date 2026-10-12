using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Operations.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Lineage;

/// <summary>
/// Concrete endpoint for lazy lineage node expansion (direct neighbors only).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExpandOperationLineageNodeEndpoint : ExpandLineageNodeEndpointBase
{
    /// <inheritdoc />
    public ExpandOperationLineageNodeEndpoint(
        LineageConfigurationProvider lineageProvider,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<ExpandLineageNodeEndpointBase> logger)
        : base(lineageProvider, pipelineProvider, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Lineage");
    }
}
