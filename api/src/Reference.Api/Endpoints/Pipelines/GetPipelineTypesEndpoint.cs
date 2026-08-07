using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint listing the ETL pipeline engine types registered in the
/// <c>EtlPipelineTypes</c> ServiceTypeCollection. Route: GET /pipelines/types.
/// </summary>
/// <remarks>
/// Why this exists: the base is abstract, and FastEndpoints only registers concrete endpoints. Without
/// this closure the route 404s, which left the pipeline wizard's engine picker permanently empty — no
/// PipelineType could be selected, so no pipeline could be created at all. The engine list is sourced
/// from the live TypeCollection, so a newly registered engine appears here with no change to this file.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineTypesEndpoint : GetPipelineTypesEndpointBase
{
    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
