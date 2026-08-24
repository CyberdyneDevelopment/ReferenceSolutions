using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-558: <see cref="DataStoreConfigurationProvider.Get(CancellationToken)"/> must return FULLY
/// COMPOSED aggregates (Paths/Containers/Fields), not bare header rows — the base
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}.Get(CancellationToken)"/> is deliberately
/// header-only (other domains, e.g. lineage, rely on the cheap flat list), so
/// <c>ListDataStoresEndpointBase.MapToSummary</c>'s <c>PathCount</c>/<c>ContainerCount</c> — which
/// dot-walk <c>config.Paths</c> — computed 0 for every store. This is a scoped override on
/// DataStoreConfigurationProvider only; it must NOT change the base class's behavior.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataStoreConfigurationProviderTests
{
    // ========================================================================
    // Fixture
    // ========================================================================

    // Why: ResolveOwnerKeyColumns("DataStore") needs the owner container's Physical (RowId) and
    // Logical (Id) key metadata to build the Path JOIN. Without it, LoadChildrenInto skips silently
    // (NoSuitableKeyForContainer) and Paths would stay empty regardless of whether ComposeAggregate ran
    // at all — this fixture is what lets the test tell "composed" apart from "silently skipped".
    private static IReadOnlyList<IDataStore> BuildDataStoreOwnerKeyTree()
    {
        var physicalField = new Mock<IDataField>();
        physicalField.Setup(f => f.Name).Returns("RowId");
        var physicalKeyField = new Mock<IContainerKeyField>();
        physicalKeyField.Setup(k => k.LocalField).Returns(physicalField.Object);
        var physicalKey = new Mock<IContainerKey>();
        physicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Physical);
        physicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { physicalKeyField.Object });
        physicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var logicalField = new Mock<IDataField>();
        logicalField.Setup(f => f.Name).Returns("Id");
        var logicalKeyField = new Mock<IContainerKeyField>();
        logicalKeyField.Setup(k => k.LocalField).Returns(logicalField.Object);
        var logicalKey = new Mock<IContainerKey>();
        logicalKey.Setup(k => k.KeyType).Returns(KeyTypes.Logical);
        logicalKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { logicalKeyField.Object });
        logicalKey.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);

        var dataStoreContainer = new Mock<IDataContainer>();
        dataStoreContainer.Setup(c => c.Name).Returns("DataStore");
        dataStoreContainer.Setup(c => c.Keys).Returns(new List<IContainerKey> { physicalKey.Object, logicalKey.Object });

        var path = new Mock<IDataPath>();
        path.Setup(p => p.Name).Returns("data");
        path.Setup(p => p.Containers).Returns(new List<IDataContainer> { dataStoreContainer.Object });
        path.Setup(p => p.Container(It.Is<string>(n => string.Equals(n, "DataStore", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(dataStoreContainer.Object));
        path.Setup(p => p.Container(It.Is<string>(n => !string.Equals(n, "DataStore", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("ConfigurationDb");
        store.Setup(s => s.Paths).Returns(new List<IDataPath> { path.Object });
        store.Setup(s => s.Path(It.Is<string>(n => string.Equals(n, "data", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataPath>.Success(path.Object));
        store.Setup(s => s.Path(It.Is<string>(n => !string.Equals(n, "data", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataPath>.Failure(new GenericMessage("path not found")));

        return new List<IDataStore> { store.Object };
    }

    private static (Mock<IConfigurationGateway> Gateway, DataStoreConfigurationProvider Provider) MakeProvider(
        DataStoreConfiguration[] headers,
        DataPathConfiguration[] pathRowsPerHeader)
    {
        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(BuildDataStoreOwnerKeyTree());

        // Why: base.Get(ct) — the flat, all-items List command every header row comes from.
        mockGateway
            .Setup(g => g.Execute<IEnumerable<DataStoreConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataStoreConfiguration>>.Success(headers));

        // Why: LoadTypedListChild's by-type Execute for the DataStore->DataPath cascade — the same
        // canned rows are returned for every owner (the mock cannot cheaply disambiguate per-owner
        // without duplicating BuildChildJoinQuery's filter-tree shape, already covered by the
        // JOIN-shape assertions in DefaultConfigurationProviderGetByIdTests). What this test asserts
        // is that EVERY header in the list gets its Paths composed via ComposeAggregate, not that the
        // rows differ per header.
        mockGateway
            .Setup(g => g.Execute(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => string.Equals(t.Container, "DataPath", StringComparison.Ordinal)),
                typeof(DataPathConfiguration),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<object>>.Success(pathRowsPerHeader));

        var containerProvider = new Lazy<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>(
            () => new DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>(
                NullLogger<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>.Instance,
                new Lazy<IConfigurationGateway>(() => mockGateway.Object),
                "ConfigurationDb", "data"));

        var provider = new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => mockGateway.Object),
            containerProvider);

        return (mockGateway, provider);
    }

    // ========================================================================
    // Get(CancellationToken) — composes every header, does not return bare rows
    // ========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetComposesEachHeaderIntoFullAggregateWithPaths()
    {
        var header1 = new DataStoreConfiguration { Id = Guid.NewGuid(), Name = "Store1", ConnectionId = Guid.NewGuid() };
        var header2 = new DataStoreConfiguration { Id = Guid.NewGuid(), Name = "Store2", ConnectionId = Guid.NewGuid() };
        var pathRow = new DataPathConfiguration { Id = Guid.NewGuid(), Name = "data" };

        var (_, provider) = MakeProvider([header1, header2], [pathRow]);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
        // Why: this is the exact defect FDW-558 fixes — before the override, config.Paths was always
        // empty on the all-items list, so ListDataStoresEndpointBase.MapToSummary computed PathCount=0
        // for every store. Both headers must come back with Paths composed, not just the header row.
        result.Value.ShouldAllBe(store => store.Paths.Count == 1 && store.Paths[0].Name == "data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetReturnsEmptyListWhenNoDataStoresExist()
    {
        var (_, provider) = MakeProvider([], []);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // ========================================================================
    // Get(CancellationToken) — one compose failure fails the WHOLE list (no partial fallback)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetFailsWholeListWhenOneHeaderComposeFailsNoPartialFallback()
    {
        var header1 = new DataStoreConfiguration { Id = Guid.NewGuid(), Name = "Store1", ConnectionId = Guid.NewGuid() };
        var header2 = new DataStoreConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Store2",
            ConnectionId = Guid.NewGuid(),
            ServiceOptionType = "Failing",
        };

        var (_, provider) = MakeProvider([header1, header2], []);

        // Why: registers a typed-body provider for "Failing" whose Get(Guid) always fails, forcing
        // ComposeTypedBody -> ComposeAggregate -> the Get(ct) override to fail for header2 specifically.
        // Why the erased interface and not IServiceConfigurationProvider{IGenericConfiguration}: the
        // registry holds typed bodies erased and ComposeTypedBody dispatches through this Get(Guid),
        // so it is the only member the stand-in needs. The two interfaces are separate, so the
        // generic one over IGenericConfiguration is no longer a way to spell the erased view.
        var failingTypedProvider = new Mock<IServiceConfigurationProvider>();
        failingTypedProvider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConfiguration>.Failure(new GenericMessage("typed body read failed")));
        provider.Register("Failing", failingTypedProvider.Object);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        // Why: NO FALLBACKS WITHOUT EXPLICIT APPROVAL — header1 composed successfully first in the loop,
        // but the whole list must still fail rather than silently return a partial 1-item list.
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage!.ShouldContain("Store2");
    }

    // ========================================================================
    // Regression: composing an MsSql-discriminated header must not recurse back into Get(ct)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public async Task GetDoesNotRecurseWhenComposingAnMsSqlDiscriminatedHeader()
    {
        // Why: the original wiring made this hang. MsSqlDataStoreType.RegisterFactory hand-constructed
        // MsSqlDataStoreConfigProvider — a projection holding a reference BACK to the very
        // DataStoreConfigurationProvider it was registered on — and its Get(Guid) fell back on a cold
        // cache to "load every DataStore and find the match", calling back into provider.Get(ct), which
        // composed the header again and called back in, forever. Each hop is a fresh async continuation,
        // not stack recursion, so the failure mode is a HANG rather than a StackOverflowException —
        // which is what blocked Reference.Api/Reference.Etl startup (never binding Kestrel while
        // issuing ~200+ SELECT ... FROM data.DataStore/sec).
        //
        // That projection is deleted. The typed provider is now a standard DefaultConfigurationProvider
        // that reads data.MsSqlDataStore directly through the gateway and holds NO reference to the
        // header provider, so the cycle is structurally unconstructable. This test keeps the P0 guard
        // pointed at the invariant rather than the deleted class: composing an MsSql-discriminated
        // header must resolve its typed body without re-entering the header provider.
        var header = new DataStoreConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "OpsDb",
            ConnectionId = Guid.NewGuid(),
            ServiceOptionType = "MsSql",
        };

        var (gateway, provider) = MakeProvider([header], []);

        // Why: there is no typed body to register any more. fractaldataworks 81ab56207 ("no typed body —
        // the kind lives on the header") closed MsSqlDataStoreType on DataStoreConfiguration and deleted
        // MsSqlDataStoreConfiguration, its command and its provider, because nothing read them. So the
        // recursion this test guards is now unconstructable twice over: the projection is gone AND there
        // is no typed provider to re-enter the header provider from. The guard that still means something
        // is the one below — composing an MsSql-discriminated header must return, not hang.
        var resultTask = provider.Get(TestContext.Current.CancellationToken);
        var winner = await Task.WhenAny(resultTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        winner.ShouldBe(resultTask,
            "DataStoreConfigurationProvider.Get(ct) did not return within 5s — this is the FDW-558 x " +
            "MsSqlDataStoreConfigProvider recursion regressing (see Get(Guid) remarks).");

        var result = await resultTask;
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("OpsDb");
    }
}
