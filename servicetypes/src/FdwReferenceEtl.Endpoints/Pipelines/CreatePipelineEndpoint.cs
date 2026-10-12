using Microsoft.Extensions.Logging;
using System;
using Fdw.Services.Pipelines.Abstractions;
using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using FdwReferencePipelines.Endpoints.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to create a new pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreatePipelineEndpoint : CreatePipelineEndpointBase<IPipelineImplementationConfiguration>
{
    /// <inheritdoc />
    public CreatePipelineEndpoint(
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations,
        ILogger<CreatePipelineEndpoint> logger)
        : base(logger, provider, dataSets, connections, dataStoreConfigurations, transformations) {
    }

    /// <summary>Builds the pipeline's implementation record from the create request.</summary>
    protected override IGenericResult<IPipelineImplementationConfiguration> CreateConfiguration(CreatePipelineRequest request, Guid pipelineId)
    {
        // Why one record, not three: the engine record IS the pipeline implementation. A read through
        // the Pipeline domain is already dispatched, so there is no header to wrap and no ETL-kind level
        // to nest -- BatchCopyPipelineConfiguration carries IEtlPipelineImplementationConfiguration, which
        // carries IPipelineImplementationConfiguration, and one row answers all three reads.
        //
        // Name and Implementation ARE set here: CreatePipelineEndpointBase.Create reads both off this
        // record to pick the domain row's name and its implementation before Save stamps them. Left
        // unset, the name is null and the domain-row insert fails on a NULL Name (SQL 515).
        var engine = BuildEngineBody(request);
        if (engine is null)
        {
            return GenericResult<IPipelineImplementationConfiguration>.Failure(
                PipelineLog.EngineNotWired(Logger, request.PipelineType));
        }

        engine.Id = pipelineId;
        engine.Name = request.Name;

        // Why MappedTransforms: the FDW base runs the full per-option dispatch
        // (TransformTypes.ByName(op).MapSpecToConfiguration) that populates every transform's typed
        // cascade children (FieldMappings, GroupByFields, Aggregations, Calculations, Lookups), not just
        // field mappings. A hand-rolled app mapper that copied only FieldMappings silently dropped
        // Aggregate/Calculate/Lookup params on write (FDW-556). null (not empty list) when no transforms
        // were supplied -- the runtime reads null as the pass-through signal.
        engine.Transforms = MappedTransforms.Count > 0 ? [.. MappedTransforms] : null;

        // Why MappedLoad: the FDW base judged the load definition (structure, columns, containers, parameter references)
        // before this runs, so what is written is what was validated: the load mode, the load rows -- the pipeline's own
        // load or its steps -- and the declared run parameters (FDW-1110).
        engine.LoadMode = MappedLoad.LoadMode;
        engine.LoadSteps = MappedLoad.LoadSteps;
        engine.RunParameters = MappedLoad.RunParameters;
        return GenericResult<IPipelineImplementationConfiguration>.Success(engine);
    }

    /// <summary>Builds the engine's implementation record (the source/sink bindings) for the requested engine.</summary>
    // Why: concrete return type — BatchCopy is the only engine wired today. Widen to
    // IEtlPipelineImplementationConfiguration? when a second engine (e.g. Streaming) is added here.
    private static BatchCopyPipelineConfiguration? BuildEngineBody(CreatePipelineRequest request)
    {
        if (string.Equals(request.PipelineType, "BatchCopy", StringComparison.OrdinalIgnoreCase))
        {
            return new BatchCopyPipelineConfiguration
            {
                // The selected engine IS the implementation, named the way CreateScheduleEndpoint names its
                // own (request.SchedulerType): each engine registers its provider into
                // PipelineServiceConfigurationProvider under its own name (FDW-837). "Etl" was the retired
                // intermediate level; nothing registers under it any more.
                Implementation = request.PipelineType,
                // Description is a column of the engine's own row, not of pipe.Pipeline: a changed one
                // versions the engine row and leaves the FK-cited domain row alone.
                Description = request.Description,
                IsEnabled = request.IsEnabled,
                SourceConnectionName = request.SourceConnectionName,
                // Why: empty-string init for a non-nullable POCO field on an optional request value is
                // an init default, not a value fallback — required-ness is enforced upstream by the
                // request validator, so this never silently substitutes a missing connection.
                SourceDataSet = request.SourceDataSet ?? string.Empty,
                // A data store container source (FDW-1103): store, path and container together, or none; the
                // request validator and the run authority refuse a partial or doubled source.
                SourceDataStoreName = request.SourceDataStoreName,
                SourceDataStorePath = request.SourceDataStorePath,
                SourceContainerName = request.SourceContainerName,
                DestinationConnectionName = request.DestinationConnectionName,
                DestinationDataSet = request.DestinationDataSet ?? string.Empty,
                // Why derived, not copied: the load mode is the one definition and Replace means truncate then load; the
                // request's truncateBeforeLoad was checked against it by the FDW base and is not a second way to truncate.
                TruncateBeforeLoad = PipelineTruncateConsistency.Derive(request.LoadMode)
            };
        }

        // Other engines (e.g. Streaming) attach their implementation record here as they are wired; an
        // engine with none is refused by CreateConfiguration rather than persisted as a bare domain row.
        return null;
    }


    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
