using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Etl;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Options;

namespace ReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to create a new pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreatePipelineEndpoint : CreatePipelineEndpointBase<PipelineConfiguration>
{
    /// <inheritdoc />
    public CreatePipelineEndpoint(PipelineServiceConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <summary>Builds the two-level pipeline aggregate from the create request.</summary>
    protected override PipelineConfiguration CreateConfiguration(CreatePipelineRequest request, Guid pipelineId)
    {
        // Why: pipelines are a general concept with ETL as one KIND and BatchCopy/Streaming as ETL
        // ENGINES. The header KIND discriminator is "Etl"; the request's PipelineType is the ENGINE
        // discriminator on the ETL-kind body; the engine body carries the source/sink bindings.
        // Transforms live on the ETL-kind body (shared across engines), not the engine body.
        return new PipelineConfiguration
        {
            Id = pipelineId,
            Name = request.Name,
            ServiceOptionType = "Etl",
            Description = request.Description,
            Configuration = new EtlPipelineConfiguration
            {
                ServiceOptionType = request.PipelineType,
                // Why: use the FDW base's MappedTransforms — it runs the full per-option dispatch
                // (TransformTypes.ByName(op).MapSpecToConfiguration) that populates every transform's typed
                // cascade children (FieldMappings, GroupByFields, Aggregations, Calculations, Lookups), not
                // just field mappings. A hand-rolled app mapper that copied only FieldMappings silently
                // dropped Aggregate/Calculate/Lookup params on write (FDW-556). null (not empty list) when
                // no transforms were supplied — the runtime reads null as the pass-through signal.
                Transforms = MappedTransforms.Count > 0 ? [.. MappedTransforms] : null,
                Configuration = BuildEngineBody(request)
            }
        };
    }

    /// <summary>Builds the ETL engine typed body (the source/sink bindings) for the requested engine.</summary>
    // Why: concrete return type — BatchCopy is the only engine wired today. Widen to
    // IEtlPipelineTypedConfiguration? when a second engine (e.g. Streaming) is added here.
    private static BatchCopyPipelineConfiguration? BuildEngineBody(CreatePipelineRequest request)
    {
        if (string.Equals(request.PipelineType, "BatchCopy", StringComparison.OrdinalIgnoreCase))
        {
            return new BatchCopyPipelineConfiguration
            {
                SourceConnectionName = request.SourceConnectionName,
                // Why: empty-string init for a non-nullable POCO field on an optional request value is
                // an init default, not a value fallback — required-ness is enforced upstream by the
                // request validator, so this never silently substitutes a missing connection.
                SourceDataSet = request.SourceDataSet ?? string.Empty,
                DestinationConnectionName = request.DestinationConnectionName,
                DestinationDataSet = request.DestinationDataSet ?? string.Empty,
                // Why: snapshot-replace ingests (current earthquakes) set this so a repeating/scheduled run
                // re-loads the latest snapshot instead of duplicate-keying the sink's unique index.
                TruncateBeforeLoad = request.TruncateBeforeLoad
            };
        }

        // Other engines (e.g. Streaming) attach their body here as they are wired; a null engine body
        // persists the header + ETL-kind level only.
        return null;
    }

    /// <summary>Maps saved configuration to detail DTO.</summary>
    protected override PipelineDetailResponse MapToDetail(PipelineConfiguration savedConfig, CreatePipelineRequest request, Guid pipelineId)
    {
        return new PipelineDetailResponse
        {
            Id = savedConfig.Id,
            Name = savedConfig.Name,
            PipelineType = request.PipelineType,
            SourceConnectionName = request.SourceConnectionName,
            DestinationConnectionName = request.DestinationConnectionName,
            SourceDataSet = request.SourceDataSet,
            DestinationDataSet = request.DestinationDataSet,
            Description = savedConfig.Description,
            IsEnabled = request.IsEnabled
        };
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
