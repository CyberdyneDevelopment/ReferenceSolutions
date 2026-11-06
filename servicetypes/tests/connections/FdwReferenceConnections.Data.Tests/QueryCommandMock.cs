using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Data.Tests;

// Builds the loose IQueryCommand double the caching tests hand to the gateway. A loose Moq double returns null for
// every member it is not told about, including the ones the real QueryCommand always answers (its command type and
// category, an empty list of joins), and CacheKeyBuilder.ComputeCacheKey writes every member that can change a read's
// answer into the key (CacheKeyText, FDW-3620) without a null check where the interface says it is never null. The
// non-nullable members it reads are CommandType, Category, Metadata and Joins (Filter, Ordering, Paging, Projection
// and Aggregation are nullable, the flags are bools), so stating them here is what makes the double honour the
// interface. One place, so a member the key starts reading later is added once rather than in every test file.
internal static class QueryCommandMock
{
    // Why IQueryCommand and not IDataCommand: the gateway caches READS, and a read is what IQueryCommand names. A
    // bare IDataCommand double is indistinguishable from an insert, and every non-query command computes the same
    // cache key, so caching one would let a second write be answered from the first one's result.
    internal static Mock<IQueryCommand> Read(IReadOnlyDictionary<string, object> metadata)
    {
        var mock = new Mock<IQueryCommand>();
        mock.Setup(c => c.CommandType).Returns("Query");
        mock.Setup(c => c.Category).Returns("Data");
        mock.Setup(c => c.Metadata).Returns(metadata);
        mock.Setup(c => c.Joins).Returns([]);
        return mock;
    }

    // Empty metadata, so CacheKeyBuilder derives the key prefix and tag from the target.
    internal static Mock<IQueryCommand> Read()
        => Read(new Dictionary<string, object>());
}
