using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Replaces one named sample's rows for a data set.</summary>
[ExcludeFromCodeCoverage]
public sealed class UpsertDataSetSampleEndpoint : UpsertDataSetSampleEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="UpsertDataSetSampleEndpoint"/>.</summary>
    public UpsertDataSetSampleEndpoint(
        DataSetConfigurationProvider dataSets, IConfigurationGatewayProvider configurationGateways, ILogger<UpsertDataSetSampleEndpoint> logger)
        : base(dataSets, configurationGateways, logger)
    {
    }
}
