using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
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
    public ListDataSetsEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<ListDataSetsEndpoint> logger)
        : base(logger, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
