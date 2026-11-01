using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-558: <see cref="DataStoreConfigurationProvider.Get(CancellationToken)"/> must return FULLY
/// COMPOSED aggregates (Paths/Containers/Fields), not bare header rows.
/// </summary>
/// <remarks>
/// Rewritten for the domain/implementation provider split (FDW-727): <c>DataStoreConfigurationProvider</c>
/// is a <c>DomainConfigurationProviderBase&lt;IDataStoreImplementationConfiguration&gt;</c> — it reads the
/// shared 4-column domain row (Id/Name/Domain/Implementation), then dispatches to whichever
/// <see cref="IImplementationConfigurationProvider{TConfiguration}"/> is registered under the domain
/// row's <c>Implementation</c> name (see <c>DataStoreServiceTypes</c>'s production wiring: the SAME
/// <see cref="DataStoreImplementationConfigurationProvider"/> instance is registered under every
/// <c>DataStoreTypes</c> kind name, since DataStore has one implementation table regardless of kind).
/// The bulk <c>Get(ct)</c> path this file exercises batches through
/// <c>ImplementationConfigurationProviderBase.GetSet</c> -&gt;
/// <see cref="IConfigurationGateway.ExecuteRelationshipRead"/> (FDW-811), not a per-row typed-body
/// registry -- the old "typed body provider" / <c>IServiceConfigurationProvider</c> registration this
/// file previously exercised was deleted along with the pre-split architecture.
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataStoreConfigurationProviderTests
{
    private const string Implementation = "MsSql";

    // ========================================================================
    // Fixture
    // ========================================================================

    // Why: ResolveOwnerKeyColumns("DataStoreConfiguration") needs the owner container's Physical
    // (RowId) and Logical (Id) key metadata to build the Path JOIN. Without it, the relationship read
    // is ineligible for the owner-key correlation and Paths would stay empty regardless of whether
    // composition ran at all -- this fixture is what lets the test tell "composed" apart from
    // "silently skipped".
    // Why a domain "DataStore" container too, wired via a Foreign key from "DataStoreConfiguration":
    // the implementation provider's grouped Get(ids) overload (what the domain provider's batched
    // Get(ct) actually dispatches to per implementation) always takes GetSet's domainIds-not-null
    // branch, which resolves the join back to the domain row via ForeignKeyToDomain/ResolveDomainJoin
    // BEFORE it ever reaches the plain owner-key resolution this fixture used to stop at — without a
    // Foreign key here, ResolveDomainJoin returns null and the whole read fails "No suitable key".
    private static IReadOnlyList<IDataStore> BuildDataStoreOwnerKeyTree()
    {
        static IReadOnlyList<IContainerKey> Identity() => [Key(KeyTypes.Physical, "RowId", null), Key(KeyTypes.Logical, "Id", null)];

        static IContainerKey Key(KeyTypeBase keyType, string column, IDataContainer? referencedContainer)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(column);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            if (referencedContainer is not null)
            {
                // A Foreign key holds its owner's Physical key (RowId).
                var referenced = new Mock<IDataField>();
                referenced.Setup(f => f.Name).Returns("RowId");
                keyField.Setup(k => k.ReferencedField).Returns(referenced.Object);
            }

            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(keyType);
            key.Setup(k => k.KeyName).Returns($"{keyType.Name}_{column}");
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            key.Setup(k => k.ReferencedContainer).Returns(referencedContainer);
            return key.Object;
        }

        // The ownership tree ConfigurationDb's data.* DDL declares, leaves first. Each container
        // lists the Foreign keys that reference it; an owner is found only through those.
        var containers = new List<IDataContainer>();
        IDataContainer Container(string name, params (string Column, IDataContainer Child)[] owns)
        {
            var container = new Mock<IDataContainer>();
            container.Setup(c => c.Name).Returns(name);
            container.Setup(c => c.Keys).Returns(Identity());
            IReadOnlyList<ReferencingKeyBinding> referencing =
                [.. owns.Select(o => new ReferencingKeyBinding(Key(KeyTypes.Foreign, o.Column, container.Object), o.Child))];
            container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
            containers.Add(container.Object);
            return container.Object;
        }

        var keyContainer = Container("DataContainerKey", ("DataContainerKeyRowId", Container("DataContainerKeyField")));
        var containerContainer = Container("DataContainer",
            ("DataContainerRowId", Container("DataContainerField")), ("DataContainerRowId", keyContainer));
        var pathContainer = Container("DataPath",
            ("DataPathRowId", containerContainer),
            ("DataPathRowId", Container("DataPathPolicy")),
            ("DataPathRowId", Container("DataPathSegment")),
            ("DataPathRowId", Container("FileTypeHandlerOverride")));
        var dataStoreContainer = Container("DataStoreConfiguration", ("DataStoreConfigurationRowId", pathContainer));
        Container("DataStore", ("DataStoreRowId", dataStoreContainer));

        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns("data");
        path.Setup(p => p.Containers).Returns(containers);
        path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            containers.FirstOrDefault(c => string.Equals(c.Name, n, StringComparison.Ordinal)) is { } found
                ? GenericResult<IDataContainer>.Success(found)
                : GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));
        foreach (var container in containers)
            Mock.Get(container).Setup(c => c.Parent).Returns(path.Object);

        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("PlatformConfiguration");
        store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
        store.Setup(s => s.Path(It.Is<string>(n => string.Equals(n, "data", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Success(path.Object));
        store.Setup(s => s.Path(It.Is<string>(n => !string.Equals(n, "data", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Failure(new GenericMessage("path not found")));

        return new List<IDataStore> { store.Object };
    }

    private static (Mock<IConfigurationGateway> Gateway, DataStoreConfigurationProvider Provider) MakeProvider(
        DataStoreImplementationConfiguration[] headers,
        DataPathConfiguration[] pathRowsPerHeader)
    {
        foreach (var header in headers)
        {
            if (string.IsNullOrEmpty(header.Implementation))
                header.Implementation = Implementation;
        }

        var mockGateway = new Mock<IConfigurationGateway>();
        mockGateway.Setup(g => g.DataStores).Returns(BuildDataStoreOwnerKeyTree());

        // Why: the domain provider's own bulk read — a data store domain row per header, carrying the
        // Implementation name the domain provider dispatches on and the fields the store's list shows
        // (FDW-1003), which a read stamps back onto the implementation.
        mockGateway
            .Setup(g => g.Execute<IEnumerable<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>>.Success(
                headers.Select(h => new global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration
                {
                    Id = h.Id,
                    Name = h.Name,
                    Domain = "DataStore",
                    Implementation = h.Implementation,
                    ConnectionId = h.ConnectionId,
                    Description = h.Description,
                    ClassificationId = h.ClassificationId,
                    LastDiscoveredAt = h.LastDiscoveredAt,
                    CreatedAt = h.CreatedAt,
                })));

        // Why: GetSet's own parent read on the implementation provider — the flat, all-items list
        // every header row comes from.
        mockGateway
            .Setup(g => g.Execute<IEnumerable<DataStoreImplementationConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataStoreImplementationConfiguration>>.Success(headers));

        // Why: every child set below a path (containers, policies, segments...) is read too; they hold
        // nothing here. Registered first so the DataPath read below takes precedence.
        mockGateway
            .Setup(g => g.Execute(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<object>>.Success([]));

        // Why: the by-type Execute for the DataStore->DataPath cascade.
        mockGateway
            .Setup(g => g.Execute(
                It.IsAny<IDataCommand>(),
                It.Is<DataStoreTarget>(t => string.Equals(t.Container, "DataPath", StringComparison.Ordinal)),
                typeof(DataPathConfiguration),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<object>>.Success(pathRowsPerHeader));

        var implementationProvider = new DataStoreImplementationConfigurationProvider(
            NullLogger<DataStoreImplementationConfigurationProvider>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "PlatformConfiguration", "data", "DataStoreConfiguration");

        var provider = new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance,
            GatewayProviderFor(mockGateway.Object),
            "PlatformConfiguration", "data", "DataStore");
        provider.Register(Implementation, implementationProvider);

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
        var header1 = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "Store1", ConnectionId = Guid.NewGuid() };
        var header2 = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "Store2", ConnectionId = Guid.NewGuid() };
        var pathRow = new DataPathConfiguration { Id = Guid.NewGuid(), Name = "data" };

        var (_, provider) = MakeProvider([header1, header2], [pathRow]);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
        // Why: this is the exact defect FDW-558 fixes — before batching, config.Paths was always
        // empty on the all-items list. Both headers must come back with Paths composed.
        result.Value.ShouldAllBe(store => store.Paths.Count == 1 && store.Paths[0].Name == "data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetReturnsEmptyListWhenNoDataStoresExist()
    {
        var (_, provider) = MakeProvider([], []);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // ========================================================================
    // Get(CancellationToken) — one implementation's failure fails the WHOLE list (no partial fallback)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetFailsWholeListWhenOneHeaderComposeFailsNoPartialFallback()
    {
        var header1 = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "Store1", ConnectionId = Guid.NewGuid(), Implementation = Implementation };
        var header2 = new DataStoreImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Store2",
            ConnectionId = Guid.NewGuid(),
            Implementation = "Failing",
        };

        var (_, provider) = MakeProvider([header1, header2], []);

        // Why: registers a SEPARATE implementation provider for "Failing" whose single-id Get(Guid)
        // always fails, so the domain provider's per-row loop fails outright on header2 — the
        // erased-typed-body registry this test previously exercised was deleted with the pre-split
        // architecture; IImplementationConfigurationProvider<T> is the current contract to mock.
        var failingProvider = new Mock<IImplementationConfigurationProvider<IDataStoreImplementationConfiguration>>();
        failingProvider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(new GenericMessage("typed body read failed")));

        // A list read composes each row through the level-limited read (FDW-951), so it fails the same way.
        failingProvider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(new GenericMessage("typed body read failed")));
        provider.Register("Failing", failingProvider.Object);

        var result = await provider.Get(TestContext.Current.CancellationToken);

        // Why: NO FALLBACKS WITHOUT EXPLICIT APPROVAL — header1's implementation group would compose
        // successfully, but the whole list must still fail rather than silently return a partial
        // 1-item list once ANY implementation group fails.
        result.IsSuccess.ShouldBeFalse();
    }

    // ========================================================================
    // Regression: composing an MsSql-discriminated header must not recurse back into Get(ct)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public async Task GetDoesNotRecurseWhenComposingAnMsSqlDiscriminatedHeader()
    {
        // Why: the original wiring made this hang (see FDW-558 history). That projection is deleted;
        // the domain/implementation split makes the cycle structurally unconstructable, since the
        // implementation provider holds no reference back to the header provider. This test keeps the
        // P0 guard pointed at the invariant: composing an MsSql-discriminated header must return, not
        // hang, within a bounded time.
        var header = new DataStoreImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "OpsDb",
            ConnectionId = Guid.NewGuid(),
            Implementation = "MsSql",
        };

        var (_, provider) = MakeProvider([header], []);

        var resultTask = provider.Get(TestContext.Current.CancellationToken);
        var winner = await Task.WhenAny(resultTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        winner.ShouldBe(resultTask,
            "DataStoreConfigurationProvider.Get(ct) did not return within 5s — this is the FDW-558 recursion regressing.");

        var result = await resultTask;
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("OpsDb");
    }

    // Why the gateway answers for any connection: these tests exercise what a provider does with
    // its gateway, not which one it selects.
    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;
        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;
        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);
        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
