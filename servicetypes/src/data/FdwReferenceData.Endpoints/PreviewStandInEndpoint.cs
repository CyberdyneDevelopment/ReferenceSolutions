using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Generates example values for a stand-in strategy without saving it.</summary>
[ExcludeFromCodeCoverage]
public sealed class PreviewStandInEndpoint : PreviewStandInEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="PreviewStandInEndpoint"/>.</summary>
    public PreviewStandInEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<PreviewStandInEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
