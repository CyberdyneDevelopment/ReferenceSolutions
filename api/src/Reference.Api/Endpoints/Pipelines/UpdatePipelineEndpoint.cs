using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to update an existing pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdatePipelineEndpoint : UpdatePipelineEndpointBase<PipelineConfiguration>
{
    /// <inheritdoc />
    public UpdatePipelineEndpoint(PipelineServiceConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <summary>Applies updates from request to original configuration.</summary>
    protected override PipelineConfiguration ApplyUpdates(UpdatePipelineRequest request, PipelineConfiguration originalConfig)
    {
        originalConfig.Description = request.Description ?? originalConfig.Description;

        return originalConfig;
    }

    /// <summary>Maps updated configuration to detail DTO.</summary>
    protected override PipelineDetailResponse MapToDetail(PipelineConfiguration config)
    {
        return new PipelineDetailResponse
        {
            Id = config.Id,
            Name = config.Name,
            PipelineType = config.ServiceOptionType ?? "Unknown",
            SourceConnectionName = string.Empty,
            DestinationConnectionName = string.Empty,
            Description = config.Description,
            IsEnabled = true
        };
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
