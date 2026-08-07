using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Authentication.Types;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Tests for <see cref="SqlAuthConfiguration"/>.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public class SqlAuthProcessorTests
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
        var type = MsSqlAuthenticationTypes.ByName("SqlAuth");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("SqlAuth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeIsAccessibleViaStaticProperty()
    {
        var type = MsSqlAuthenticationTypes.SqlAuth;

        type.ShouldNotBeNull();
        type.Name.ShouldBe("SqlAuth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentAppendsUserIdAndPassword()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("Username", "testuser"), ("SecretKeyName", "db-password")), "test-password-123");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("User Id=testuser;");
        result.Value!.ShouldContain("Password=test-password-123;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentAppendsUserIdWithoutPasswordWhenPasswordNotResolved()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.BuildAuthFragment(Kvp(("Username", "testuser"), ("SecretKeyName", "db-password")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("User Id=testuser;");
        result.Value!.ShouldNotContain("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildAuthFragmentFailsWhenValidationFails()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.BuildAuthFragment(Kvp(), null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenUsernameIsMissing()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("SecretKeyName", "db-password")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("Username is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenSecretKeyNameIsMissing()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("Username", "testuser")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("SecretKeyName is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateSucceedsWhenAllRequiredPropertiesPresent()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("Username", "testuser"), ("SecretKeyName", "db-password")));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RequiredPropertiesContainsUsername()
    {
        var type = MsSqlAuthenticationTypes.SqlAuth;

        type.RequiredProperties.ShouldContain("Username");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RequiredPropertiesContainsSecretKeyName()
    {
        var type = MsSqlAuthenticationTypes.SqlAuth;

        type.RequiredProperties.ShouldContain("SecretKeyName");
    }
}
