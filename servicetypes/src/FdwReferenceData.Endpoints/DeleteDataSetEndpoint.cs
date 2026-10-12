using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Endpoint to delete a DataSet.</summary>
[ExcludeFromCodeCoverage]
public class DeleteDataSetEndpoint : DeleteDataSetEndpointBase
{
    /// <inheritdoc />
    public DeleteDataSetEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        IDataSetVariationConfigurationProvider variations,
        ILogger<DeleteDataSetEndpoint> logger)
        : base(logger, dataSetProvider, variations) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
