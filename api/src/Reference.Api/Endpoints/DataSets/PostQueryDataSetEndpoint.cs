using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// POST /datasets/{DataSetName}/query
/// Accepts field filters in the request body for richer querying than the GET variant.
/// </summary>
public sealed class PostQueryDataSetEndpoint : PostQueryDataSetEndpointBase
{
    public PostQueryDataSetEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        IDataGateway dataGateway,
        ILogger<PostQueryDataSetEndpoint> logger)
        : base(dataSetProvider, dataGateway, logger ?? NullLogger<PostQueryDataSetEndpoint>.Instance)
    {
    }
}
