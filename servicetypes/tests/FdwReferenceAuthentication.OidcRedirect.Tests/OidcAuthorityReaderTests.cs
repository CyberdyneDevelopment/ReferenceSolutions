using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;

namespace FdwReferenceAuthentication.OidcRedirect.Tests;

/// <summary>
/// Covers what <see cref="OidcAuthorityReader"/> accepts as a usable authority row where the client
/// authentication type and its properties are concerned.
/// </summary>
public class OidcAuthorityReaderTests
{
    private static OidcAuthorityConfiguration Row(string type, IDictionary<string, string?>? properties = null)
        => new()
        {
            Name = "Entra",
            Issuer = "https://login.example/tenant/v2.0",
            AuthorizationEndpoint = "https://login.example/tenant/authorize",
            TokenEndpoint = "https://login.example/tenant/token",
            JwksUri = "https://login.example/tenant/keys",
            ClientId = "client-id",
            RedirectUri = "https://app.example/callback",
            Scopes = "openid,profile",
            SubjectClaim = "oid",
            ValidAudiences = "client-id",
            ValidAlgorithms = "RS256",
            AssertableMethods = "pwd",
            ClockSkewSeconds = 30,
            IsCurrent = true,
            ClientAuthenticationType = type,
            AdditionalProperties = properties ?? new Dictionary<string, string?>(),
        };

    private static OidcAuthorityReader Reader(OidcAuthorityConfiguration row)
    {
        var authorities = new Mock<IOidcAuthorityConfigurationProvider>();
        authorities.Setup(a => a.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IOidcAuthorityImplementationConfiguration>>.Success([row]));

        return new OidcAuthorityReader(authorities.Object);
    }

    private static Dictionary<string, string?> SecretProperties()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["SecretManagerName"] = "MsSqlSecrets",
            ["SecretKeyName"] = "authentik-client",
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task RefusesARowWhoseClientAuthenticationTypeIsUnknown()
    {
        var result = await Reader(Row("Certificate")).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task RefusesARowWithABlankClientAuthenticationTypeRatherThanAssumingAPublicClient()
    {
        var result = await Reader(Row(string.Empty)).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task RefusesAClientSecretRowMissingTheSecretKeyName()
    {
        var properties = SecretProperties();
        properties.Remove("SecretKeyName");

        var result = await Reader(Row("ClientSecret", properties)).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task RefusesAClientSecretRowMissingTheSecretManagerName()
    {
        var properties = SecretProperties();
        properties.Remove("SecretManagerName");

        var result = await Reader(Row("ClientSecret", properties)).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task AcceptsAPublicClientRowWithNoProperties()
    {
        var result = await Reader(Row("None")).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClientAuthenticationType.ShouldBe("None");
        result.Value!.ClientAuthenticationProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public async Task AcceptsAClientSecretRowAndCarriesItsProperties()
    {
        var result = await Reader(Row("ClientSecret", SecretProperties())).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClientAuthenticationType.ShouldBe("ClientSecret");
        result.Value!.ClientAuthenticationProperties["SecretManagerName"].ShouldBe("MsSqlSecrets");
        result.Value!.ClientAuthenticationProperties["SecretKeyName"].ShouldBe("authentik-client");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public async Task ReturnsACopyOfThePropertiesThatLaterChangesToTheRowDoNotReach()
    {
        var properties = SecretProperties();
        var result = await Reader(Row("ClientSecret", properties)).Get(TestContext.Current.CancellationToken);

        properties["SecretKeyName"] = "changed";

        result.Value!.ClientAuthenticationProperties["SecretKeyName"].ShouldBe("authentik-client");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public async Task ReadsThePropertyKeysWithoutRegardToCase()
    {
        var properties = new Dictionary<string, string?>
        {
            ["secretmanagername"] = "MsSqlSecrets",
            ["secretkeyname"] = "authentik-client",
        };

        var result = await Reader(Row("ClientSecret", properties)).Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClientAuthenticationProperties["SecretKeyName"].ShouldBe("authentik-client");
    }
}
