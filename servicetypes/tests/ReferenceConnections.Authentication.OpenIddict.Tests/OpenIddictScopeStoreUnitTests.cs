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
/// Unit tests for OpenIddictScopeStore using Mock&lt;IDataGateway&gt;.
/// Asserts that the store builds the correct commands (parent version + child rows,
/// SetResources supersede-then-insert) and maps query results correctly.
/// No real DB — all gateway calls return canned results.
/// </summary>
public sealed class OpenIddictScopeStoreUnitTests
{
    private readonly Mock<IDataGateway> _gatewayMock;
    private readonly OpenIddictScopeStore _store;

    public OpenIddictScopeStoreUnitTests()
    {
        _gatewayMock = new Mock<IDataGateway>(MockBehavior.Loose);

        // Default: inserts return success with 1 row affected.
        _gatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var lazyGateway = new Lazy<IDataGateway>(() => _gatewayMock.Object);
        _store = new OpenIddictScopeStore(lazyGateway, NullLogger<OpenIddictScopeStore>.Instance);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateAsync_WithNoResources_ExecutesSingleInsertForParent()
    {
        var scope = new OpenIddictScopeRecord { Name = "fdw.api" };

        await _store.CreateAsync(scope, TestContext.Current.CancellationToken);

        // Exactly one insert call (the parent row).
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScope")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        scope.Id.ShouldNotBe(Guid.Empty);
        scope.IsCurrent.ShouldBeTrue();
        scope.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateAsync_WithPendingResources_InsertsParentThenChildRows()
    {
        var scope = new OpenIddictScopeRecord { Name = "fdw.api" };
        // Stage two resources before CreateAsync (normal OpenIddict manager flow).
        await _store.SetResourcesAsync(scope, ImmutableArray.Create("https://api.fdw.local", "https://etl.fdw.local"), TestContext.Current.CancellationToken);

        await _store.CreateAsync(scope, TestContext.Current.CancellationToken);

        // One parent insert.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScope")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Two child inserts (one per resource).
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScopeResource")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // PendingResources cleared after flush (verified by absence of extra calls).
    }

    // ── UpdateAsync ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task UpdateAsync_SupersedesCurrentParentAndInsertsNewVersion()
    {
        var scope = new OpenIddictScopeRecord { Id = Guid.NewGuid(), Name = "fdw.api", IsCurrent = true };

        await _store.UpdateAsync(scope, TestContext.Current.CancellationToken);

        // One UPDATE to supersede old parent version.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Update")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScope")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // One INSERT for the new parent version.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScope")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // No child operations when PendingResources is null.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScopeResource")),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task UpdateAsync_WithPendingResources_SupersedesOldChildSetAndInsertsNew()
    {
        var scope = new OpenIddictScopeRecord { Id = Guid.NewGuid(), Name = "fdw.api", IsCurrent = true };
        await _store.SetResourcesAsync(scope, ImmutableArray.Create("https://api.fdw.local"), TestContext.Current.CancellationToken);

        await _store.UpdateAsync(scope, TestContext.Current.CancellationToken);

        // UPDATE to supersede old parent + UPDATE to supersede old child set.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Update")),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // INSERT for new parent version + INSERT for new child row.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // Pending resources cleared after UpdateAsync (verified by absence of extra calls).
    }

    // ── GetResourcesAsync ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetResourcesAsync_QueriesScopeResourceContainerAndReturnsImmutableArray()
    {
        var scopeId = Guid.NewGuid();
        var scope = new OpenIddictScopeRecord { Id = scopeId };

        var cannedRows = new List<OpenIddictScopeResourceRecord>
        {
            new() { ScopeId = scopeId, Resource = "https://api.fdw.local", IsCurrent = true },
            new() { ScopeId = scopeId, Resource = "https://etl.fdw.local", IsCurrent = true }
        };

        _gatewayMock
            .Setup(g => g.Execute<IEnumerable<OpenIddictScopeResourceRecord>>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScopeResource")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<OpenIddictScopeResourceRecord>>.Success(cannedRows));

        var resources = await _store.GetResourcesAsync(scope, TestContext.Current.CancellationToken);

        resources.Length.ShouldBe(2);
        resources.ShouldContain("https://api.fdw.local");
        resources.ShouldContain("https://etl.fdw.local");

        _gatewayMock.Verify(
            g => g.Execute<IEnumerable<OpenIddictScopeResourceRecord>>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Query")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScopeResource")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetResourcesAsync_WithEmptyScopeId_ReturnsEmptyWithoutDbCall()
    {
        var scope = new OpenIddictScopeRecord { Id = Guid.Empty };

        var resources = await _store.GetResourcesAsync(scope, TestContext.Current.CancellationToken);

        resources.ShouldBeEmpty();
        _gatewayMock.Verify(g => g.Execute<IEnumerable<OpenIddictScopeResourceRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── FindByNameAsync ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task FindByNameAsync_QueriesScopeContainerAndReturnsFirstMatch()
    {
        var expectedScope = new OpenIddictScopeRecord { Id = Guid.NewGuid(), Name = "fdw.api", IsCurrent = true };
        _gatewayMock
            .Setup(g => g.Execute<IEnumerable<OpenIddictScopeRecord>>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictScope")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<OpenIddictScopeRecord>>.Success(new[] { expectedScope }));

        var result = await _store.FindByNameAsync("fdw.api", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("fdw.api");
    }

    // ── SetResourcesAsync staging ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task SetResourcesAsync_StagesResourcesOnScope_WithoutDbCall()
    {
        var scope = new OpenIddictScopeRecord { Id = Guid.NewGuid(), Name = "fdw.api" };

        await _store.SetResourcesAsync(scope, ImmutableArray.Create("https://api.fdw.local"), TestContext.Current.CancellationToken);

        // SetResourcesAsync is staging only — no DB calls yet.
        _gatewayMock.VerifyNoOtherCalls();
        // Resources are staged internally (ConditionalWeakTable) — verified by next Create/Update call.
    }

    // ── GetPropertiesAsync / SetPropertiesAsync ─────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPropertiesAsync_ReturnsEmptyDictionary()
    {
        var scope = new OpenIddictScopeRecord { Id = Guid.NewGuid(), Name = "fdw.api" };

        var properties = await _store.GetPropertiesAsync(scope, TestContext.Current.CancellationToken);

        properties.ShouldBeEmpty();
        _gatewayMock.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SetPropertiesAsync_IsNoOp()
    {
        var scope = new OpenIddictScopeRecord { Id = Guid.NewGuid(), Name = "fdw.api" };

        await _store.SetPropertiesAsync(scope, System.Collections.Immutable.ImmutableDictionary<string, System.Text.Json.JsonElement>.Empty, TestContext.Current.CancellationToken);

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

    // ── Helpers ────────────────────────────────────────────────────────────────────

    // Why: IDataCommand.ContainerName was removed when addressing moved to DataStoreTarget.
    // Container assertions now check DataStoreTarget.Container in a separate It.Is<DataStoreTarget> predicate.
    private static bool TargetContainerIs(DataStoreTarget target, string containerName)
        => string.Equals(target.Container, containerName, StringComparison.Ordinal);

    private static bool TypeIs(IDataCommand cmd, string commandType)
        => string.Equals(cmd.CommandType, commandType, StringComparison.OrdinalIgnoreCase);
}
