using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to get a DataSet by name, or a variation of one such as a saved view. The base resolves the name and maps the whole
/// record, including its id, sources, joins, caching and audit fields.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataSetEndpoint : GetDataSetEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetEndpoint"/> class.
    /// </summary>
    public GetDataSetEndpoint(IDataSetResolver resolver, CategoryConfigurationProvider categories, ILogger<GetDataSetEndpoint> logger)
        : base(logger, resolver, categories)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
