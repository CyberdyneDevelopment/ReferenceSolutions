using Microsoft.Extensions.Logging;
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
/// Endpoint to update an existing pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdatePipelineEndpoint : UpdatePipelineEndpointBase<IPipelineImplementationConfiguration>
{
    /// <inheritdoc />
    public UpdatePipelineEndpoint(
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations,
        ILogger<UpdatePipelineEndpoint> logger)
        : base(logger, provider, dataSets, connections, dataStoreConfigurations, transformations) {
    }

    /// <summary>Applies updates from request to original configuration.</summary>
    protected override IGenericResult<IPipelineImplementationConfiguration> ApplyUpdates(UpdatePipelineRequest request, IPipelineImplementationConfiguration originalConfig)
    {
        // The one engine limit is WiredEngine's, shared with the check of an edit (FDW-1105).
        var wired = WiredEngine.Of(originalConfig, Logger);
        if (wired.IsFailure)
        {
            return wired.ToNewResult<IPipelineImplementationConfiguration>();
        }

        var engine = wired.Value!;

        // Why a supplied side replaces the whole binding: the engine takes EXACTLY ONE of
        // {DataSet, ConnectionName, data store container} per side, so naming one clears the others.
        if (request.SourceDataStoreName is not null || request.SourceDataStorePath is not null || request.SourceContainerName is not null)
        {
            engine.SourceDataStoreName = request.SourceDataStoreName;
            engine.SourceDataStorePath = request.SourceDataStorePath;
            engine.SourceContainerName = request.SourceContainerName;
            engine.SourceDataSet = string.Empty;
            engine.SourceConnectionName = string.Empty;
        }
        else if (request.SourceDataSet is not null)
        {
            engine.SourceDataSet = request.SourceDataSet;
            engine.SourceConnectionName = string.Empty;
            ClearDataStoreSource(engine);
        }
        else if (request.SourceConnectionName is not null)
        {
            engine.SourceConnectionName = request.SourceConnectionName;
            engine.SourceDataSet = string.Empty;
            ClearDataStoreSource(engine);
        }

        if (request.DestinationDataSet is not null)
        {
            engine.DestinationDataSet = request.DestinationDataSet;
            engine.DestinationConnectionName = string.Empty;
        }
        else if (request.DestinationConnectionName is not null)
        {
            engine.DestinationConnectionName = request.DestinationConnectionName;
            engine.DestinationDataSet = string.Empty;
        }

        if (request.IsEnabled is { } isEnabled)
        {
            engine.IsEnabled = isEnabled;
        }
        // Why not-null rather than non-empty: null means the request left the description out, so the stored
        // one stays; an empty string is a request to clear it. It is a column of the engine's own row.
        if (request.Description is not null)
        {
            engine.Description = request.Description;
        }

        // Null means the request left transforms out: keep the stored set. A supplied list (empty
        // included) is the fully mapped replacement, with null as the runtime's pass-through signal
        // exactly as create writes it.
        if (MappedTransforms is not null)
        {
            engine.Transforms = MappedTransforms.Count > 0 ? [.. MappedTransforms] : null;
        }

        // The load definition the saved pipeline carries, judged by the FDW base: the request's where it states one, the
        // stored one where it states none (contract v3.1 B), so assigning it whole never drops what the body left alone.
        engine.LoadMode = EffectiveLoad.LoadMode;
        engine.LoadSteps = EffectiveLoad.LoadSteps;
        engine.RunParameters = EffectiveLoad.RunParameters;
        // Derived from the load mode the saved pipeline carries; a stated truncateBeforeLoad was checked against it.
        engine.TruncateBeforeLoad = PipelineTruncateConsistency.Derive(EffectiveLoad.LoadMode);

        return GenericResult<IPipelineImplementationConfiguration>.Success(engine);
    }


    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }

    // The data store source is three columns set together; replacing the source with a data set or a connection
    // clears all three (FDW-1103).
    private static void ClearDataStoreSource(BatchCopyPipelineConfiguration engine)
    {
        engine.SourceDataStoreName = null;
        engine.SourceDataStorePath = null;
        engine.SourceContainerName = null;
    }
}
