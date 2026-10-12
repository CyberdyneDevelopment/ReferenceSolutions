using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Endpoint to list the chart types a view can be drawn as: <c>GET /chart-types</c> (FDW-1032).</summary>
[ExcludeFromCodeCoverage]
public sealed class ListChartTypesEndpoint : ListChartTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListChartTypesEndpoint"/> class.</summary>
    public ListChartTypesEndpoint(ILogger<ListChartTypesEndpointBase> logger) : base(logger)
    {
    }
}
