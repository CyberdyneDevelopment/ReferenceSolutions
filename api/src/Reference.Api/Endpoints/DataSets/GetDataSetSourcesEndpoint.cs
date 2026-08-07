using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get sources for a DataSet.
/// Delegates fully to the base class which resolves sources via DataSetConfigurationProvider.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetSourcesEndpoint : GetDataSetSourcesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetSourcesEndpoint"/> class.
    /// </summary>
    public GetDataSetSourcesEndpoint(
        DataSetConfigurationProvider dataSetProvider)
        : base(dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/sources";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get data set sources";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all sources for a specific data set.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<DataSetSourceResponse>?>> LoadDataSetSources(string dataSetName, CancellationToken ct)
    {
        return base.LoadDataSetSources(dataSetName, ct);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
