using Fdw.Services.Multitenancy;
using ReferenceMultitenancy.Sql;
using ReferenceMultitenancy.Sql.Logging;
using ReferenceMultitenancy.Sql.Middleware;
using ReferenceMultitenancy.Sql.Models;
using ReferenceMultitenancy.Sql.Results;
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
