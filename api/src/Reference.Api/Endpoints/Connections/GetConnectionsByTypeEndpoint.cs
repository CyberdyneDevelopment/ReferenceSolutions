using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Fdw.Services.Connections;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get connections filtered by type.
/// Overrides route to /connections/by-type/{Name} to avoid conflict with GET /connections/{Name}.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetConnectionsByTypeEndpoint : Fdw.Services.Data.Endpoints.GetConnectionsByTypeEndpointBase
{
    /// <inheritdoc />
    public GetConnectionsByTypeEndpoint(ConnectionConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override string Route => "/connections/by-type/{Name}";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
