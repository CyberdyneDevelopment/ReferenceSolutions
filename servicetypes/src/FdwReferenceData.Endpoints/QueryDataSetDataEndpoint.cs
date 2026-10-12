using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Concrete endpoint to query a data set directly: <c>POST /data/{DataSetName}</c> (FDW-975).</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class QueryDataSetDataEndpoint : QueryDataSetDataEndpointBase
{
    /// <inheritdoc />
    public QueryDataSetDataEndpoint(
        ILogger<QueryDataSetDataEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        IDataGatewayProvider dataGateways)
        : base(logger, dataSets, dataGateways)
    {
    }
}
