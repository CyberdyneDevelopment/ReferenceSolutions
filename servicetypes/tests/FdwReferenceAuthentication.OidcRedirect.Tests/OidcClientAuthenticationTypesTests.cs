using FdwReferenceAuthentication.OidcRedirect.Authentication;

namespace FdwReferenceAuthentication.OidcRedirect.Tests;

/// <summary>
/// Covers the client authentication types an OIDC authority row chooses between: None (a public
/// client) and ClientSecret (a confidential client).
/// </summary>
public class OidcClientAuthenticationTypesTests
{
    private static Dictionary<string, string?> Properties(params (string Key, string? Value)[] pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ByNameResolvesNoneAndClientSecret()
    {
        OidcClientAuthenticationTypes.ByName("None").IsEmpty.ShouldBeFalse();
        OidcClientAuthenticationTypes.ByName("ClientSecret").IsEmpty.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ByNameReturnsTheEmptySentinelForAnUnknownName()
    {
        OidcClientAuthenticationTypes.ByName("Certificate").IsEmpty.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authentication")]
    public void KnownNamesListsBothTypes()
    {
        var names = OidcClientAuthenticationTypes.KnownNames();

        names.ShouldContain("None");
        names.ShouldContain("ClientSecret");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void NoneIsAPublicClientThatExpectsNoProperties()
    {
        var none = OidcClientAuthenticationTypes.ByName("None");

        none.DisplayName.ShouldBe("Public client");
        none.RequiresSecret.ShouldBeFalse();
        none.ExpectedProperties.ShouldBeEmpty();
        none.RequiredProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void NoneValidatesWithNoProperties()
    {
        OidcClientAuthenticationTypes.ByName("None").Validate(Properties()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void NoneRequiresNoSecret()
    {
        var required = OidcClientAuthenticationTypes.ByName("None").RequiredSecret(Properties());

        required.IsSuccess.ShouldBeTrue();
        required.Value.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ClientSecretDeclaresItsPropertiesAndMarksTheKeyAsSecret()
    {
        var clientSecret = OidcClientAuthenticationTypes.ByName("ClientSecret");

        clientSecret.RequiresSecret.ShouldBeTrue();
        clientSecret.ExpectedProperties.ShouldBe(["SecretManagerName", "SecretKeyName"]);
        clientSecret.RequiredProperties.ShouldBe(["SecretManagerName", "SecretKeyName"]);
        clientSecret.SecretPropertyNames.ShouldBe(["SecretKeyName"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ClientSecretValidatesWhenBothPropertiesAreSet()
    {
        var values = Properties(("SecretManagerName", "MsSqlSecrets"), ("SecretKeyName", "authentik-client"));

        OidcClientAuthenticationTypes.ByName("ClientSecret").Validate(values).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    [InlineData(null, "authentik-client")]
    [InlineData("", "authentik-client")]
    [InlineData("  ", "authentik-client")]
    [InlineData("MsSqlSecrets", null)]
    [InlineData("MsSqlSecrets", "")]
    [InlineData("MsSqlSecrets", "  ")]
    [InlineData(null, null)]
    public void ClientSecretFailsValidationWhenEitherPropertyIsBlank(string? manager, string? key)
    {
        var values = Properties(("SecretManagerName", manager), ("SecretKeyName", key));

        OidcClientAuthenticationTypes.ByName("ClientSecret").Validate(values).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ClientSecretFailsValidationWhenThePropertiesAreAbsent()
    {
        OidcClientAuthenticationTypes.ByName("ClientSecret").Validate(Properties()).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ClientSecretNamesTheManagerAndKeyItDeclared()
    {
        var values = Properties(("SecretManagerName", "MsSqlSecrets"), ("SecretKeyName", "authentik-client"));

        var required = OidcClientAuthenticationTypes.ByName("ClientSecret").RequiredSecret(values);

        required.IsSuccess.ShouldBeTrue();
        required.Value.ManagerName.ShouldBe("MsSqlSecrets");
        required.Value.KeyName.ShouldBe("authentik-client");
        required.Value.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ClientSecretFailsToNameASecretWhenAPropertyIsMissing()
    {
        var values = Properties(("SecretKeyName", "authentik-client"));

        OidcClientAuthenticationTypes.ByName("ClientSecret").RequiredSecret(values).IsSuccess.ShouldBeFalse();
    }
}
