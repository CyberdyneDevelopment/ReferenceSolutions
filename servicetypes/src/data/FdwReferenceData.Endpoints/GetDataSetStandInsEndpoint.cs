using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Reads the active stand-in of every field of a data set in one call.</summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetStandInsEndpoint : GetDataSetStandInsEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="GetDataSetStandInsEndpoint"/>.</summary>
    public GetDataSetStandInsEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<GetDataSetStandInsEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
