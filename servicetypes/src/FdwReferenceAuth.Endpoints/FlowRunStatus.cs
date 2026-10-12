using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Results;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// The status a route answers with when a flow it ran did not complete.
/// </summary>
/// <remarks>
/// The run's counterpart to <see cref="FlowSelectionStatus"/>. A flow that refused its caller is 401.
/// A flow that accepted its caller and then could not issue the token or session it earned is this
/// host's fault and says so (500): answering it 401 told a user with correct credentials that they
/// were wrong, and sent whoever read it looking at the password instead of the server (FDW-918).
/// </remarks>
internal static class FlowRunStatus
{
    /// <summary>Whether a run failed after accepting its caller, while issuing its token or session.</summary>
    /// <param name="run">The run's result.</param>
    /// <returns><see langword="true"/> when the failure is the host's, not a refusal.</returns>
    internal static bool IsIssuanceFault(IGenericResult run)
        => run.CodeChain.Contains(AuthenticationResultCodes.ByName("IssuanceFailed"));

    /// <summary>The status for a run that did not complete.</summary>
    /// <param name="run">The run's result.</param>
    /// <returns>500 for an issuance fault, otherwise 401.</returns>
    internal static int For(IGenericResult run)
        => IsIssuanceFault(run)
            ? StatusCodes.Status500InternalServerError
            : StatusCodes.Status401Unauthorized;
}
