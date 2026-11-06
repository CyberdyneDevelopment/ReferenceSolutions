using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>
/// The TLS modes a PostgreSQL connection can state (FDW-4153). A connection's SslMode is one of these names; <c>ByName</c> answers the
/// option, or <c>NotFound</c> for a name that is none of them.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PostgreSqlSslModeBase), typeof(IPostgreSqlSslMode), typeof(PostgreSqlSslModes))]
public abstract partial class PostgreSqlSslModes : TypeCollectionBase<PostgreSqlSslModeBase, IPostgreSqlSslMode>
{
}
