using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
namespace Fdw.Services.Data.Tests;

/// <summary>
/// Verifies the base provider's recursive READ compose builds the full DataStore aggregate
/// (DataStore → Paths → Containers) driven ENTIRELY by the generated mapper's FK metadata. RowId is
/// invisible to the app (DB-managed IDENTITY, never a POCO property); each child set is read by JOINing
/// the child to the owner ON owner.{PhysicalKey}=child.{Owner}RowId and filtering by the owner's durable
/// Id — the column names resolved from each owner container's key metadata in
/// <see cref="IConfigurationGateway.DataStores"/>.
/// </summary>
/// <remarks>
/// Why: this is the exact runtime-store condition that 500'd in production (AuthDb → OpenIddictScope).
/// AuthDb lives in ConfigurationDb's <c>data.*</c> rows. The old read cascade composed the store with its
/// path but ZERO containers ("1 path(s), 0 container(s)"). NOTHING tested this: every existing provider
/// test mocked the gateway empty, so a 0-children compose was indistinguishable from a pass. This pins the
/// metadata-driven compose AND the physical FK columns the cascade keys on. Uses the REAL
/// DataStore/Path/Container config types (auto-registered via their package module initializers) — no
/// manual TypeCollection registration.
/// <para>
/// Under the RowId-invisible mechanism every OWNER container at every level (DataStore, DataPath) must be
/// present in the gateway's schema tree carrying BOTH a Physical key (column "RowId") and a Logical key
/// (column "Id"); the cascade resolves those column names from that metadata to build the JOIN.
/// </para>
/// </remarks>
public sealed class RecursiveComposeReadTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task GetComposesDataStoreAggregateFromMapperFkMetadata()
    {
        // Arrange — a store with one path holding one container, linked by physical RowId FKs, exactly
        // as the seeded AuthDb → auth → OpenIddictScope rows are in ConfigurationDb's data.* tables.
        // RowId is never set: it is DB-managed and absent from every config POCO; only the durable Id is.
        var store = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "AuthDb" };
        var path = new DataPathConfiguration { Id = Guid.NewGuid(), Name = "auth" };
        var container = new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "OpenIddictScope" };

        var gateway = new ComposingReadGateway(store);
        gateway.Seed(typeof(DataPathConfiguration), path);
        gateway.Seed(typeof(DataContainerConfiguration), container);

        // Why the named provider and not the base: the base is abstract now, and the cascade under test
        // lives in ImplementationProviderBase, which DataStoreImplementationConfigurationProvider closes.
        var provider = new DataStoreImplementationConfigurationProvider(
            NullLogger<DataStoreImplementationConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "ConfigurationDb", "data", "DataStoreConfiguration");

        // Act -- read through the domain provider the implementation is registered under: the
        // implementation joins to the domain container that registered it.
        var domain = new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance, GatewayProviderFor(gateway), "ConfigurationDb", "data", "DataStore");
        domain.Register("MsSql", provider);
        var result = await domain.Get(store.Id, TestContext.Current.CancellationToken);

        // Assert — the FULL aggregate composed: DataStore → Paths(1) → Containers(1).
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Paths.ShouldHaveSingleItem();
        result.Value.Paths[0].Name.ShouldBe("auth");
        result.Value.Paths[0].Containers.ShouldHaveSingleItem();
        result.Value.Paths[0].Containers[0].Name.ShouldBe("OpenIddictScope");

        // Assert — the cascade keyed children via the metadata-driven JOIN to each owner (RowId is invisible;
        // the join filters by the owner's durable Id). At least one typed child read ran per owner level.
        gateway.ChildReads.ShouldNotBeEmpty();
        gateway.ChildReads.ShouldContain(typeof(DataPathConfiguration));
        gateway.ChildReads.ShouldContain(typeof(DataContainerConfiguration));
    }

    /// <summary>
    /// Gateway double: returns the store header on the by-name read and seeded child rows on the by-type
    /// child read, recording the child <see cref="Type"/> each child query requested so the test can assert
    /// the cascade ran a JOIN-based read per owner level. <see cref="DataStores"/> carries the schema tree
    /// with each owner container's Physical (RowId) + Logical (Id) keys — the column names the cascade
    /// resolves to build the JOIN. The seeded children are returned for the requested row-type regardless of
    /// the JOIN filter (the new query filters by owner.Id via a JOIN, not by a single RowId condition).
    /// </summary>
    private sealed class ComposingReadGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly DataStoreImplementationConfiguration _header;
        private readonly Dictionary<Type, List<object>> _childrenByType = new();
        private readonly IReadOnlyList<IDataStore> _stores;

        public ComposingReadGateway(DataStoreImplementationConfiguration header)
        {
            _header = header;
            _stores = [BuildTree()];
        }

        public List<Type> ChildReads { get; } = [];

        public IReadOnlyList<IDataStore> DataStores => _stores;

        public void Seed(Type rowType, params object[] rows) => _childrenByType[rowType] = rows.ToList();

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised in compose-read tests; delegates to existing implementation.
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // The domain row the implementation hangs from carries the implementation's Id.
            // The data stores domain reads its rows as its own record, which holds the fields the store's list
            // shows (FDW-1003); a read reads them back onto the implementation, so the row carries what the header does.
            if (typeof(T) == typeof(IEnumerable<global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new[]
                {
                    new global::Fdw.Services.Data.Configuration.DataStoreDomainConfiguration
                    {
                        Id = _header.Id,
                        Name = _header.Name,
                        Implementation = "MsSql",
                        ConnectionId = _header.ConnectionId,
                        Description = _header.Description,
                        ClassificationId = _header.ClassificationId,
                        LastDiscoveredAt = _header.LastDiscoveredAt,
                        CreatedAt = _header.CreatedAt,
                    },
                }));
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new[] { new DomainConfiguration { Id = _header.Id, Name = _header.Name, Implementation = "MsSql" } }));
            if (typeof(T) == typeof(IEnumerable<DataStoreImplementationConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new List<DataStoreImplementationConfiguration> { _header }));

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        // The typed-list child read — record the requested row-type and return the seeded rows, regardless of
        // the JOIN filter (the new cascade joins on owner.Id rather than a single materialized RowId value).
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            ChildReads.Add(rowType);
            var rows = _childrenByType.TryGetValue(rowType, out var list) ? list : [];
            return Task.FromResult(GenericResult<IEnumerable<object>>.Success(rows));
        }

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in ComposingReadGateway test double")));

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));

        // Why: the schema tree mirrors ConfigurationDb's data.* schema. Each OWNER container (DataStore,
        // DataPath) carries the Physical (RowId) + Logical (Id) keys the cascade resolves to build the
        // child JOIN; the leaf DataContainer is given the same keys so a nested recurse into it never
        // mis-resolves. Modeled on the calc-domain provider test's BuildTree.
        private IDataStore BuildTree()
        {
            // Leaves first: an owner lists the keys that reference it, so its children must exist.
            // DataContainer owns its Fields and Keys, and a Key owns its KeyFields.
            var fieldContainer = Leaf("DataContainerField");
            var keyFieldContainer = Leaf("DataContainerKeyField");
            var keyContainer = Container("DataContainerKey",
                [Binding("DataContainerKeyRowId", keyFieldContainer)],
                [Key("Physical", "PK_DataContainerKey", "RowId", null), Key("Logical", "AK_DataContainerKey", "Id", null)]);
            var containerContainer = Container("DataContainer",
                [Binding("DataContainerRowId", fieldContainer), Binding("DataContainerRowId", keyContainer)],
                [Key("Physical", "PK_DataContainer", "RowId", null), Key("Logical", "AK_DataContainer", "Id", null)]);

            // DataPath owns its Containers, Policies, Segments and FileTypeHandlerOverrides.
            var policyContainer = Leaf("DataPathPolicy");
            var segmentContainer = Leaf("DataPathSegment");
            var handlerOverrideContainer = Leaf("FileTypeHandlerOverride");
            var pathContainer = Container("DataPath",
                [
                    Binding("DataPathRowId", containerContainer),
                    Binding("DataPathRowId", policyContainer),
                    Binding("DataPathRowId", segmentContainer),
                    Binding("DataPathRowId", handlerOverrideContainer),
                ],
                [Key("Physical", "PK_DataPath", "RowId", null), Key("Logical", "AK_DataPath", "Id", null)]);

            // The implementation row owns the Paths (data.DataPath.DataStoreConfigurationRowId), and
            // hangs from the domain row by its Foreign key to it.
            var storeReferencing = new List<ReferencingKeyBinding> { Binding("DataStoreConfigurationRowId", pathContainer) };
            var domainReferencing = new List<ReferencingKeyBinding>();
            var domainContainer = Container("DataStore", domainReferencing,
                [Key("Physical", "PK_DataStore", "RowId", null), Key("Logical", "AK_DataStore", "Id", null)]);
            var storeContainer = Container("DataStoreConfiguration", storeReferencing,
                [
                    Key("Physical", "PK_DataStoreConfiguration", "RowId", null),
                    Key("Logical", "AK_DataStoreImplementation", "Id", null),
                    Key("Foreign", "FK_DataStoreConfiguration_DataStore", "DataStoreRowId", domainContainer),
                ]);
            domainReferencing.Add(Binding("DataStoreRowId", storeContainer));

            var containers = new List<IDataContainer>
            {
                storeContainer, domainContainer, pathContainer, containerContainer, fieldContainer, keyContainer,
                keyFieldContainer, policyContainer, segmentContainer, handlerOverrideContainer,
            };

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("data");
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            {
                var c = containers.FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.Ordinal));
                return c is null ? GenericResult<IDataContainer>.Failure(new GenericMessage("nf")) : GenericResult<IDataContainer>.Success(c);
            });
            foreach (var c in containers)
                Mock.Get(c).Setup(x => x.Parent).Returns(path.Object);

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("ConfigurationDb");
            store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "data", StringComparison.Ordinal)
                    ? GenericResult<IDataNodePath>.Success(path.Object)
                    : GenericResult<IDataNodePath>.Failure(new GenericMessage("nf")));
            return store.Object;
        }

        private static IDataContainer Leaf(string name)
            => Container(name, [], [Key("Physical", $"PK_{name}", "RowId", null), Key("Logical", $"AK_{name}", "Id", null)]);

        private static IDataContainer Container(
            string name, IReadOnlyList<ReferencingKeyBinding> referencing, IReadOnlyList<IContainerKey>? keys)
        {
            var c = new Mock<IDataContainer>();
            c.Setup(x => x.Name).Returns(name);
            c.Setup(x => x.Keys).Returns(keys ?? new List<IContainerKey>());
            c.Setup(x => x.Nodes).Returns(new List<IDataNode>());
            c.Setup(x => x.ReferencingKeys).Returns(
                GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
            return c.Object;
        }

        private static ReferencingKeyBinding Binding(string fkColumn, IDataContainer owner)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(fkColumn);
            // An owner is found by the key that references its Physical key (RowId).
            var referenced = new Mock<IDataField>();
            referenced.Setup(f => f.Name).Returns("RowId");
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            keyField.Setup(k => k.ReferencedField).Returns(referenced.Object);
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyName).Returns($"FK_{fkColumn}_{owner.Name}");
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            return new ReferencingKeyBinding(key.Object, owner);
        }

        private static IContainerKey Key(string keyType, string keyName, string localField, IDataContainer? referenced)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            // Why: KeyType is the abstract KeyTypeBase TypeOption — use the real concrete instances so
            // FindKeyFieldName reads the genuine Name ("Physical"/"Logical").
            KeyTypeBase kt = keyType switch
            {
                "Foreign" => new ForeignKeyType(),
                "Physical" => new PhysicalKeyType(),
                "Logical" => new LogicalKeyType(),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test")
            };
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(kt);
            key.Setup(k => k.KeyName).Returns(keyName);
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            key.Setup(k => k.ReferencedContainer).Returns(referenced);
            return key.Object;
        }

        // The fixture addresses every call; a gateway's generic command surface carries no address,
        // so nothing here routes one and reaching these would be the test lying about the seam.
        string IPlatformService.Id => "ComposingReadGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");
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
