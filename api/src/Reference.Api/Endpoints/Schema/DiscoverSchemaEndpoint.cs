using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Data;

namespace Reference.Api.Endpoints.Schema;

/// <summary>
/// Endpoint to discover connection schema.
/// Route: GET /connections/{Name}/schema
/// </summary>
// Why: Replaced custom ISchemaDiscovererProvider logic with FDW's GetConnectionSchemaEndpointBase
// which delegates to ISchemaInformationService (lazy-loads and caches schema).
[ExcludeFromCodeCoverage]
public sealed class DiscoverSchemaEndpoint : GetConnectionSchemaEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverSchemaEndpoint"/> class.
    /// </summary>
    public DiscoverSchemaEndpoint(ISchemaInformationService schemaService)
        : base(schemaService)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Schema");
    }
}
