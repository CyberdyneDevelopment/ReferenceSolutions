using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.PostgreSql.Discovery;

/// <summary>
/// The PostgreSQL catalog: the schema discovery option PostgreSqlConnectionType offers, and performs in
/// PostgreSqlConnectionType.DiscoverSchema().
/// </summary>
/// <remarks>
/// IPostgreSqlSchemaDiscoverer is registered by PostgreSqlConnectionType, which resolves it.
/// Discovery is performed directly via ISchemaDiscovery on PostgreSqlConnectionType —
/// no intermediate ISchemaDiscovererProvider layer is needed.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaDiscoveryTypes), "PostgreSql")]
public sealed class PostgreSqlSchemaDiscoveryType : SchemaDiscoveryTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSchemaDiscoveryType"/> class.
    /// Instance is created by source generator in SchemaDiscoveryTypes collection.
    /// </summary>
    public PostgreSqlSchemaDiscoveryType() : base(
        name: "PostgreSql",
        displayName: "PostgreSQL Schema Discovery",
        description: "Schema discovery for PostgreSQL databases",
        discoversSchema: true)
    {

    }

}
