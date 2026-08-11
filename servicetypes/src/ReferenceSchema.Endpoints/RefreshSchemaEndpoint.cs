using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Data;

namespace ReferenceSchema.Endpoints;

/// <summary>
/// Endpoint to refresh/re-discover connection schema.
/// Route: POST /connections/{Name}/schema/refresh
/// </summary>
// Why: Replaced custom ISchemaDiscoveryOrchestrator logic with FDW's RefreshConnectionSchemaEndpointBase
// which delegates to ISchemaInformationService.RefreshSchema (always runs live discovery).
[ExcludeFromCodeCoverage]
public class RefreshSchemaEndpoint : RefreshConnectionSchemaEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSchemaEndpoint"/> class.
    /// </summary>
    public RefreshSchemaEndpoint(ISchemaInformationService schemaService)
        : base(schemaService)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Schema");
    }
}
