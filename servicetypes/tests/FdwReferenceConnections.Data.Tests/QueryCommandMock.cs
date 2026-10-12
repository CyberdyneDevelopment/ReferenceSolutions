using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Data.Tests;

// Builds the loose IQueryCommand double the caching tests hand to the gateway. A loose Moq double returns null for
// every member it is not told about, including the collection ones the real QueryCommand always answers (an empty
// list), and CacheKeyBuilder.ComputeCacheKey reads Joins without a null check because the interface says it is never
// null. Joins is the only non-nullable member the cache key reads (Filter, Ordering and Paging are nullable), so
// stating it here is what makes the double honour the interface. One place, so a member the key starts reading
// later is added once rather than in every test file.
internal static class QueryCommandMock
{
    // Why IQueryCommand and not IDataCommand: the gateway caches READS, and a read is what IQueryCommand names. A
    // bare IDataCommand double is indistinguishable from an insert, and every non-query command computes the same
    // cache key, so caching one would let a second write be answered from the first one's result.
    internal static Mock<IQueryCommand> Read(IReadOnlyDictionary<string, object> metadata)
    {
        var mock = new Mock<IQueryCommand>();
        mock.Setup(c => c.CommandType).Returns("Query");
        mock.Setup(c => c.Metadata).Returns(metadata);
        mock.Setup(c => c.Joins).Returns([]);
        return mock;
    }

    // Empty metadata, so CacheKeyBuilder derives the key prefix and tag from the target.
    internal static Mock<IQueryCommand> Read()
        => Read(new Dictionary<string, object>());
}
