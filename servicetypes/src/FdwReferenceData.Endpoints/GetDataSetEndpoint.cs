using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to get a DataSet by name. The base loads it through the provider and maps the whole
/// record, including its id, sources, joins, caching and audit fields.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataSetEndpoint : GetDataSetEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetEndpoint"/> class.
    /// </summary>
    public GetDataSetEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<GetDataSetEndpoint> logger)
        : base(logger, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
