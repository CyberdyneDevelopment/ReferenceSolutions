using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>Endpoint to delete a DataSet.</summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataSetEndpoint : DeleteDataSetEndpointBase
{
    /// <inheritdoc />
    public DeleteDataSetEndpoint(DataSetConfigurationProvider dataSetProvider)
        : base(dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
