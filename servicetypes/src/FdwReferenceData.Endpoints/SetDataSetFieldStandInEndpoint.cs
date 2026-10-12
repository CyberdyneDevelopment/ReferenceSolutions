using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Sets a data set field's stand-in strategy.</summary>
[ExcludeFromCodeCoverage]
public sealed class SetDataSetFieldStandInEndpoint : SetDataSetFieldStandInEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="SetDataSetFieldStandInEndpoint"/>.</summary>
    public SetDataSetFieldStandInEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<SetDataSetFieldStandInEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
