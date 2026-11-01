using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Endpoint to create a new DataSet.</summary>
[ExcludeFromCodeCoverage]
public class CreateDataSetEndpoint : CreateDataSetEndpointBase
{
    /// <inheritdoc />
    public CreateDataSetEndpoint(DataSetConfigurationProvider dataSetProvider, CategoryConfigurationProvider categories, ILogger<CreateDataSetEndpoint> logger)
        : base(logger, dataSetProvider, categories) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
