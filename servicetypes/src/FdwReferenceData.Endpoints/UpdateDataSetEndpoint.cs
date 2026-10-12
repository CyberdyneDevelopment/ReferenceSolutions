using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Endpoint to update a DataSet.</summary>
[ExcludeFromCodeCoverage]
public class UpdateDataSetEndpoint : UpdateDataSetEndpointBase
{
    /// <inheritdoc />
    public UpdateDataSetEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<UpdateDataSetEndpoint> logger)
        : base(logger, dataSetProvider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
