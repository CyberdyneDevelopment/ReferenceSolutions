using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>Endpoint to update a DataSet.</summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataSetEndpoint : UpdateDataSetEndpointBase
{
    /// <inheritdoc />
    public UpdateDataSetEndpoint(DataSetConfigurationProvider dataSetProvider)
        : base(dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
