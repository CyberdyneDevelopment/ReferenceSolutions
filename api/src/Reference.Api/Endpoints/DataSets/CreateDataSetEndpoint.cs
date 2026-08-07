using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>Endpoint to create a new DataSet.</summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataSetEndpoint : CreateDataSetEndpointBase
{
    /// <inheritdoc />
    public CreateDataSetEndpoint(DataSetConfigurationProvider dataSetProvider)
        : base(dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
