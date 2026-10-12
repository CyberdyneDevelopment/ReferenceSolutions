using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Reads a data set field's currently active stand-in.</summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetFieldStandInEndpoint : GetDataSetFieldStandInEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="GetDataSetFieldStandInEndpoint"/>.</summary>
    public GetDataSetFieldStandInEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<GetDataSetFieldStandInEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
