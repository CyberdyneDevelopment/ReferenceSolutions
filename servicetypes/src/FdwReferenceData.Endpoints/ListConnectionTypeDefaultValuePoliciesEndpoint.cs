using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Lists the default-value policies a connection type registers (API-169):
/// <c>GET /connections/types/{Type}/default-value-policies</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionTypeDefaultValuePoliciesEndpoint : Fdw.Services.Data.Endpoints.ListConnectionTypeDefaultValuePoliciesEndpointBase
{
    /// <inheritdoc />
    public ListConnectionTypeDefaultValuePoliciesEndpoint(ILogger<ListConnectionTypeDefaultValuePoliciesEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
