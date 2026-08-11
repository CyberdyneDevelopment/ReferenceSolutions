using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to map a DataSet as a source within a parent DataSet composition.
/// Finds the named source on the parent DataSet, updates its SourceKind to 'DataSet' and
/// binds the specified source DataSet via SourceDataSetId, then saves the whole aggregate.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapDataSetSourceEndpoint : Endpoint<MapDataSetSourceRequest, DataSetSourceMappingResultDto>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<MapDataSetSourceEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapDataSetSourceEndpoint"/> class.
    /// </summary>
    public MapDataSetSourceEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<MapDataSetSourceEndpoint> logger)
    {
        _dataSetProvider = dataSetProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/datasets/{ParentDataSetName}/sources/{SourceName}/map");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Map a DataSet as a source";
            s.Description = "Binds a source DataSet to a named source within a parent DataSet for compound/federated composition. Sets SourceKind='DataSet' and SourceDataSetId on the source record.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(MapDataSetSourceRequest req, CancellationToken ct)
    {
        // Validate parent DataSet exists
        var parentResult = await _dataSetProvider.Get(req.ParentDataSetName, ct).ConfigureAwait(false);
        if (!parentResult.IsSuccess)
        {
            DataSetEndpointLog.DataSetNotFound(_logger, req.ParentDataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Validate source DataSet exists and its Id matches the request
        var sourceResult = await _dataSetProvider.Get(req.SourceDataSetName, ct).ConfigureAwait(false);
        if (!sourceResult.IsSuccess || sourceResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(_logger, req.SourceDataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        if (sourceResult.Value.Id != req.SourceDataSetId)
        {
            DataSetEndpointLog.DataSetNotFound(_logger, req.SourceDataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Why: IsSuccess verified above; Value is non-null on a successful get by name.
        var parentDataSet = parentResult.Value;
        if (parentDataSet is null)
        {
            DataSetEndpointLog.DataSetNotFound(_logger, req.ParentDataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Find the named source on the parent DataSet
        var source = parentDataSet.Sources
            .FirstOrDefault(s => string.Equals(s.SourceName, req.SourceName, System.StringComparison.OrdinalIgnoreCase));

        if (source is null)
        {
            DataSetEndpointLog.DataSetNotFound(_logger, req.SourceName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Bind the source DataSet — version-on-write; the aggregate save replaces child rows.
        // Why: SourceKind discriminator lives on DataSetFieldMappingConfiguration (per-field binding),
        // not on DataSetSourceConfiguration. Here we set the FK/name that identifies the upstream DataSet.
        source.SourceDataSetId = req.SourceDataSetId;
        source.SourceDataSetName = req.SourceDataSetName;

        var saveResult = await _dataSetProvider.Save(parentDataSet, ct).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            DataSetEndpointLog.DataSetUpdateFailed(_logger, req.ParentDataSetName, "Save failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        DataSetEndpointLog.DataSetUpdated(_logger, req.ParentDataSetName);

        var mappingResult = new DataSetSourceMappingResultDto
        {
            ParentDataSetId = parentDataSet.Id,
            ParentDataSetName = req.ParentDataSetName,
            SourceName = req.SourceName,
            SourceDataSetId = req.SourceDataSetId,
            SourceDataSetName = req.SourceDataSetName,
            SourceKind = "DataSet",
            MappedAt = DateTimeOffset.UtcNow
        };

        await Send.OkAsync(mappingResult, ct).ConfigureAwait(false);
    }
}
