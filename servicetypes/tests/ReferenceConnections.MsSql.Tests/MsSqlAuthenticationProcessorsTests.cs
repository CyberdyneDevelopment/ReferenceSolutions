using Fdw.Services.Connections.MsSql.Authentication;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Tests for the <see cref="MsSqlAuthenticationTypes"/> TypeCollection.
/// Validates that expected type names remain registered (catches renames and removals).
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public class MsSqlAuthenticationProcessorsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameWithSqlAuthReturnsCorrectType()
    {
        var type = MsSqlAuthenticationTypes.ByName("SqlAuth");

        type.IsEmpty.ShouldBeFalse();
        type.Name.ShouldBe("SqlAuth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameWithWindowsAuthReturnsCorrectType()
    {
        var type = MsSqlAuthenticationTypes.ByName("WindowsAuth");

        type.IsEmpty.ShouldBeFalse();
        type.Name.ShouldBe("WindowsAuth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameWithEntraIdReturnsCorrectType()
    {
        var type = MsSqlAuthenticationTypes.ByName("EntraId");

        type.IsEmpty.ShouldBeFalse();
        type.Name.ShouldBe("EntraId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameWithManagedIdentityReturnsCorrectType()
    {
        var type = MsSqlAuthenticationTypes.ByName("ManagedIdentity");

        type.IsEmpty.ShouldBeFalse();
        type.Name.ShouldBe("ManagedIdentity");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameWithAzureCliReturnsCorrectType()
    {
        var type = MsSqlAuthenticationTypes.ByName("AzureCli");

        type.IsEmpty.ShouldBeFalse();
        type.Name.ShouldBe("AzureCli");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameWithInvalidNameReturnsEmpty()
    {
        var type = MsSqlAuthenticationTypes.ByName("InvalidAuth");

        type.IsEmpty.ShouldBeTrue();
    }
}
