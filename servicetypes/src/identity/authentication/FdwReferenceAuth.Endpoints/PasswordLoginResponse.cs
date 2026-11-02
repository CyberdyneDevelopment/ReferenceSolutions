using System;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// The token a successful login issues.
/// </summary>
/// <remarks>
/// Deliberately not OAuth2's token response shape. This endpoint is not an OAuth2 token endpoint —
/// it takes a username and password and returns a token, and naming the fields after a protocol it
/// does not implement would imply grants, scopes and refresh semantics it does not have.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PasswordLoginResponse
{
    /// <summary>Gets or sets the token to present on subsequent requests.</summary>
    /// <remarks>
    /// Empty when <see cref="TokenType"/> is <c>Session</c> (FDW-903): the session id travels only in
    /// the HttpOnly cookie the response sets, never in a body script can read.
    /// </remarks>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how the credential is presented: <c>Bearer</c> in the Authorization header, or
    /// <c>Session</c>, a cookie the browser sends on its own.
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>Gets or sets when the token stops being accepted.</summary>
    /// <remarks>
    /// An absolute instant rather than a lifetime in seconds: a client that stores a duration has to
    /// remember when it started, and a client that stores an instant does not.
    /// </remarks>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating that the password was right but must be changed before a
    /// token is issued — the account was marked to change it at next login, or it expired. Answered
    /// with 403 and no token; the caller changes it at <c>POST /auth/password</c> and signs in again.
    /// </summary>
    public bool PasswordChangeRequired { get; set; }
}
