using System;
using System.Linq;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Fdw.Services.Authentication.Abstractions.Security;

namespace ReferenceConnections.MsSql.Tests;

/// <summary>
/// Tests for <see cref="MsSqlConnection.BuildSessionContextPlan"/> — the context-layer tenant-deny
/// decision (SYSTEM vs USER vs DENY) that determines which SESSION_CONTEXT keys
/// <see cref="MsSqlConnection.SetUserSessionContext"/> will set. Exercises the pure decision logic
/// directly (no live SQL Server connection required).
/// </summary>
[Collection(nameof(MsSqlTestCollection))]
public sealed class MsSqlConnectionSessionContextTests
{
    // Why an accessor and not a captured context: the connection resolves the calling principal
    // from IAuthenticationContextAccessor.Current at PLAN TIME. Passing the context through an
    // accessor is what exercises the production path — a connection built by the Singleton factory
    // never receives a context directly.
    private static MsSqlConnection BuildConnection(IAuthenticationContext? authContext)
        => new(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            accessToken: null,
            authenticationContextAccessor: new StubAuthenticationContextAccessor(authContext));

    // Why a stub rather than the real AuthenticationContextAccessor: that type backs Current with a
    // STATIC AsyncLocal, so tests sharing a flow could observe each other's writes. The stub gives
    // each connection its own slot and keeps these assertions deterministic.
    private sealed class StubAuthenticationContextAccessor(IAuthenticationContext? current)
        : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; } = current;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SystemContextPlanSetsNothingAtAllNoUserId()
    {
        // Arrange
        var connection = BuildConnection(new SystemAuthenticationContext());

        // Act
        var plan = connection.BuildSessionContextPlan();

        // Assert: explicit system elevation sets NOTHING — the resulting NULL UserId is what
        // security.fn_TenantFilter's Mode 1 bypass checks for. There is no SystemContext key.
        plan.IsSystem.ShouldBeTrue();
        plan.UserId.ShouldBeNull();
        plan.TenantId.ShouldBeNull();
        plan.IsCrossTenant.ShouldBeFalse();
        plan.CanReadSecrets.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AuthenticatedGuidUserPlanSetsUserIdAndTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var connection = BuildConnection(new WorkAuthenticationContext(tenantId, userId.ToString()));

        // Act
        var plan = connection.BuildSessionContextPlan();

        // Assert
        plan.IsSystem.ShouldBeFalse();
        plan.UserId.ShouldBe(userId);
        plan.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NullAuthContextResolvesToTheReservedDenyEverywherePrincipal()
    {
        // Arrange
        // Why: this is the exact case that was a fail-OPEN security bug under the prior design — a
        // connection with no established IAuthenticationContext used to set NOTHING, which fell
        // through to the SAME null-UserId bypass reserved for system connections (full visibility).
        // The confirmed design resolves this to the reserved NoAccessPrincipalId instead — a real,
        // non-null UserId that holds zero tenant grants, denied by every tenant-scoped RLS branch.
        var connection = BuildConnection(null);

        // Act
        var plan = connection.BuildSessionContextPlan();

        // Assert
        plan.IsSystem.ShouldBeFalse();
        plan.UserId.ShouldBe(AuthConstants.NoAccessPrincipalId);
        plan.TenantId.ShouldBeNull();
        plan.IsCrossTenant.ShouldBeFalse();
        plan.CanReadSecrets.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AuthenticatedNonGuidUserIdWithNoSystemElevationResolvesToTheReservedDenyEverywherePrincipal()
    {
        // Arrange
        // Why: WorkAuthenticationContext defaults UserId to the literal "system" (not a parseable
        // Guid) when no explicit userId is supplied, and is NOT a SystemAuthenticationContext
        // (IsSystemContext is false). Under the confirmed model this resolves to the reserved
        // NoAccessPrincipalId — even though ActiveTenantId is populated on the context, TenantId is
        // NOT set, because it is only ever set alongside a real resolved user identity. This is the
        // open sub-problem flagged on WorkAuthenticationContext's own remarks: a background execution
        // built this way denies (sees only shared rows), it does not see "exactly its own tenant".
        var tenantId = Guid.NewGuid();
        var connection = BuildConnection(new WorkAuthenticationContext(tenantId));

        // Act
        var plan = connection.BuildSessionContextPlan();

        // Assert
        plan.IsSystem.ShouldBeFalse();
        plan.UserId.ShouldBe(AuthConstants.NoAccessPrincipalId);
        plan.TenantId.ShouldBeNull("TenantId must never be set alongside the deny-everywhere principal, even when ActiveTenantId was populated on the source context");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UnauthenticatedContextResolvesToTheReservedDenyEverywherePrincipal()
    {
        // Arrange
        var context = new TestAuthContext(userId: Guid.NewGuid().ToString(), isAuthenticated: false);
        var connection = BuildConnection(context);

        // Act
        var plan = connection.BuildSessionContextPlan();

        // Assert: authenticated=false always denies, regardless of whether UserId happens to parse.
        plan.IsSystem.ShouldBeFalse();
        plan.UserId.ShouldBe(AuthConstants.NoAccessPrincipalId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoAccessorAtAllResolvesToTheReservedDenyPrincipalRatherThanTheSystemBypass()
    {
        // Arrange
        // Why: a connection built with no accessor is the shape a host gets when nothing registered
        // one. It must NOT reach the Mode 1 system bypass by omission — "set nothing" is full
        // elevation, so falling through to it would be fail-OPEN.
        var connection = new MsSqlConnection(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            accessToken: null,
            authenticationContextAccessor: null);

        // Act
        var plan = connection.BuildSessionContextPlan();

        // Assert
        plan.IsSystem.ShouldBeFalse();
        plan.UserId.ShouldBe(AuthConstants.NoAccessPrincipalId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContextEstablishedAfterConstructionIsSeenAtPlanTime()
    {
        // Arrange
        // Why this is THE regression test for FDW-642: the factory that builds connections is a DI
        // Singleton, so any principal captured at construction is captured before a request exists
        // and stays null forever — which is why every connection computed the deny plan. Resolving
        // .Current at plan time is the fix, and this asserts exactly that: the same connection
        // instance answers differently once a context is established on the accessor.
        var accessor = new StubAuthenticationContextAccessor(null);
        var connection = new MsSqlConnection(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            accessToken: null,
            authenticationContextAccessor: accessor);

        connection.BuildSessionContextPlan().UserId.ShouldBe(
            AuthConstants.NoAccessPrincipalId,
            "no context established yet");

        // Act: establish a real user AFTER the connection was constructed.
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        accessor.Current = new WorkAuthenticationContext(tenantId, userId.ToString());

        // Assert: a non-deny plan, from the same connection instance.
        var plan = connection.BuildSessionContextPlan();
        plan.UserId.ShouldBe(userId);
        plan.TenantId.ShouldBe(tenantId);
        plan.IsSystem.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BootTimeSystemElevationScopeProducesTheSystemPlanAndDoesNotOutliveTheScope()
    {
        // Arrange
        // Why: this models the MsSqlConnectionType.Initialization path — boot-time reads run inside a
        // SystemAuthenticationContextScope so they are elevated rather than denied, and the scope
        // restores the prior value on dispose so the request pipeline starts with none.
        var accessor = new StubAuthenticationContextAccessor(null);
        var connection = new MsSqlConnection(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString: "Server=unused;",
            accessToken: null,
            authenticationContextAccessor: accessor);

        // Act + Assert: elevated inside the scope.
        using (new SystemAuthenticationContextScope(accessor))
        {
            var elevated = connection.BuildSessionContextPlan();
            elevated.IsSystem.ShouldBeTrue();
            elevated.UserId.ShouldBeNull("system elevation sets NOTHING — Mode 1 keys off UserId being NULL");
        }

        // Assert: back to deny once the scope is disposed — the elevation never leaks past boot.
        var afterScope = connection.BuildSessionContextPlan();
        afterScope.IsSystem.ShouldBeFalse();
        afterScope.UserId.ShouldBe(AuthConstants.NoAccessPrincipalId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EveryAuthenticationContextIsGovernedByExactlyOneSessionContext()
    {
        // Why: MsSqlSessionContextTypes.For uses Single(), which is only safe because the three
        // options' Governs predicates partition the space exhaustively and exclusively. This asserts
        // that partition directly, so a future edit that makes two options overlap — or leaves a gap —
        // fails here rather than at runtime on a live connection.
        IAuthenticationContext?[] contexts =
        [
            null,
            new SystemAuthenticationContext(),
            new WorkAuthenticationContext(Guid.NewGuid(), Guid.NewGuid().ToString()),
            new WorkAuthenticationContext(Guid.NewGuid()),
            new TestAuthContext(userId: Guid.NewGuid().ToString(), isAuthenticated: false),
            new TestAuthContext(userId: "not-a-guid", isAuthenticated: true),
        ];

        foreach (var context in contexts)
        {
            MsSqlSessionContextTypes.All()
                .OfType<MsSqlSessionContextBase>()
                .Count(c => c.Governs(context))
                .ShouldBe(1, $"exactly one session context must govern {context?.GetType().Name ?? "null"}");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void TheTwoSessionContextCollectionsAreDisjointAndBothExposeIdentityOnly()
    {
        // Why: participation is declared per kind by pointing SessionContextTypes at a collection,
        // and the collection is the unit of replacement. These two must stay separate lists —
        // NoSessionContextTypes is the base default every kind holds until it declares a scheme, and
        // it must never acquire the reference scheme's members (notably SystemContext, whose
        // "set nothing" is indistinguishable on the wire from applying no session context at all).
        MsSqlSessionContextTypes.All().Select(c => c.Name)
            .ShouldBe(["SystemContext", "ForUser", "Deny"], ignoreOrder: true);

        NoSessionContextTypes.All().Select(c => c.Name).ShouldBe(["None"]);

        // Both collections expose the same identity-only interface, which is what lets a connection
        // type point at either one.
        MsSqlSessionContextTypes.All().ShouldAllBe(c => c is ISessionContext);
        NoSessionContextTypes.All().ShouldAllBe(c => c is ISessionContext);

        // Name is the durable identity a future discriminator column joins on, so ByName must be a
        // real lookup on both — not the generator's always-NotFound stub.
        MsSqlSessionContextTypes.ByName("Deny").ShouldBe(MsSqlSessionContextTypes.Deny);
        NoSessionContextTypes.ByName("None").Name.ShouldBe("None");
    }

    // Why: a minimal test-local IAuthenticationContext carrying only what BuildSessionContextPlan
    // reads, so IsAuthenticated can be forced to false independently of UserId shape.
    private sealed class TestAuthContext : IAuthenticationContext
    {
        public TestAuthContext(string userId, bool isAuthenticated)
        {
            UserId = userId;
            IsAuthenticated = isAuthenticated;
        }

        public string UserId { get; }
        public string Username => UserId;
        public System.Collections.Generic.IDictionary<string, object> Claims { get; }
            = new System.Collections.Generic.Dictionary<string, object>(StringComparer.Ordinal);
        public System.Collections.Generic.IEnumerable<string> Roles { get; } = [];
        public System.Collections.Generic.IEnumerable<string> Permissions { get; } = [];
        public bool IsAuthenticated { get; }
        public Fdw.Web.Http.Abstractions.Security.SecurityMethodBase AuthenticationMethod
            => (Fdw.Web.Http.Abstractions.Security.SecurityMethodBase)Fdw.Web.Http.Abstractions.Security.SecurityMethods.ByName("None");
        public DateTimeOffset? ExpiresAt => null;
        public Guid? ActiveTenantId => null;
        public Guid? ActiveOrgId => null;
        public bool IsCrossTenant => false;
        public bool IsSystemContext => false;
    }
}
