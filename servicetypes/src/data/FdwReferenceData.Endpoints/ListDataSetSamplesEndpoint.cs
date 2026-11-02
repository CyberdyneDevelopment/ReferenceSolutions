using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Lists a data set's hand-picked samples.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetSamplesEndpoint : ListDataSetSamplesEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="ListDataSetSamplesEndpoint"/>.</summary>
    public ListDataSetSamplesEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<ListDataSetSamplesEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
