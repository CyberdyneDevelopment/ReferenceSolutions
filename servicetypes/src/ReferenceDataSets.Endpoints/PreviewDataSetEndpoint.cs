using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to preview data from a DataSet with pagination and filter support.
/// Delegates fully to the base class which resolves config via DataSetConfigurationProvider
/// and executes live queries via IDataGateway.
/// </summary>
[ExcludeFromCodeCoverage]
public class PreviewDataSetEndpoint : PreviewDataSetEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewDataSetEndpoint"/> class.
    /// </summary>
    public PreviewDataSetEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        IDataGateway dataGateway,
        ILogger<PreviewDataSetEndpoint> logger)
        : base(dataSetProvider, dataGateway, logger ?? NullLogger<PreviewDataSetEndpoint>.Instance)
    {
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/preview";

    /// <inheritdoc />
    protected override string EndpointSummary => "Preview data set rows";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns a preview of sample rows from the data set's primary source.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
