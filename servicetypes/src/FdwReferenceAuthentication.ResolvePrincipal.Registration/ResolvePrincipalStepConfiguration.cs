using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuthentication.ResolvePrincipal.Registration;

/// <summary>
/// What a ResolvePrincipal step row's Configuration column holds. A row with no configuration
/// resolves only subjects already bound, then consults provisioning, as the step always has.
/// </summary>
/// <remarks>
/// <code>{ "Invitations": { "EmailClaim": "email", "EmailVerifiedClaim": "email_verified" } }</code>
/// <code>{ "Invitations": { "EmailClaim": "email", "TrustIssuerEmail": true } }</code>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record ResolvePrincipalStepConfiguration
{
    /// <summary>Gets how this flow redeems operator invitations; null when it does not.</summary>
    public InvitationRedemption? Invitations { get; init; }
}
