using Fdw.Services.Authentication.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Storage;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// Unit tests for OpenIddictAuthorizationStore using Mock&lt;IDataGateway&gt;.
/// Asserts that the store builds the correct commands (single-row update-in-place for parent;
/// delete-then-insert for child scope set on SetScopesAsync) and maps query results correctly.
/// No real DB — all gateway calls return canned results.
/// </summary>
public sealed class OpenIddictAuthorizationStoreUnitTests
{
    private readonly Mock<IDataGateway> _gatewayMock;
    private readonly OpenIddictAuthorizationStore _store;

    public OpenIddictAuthorizationStoreUnitTests()
    {
        _gatewayMock = new Mock<IDataGateway>(MockBehavior.Loose);

        _gatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var lazyGateway = new Lazy<IDataGateway>(() => _gatewayMock.Object);
        _store = new OpenIddictAuthorizationStore(lazyGateway, new AuthenticationContextAccessor(), NullLogger<OpenIddictAuthorizationStore>.Instance);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateAsync_WithNoScopes_ExecutesSingleParentInsert()
    {
        var auth = new OpenIddictAuthorizationRecord
        {
            Subject = "user1",
            Status = "valid",
            AuthorizationType = "permanent"
        };

        await _store.CreateAsync(auth, TestContext.Current.CancellationToken);

        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorization")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        auth.Id.ShouldNotBe(Guid.Empty);

        // No child scope inserts.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorizationScope")),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateAsync_WithPendingScopes_InsertsParentThenChildRows()
    {
        var auth = new OpenIddictAuthorizationRecord { Subject = "user1", Status = "valid", AuthorizationType = "permanent" };
        var ct = TestContext.Current.CancellationToken;

        await _store.SetScopesAsync(auth, ImmutableArray.Create("openid", "fdw.api", "offline_access"), ct);
        await _store.CreateAsync(auth, ct);

        // One parent insert.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorization")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Three child scope inserts.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorizationScope")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    // ── UpdateAsync ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task UpdateAsync_IssuesSingleUpdateInPlace()
    {
        // Why: Update-in-place — exactly ONE Update on the parent; zero Inserts; no scope operations.
        var auth = new OpenIddictAuthorizationRecord
        {
            Id = Guid.NewGuid(), Subject = "user1", Status = "valid",
            AuthorizationType = "permanent"
        };

        await _store.UpdateAsync(auth, TestContext.Current.CancellationToken);

        // Exactly one UPDATE (in-place modification of parent row).
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Update")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorization")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Zero INSERT on parent — no version row.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorization")),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // No scope operations when nothing staged.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorizationScope")),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task UpdateAsync_WithStagedScopes_UpdatesParentDeletesOldScopesAndInsertsNew()
    {
        // Why: Update-in-place parent + delete-then-insert child scope set.
        // Previous scope set is DELETED (one Delete on AuthorizationScope), then the new set is inserted.
        var auth = new OpenIddictAuthorizationRecord
        {
            Id = Guid.NewGuid(), Subject = "user1", Status = "valid",
            AuthorizationType = "permanent"
        };
        var ct = TestContext.Current.CancellationToken;

        await _store.SetScopesAsync(auth, ImmutableArray.Create("openid", "fdw.api"), ct);
        await _store.UpdateAsync(auth, ct);

        // One UPDATE on parent (in-place).
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Update")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorization")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // One DELETE on scope child set.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Delete")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorizationScope")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Two INSERTs for the new scope rows.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorizationScope")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ── GetScopesAsync ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetScopesAsync_QueriesScopeContainerAndReturnsImmutableArray()
    {
        var authId = Guid.NewGuid();
        var auth = new OpenIddictAuthorizationRecord { Id = authId };
        var cannedRows = new List<OpenIddictAuthorizationScopeRecord>
        {
            new() { AuthorizationId = authId, Scope = "openid" },
            new() { AuthorizationId = authId, Scope = "fdw.api" }
        };

        _gatewayMock
            .Setup(g => g.Execute<IEnumerable<OpenIddictAuthorizationScopeRecord>>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictAuthorizationScope")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<OpenIddictAuthorizationScopeRecord>>.Success(cannedRows));

        var scopes = await _store.GetScopesAsync(auth, TestContext.Current.CancellationToken);

        scopes.Length.ShouldBe(2);
        scopes.ShouldContain("openid");
        scopes.ShouldContain("fdw.api");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetScopesAsync_WithEmptyId_ReturnsEmptyWithoutDbCall()
    {
        var auth = new OpenIddictAuthorizationRecord { Id = Guid.Empty };

        var scopes = await _store.GetScopesAsync(auth, TestContext.Current.CancellationToken);

        scopes.ShouldBeEmpty();
        _gatewayMock.Verify(
            g => g.Execute<IEnumerable<OpenIddictAuthorizationScopeRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SetScopesAsync staging ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task SetScopesAsync_StagesScopes_WithoutDbCall()
    {
        var auth = new OpenIddictAuthorizationRecord { Id = Guid.NewGuid() };

        await _store.SetScopesAsync(auth, ImmutableArray.Create("openid"), TestContext.Current.CancellationToken);

        _gatewayMock.VerifyNoOtherCalls();
    }

    // ── GetPropertiesAsync ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPropertiesAsync_ReturnsEmptyDictionary()
    {
        var auth = new OpenIddictAuthorizationRecord { Id = Guid.NewGuid() };

        var properties = await _store.GetPropertiesAsync(auth, TestContext.Current.CancellationToken);

        properties.ShouldBeEmpty();
        _gatewayMock.VerifyNoOtherCalls();
    }

    // ── CountAsync (LINQ throws) ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void CountAsync_WithLinqQuery_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() =>
            _store.CountAsync<int>(_ => System.Linq.Enumerable.Empty<int>().AsQueryable(), TestContext.Current.CancellationToken));
    }

    // ── Getters ────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetApplicationIdAsync_ReturnsGuidString()
    {
        var appId = Guid.NewGuid();
        var auth = new OpenIddictAuthorizationRecord { Id = Guid.NewGuid(), ApplicationId = appId };

        var result = await _store.GetApplicationIdAsync(auth, TestContext.Current.CancellationToken);

        result.ShouldBe(appId.ToString());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetApplicationIdAsync_WhenNull_ReturnsNull()
    {
        var auth = new OpenIddictAuthorizationRecord { Id = Guid.NewGuid(), ApplicationId = null };

        var result = await _store.GetApplicationIdAsync(auth, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────

    // Why: Addressing moved off IDataCommand to DataStoreTarget. Container assertions now check
    // the target passed alongside the command rather than a property on the command itself.
    private static bool TargetContainerIs(DataStoreTarget target, string containerName)
        => string.Equals(target.Container, containerName, StringComparison.Ordinal);

    private static bool TypeIs(IDataCommand cmd, string commandType)
        => string.Equals(cmd.CommandType, commandType, StringComparison.OrdinalIgnoreCase);
}
