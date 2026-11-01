using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Reads every stored version of a data set field (API-174).</summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetFieldHistoryEndpoint : GetDataSetFieldHistoryEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="GetDataSetFieldHistoryEndpoint"/>.</summary>
    public GetDataSetFieldHistoryEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<GetDataSetFieldHistoryEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
