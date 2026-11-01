using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Results;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// The status a route answers with when the flow its caller named cannot be run.
/// </summary>
/// <remarks>
/// One mapping for every route that runs a caller-named flow, so a switched-off sign-in method reads
/// the same wherever it is selected. A flow nobody configured is the caller's mistake (404); a flow an
/// operator switched off is a refusal (403); anything else — the flow rows unreadable, or the flow
/// configured and invalid — is this host's fault and says so (500), because reporting it as the
/// caller's would send whoever reads it looking at the wrong thing (FDW-910).
/// </remarks>
internal static class FlowSelectionStatus
{
    /// <summary>The status for a failed flow lookup.</summary>
    /// <param name="failed">The lookup's failure.</param>
    /// <returns>404, 403 or 500.</returns>
    internal static int For(IGenericResult failed)
        => failed.CodeChain.Contains(AuthenticationResultCodes.ByName("FlowNotFound"))
            ? StatusCodes.Status404NotFound
            : failed.CodeChain.Contains(AuthenticationResultCodes.ByName("FlowDisabled"))
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status500InternalServerError;
}
