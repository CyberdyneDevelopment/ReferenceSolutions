using System;
using System.Security.Claims;
using Fdw.Services.Authentication.Abstractions.Security;

namespace FdwReferenceEtlJobs.Endpoints;

/// <summary>
/// The tenant a triggered run belongs to (FDW-3646): the one rule both the pipeline and the project triggers follow.
/// </summary>
/// <remarks>
/// Why the caller's own claim wins over the request's TenantId: an authenticated per-tenant caller's token always carries its own
/// tenant, so honouring a body-supplied tenant would let it run work scoped to a different tenant (cross-tenant RLS escalation).
/// The request names the tenant only for a caller whose token carries none, such as the scheduler, which dispatches for every
/// tenant's schedules and has no single tenant of its own. This is a choice by kind of caller, not a value assumed in its absence.
/// </remarks>
internal static class TriggerTenant
{
    /// <summary>Gets the tenant the run belongs to.</summary>
    /// <param name="caller">The calling principal.</param>
    /// <param name="requested">The tenant the request names, honoured only for a caller whose token carries none.</param>
    /// <returns>The caller's own tenant; for a caller with none, the requested tenant, which may be absent.</returns>
    internal static Guid? For(ClaimsPrincipal caller, Guid? requested)
    {
        if (new ClaimsPrincipalAuthenticationContext(caller).ActiveTenantId is { } own)
            return own;

        return requested;
    }
}
