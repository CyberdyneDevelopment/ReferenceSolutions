using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.MsSql.Discovery;

/// <summary>
/// The SQL Server catalog: the schema discovery option MsSqlConnectionType offers, and performs in
/// MsSqlConnectionType.DiscoverSchema().
/// </summary>
/// <remarks>
/// IMsSqlSchemaDiscoverer is registered by MsSqlConnectionType, which resolves it.
/// Discovery is performed directly via ISchemaDiscovery on MsSqlConnectionType —
/// no intermediate ISchemaDiscovererProvider layer is needed.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaDiscoveryTypes), "MsSql")]
public sealed class MsSqlSchemaDiscoveryType : SchemaDiscoveryTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSchemaDiscoveryType"/> class.
    /// Instance is created by source generator in SchemaDiscoveryTypes collection.
    /// </summary>
    public MsSqlSchemaDiscoveryType() : base(
        name: "MsSql",
        displayName: "SQL Server Schema Discovery",
        description: "Schema discovery for Microsoft SQL Server databases",
        discoversSchema: true)
    {

    }

}
