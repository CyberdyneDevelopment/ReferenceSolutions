using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Lineage;

/// <summary>
/// Concrete endpoint for lazy lineage node expansion (direct neighbors only).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExpandLineageNodeEndpoint : ExpandLineageNodeEndpointBase
{
    /// <inheritdoc />
    public ExpandLineageNodeEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<ExpandLineageNodeEndpointBase> logger)
        : base(configurationGateway, pipelineProvider, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Lineage");
    }
}
