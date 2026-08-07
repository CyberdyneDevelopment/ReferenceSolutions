using System;
using Fdw.Data;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage.Models;

/// <summary>
/// Record model for the <c>auth.RevokedAccessToken</c> table. A stateless RS256 access token is
/// invalidated by inserting its <c>jti</c> (JWT ID) here; <see cref="OpenIdTokenManager.Validate"/>
/// rejects any token whose <c>jti</c> has an unexpired row. Rows past <see cref="ExpiresAt"/> are
/// harmless (the token itself would already fail signature/expiry validation) and may be pruned.
/// </summary>
[GenerateMapper]
internal sealed partial class RevokedAccessTokenRecord
{
    /// <summary>The revoked token's <c>jti</c> claim — natural primary key.</summary>
    public Guid Jti { get; set; }

    /// <summary>When the token was revoked.</summary>
    public DateTimeOffset RevokedAt { get; set; }

    /// <summary>The token's original expiry — rows with <c>ExpiresAt</c> in the past are eligible for pruning.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
