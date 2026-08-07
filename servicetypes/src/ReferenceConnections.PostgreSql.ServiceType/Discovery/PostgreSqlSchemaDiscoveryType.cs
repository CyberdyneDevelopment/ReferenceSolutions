using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Discovery;
using Fdw.Services.Connections.PostgreSql.Logging;

using ReferenceConnections.PostgreSql;

namespace ReferenceConnections.PostgreSql.Discovery;

/// <summary>
/// Schema discovery type definition for PostgreSQL.
/// Registers IPostgreSqlSchemaDiscoverer in DI for use by PostgreSqlConnectionType.DiscoverSchema().
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
        description: "Schema discovery for PostgreSQL databases")
    {

    }

}
