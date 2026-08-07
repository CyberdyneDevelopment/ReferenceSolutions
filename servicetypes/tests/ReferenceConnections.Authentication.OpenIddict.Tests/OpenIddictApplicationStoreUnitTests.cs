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
/// Unit tests for OpenIddictApplicationStore using Mock&lt;IDataGateway&gt;.
/// Asserts that the store builds the correct commands (parent version + all 4 child sets,
/// Set*Async supersede-then-insert) and maps query results correctly.
/// No real DB — all gateway calls return canned results.
/// </summary>
public sealed class OpenIddictApplicationStoreUnitTests
{
    private readonly Mock<IDataGateway> _gatewayMock;
    private readonly OpenIddictApplicationStore _store;

    public OpenIddictApplicationStoreUnitTests()
    {
        _gatewayMock = new Mock<IDataGateway>(MockBehavior.Loose);

        // Default: inserts and updates return success.
        _gatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var lazyGateway = new Lazy<IDataGateway>(() => _gatewayMock.Object);
        _store = new OpenIddictApplicationStore(lazyGateway, NullLogger<OpenIddictApplicationStore>.Instance);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateAsync_WithNoChildSets_ExecutesSingleParentInsert()
    {
        var app = new OpenIddictApplicationRecord { ClientId = "fdw.service", ClientType = "confidential" };

        await _store.CreateAsync(app, TestContext.Current.CancellationToken);

        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplication")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        app.Id.ShouldNotBe(Guid.Empty);
        app.IsCurrent.ShouldBeTrue();
        app.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateAsync_WithAllChildSets_InsertsParentThenAllFourChildSets()
    {
        var app = new OpenIddictApplicationRecord { ClientId = "fdw.service", ClientType = "confidential" };
        var ct = TestContext.Current.CancellationToken;

        // Stage all four child sets before CreateAsync.
        await _store.SetPermissionsAsync(app, ImmutableArray.Create("ept:token", "gt:client_credentials"), ct);
        await _store.SetRedirectUrisAsync(app, ImmutableArray.Create("https://app.local/callback"), ct);
        await _store.SetPostLogoutRedirectUrisAsync(app, ImmutableArray.Create("https://app.local/signout"), ct);
        await _store.SetRequirementsAsync(app, ImmutableArray.Create("pkce"), ct);

        await _store.CreateAsync(app, ct);

        // One parent insert.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplication")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Two permission inserts.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplicationPermission")),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // One redirect URI insert.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplicationRedirectUri")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // One post-logout redirect URI insert.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplicationPostLogoutRedirectUri")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // One requirement insert.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplicationRequirement")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── UpdateAsync ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task UpdateAsync_SupersedesCurrentParentAndInsertsNewVersion()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.NewGuid(), ClientId = "fdw.service", ClientType = "confidential", IsCurrent = true };

        await _store.UpdateAsync(app, TestContext.Current.CancellationToken);

        // One UPDATE to supersede old parent version.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Update")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplication")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // One INSERT for the new parent version.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplication")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // No child operations when no child sets are staged.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplicationPermission")),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task UpdateAsync_WithStagedPermissions_SupersedesOldSetAndInsertsNew()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.NewGuid(), ClientId = "fdw.service", ClientType = "confidential", IsCurrent = true };
        var ct = TestContext.Current.CancellationToken;

        await _store.SetPermissionsAsync(app, ImmutableArray.Create("ept:token", "gt:client_credentials", "scp:fdw.api"), ct);
        await _store.UpdateAsync(app, ct);

        // UPDATE parent + UPDATE child set (supersede).
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Update")),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // INSERT new parent + 3 new permission rows.
        _gatewayMock.Verify(
            g => g.Execute<int>(
                It.Is<IDataCommand>(cmd => TypeIs(cmd, "Insert")),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    // ── GetPermissionsAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPermissionsAsync_QueriesPermissionContainerAndReturnsImmutableArray()
    {
        var appId = Guid.NewGuid();
        var app = new OpenIddictApplicationRecord { Id = appId };
        var cannedRows = new List<OpenIddictApplicationPermissionRecord>
        {
            new() { ApplicationId = appId, Permission = "ept:token", IsCurrent = true },
            new() { ApplicationId = appId, Permission = "gt:client_credentials", IsCurrent = true }
        };

        _gatewayMock
            .Setup(g => g.Execute<IEnumerable<OpenIddictApplicationPermissionRecord>>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplicationPermission")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<OpenIddictApplicationPermissionRecord>>.Success(cannedRows));

        var permissions = await _store.GetPermissionsAsync(app, TestContext.Current.CancellationToken);

        permissions.Length.ShouldBe(2);
        permissions.ShouldContain("ept:token");
        permissions.ShouldContain("gt:client_credentials");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPermissionsAsync_WithEmptyId_ReturnsEmptyWithoutDbCall()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.Empty };

        var permissions = await _store.GetPermissionsAsync(app, TestContext.Current.CancellationToken);

        permissions.ShouldBeEmpty();
        _gatewayMock.Verify(
            g => g.Execute<IEnumerable<OpenIddictApplicationPermissionRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── FindByClientIdAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task FindByClientIdAsync_QueriesApplicationContainerAndReturnsFirstMatch()
    {
        var expected = new OpenIddictApplicationRecord { Id = Guid.NewGuid(), ClientId = "fdw.service", IsCurrent = true };
        _gatewayMock
            .Setup(g => g.Execute<IEnumerable<OpenIddictApplicationRecord>>(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => TargetContainerIs(t, "OpenIddictApplication")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<OpenIddictApplicationRecord>>.Success(new[] { expected }));

        var result = await _store.FindByClientIdAsync("fdw.service", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.ClientId.ShouldBe("fdw.service");
    }

    // ── GetPropertiesAsync / SetPropertiesAsync ─────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPropertiesAsync_ReturnsEmptyDictionary()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.NewGuid() };

        var properties = await _store.GetPropertiesAsync(app, TestContext.Current.CancellationToken);

        properties.ShouldBeEmpty();
        _gatewayMock.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SetPropertiesAsync_IsNoOp()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.NewGuid() };

        await _store.SetPropertiesAsync(app, System.Collections.Immutable.ImmutableDictionary<string, System.Text.Json.JsonElement>.Empty, TestContext.Current.CancellationToken);

        _gatewayMock.VerifyNoOtherCalls();
    }

    // ── GetJsonWebKeySetAsync / SetJsonWebKeySetAsync ──────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task GetJsonWebKeySetAsync_ReturnsNull()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.NewGuid() };

        var jwks = await _store.GetJsonWebKeySetAsync(app, TestContext.Current.CancellationToken);

        jwks.ShouldBeNull();
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

    // ── SetPermissionsAsync staging ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task SetPermissionsAsync_StagesPermissions_WithoutDbCall()
    {
        var app = new OpenIddictApplicationRecord { Id = Guid.NewGuid() };

        await _store.SetPermissionsAsync(app, ImmutableArray.Create("ept:token"), TestContext.Current.CancellationToken);

        _gatewayMock.VerifyNoOtherCalls();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────

    // Why: Addressing moved off IDataCommand to DataStoreTarget. Container assertions now check
    // the target passed alongside the command rather than a property on the command itself.
    private static bool TargetContainerIs(DataStoreTarget target, string containerName)
        => string.Equals(target.Container, containerName, StringComparison.Ordinal);

    private static bool TypeIs(IDataCommand cmd, string commandType)
        => string.Equals(cmd.CommandType, commandType, StringComparison.OrdinalIgnoreCase);
}
