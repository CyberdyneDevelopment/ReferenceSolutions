using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.ExternalIdentityProviders.Oidc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Fdw.Services.ExternalIdentityProviders.Tests;

/// <summary>
/// Validation behaviour of the reference OIDC external identity provider: a genuine token yields a
/// principal; missing config, wrong audience, expiry, and an untrusted signing key each fail loud.
/// A <see cref="StaticConfigurationManager{T}"/> injects a test signing key so validation runs offline
/// (no discovery/JWKS network call).
/// </summary>
public sealed class OidcExternalIdentityProviderTests
{
    private const string Authority = "https://issuer.example.test";
    private const string ClientId = "test-client";

    // Why: signing key holds the private material; the verifying key is the public half with the SAME
    // KeyId, so the token's kid header matches the key the provider trusts.
    private static (RsaSecurityKey Signing, RsaSecurityKey Verifying) NewKeyPair(string keyId)
    {
        var rsa = RSA.Create(2048);
        var signing = new RsaSecurityKey(rsa) { KeyId = keyId };
        var verifying = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = keyId };
        return (signing, verifying);
    }

    private static OidcExternalIdentityProvider BuildProvider(
        RsaSecurityKey trustedKey, string? authority = Authority, string? clientId = ClientId, string? audience = null)
    {
        var header = new ExternalIdentityProviderConfiguration { Name = "oidc" };
        var typed = new OidcExternalIdentityProviderConfiguration
        {
            Authority = authority,
            ClientId = clientId,
            Audience = audience,
        };
        var discovery = new OpenIdConnectConfiguration { Issuer = Authority };
        discovery.SigningKeys.Add(trustedKey);
        return new OidcExternalIdentityProvider(
            header, typed, NullLogger<OidcExternalIdentityProvider>.Instance,
            new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery));
    }

    private static string CreateToken(
        RsaSecurityKey signingKey, string issuer = Authority, string audience = ClientId,
        string subject = "user-1", DateTime? expires = null)
    {
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(new[] { new Claim("sub", subject) }),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
        });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ValidatesGenuineTokenAndReturnsPrincipal()
    {
        var (signing, verifying) = NewKeyPair("k1");
        var provider = BuildProvider(verifying);

        var result = await provider.ValidateExternalToken(CreateToken(signing), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.FindFirst("sub")!.Value.ShouldBe("user-1");
    }

    [Fact]
    public async Task FailsWhenAuthorityMissing()
    {
        var (_, verifying) = NewKeyPair("k1");
        var result = await BuildProvider(verifying, authority: null).ValidateExternalToken("irrelevant", Ct);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task FailsWhenClientIdMissing()
    {
        var (_, verifying) = NewKeyPair("k1");
        var result = await BuildProvider(verifying, clientId: null).ValidateExternalToken("irrelevant", Ct);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task FailsWhenAudienceDoesNotMatch()
    {
        var (signing, verifying) = NewKeyPair("k1");
        var token = CreateToken(signing, audience: "a-different-audience");
        (await BuildProvider(verifying).ValidateExternalToken(token, Ct)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task FailsWhenTokenExpired()
    {
        var (signing, verifying) = NewKeyPair("k1");
        // Well past the default 5-minute clock skew.
        var token = CreateToken(signing, expires: DateTime.UtcNow.AddMinutes(-30));
        (await BuildProvider(verifying).ValidateExternalToken(token, Ct)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task FailsWhenSignedByUntrustedKey()
    {
        var (_, trusted) = NewKeyPair("k1");
        var (attackerSigning, _) = NewKeyPair("k2");
        var token = CreateToken(attackerSigning);
        (await BuildProvider(trusted).ValidateExternalToken(token, Ct)).IsSuccess.ShouldBeFalse();
    }
}
