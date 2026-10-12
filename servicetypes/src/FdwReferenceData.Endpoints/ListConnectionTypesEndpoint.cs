using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Concrete endpoint to list available connection types.
/// Overrides route to /connections/types to avoid conflict with GET /connections/{Name}.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionTypesEndpoint : Fdw.Services.Data.Endpoints.ListConnectionTypesEndpointBase
{
    /// <inheritdoc />
    public ListConnectionTypesEndpoint(ILogger<ListConnectionTypesEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string Route => "/connections/types";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
