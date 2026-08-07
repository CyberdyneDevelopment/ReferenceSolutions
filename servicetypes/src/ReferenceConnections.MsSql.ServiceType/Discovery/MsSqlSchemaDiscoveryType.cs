using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Discovery;
using Fdw.Services.Connections.MsSql.Logging;

using ReferenceConnections.MsSql;

using ReferenceConnections.MsSql.Discovery;

namespace ReferenceConnections.MsSql.Discovery;

/// <summary>
/// Schema discovery type definition for Microsoft SQL Server.
/// Registers IMsSqlSchemaDiscoverer in DI for use by MsSqlConnectionType.DiscoverSchema().
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
        description: "Schema discovery for Microsoft SQL Server databases")
    {

    }

}
