using Fdw.Services.Multitenancy;
using ReferenceMultitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Extensions;
using ReferenceMultitenancy.Sql.Logging;
using Fdw.Services.Multitenancy.Sql.Middleware;
using Fdw.Services.Multitenancy.Sql.Models;
using Fdw.Services.Multitenancy.Sql.Results;
using Fdw.Services.Multitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Logging;
using Fdw.Services;
using Fdw;

namespace ReferenceMultitenancy.Sql;

/// <summary>
/// Marker factory contract for the "Sql" multitenancy option.
/// </summary>
/// <remarks>
/// Why a per-option interface: each ServiceTypeOption closes its base with its OWN factory
/// interface (the canonical shape — <c>MsSqlConnectionType</c>/<c>IMsSqlConnectionFactory</c>),
/// which is what gives every option a distinct auto-generated Id. Options sharing the domain
/// factory interface in the closure collide and the second one never registers.
/// </remarks>
public interface ISqlMultitenancyFactory : IMultitenancyFactory
{
}
