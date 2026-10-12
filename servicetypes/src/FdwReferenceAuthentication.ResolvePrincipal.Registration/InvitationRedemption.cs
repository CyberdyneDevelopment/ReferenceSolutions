using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuthentication.ResolvePrincipal.Registration;

/// <summary>
/// How a flow reads a verified address off the issuer's token to redeem an operator's invitation.
/// </summary>
/// <remarks>
/// <para>
/// An invitation binds a pre-created account to the first sign-in that carries the invited address
/// (FDW-910). The address is only as good as the issuer's verification of it, so the flow states how
/// it knows the address was verified — exactly one of:
/// </para>
/// <list type="bullet">
/// <item><see cref="EmailVerifiedClaim"/>: a claim the issuer sets to <c>true</c> for a verified
/// address (OIDC's <c>email_verified</c>).</item>
/// <item><see cref="TrustIssuerEmail"/> set to true: the operator vouches for every address this issuer
/// asserts, for an issuer that sends no verification claim (Entra ID's <c>email</c> claim, for
/// example). Only for an issuer that controls its users' addresses.</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record InvitationRedemption
{
    /// <summary>Gets the claim that carries the person's address, e.g. <c>email</c>.</summary>
    public string? EmailClaim { get; init; }

    /// <summary>Gets the claim that is <c>true</c> when the issuer verified the address, e.g. <c>email_verified</c>.</summary>
    public string? EmailVerifiedClaim { get; init; }

    /// <summary>Gets a value indicating whether the operator vouches for every address this issuer asserts.</summary>
    public bool TrustIssuerEmail { get; init; }
}
