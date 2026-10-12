using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.Http.Abstractions.Security;
using FdwReferenceConnections.FileSystem.Registration;

namespace FdwReferenceConnections.FileSystem.Tests;

/// <summary>
/// Asserts that a connection kind with no session-context concept reports one cache partition for
/// every caller, so its results stay shareable.
/// </summary>
/// <remarks>
/// <para>
/// The partition exists to keep a result cache from serving one caller's row-level-security-filtered
/// rows to another. A kind that never describes the calling principal to the store it opens cannot
/// produce caller-varying rows, so partitioning it would fragment identical data per principal and
/// strip the cache of nearly every hit while protecting nothing.
/// </para>
/// <para>
/// This is the counterpart to <see cref="FileSystemSessionContextDeclarationTests"/>: that one holds
/// "declares nothing, needs nothing"; this one holds "declares nothing, costs nothing".
/// </para>
/// </remarks>
public sealed class FileSystemCachePartitionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ReportsOnePartitionForEveryCallerIncludingNone()
    {
        // Arrange: principals that a scheme-declaring kind would separate — different users,
        // different tenants, and no principal at all.
        var connectionType = new FileSystemConnectionType();

        // Act
        var partitions = new HashSet<string>(StringComparer.Ordinal)
        {
            connectionType.CachePartition(null),
            connectionType.CachePartition(new StubAuthenticationContext(Guid.NewGuid(), Guid.NewGuid())),
            connectionType.CachePartition(new StubAuthenticationContext(Guid.NewGuid(), Guid.NewGuid())),
            connectionType.CachePartition(new SystemAuthenticationContext()),
        };

        // Assert: one partition. The store cannot tell these callers apart, so neither should the key.
        partitions.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ReportsANonEmptyPartitionRatherThanAnAbsentOne()
    {
        // Why this matters: an empty segment in a composed cache key reads as a bug and collides with
        // any other empty-partition producer. "No session-context concept" is a declared position with
        // a named member behind it, and the token it yields says so.
        new FileSystemConnectionType().CachePartition(null).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ReportsNoCacheCeilingForEveryCallerIncludingNone()
    {
        // Why this is asserted for FileSystem: it is the kind behind the ServerConfiguration tier, whose
        // gateway now caches (FDW-977). Reading the cache asks the kind for BOTH a partition and a ceiling,
        // and a kind that cannot answer FAILS the read rather than bypassing the cache, so every
        // server-tier read would fail. No session-context concept means nothing can go stale under a
        // caller whose identity did not change, so the ceiling is unbounded and only the command's own
        // duration limits an entry.
        var connectionType = new FileSystemConnectionType();

        connectionType.MaxCacheDuration(null).ShouldBe(TimeSpan.MaxValue);
        connectionType.MaxCacheDuration(new StubAuthenticationContext(Guid.NewGuid(), Guid.NewGuid())).ShouldBe(TimeSpan.MaxValue);
        connectionType.MaxCacheDuration(new SystemAuthenticationContext()).ShouldBe(TimeSpan.MaxValue);
    }

    private sealed class StubAuthenticationContext(Guid userId, Guid tenantId) : IAuthenticationContext
    {
        public string UserId { get; } = userId.ToString();

        public string Username => UserId;

        public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        public IEnumerable<string> Roles { get; } = [];

        public IEnumerable<string> Permissions { get; } = [];

        public bool IsAuthenticated => true;

        public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("None");

        public DateTimeOffset? ExpiresAt => null;

        public Guid? ActiveTenantId { get; } = tenantId;

        public Guid? ActiveOrgId => null;


        public bool IsSystemContext => false;
    }
}
