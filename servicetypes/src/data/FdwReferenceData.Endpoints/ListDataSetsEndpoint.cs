using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Tags;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to list DataSets, one page at a time. Each data set is read as a record only; its fields
/// and sources are read by their own routes (FDW-951).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataSetsEndpoint : ListDataSetsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataSetsEndpoint"/> class.
    /// </summary>
    public ListDataSetsEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        CategoryConfigurationProvider categories,
        IDataSetWriters writers,
        IExecutionTracker runs,
        IEffectiveSettingsProvider settings,
        ITenantContext tenant,
        TimeProvider time,
        ILogger<ListDataSetsEndpoint> logger,
        ITagService? tags = null)
        : base(logger, dataSetProvider, categories, writers, runs, settings, tenant, time, tags)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
