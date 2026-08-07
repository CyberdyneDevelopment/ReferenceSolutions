using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Authentication.Types;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

[Collection(nameof(MsSqlTestCollection))]
public class ManagedIdentityAuthProcessorTests
{
    private static IReadOnlyDictionary<string, string?> EmptyKvp { get; }
        = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeIsDiscoverableByName()
    {
        var type = MsSqlAuthenticationTypes.ByName("ManagedIdentity");
        type.ShouldNotBeNull();
        type.Name.ShouldBe("ManagedIdentity");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentAppendsActiveDirectoryDefault()
    {
        var auth = new ManagedIdentityConfiguration();
        var result = auth.BuildAuthFragment(EmptyKvp, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Authentication=Active Directory Default;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentDoesNotAppendPasswordOrUserId()
    {
        var auth = new ManagedIdentityConfiguration();
        var result = auth.BuildAuthFragment(EmptyKvp, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldNotContain("User Id");
        result.Value!.ShouldNotContain("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateAlwaysSucceeds()
    {
        var auth = new ManagedIdentityConfiguration();
        var result = auth.Validate(EmptyKvp);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RequiredPropertiesIsEmpty()
    {
        var type = MsSqlAuthenticationTypes.ManagedIdentity;
        type.RequiredProperties.ShouldBeEmpty();
    }
}
