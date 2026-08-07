using ReferenceConnections.MsSql.Mapping;
using ReferenceConnections.MsSql.Tests;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests.Mapping;

/// <summary>
/// Additional tests for <see cref="DictionaryPool"/> covering max pool size branch
/// and boundary conditions not covered by <see cref="DictionaryPoolTests"/>.
/// </summary>
[Collection("DictionaryPoolTestCollection")]
public sealed class DictionaryPoolAdditionalTests : IDisposable
{
    public DictionaryPoolAdditionalTests()
    {
        DictionaryPool.Clear();
    }

    public void Dispose()
    {
        DictionaryPool.Clear();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDiscardsWhenPoolIsFull()
    {
        // Arrange - Fill the pool to MaxPoolSize (1000)
        var dicts = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 1001; i++)
        {
            dicts.Add(new Dictionary<string, object?>(5, StringComparer.OrdinalIgnoreCase));
        }

        // Return 1000 to fill pool
        for (int i = 0; i < 1000; i++)
        {
            DictionaryPool.Return(dicts[i]);
        }

        // ConcurrentBag.Count is approximate; pool may also be affected by
        // parallel tests since DictionaryPool is static. Just verify we are near capacity.
        DictionaryPool.CurrentPoolSize.ShouldBeGreaterThanOrEqualTo(990);

        // Act - Return one more (should be discarded since pool is at/near max)
        DictionaryPool.Return(dicts[1000]);

        // Assert - Pool size should not exceed 1000
        DictionaryPool.CurrentPoolSize.ShouldBeLessThanOrEqualTo(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentFromPoolThenNewWhenEmpty()
    {
        // Arrange - Put one in pool
        var original = DictionaryPool.Rent(5);
        DictionaryPool.Return(original);

        // Act - Rent twice: first from pool, second creates new
        var first = DictionaryPool.Rent(5);
        var second = DictionaryPool.Rent(5);

        // Assert - first should be the same reference as original
        ReferenceEquals(first, original).ShouldBeTrue();
        // second should be a different instance
        ReferenceEquals(second, original).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDictionaryWithExactlyMaxSizeIsPooled()
    {
        // Arrange - Dictionary with exactly MaxDictionarySize entries (100)
        var dict = new Dictionary<string, object?>(100, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < 100; i++)
        {
            dict[$"key{i}"] = $"value{i}";
        }

        // Act
        DictionaryPool.Return(dict);

        // Assert - 100 items == MaxDictionarySize, should be pooled
        DictionaryPool.CurrentPoolSize.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ClearOnEmptyPoolDoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => DictionaryPool.Clear());
        DictionaryPool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentWithDifferentCapacitiesWorks()
    {
        // Act
        var small = DictionaryPool.Rent(1);
        var medium = DictionaryPool.Rent(50);
        var large = DictionaryPool.Rent(100);

        // Assert
        small.ShouldNotBeNull();
        medium.ShouldNotBeNull();
        large.ShouldNotBeNull();
    }
}
