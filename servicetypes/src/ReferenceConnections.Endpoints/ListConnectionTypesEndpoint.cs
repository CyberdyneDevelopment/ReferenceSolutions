using System.Diagnostics.CodeAnalysis;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// Concrete endpoint to list available connection types.
/// Overrides route to /connections/types to avoid conflict with GET /connections/{Name}.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionTypesEndpoint : Fdw.Services.Data.Endpoints.ListConnectionTypesEndpointBase
{
    /// <inheritdoc />
    protected override string Route => "/connections/types";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
