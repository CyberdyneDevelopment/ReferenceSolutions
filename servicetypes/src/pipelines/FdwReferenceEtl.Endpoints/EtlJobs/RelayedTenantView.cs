using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace FdwReferenceEtlJobs.Endpoints;

/// <summary>
/// For the length of a trigger's pipeline lookup, sees configuration as the relayed tenant does (FDW-1087).
/// </summary>
/// <remarks>
/// A service caller with no tenant of its own -- the scheduler, relaying a schedule's tenant -- would read
/// configuration under the deny plan and find no tenant's pipeline. This view acts as a tenant run of the
/// relayed tenant holding no roles: it sees that tenant's rows and shared rows, nothing else, and grants
/// nothing. A caller with a tenant of its own gets no view: it looks in its own. The run itself never uses this
/// view; it acts as the pipeline's own tenant run.
/// </remarks>
internal sealed class RelayedTenantView : IDisposable
{
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly IAuthenticationContext? _previous;
    private readonly bool _active;

    /// <summary>Initializes a new instance of the <see cref="RelayedTenantView"/> class.</summary>
    /// <param name="accessor">The ambient authentication context.</param>
    /// <param name="relayedTenant">The relayed tenant, or <see langword="null"/> when the caller has its own.</param>
    public RelayedTenantView(IAuthenticationContextAccessor accessor, Guid? relayedTenant)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _previous = accessor.Current;
        if (relayedTenant is not { } tenant)
            return;

        // The tenant's own id names the lookup's principal: it is no pipeline and holds no role.
        accessor.Current = new TenantRunAuthenticationContext(tenant, "relayed-tenant-lookup", tenant, [], []);
        _active = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_active)
            _accessor.Current = _previous;
    }
}
