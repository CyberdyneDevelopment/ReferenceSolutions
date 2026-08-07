using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Authentication.Types;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

[Collection(nameof(MsSqlTestCollection))]
public class EntraIdAuthProcessorTests
{
    private static IReadOnlyDictionary<string, string?> Kvp(params (string Key, string? Value)[] pairs)
    {
        var dict = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var p in pairs) dict[p.Key] = p.Value;
        return dict;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeIsDiscoverableByName()
    {
        var type = MsSqlAuthenticationTypes.ByName("EntraId");
        type.ShouldNotBeNull();
        type.Name.ShouldBe("EntraId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithDefaultModeAppendsActiveDirectoryDefault()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "Default")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Default;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithNullAzureAdModeUsesDefault()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Default;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithServicePrincipalModeAppendsCorrectAuth()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "ServicePrincipal"), ("ClientId", "my-client-id")), "my-secret");

        result.IsSuccess.ShouldBeTrue();
        var fragment = result.Value!;
        fragment.ShouldContain("Authentication=Active Directory Service Principal;");
        fragment.ShouldContain("User Id=my-client-id;");
        fragment.ShouldContain("Password=my-secret;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithSpnModeAppendsServicePrincipal()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "SPN"), ("ClientId", "spn-client")), "spn-secret");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Service Principal;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithServicePrincipalWithoutPasswordOmitsPassword()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "ServicePrincipal"), ("ClientId", "my-client-id")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldNotContain("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithServicePrincipalWithoutClientIdOmitsUserId()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "ServicePrincipal")), "secret");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldNotContain("User Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithInteractiveModeAppendsActiveDirectoryInteractive()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "Interactive")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Interactive;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithManagedIdentityModeAppendsActiveDirectoryManagedIdentity()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "ManagedIdentity")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Managed Identity;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentWithMsiModeAppendsActiveDirectoryManagedIdentity()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("AzureAdMode", "MSI")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Managed Identity;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateAlwaysSucceeds()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.Validate(Kvp());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RequiredPropertiesIsEmpty()
    {
        var type = MsSqlAuthenticationTypes.EntraId;
        type.RequiredProperties.ShouldBeEmpty();
    }
}
