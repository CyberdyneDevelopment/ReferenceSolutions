using System.Threading;
using Fdw.Services.Data.Limits;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for <see cref="PassThroughConnectionLimitResolver"/> — the default no-op
/// <see cref="IConnectionLimitResolver"/> that disables limit enforcement until a domain-specific
/// resolver is registered.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class PassThroughConnectionLimitResolverTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Resolve_AnyConnectionName_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var resolver = new PassThroughConnectionLimitResolver();

        // Act
        var result = resolver.Resolve("any-connection", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("conn-a")]
    [InlineData("")]
    [InlineData("Conn-With-Mixed-Case")]
    public void Resolve_VariousConnectionNames_AlwaysReturnsSuccessWithEmptyList(string connectionName)
    {
        // Arrange
        var resolver = new PassThroughConnectionLimitResolver();

        // Act
        var result = resolver.Resolve(connectionName, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void Resolve_DefaultCancellationToken_StillReturnsSuccess()
    {
        // Arrange
        var resolver = new PassThroughConnectionLimitResolver();

        // Act
#pragma warning disable xUnit1051 // Test uses default CancellationToken
        var result = resolver.Resolve("conn");
#pragma warning restore xUnit1051

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "DataIntegrity")]
    public void Resolve_CalledRepeatedly_ReturnsTheSameCachedSuccessInstance()
    {
        // Arrange — the implementation returns one cached static "empty success" result for every
        // call regardless of connection name; documenting this with reference equality pins that
        // intentional behavior (a no-op resolver, not a per-call allocation).
        var resolver = new PassThroughConnectionLimitResolver();

        // Act
        var first = resolver.Resolve("conn-1", CancellationToken.None);
        var second = resolver.Resolve("conn-2", CancellationToken.None);

        // Assert
        ReferenceEquals(first, second).ShouldBeTrue();
    }
}
