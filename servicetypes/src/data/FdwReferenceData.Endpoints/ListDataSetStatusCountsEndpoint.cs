using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Counts the data sets by status (FDW-4488).</summary>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetStatusCountsEndpoint : ListDataSetStatusCountsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataSetStatusCountsEndpoint"/> class.</summary>
    public ListDataSetStatusCountsEndpoint(
        DataSetConfigurationProvider dataSets,
        IDataSetWriters writers,
        IExecutionTracker runs,
        IEffectiveSettingsProvider settings,
        ITenantContext tenant,
        TimeProvider time,
        ILogger<ListDataSetStatusCountsEndpoint> logger)
        : base(logger, dataSets, writers, runs, settings, tenant, time)
    {
    }
}
