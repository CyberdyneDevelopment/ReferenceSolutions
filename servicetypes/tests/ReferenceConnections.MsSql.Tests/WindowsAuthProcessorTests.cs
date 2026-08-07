using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Authentication.Types;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

[Collection(nameof(MsSqlTestCollection))]
public class WindowsAuthProcessorTests
{
    private static IReadOnlyDictionary<string, string?> EmptyKvp { get; }
        = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeIsDiscoverableByName()
    {
        var type = MsSqlAuthenticationTypes.ByName("WindowsAuth");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("WindowsAuth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeIsAccessibleViaStaticProperty()
    {
        var type = MsSqlAuthenticationTypes.WindowsAuth;

        type.ShouldNotBeNull();
        type.Name.ShouldBe("WindowsAuth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentAppendsIntegratedSecurity()
    {
        var auth = new WindowsAuthConfiguration();
        var result = auth.BuildAuthFragment(EmptyKvp, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Integrated Security=True;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentDoesNotContainPasswordOrUserId()
    {
        var auth = new WindowsAuthConfiguration();
        var result = auth.BuildAuthFragment(EmptyKvp, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldNotContain("Password");
        result.Value!.ShouldNotContain("User");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateAlwaysSucceeds()
    {
        var auth = new WindowsAuthConfiguration();
        var result = auth.Validate(EmptyKvp);

        result.IsSuccess.ShouldBeTrue();
    }
}
