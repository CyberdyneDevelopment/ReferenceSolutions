using ReferenceConnections.MsSql.Mapping;
using ReferenceConnections.MsSql.Tests;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests.Mapping;

/// <summary>
/// Unit tests for <see cref="DictionaryPool"/>.
/// </summary>
[Collection("DictionaryPoolTestCollection")]
public sealed class DictionaryPoolTests : IDisposable
{
    public DictionaryPoolTests()
    {
        // Clear pool before each test
        DictionaryPool.Clear();
    }

    public void Dispose()
    {
        // Clear pool after each test
        DictionaryPool.Clear();
    }

    #region Rent Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentReturnsNewDictionaryWhenPoolEmpty()
    {
        // Arrange & Act
        var dict = DictionaryPool.Rent(10);

        // Assert
        dict.ShouldNotBeNull();
        dict.Count.ShouldBe(0);
        dict.Comparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentReturnsPooledDictionaryWhenAvailable()
    {
        // Arrange
        var original = DictionaryPool.Rent(10);
        original["key"] = "value";
        DictionaryPool.Return(original);

        // Act
        var reused = DictionaryPool.Rent(10);

        // Assert
        reused.ShouldBe(original);
        reused.Count.ShouldBe(0); // Should be cleared
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentClearsPooledDictionary()
    {
        // Arrange
        var dict = DictionaryPool.Rent(10);
        dict["key1"] = "value1";
        dict["key2"] = "value2";
        DictionaryPool.Return(dict);

        // Act
        var reused = DictionaryPool.Rent(10);

        // Assert
        reused.Count.ShouldBe(0);
        reused.ContainsKey("key1").ShouldBeFalse();
    }

    #endregion

    #region Return Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnAddsDictionaryToPool()
    {
        // Arrange
        var initialPoolSize = DictionaryPool.CurrentPoolSize;
        var dict = DictionaryPool.Rent(10);

        // Act
        DictionaryPool.Return(dict);

        // Assert
        DictionaryPool.CurrentPoolSize.ShouldBe(initialPoolSize + 1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnIgnoresNullDictionary()
    {
        // Arrange
        var initialPoolSize = DictionaryPool.CurrentPoolSize;

        // Act
        DictionaryPool.Return(null!);

        // Assert
        DictionaryPool.CurrentPoolSize.ShouldBe(initialPoolSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnIgnoresOversizedDictionary()
    {
        // Arrange
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < 101; i++)
        {
            dict[$"key{i}"] = $"value{i}";
        }

        // Act
        DictionaryPool.Return(dict);

        // Assert
        DictionaryPool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnClearsDictionary()
    {
        // Arrange
        var dict = DictionaryPool.Rent(10);
        dict["key"] = "value";

        // Act
        DictionaryPool.Return(dict);

        // Assert
        dict.Count.ShouldBe(0);
    }

    #endregion

    #region Pool Size Limits Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PoolReusesMultipleDictionaries()
    {
        // Arrange
        var dicts = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 5; i++)
        {
            dicts.Add(DictionaryPool.Rent(10));
        }

        // Return all
        foreach (var dict in dicts)
        {
            DictionaryPool.Return(dict);
        }

        // Act - Rent again
        var reused = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 5; i++)
        {
            reused.Add(DictionaryPool.Rent(10));
        }

        // Assert - All should be from the pool
        foreach (var dict in reused)
        {
            dicts.ShouldContain(dict);
        }
    }

    #endregion

    #region Clear Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ClearEmptiesPool()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            DictionaryPool.Return(DictionaryPool.Rent(10));
        }
        DictionaryPool.CurrentPoolSize.ShouldBeGreaterThan(0);

        // Act
        DictionaryPool.Clear();

        // Assert
        DictionaryPool.CurrentPoolSize.ShouldBe(0);
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentedDictionaryIsCaseInsensitive()
    {
        // Arrange & Act
        var dict = DictionaryPool.Rent(10);
        dict["Key"] = "value";

        // Assert
        dict["KEY"].ShouldBe("value");
        dict["key"].ShouldBe("value");
        dict["kEy"].ShouldBe("value");
    }

    #endregion
}
