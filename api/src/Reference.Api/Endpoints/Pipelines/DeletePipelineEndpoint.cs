using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to delete a pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeletePipelineEndpoint : DeletePipelineEndpointBase<PipelineConfiguration>
{
    /// <inheritdoc />
    public DeletePipelineEndpoint(PipelineServiceConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
