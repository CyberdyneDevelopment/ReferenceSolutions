using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to get fields for a DataSet. The base loads the data set through the provider and maps
/// each field in full.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataSetFieldsEndpoint : GetDataSetFieldsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetFieldsEndpoint"/> class.
    /// </summary>
    public GetDataSetFieldsEndpoint(IDataSetConfigurationProvider dataSetProvider, IDataSetResolver resolver, ILogger<GetDataSetFieldsEndpoint> logger)
        : base(logger, dataSetProvider, resolver)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Get data set fields";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all fields for a specific data set.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
