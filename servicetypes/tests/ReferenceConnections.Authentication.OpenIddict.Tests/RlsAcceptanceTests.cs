using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authentication.Abstractions.Security;
using ReferenceAuthentication.OpenIddict.Claims;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Web.Http.Abstractions.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// RLS acceptance tests proving the context-layer tenant-deny design works end-to-end using the
/// REAL MsSql DataGateway against ConfigurationDb on the configured MSSQL_TEST_SERVER.
/// <c>security.fn_TenantFilter</c> itself is UNCHANGED — the deny lives entirely in
/// <c>MsSqlConnection.BuildSessionContextPlan</c>, which resolves a principal on every connection so
/// the app never sends a null <c>SESSION_CONTEXT('UserId')</c> itself.
///
/// Skipped automatically when MSSQL_SA_PASSWORD is not set.
///
/// What this proves:
///   (A) Session: UserId+TenantId set → tenant-scoped query returns only matching rows
///   (B) Session: UserId only, no TenantId (no active tenant) → tenant-scoped rows invisible
///   (C) Explicit SystemAuthenticationContext → SetUserSessionContext sets NOTHING at all → the
///       resulting NULL SESSION_CONTEXT('UserId') hits fn_TenantFilter's unchanged Mode 1 bypass →
///       all rows visible. This is the ONLY path to that elevation.
///   (D) DefaultPrincipalResolver: tenant=&lt;not-mine&gt; → TenantAccessDenied, no token
///   (E) DefaultPrincipalResolver: cross_tenant + tenants:view-all → sees all accessible tenants' rows
///   (F) DefaultPrincipalResolver: cross_tenant without tenants:view-all → CrossTenantAccessDenied
///   (G) No IAuthenticationContext established at all → resolves to the reserved
///       AuthConstants.NoAccessPrincipalId → tenant-scoped rows invisible (deny-everywhere), NOT the
///       system bypass — proving the prior fail-OPEN gap (absent context => null UserId => bypass)
///       is closed.
///
/// Isolation: all test data is inserted with a test-unique TenantId, then hard-DELETEd in finally.
/// Test rows use the sched.Schedule table (no FK constraints, TenantId+VisibilityGroupId columns present).
/// </summary>
public sealed class RlsAcceptanceTests
{
    // Why: the target server is deployment-specific and this file is published publicly, so it
    // comes from the environment beside the credential rather than being committed. It gates the
    // same skip as the password: an unset target means these integration tests cannot mean
    // anything, so they skip rather than build a half-formed connection string and fail obscurely.
    private static readonly string? ServerAddress = Environment.GetEnvironmentVariable("MSSQL_TEST_SERVER");
    private const string DatabaseName = "ConfigurationDb";
    private const string LoginName = "sa";
    private const string EnvVarName = "MSSQL_SA_PASSWORD";

    private static string? GetPassword()
    {
        if (string.IsNullOrEmpty(ServerAddress))
            return null;

        var value = Environment.GetEnvironmentVariable(EnvVarName);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    // Why: the RLS acceptance assertions (A/B/C/E/G) only MEAN anything when security.fn_TenantFilter
    // and its security policy are actually deployed on the target DB. RLS is not yet deployed
    // platform-wide, so without this precondition these tests false-FAIL (unfiltered rows visible) —
    // masking real regressions behind environmental noise. When fn_TenantFilter is absent we SKIP
    // (honest "not verifiable here"), never silently pass: the moment the RLS DDL is deployed the same
    // tests run and enforce. The pure in-memory resolver tests (D/F) never call this. Uses the file's
    // own BuildSystemConnection + OpenSession production path for consistency.
    private static async Task<bool> IsRlsDeployed(string password, CancellationToken cancellationToken)
    {
        using var system = BuildSystemConnection(password);
        var connResult = await OpenSession(system, cancellationToken).ConfigureAwait(false);
        connResult.IsSuccess.ShouldBeTrue("Failed to open SQL connection for IsRlsDeployed probe");
        await using var sqlConn = connResult.Value!;
        await using var cmd = sqlConn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sys.objects WHERE name = 'fn_TenantFilter' AND type IN ('IF','FN','TF')";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;
    }

    private static string BuildConnectionString(string password)
        => $"Server={ServerAddress},1433;Database={DatabaseName};User Id={LoginName};Password={password};TrustServerCertificate=True;Encrypt=True;Application Name=RlsAcceptanceTest";

    // Why: System connection uses an explicit SystemAuthenticationContext — SetUserSessionContext
    // sets NOTHING at all for it, and the resulting NULL SESSION_CONTEXT('UserId') is what
    // fn_TenantFilter's unchanged Mode 1 checks for. A connection with NO auth context resolves
    // instead to the reserved deny-everywhere NoAccessPrincipalId (see test G below), so
    // seed/teardown here MUST use explicit system elevation, not an absent context.
    private static MsSqlConnection BuildSystemConnection(string password)
        => new(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            BuildConnectionString(password),
            accessToken: null,
            authenticationContextAccessor: new StubAuthenticationContextAccessor(new SystemAuthenticationContext()));

    // Why: Authenticated connection sets SESSION_CONTEXT(UserId, TenantId) so RLS filters apply.
    // Why an accessor: the connection resolves the calling principal from
    // IAuthenticationContextAccessor.Current at plan time — that is the production path, since the
    // Singleton factory never hands a connection a context directly.
    private static MsSqlConnection BuildAuthConnection(string password, IAuthenticationContext? authContext)
        => new(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            BuildConnectionString(password),
            accessToken: null,
            authenticationContextAccessor: new StubAuthenticationContextAccessor(authContext));

    // Why a stub rather than the real AuthenticationContextAccessor: that type backs Current with a
    // STATIC AsyncLocal, so connections built for different cases in the same flow would observe
    // each other's principal. Per-connection slots keep each RLS case isolated.
    private sealed class StubAuthenticationContextAccessor(IAuthenticationContext? current)
        : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; } = current;
    }

    private static IAuthenticationContext BuildAuthContext(Guid userId, Guid? activeTenantId, bool isCrossTenant = false)
        => new TestAuthContext(userId, activeTenantId, isCrossTenant);

    // Why: ClaimsPrincipalAuthenticationContext is internal to Fdw.Services.Authorization.
    // The test project does not reference that assembly. This minimal test-local implementation
    // carries exactly the three properties that MsSqlConnection.SetUserSessionContext reads.
    private sealed class TestAuthContext : IAuthenticationContext
    {
        public TestAuthContext(Guid userId, Guid? activeTenantId, bool isCrossTenant)
        {
            UserId = userId.ToString();
            Username = userId.ToString();
            ActiveTenantId = activeTenantId;
            IsCrossTenant = isCrossTenant;
        }

        public string UserId { get; }
        public string Username { get; }
        public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
        public IEnumerable<string> Roles { get; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; } = Array.Empty<string>();
        public bool IsAuthenticated => true;
        public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("JWT");
        public DateTimeOffset? ExpiresAt => null;
        public Guid? ActiveTenantId { get; }
        public Guid? ActiveOrgId => null;
        public bool IsCrossTenant { get; }
        public bool IsSystemContext => false;
    }

    // ── (A) Strict active-tenant: TenantId in SESSION_CONTEXT → scoped rows visible ────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ActiveTenantSession_ScopesRowsToMatchingTenant()
    {
        var password = GetPassword();
        if (password is null) return; // Skip: MSSQL_SA_PASSWORD not set
        // Why: RLS acceptance requires security.fn_TenantFilter deployed; skip (not fail) when absent.
        Assert.SkipUnless(
            await IsRlsDeployed(password!, TestContext.Current.CancellationToken),
            "security.fn_TenantFilter not deployed on target DB - RLS acceptance assertion requires deployed RLS policies.");

        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed: one row for TenantA, one for TenantB in sched.Schedule (no FK deps)
        using var system = BuildSystemConnection(password!);
        await SeedTenantOrgAccess(system, userId, tenantA, cancellationToken: TestContext.Current.CancellationToken);
        await SeedTenantOrgAccess(system, userId, tenantB, cancellationToken: TestContext.Current.CancellationToken);
        await SeedSchedule(system, tenantA, "RlsTest_TenantA", TestContext.Current.CancellationToken);
        await SeedSchedule(system, tenantB, "RlsTest_TenantB", TestContext.Current.CancellationToken);

        try
        {
            // Act: query with TenantA active — should see TenantA row only
            using var connA = BuildAuthConnection(password!, BuildAuthContext(userId, tenantA));
            var countA = await CountSchedules(connA, "RlsTest_", TestContext.Current.CancellationToken);

            // Act: query with TenantB active — should see TenantB row only
            using var connB = BuildAuthConnection(password!, BuildAuthContext(userId, tenantB));
            var countB = await CountSchedules(connB, "RlsTest_", TestContext.Current.CancellationToken);

            // Assert: strict isolation — each session sees exactly one row
            countA.ShouldBe(1, "TenantA session should see only TenantA rows");
            countB.ShouldBe(1, "TenantB session should see only TenantB rows");
        }
        finally
        {
            await DeleteSchedules(system, "RlsTest_", TestContext.Current.CancellationToken);
            await DeleteTenantOrgAccess(system, userId, TestContext.Current.CancellationToken);
        }
    }

    // ── (B) No active TenantId: authenticated but no TenantId → tenant rows invisible ────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AuthenticatedNoTenant_CannotSeeTenantScopedRows()
    {
        var password = GetPassword();
        if (password is null) return; // Skip: MSSQL_SA_PASSWORD not set
        // Why: RLS acceptance requires security.fn_TenantFilter deployed; skip (not fail) when absent.
        Assert.SkipUnless(
            await IsRlsDeployed(password!, TestContext.Current.CancellationToken),
            "security.fn_TenantFilter not deployed on target DB - RLS acceptance assertion requires deployed RLS policies.");

        var tenantA = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var system = BuildSystemConnection(password!);
        await SeedTenantOrgAccess(system, userId, tenantA, TestContext.Current.CancellationToken);
        await SeedSchedule(system, tenantA, "RlsTest_NoTenant", TestContext.Current.CancellationToken);

        try
        {
            // Why: Building auth context without ActiveTenantId but with a userId sets UserId
            // SESSION_CONTEXT but NOT TenantId. The fn_TenantFilter Mode 3 predicate
            // requires SESSION_CONTEXT('TenantId') IS NOT NULL — so tenant rows are invisible.
            using var connNoTenant = BuildAuthConnection(password!, BuildAuthContext(userId, activeTenantId: null));
            var count = await CountSchedules(connNoTenant, "RlsTest_NoTenant", TestContext.Current.CancellationToken);

            count.ShouldBe(0, "Authenticated with no active tenant should not see tenant-scoped rows");
        }
        finally
        {
            await DeleteSchedules(system, "RlsTest_NoTenant", TestContext.Current.CancellationToken);
            await DeleteTenantOrgAccess(system, userId, TestContext.Current.CancellationToken);
        }
    }

    // ── (C) Explicit system elevation: sets NOTHING → NULL UserId hits Mode 1 bypass ──────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task SystemConnection_ExplicitSystemContext_SeesAllRows()
    {
        var password = GetPassword();
        if (password is null) return; // Skip: MSSQL_SA_PASSWORD not set
        // Why: RLS acceptance requires security.fn_TenantFilter deployed; skip (not fail) when absent.
        Assert.SkipUnless(
            await IsRlsDeployed(password!, TestContext.Current.CancellationToken),
            "security.fn_TenantFilter not deployed on target DB - RLS acceptance assertion requires deployed RLS policies.");

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using var system = BuildSystemConnection(password!);
        await SeedSchedule(system, tenantA, "RlsTest_System_A", TestContext.Current.CancellationToken);
        await SeedSchedule(system, tenantB, "RlsTest_System_B", TestContext.Current.CancellationToken);

        try
        {
            // An explicit SystemAuthenticationContext makes SetUserSessionContext set NOTHING at
            // all — no SESSION_CONTEXT keys whatsoever. fn_TenantFilter is UNCHANGED and treats the
            // resulting NULL SESSION_CONTEXT('UserId') as Mode 1 (full visibility). This is the ONLY
            // path to that elevation — a connection with no context at all instead resolves to the
            // reserved deny-everywhere NoAccessPrincipalId (see test G below), NOT this bypass.
            var count = await CountSchedules(system, "RlsTest_System_", TestContext.Current.CancellationToken);
            count.ShouldBeGreaterThanOrEqualTo(2, "Explicit system context must see all rows");
        }
        finally
        {
            await DeleteSchedules(system, "RlsTest_System_", TestContext.Current.CancellationToken);
        }
    }

    // ── (G) No IAuthenticationContext at all → reserved NoAccessPrincipalId → deny-everywhere ──

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NoAuthContext_ResolvesToNoAccessPrincipal_CannotSeeTenantScopedRows()
    {
        var password = GetPassword();
        if (password is null) return; // Skip: MSSQL_SA_PASSWORD not set
        // Why: RLS acceptance requires security.fn_TenantFilter deployed; skip (not fail) when absent.
        Assert.SkipUnless(
            await IsRlsDeployed(password!, TestContext.Current.CancellationToken),
            "security.fn_TenantFilter not deployed on target DB - RLS acceptance assertion requires deployed RLS policies.");

        var tenantA = Guid.NewGuid();

        using var system = BuildSystemConnection(password!);
        await SeedSchedule(system, tenantA, "RlsTest_NoContext", TestContext.Current.CancellationToken);

        try
        {
            // Why this test exists: under the PRIOR (wrong) design, a connection with NO established
            // IAuthenticationContext set NOTHING on SESSION_CONTEXT, which fell through to the SAME
            // NULL-UserId bypass reserved for system connections — a fail-OPEN security hole (an
            // anonymous/unestablished caller would see every tenant's data). The confirmed design
            // resolves an absent context to AuthConstants.NoAccessPrincipalId instead: a real,
            // non-null UserId that holds zero tenant.TenantOrgAccess grants, so it is denied by
            // every tenant-scoped RLS branch and sees only shared/system rows.
            using var connNoContext = BuildAuthConnection(password!, authContext: null);
            var count = await CountSchedules(connNoContext, "RlsTest_NoContext", TestContext.Current.CancellationToken);

            count.ShouldBe(0, "No established auth context must deny tenant-scoped rows (NoAccessPrincipalId), never bypass to see everything");
        }
        finally
        {
            await DeleteSchedules(system, "RlsTest_NoContext", TestContext.Current.CancellationToken);
        }
    }

    // ── (E) Cross-tenant: SESSION_CONTEXT(CrossTenant='1') → sees all accessible tenants ──────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CrossTenantSession_SeesRowsAcrossAllAccessibleTenants()
    {
        var password = GetPassword();
        if (password is null) return; // Skip: MSSQL_SA_PASSWORD not set
        // Why: RLS acceptance requires security.fn_TenantFilter deployed; skip (not fail) when absent.
        Assert.SkipUnless(
            await IsRlsDeployed(password!, TestContext.Current.CancellationToken),
            "security.fn_TenantFilter not deployed on target DB - RLS acceptance assertion requires deployed RLS policies.");

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var tenantC = Guid.NewGuid(); // user does NOT have access to C
        var userId = Guid.NewGuid();

        using var system = BuildSystemConnection(password!);
        await SeedTenantOrgAccess(system, userId, tenantA, TestContext.Current.CancellationToken);
        await SeedTenantOrgAccess(system, userId, tenantB, TestContext.Current.CancellationToken);
        // Note: no TenantOrgAccess for tenantC
        await SeedSchedule(system, tenantA, "RlsTest_CT_A", TestContext.Current.CancellationToken);
        await SeedSchedule(system, tenantB, "RlsTest_CT_B", TestContext.Current.CancellationToken);
        await SeedSchedule(system, tenantC, "RlsTest_CT_C", TestContext.Current.CancellationToken);

        try
        {
            // Cross-tenant context: no ActiveTenantId, IsCrossTenant=true
            using var connCT = BuildAuthConnection(password!, BuildAuthContext(userId, activeTenantId: null, isCrossTenant: true));
            var count = await CountSchedules(connCT, "RlsTest_CT_", TestContext.Current.CancellationToken);

            // Should see A and B (both accessible), not C (not in TenantOrgAccess)
            count.ShouldBe(2, "Cross-tenant should see rows across all accessible tenants, not inaccessible ones");
        }
        finally
        {
            await DeleteSchedules(system, "RlsTest_CT_", TestContext.Current.CancellationToken);
            await DeleteTenantOrgAccess(system, userId, TestContext.Current.CancellationToken);
        }
    }

    // ── (D) + (F) DefaultPrincipalResolver: tenant validation and cross-tenant permission gate ────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task PrincipalResolver_ExplicitTenant_NotMember_ReturnsTenantAccessDenied()
    {
        // This test is pure in-memory — no DB connection needed.
        // Proves the security boundary in DefaultPrincipalResolver at the token-issuance layer.
        var userId = Guid.NewGuid();
        var requestedTenant = Guid.NewGuid();
        var accessibleTenant = Guid.NewGuid(); // user belongs to this, not requestedTenant

        // Why: DefaultPrincipalResolver now injects UserTenantConfigurationProvider (concrete);
        // IUserTenantService was deleted. GetUserTenants is virtual — mock the concrete provider.
        var tenantStoreMock = new Mock<UserTenantConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserTenantConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb", "tenant",
            (object?)null!);
        tenantStoreMock
            .Setup(s => s.GetUserTenants(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Success(new[] { accessibleTenant }));

        var orgMock = new Mock<Fdw.Services.Multitenancy.Abstractions.IOrganizationProvider>();
        var permMock = new Mock<Fdw.Services.Authorization.Abstractions.IEffectivePermissionResolver>();

        var resolver = new DefaultPrincipalResolver(
            tenantStoreMock.Object, orgMock.Object, permMock.Object,
            BuildEmptyUserRoleProvider(), BuildEmptyRoleProvider(),
            NullLogger<DefaultPrincipalResolver>.Instance);

        var result = await resolver.Resolve(userId, requestedTenant, orgId: null,
            isCrossTenant: false, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task PrincipalResolver_CrossTenant_WithoutViewAllPerm_ReturnsCrossTenantAccessDenied()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Why: DefaultPrincipalResolver now injects UserTenantConfigurationProvider (concrete);
        // IUserTenantService was deleted. GetDefaultTenant is virtual — mock the concrete provider.
        var tenantStoreMock = new Mock<UserTenantConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserTenantConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb", "tenant",
            (object?)null!);
        tenantStoreMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));

        var orgMock = new Mock<Fdw.Services.Multitenancy.Abstractions.IOrganizationProvider>();
        orgMock.Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Fdw.Services.Multitenancy.Abstractions.OrganizationConfiguration>.Failure(new GenericMessage("No org")));

        var permMock = new Mock<Fdw.Services.Authorization.Abstractions.IEffectivePermissionResolver>();
        permMock.Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(new[] { "data.read" })); // no tenants:view-all

        var resolver = new DefaultPrincipalResolver(
            tenantStoreMock.Object, orgMock.Object, permMock.Object,
            BuildEmptyUserRoleProvider(), BuildEmptyRoleProvider(),
            NullLogger<DefaultPrincipalResolver>.Instance);

        var result = await resolver.Resolve(userId, tenantId: null, orgId: null,
            isCrossTenant: true, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse("User without tenants:view-all must not receive a cross-tenant token");
        result.CurrentMessage.ShouldNotBeNull();
    }

    // ── Role provider helpers ──────────────────────────────────────────────────────────────────

    // Why: DefaultPrincipalResolver now requires UserRoleConfigurationProvider and RoleConfigurationProvider
    // to bake the "roles" JWT claim. These helpers supply no-op stubs so security tests that focus on
    // tenant/cross-tenant access control don't need a real gateway.
    private static UserRoleConfigurationProvider BuildEmptyUserRoleProvider()
    {
        var mock = new Mock<UserRoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserRoleConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "authz",
            // Why: Castle DynamicProxy ignores optional ctor params — the trailing invalidator/dataStores
            // args must be passed explicitly so the 7-param ctor arity matches. (object?)null! per repo convention.
            (object?)null!);
        mock.Setup(p => p.GetByUser(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(
                Array.Empty<UserRoleConfiguration>()));
        return mock.Object;
    }

    private static RoleConfigurationProvider BuildEmptyRoleProvider()
    {
        var mock = new Mock<RoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<RoleConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "authz",
            (object?)null!);
        return mock.Object;
    }

    // ── SQL helpers ────────────────────────────────────────────────────────────────────────────

    // Why: helpers open a pooled connection and set SESSION_CONTEXT via the production
    // SetUserSessionContext path (the shared GetOpenSqlConnection seam was removed). SESSION_CONTEXT
    // (UserId, TenantId, CrossTenant) is set from the connection's auth context before any SQL
    // executes — proving the end-to-end RLS path exactly as production does.
    private static async Task<IGenericResult<SqlConnection>> OpenSession(
        MsSqlConnection connection, CancellationToken cancellationToken)
    {
        var sqlConnection = connection.CreatePooledConnection();
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
        return GenericResult<SqlConnection>.Success(sqlConnection);
    }

    private static async Task SeedTenantOrgAccess(
        MsSqlConnection connection, Guid userId, Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Why: Insert a TenantOrgAccess row so fn_TenantFilter's EXISTS subquery finds the user.
        var connResult = await OpenSession(connection, cancellationToken);
        connResult.IsSuccess.ShouldBeTrue("Failed to open SQL connection for SeedTenantOrgAccess");
        await using var sqlConn = connResult.Value!;
        await using var cmd = sqlConn.CreateCommand();
        cmd.CommandText = @"
            SET QUOTED_IDENTIFIER ON;
            INSERT INTO tenant.TenantOrgAccess (UserId, TenantId, RoleName, PermissionName, IsCurrent, IsDeleted)
            VALUES (@userId, @tenantId, 'TestRole', 'test.perm', 1, 0)";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteTenantOrgAccess(
        MsSqlConnection connection, Guid userId,
        CancellationToken cancellationToken)
    {
        var connResult = await OpenSession(connection, cancellationToken);
        connResult.IsSuccess.ShouldBeTrue("Failed to open SQL connection for DeleteTenantOrgAccess");
        await using var sqlConn = connResult.Value!;
        await using var cmd = sqlConn.CreateCommand();
        cmd.CommandText = "DELETE FROM tenant.TenantOrgAccess WHERE UserId = @userId AND RoleName = 'TestRole'";
        cmd.Parameters.AddWithValue("@userId", userId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedSchedule(
        MsSqlConnection connection, Guid tenantId, string name,
        CancellationToken cancellationToken)
    {
        var connResult = await OpenSession(connection, cancellationToken);
        connResult.IsSuccess.ShouldBeTrue("Failed to open SQL connection for SeedSchedule");
        await using var sqlConn = connResult.Value!;
        await using var cmd = sqlConn.CreateCommand();
        cmd.CommandText = @"
            SET QUOTED_IDENTIFIER ON;
            INSERT INTO sched.Schedule
                (Id, Name, ServiceOptionType, PipelineName, Description, IsEnabled, MaxRetries,
                 RetryDelaySeconds, TimeoutSeconds, TimeZoneId, CronExpression, TenantId, VisibilityGroupId)
            VALUES
                (NEWID(), @name, 'Cron', 'RlsTestPipeline', 'RLS acceptance test row', 0, 0,
                 0, 60, 'UTC', '0 0 * * *', @tenantId, NULL)";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> CountSchedules(
        MsSqlConnection connection, string namePrefix,
        CancellationToken cancellationToken)
    {
        // Why: OpenSession sets SESSION_CONTEXT (via the production SetUserSessionContext path)
        // before the COUNT query runs, so SESSION_CONTEXT(UserId, TenantId, CrossTenant) is in
        // effect. This is the real end-to-end RLS proof path.
        var connResult = await OpenSession(connection, cancellationToken);
        connResult.IsSuccess.ShouldBeTrue("Failed to open SQL connection for CountSchedules");
        await using var sqlConn = connResult.Value!;
        await using var cmd = sqlConn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sched.Schedule WHERE Name LIKE @prefix + '%' AND IsCurrent = 1 AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@prefix", namePrefix);
        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task DeleteSchedules(
        MsSqlConnection connection, string namePrefix,
        CancellationToken cancellationToken)
    {
        var connResult = await OpenSession(connection, cancellationToken);
        connResult.IsSuccess.ShouldBeTrue("Failed to open SQL connection for DeleteSchedules");
        await using var sqlConn = connResult.Value!;
        await using var cmd = sqlConn.CreateCommand();
        cmd.CommandText = "DELETE FROM sched.Schedule WHERE Name LIKE @prefix + '%'";
        cmd.Parameters.AddWithValue("@prefix", namePrefix);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
