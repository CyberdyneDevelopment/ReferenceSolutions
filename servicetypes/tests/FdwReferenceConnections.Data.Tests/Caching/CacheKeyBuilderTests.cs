using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;

namespace Fdw.Services.Data.Tests.Caching;

/// <summary>
/// Regression tests proving <see cref="CacheKeyBuilder.ComputeCacheKey"/> is deterministic across two
/// separately-built, structurally-identical filter trees — the exact shape every query builder call
/// produces (a fresh <see cref="FilterGroup"/>/<see cref="FilterCondition"/> tree per call, e.g.
/// <c>ConfigurationCommandBase&lt;TConfig,TCommand&gt;.List</c>'s <c>IsCurrent=1 AND IsDeleted=0</c>).
/// </summary>
/// <remarks>
/// Before <c>FilterGroup</c>/<c>FilterCondition</c> got value-based equality, <c>ComputeCacheKey</c> hashed
/// <c>query.Filter.Root</c> via the record-synthesized <c>GetHashCode()</c>, which for <c>FilterGroup.Nodes</c>
/// (an <see cref="IReadOnlyList{T}"/> — typically a <c>List&lt;T&gt;</c> or array) fell back to
/// <c>object.GetHashCode()</c> reference identity, because <c>List&lt;T&gt;</c>/arrays don't override
/// value equality. Every call built a fresh <c>Nodes</c> list, so the SAME logical query produced a
/// DIFFERENT cache key on every call — the DataGateway result cache never hit for any query using a
/// FilterGroup, which is what let the all-items DataStore query hammer the database at ~200+ req/sec.
/// </remarks>
public sealed class CacheKeyBuilderTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public void ComputeCacheKeyIsDeterministicForEquivalentFreshlyBuiltAndFilters()
    {
        var target = new DataStoreTarget("ConfigurationDb", "data", "DataStore");

        var firstKey = CacheKeyBuilder.ComputeCacheKey(BuildIsCurrentNotDeletedQuery(), target);
        var secondKey = CacheKeyBuilder.ComputeCacheKey(BuildIsCurrentNotDeletedQuery(), target);

        firstKey.ShouldBe(secondKey);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public void ComputeCacheKeyDiffersWhenFilterValueDiffers()
    {
        var target = new DataStoreTarget("ConfigurationDb", "data", "DataStore");

        var matchingKey = CacheKeyBuilder.ComputeCacheKey(BuildIsCurrentNotDeletedQuery(), target);
        var differentKey = CacheKeyBuilder.ComputeCacheKey(BuildSingleConditionQuery("Name", "Acme"), target);

        matchingKey.ShouldNotBe(differentKey);
    }

    private static QueryCommand<object> BuildIsCurrentNotDeletedQuery() =>
        new()
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
                    ]
                }
            }
        };

    private static QueryCommand<object> BuildSingleConditionQuery(string propertyName, object value) =>
        new()
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition { PropertyName = propertyName, Operator = new EqualOperator(), Value = value }
            }
        };
}
