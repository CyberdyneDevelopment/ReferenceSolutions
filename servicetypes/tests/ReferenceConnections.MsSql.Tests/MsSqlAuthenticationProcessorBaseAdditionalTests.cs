using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Authentication.Types;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Tests for MsSqlAuthenticationConfiguration and concrete TypeOptions
/// to cover validation branches for SqlAuth and other types.
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlAuthenticationProcessorBaseAdditionalTests
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
    public void SqlAuthValidateFailsWhenUsernameIsMissing()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("SecretKeyName", "my-secret")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Username is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlAuthValidateFailsWhenUsernameIsEmpty()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("Username", ""), ("SecretKeyName", "my-secret")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Username is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlAuthValidateFailsWhenSecretKeyNameIsMissing()
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
    public void SqlAuthValidateFailsWhenSecretKeyNameIsEmpty()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("Username", "testuser"), ("SecretKeyName", "")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("SecretKeyName is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlAuthValidateFailsWhenBothRequiredPropertiesMissing()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp());

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Username is required");
        result.CurrentMessage.ShouldContain("SecretKeyName is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlAuthValidateSucceedsWhenAllRequiredPropertiesPresent()
    {
        var auth = new SqlAuthConfiguration();
        var result = auth.Validate(Kvp(("Username", "testuser"), ("SecretKeyName", "my-secret")));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WindowsAuthValidateAlwaysSucceeds()
    {
        var auth = new WindowsAuthConfiguration();
        var result = auth.Validate(Kvp());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ManagedIdentityValidateAlwaysSucceeds()
    {
        var auth = new ManagedIdentityConfiguration();
        var result = auth.Validate(Kvp());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EntraIdValidateAlwaysSucceeds()
    {
        var auth = new EntraIdConfiguration();
        var result = auth.Validate(Kvp());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptySentinelIsEmptyReturnsTrue()
    {
        var empty = MsSqlAuthenticationTypes.NotFound;

        empty.IsEmpty.ShouldBeTrue();
    }
}
