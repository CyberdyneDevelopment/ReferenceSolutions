using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// What a caller presents to log in with a password.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PasswordLoginRequest
{
    /// <summary>Gets or sets the username.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the password.</summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the flow to run, named by the caller as a redirect sign-in names it (FDW-903).
    /// </summary>
    /// <remarks>
    /// Which flow decides what the caller gets back: <c>PasswordLogin</c> returns a bearer token,
    /// <c>PasswordSession</c> sets a server-side session cookie. Required; there is no default flow.
    /// </remarks>
    public string? FlowName { get; set; }
}
