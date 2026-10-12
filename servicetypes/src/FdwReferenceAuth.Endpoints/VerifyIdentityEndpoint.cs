using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Endpoints;

namespace FdwReferenceAuth.Endpoints;

/// <summary><c>POST identities/{Name}/verify</c> — proves an identity can obtain a token.</summary>
/// <remarks>
/// The one endpoint that exercises the credential end to end without waiting for a scheduled
/// dispatch to fail. It reports issuer, audience, scopes and expiry, never the token.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class VerifyIdentityEndpoint : VerifyIdentityEndpointBase
{
    private readonly IIdentityServiceProvider _identities;

    /// <summary>Initializes a new instance of the <see cref="VerifyIdentityEndpoint"/> class.</summary>
    /// <param name="identities">Resolves the identity service that performs the exchange.</param>
    public VerifyIdentityEndpoint(IIdentityServiceProvider identities) =>
        _identities = identities;

    /// <inheritdoc />
    protected override IIdentityServiceProvider Identities => _identities;
}
