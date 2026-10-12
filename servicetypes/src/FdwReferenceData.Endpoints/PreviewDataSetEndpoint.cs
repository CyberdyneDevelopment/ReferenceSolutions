using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to preview data from a DataSet with pagination and filter support.
/// Delegates fully to the base class, which reads the rows through DataSetQuery (FDW-1075).
/// </summary>
[ExcludeFromCodeCoverage]
public class PreviewDataSetEndpoint : PreviewDataSetEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewDataSetEndpoint"/> class.
    /// </summary>
    public PreviewDataSetEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        IDataGatewayProvider dataGateways,
        ILogger<PreviewDataSetEndpoint> logger)
        : base(dataSetProvider, dataGateways, logger ?? NullLogger<PreviewDataSetEndpoint>.Instance)
    {
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/preview";

    /// <inheritdoc />
    protected override string EndpointSummary => "Preview data set rows";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns the data set's first rows, carrying only its declared fields, read the way every data set read is.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
