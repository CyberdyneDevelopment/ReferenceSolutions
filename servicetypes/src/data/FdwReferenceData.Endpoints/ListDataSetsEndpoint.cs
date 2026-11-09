using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
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
        IDataSetConfigurationProvider dataSetProvider,
        ICategoryConfigurationProvider categories,
        IDataSetStatuses statuses,
        ILogger<ListDataSetsEndpoint> logger,
        ITagService? tags = null)
        : base(logger, dataSetProvider, categories, statuses, tags)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
