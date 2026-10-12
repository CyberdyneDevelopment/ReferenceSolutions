using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Lineage;

/// <summary>
/// Concrete endpoint to get field-level lineage for a specific field within an entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetFieldLineageEndpoint : Fdw.Operations.Endpoints.GetFieldLineageEndpointBase
{
    /// <inheritdoc />
    public GetFieldLineageEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        LineageConfigurationProvider lineageProvider,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetFieldLineageEndpointBase> logger)
        : base(dataSetProvider, lineageProvider, pipelineProvider, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Lineage");
    }
}
