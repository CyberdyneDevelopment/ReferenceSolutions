using System.Diagnostics.CodeAnalysis;
using Fdw.ServiceTypes;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Endpoints;

namespace ReferenceIdentity.Endpoints;

/// <summary><c>POST identities/{Name}/verify</c> — proves an identity can obtain a token.</summary>
/// <remarks>
/// The one endpoint that exercises the credential end to end without waiting for a scheduled
/// dispatch to fail. It reports issuer, audience, scopes and expiry, never the token.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class VerifyIdentityEndpoint : VerifyIdentityEndpointBase
{
    private readonly IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration> _identities;

    /// <summary>Initializes a new instance of the <see cref="VerifyIdentityEndpoint"/> class.</summary>
    /// <param name="identities">Resolves the identity service that performs the exchange.</param>
    public VerifyIdentityEndpoint(IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration> identities) =>
        _identities = identities;

    /// <inheritdoc />
    protected override IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration> Identities => _identities;
}
