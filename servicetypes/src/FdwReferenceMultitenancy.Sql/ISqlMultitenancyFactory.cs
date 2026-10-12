using Fdw.Services.Multitenancy;

namespace FdwReferenceMultitenancy.Sql;

/// <summary>
/// Marker factory contract for the "Sql" multitenancy option.
/// </summary>
/// <remarks>
/// Why a per-option interface: each Implementation closes its base with its OWN factory
/// interface (the canonical shape — <c>MsSqlConnectionType</c>/<c>IMsSqlConnectionFactory</c>),
/// which is what gives every option a distinct auto-generated Id. Options sharing the domain
/// factory interface in the closure collide and the second one never registers.
/// </remarks>
public interface ISqlMultitenancyFactory : IMultitenancyFactory
{
}
