using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Clears a data set field's stand-in strategy (FDW-982).</summary>
[ExcludeFromCodeCoverage]
public sealed class ClearDataSetFieldStandInEndpoint : ClearDataSetFieldStandInEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="ClearDataSetFieldStandInEndpoint"/>.</summary>
    public ClearDataSetFieldStandInEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<ClearDataSetFieldStandInEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
