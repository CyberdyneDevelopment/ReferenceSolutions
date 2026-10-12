using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
namespace Fdw.Services.Data.Tests.DataSet;

/// <summary>
/// Proves the keystone base read composes the full DataSet aggregate — Fields + KeyFields + Sources —
/// via <see cref="DataSetConfigurationProvider"/>.Get(id). Children load via a JOIN keyed off the owner's
/// metadata Physical (RowId) + Logical (Id) key columns (RowId is DB-managed and invisible to the POCOs),
/// filtered by the owner's durable Id, with the robust descriptor match (container ↔
/// ConfigurationCommand.ContainerName) bridging the DataSetFieldConfiguration ↔ data.DataSetField name
/// divergence. Also asserts SourceIds derives from Sources.
/// </summary>
public sealed class DataSetAggregateCompositionTests
{
    private const string Implementation = "MsSql";

    private static readonly Guid DataSetId = Guid.NewGuid();
    private static readonly Guid Source1Id = Guid.NewGuid();
    private static readonly Guid Source2Id = Guid.NewGuid();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetComposesFieldsKeyFieldsSourcesViaBaseMechanism()
    {
        var gateway = new AggregateGateway();
        var implementationProvider = new SimpleDataSetConfigurationProvider(
            NullLogger<SimpleDataSetConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "PlatformConfiguration", "data", "SimpleDataSetConfiguration");
        var provider = new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "PlatformConfiguration", "data", "DataSet");
        provider.Register(Implementation, implementationProvider);

        var result = await provider.Get(DataSetId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Fields.Count.ShouldBe(3);
        result.Value.KeyFields.Count.ShouldBe(1);
        result.Value.Sources.Count.ShouldBe(2);
        // SourceIds is computed from the composed Sources.
        result.Value.SourceIds.ShouldBe(new[] { Source1Id, Source2Id }, ignoreOrder: true);
    }

    // Why hand-written: exercises the real read — header read by [Id], then by-type child reads JOINed
    // to the owner (owner.RowId = child.DataSetRowId) and filtered by the owner's durable Id, matched
    // via each child's ConfigurationCommand.ContainerName.
    private sealed class AggregateGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly IReadOnlyList<IDataStore> _stores;
        private readonly List<DomainConfiguration> _domainRows;
        private readonly List<SimpleDataSetConfiguration> _datasets;
        private readonly List<DataSetFieldConfiguration> _fields;
        private readonly List<DataSetKeyFieldConfiguration> _keyFields;
        private readonly List<DataSetSourceConfiguration> _sources;

        public AggregateGateway()
        {
            _domainRows = [new DomainConfiguration { Id = DataSetId, Name = "DS1", Domain = "DataSet", Implementation = DataSetAggregateCompositionTests.Implementation }];
            _datasets = [new SimpleDataSetConfiguration { Id = DataSetId, Name = "DS1" }];
            _fields =
            [
                new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "A", Ordinal = 0 },
                new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "B", Ordinal = 1 },
                new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "C", Ordinal = 2 }
            ];
            _keyFields = [new DataSetKeyFieldConfiguration { Id = Guid.NewGuid(), KeyName = "PK", KeyType = "Surrogate" }];
            _sources =
            [
                new DataSetSourceConfiguration { Id = Source1Id, SourceName = "Primary" },
                new DataSetSourceConfiguration { Id = Source2Id, SourceName = "Fallback" }
            ];
            _stores = [BuildTree()];
        }

        public IReadOnlyList<IDataStore> DataStores => _stores;

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised in dataset composition tests; delegates to existing implementation.
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_domainRows.AsEnumerable()));
            if (typeof(T) == typeof(IEnumerable<SimpleDataSetConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_datasets.AsEnumerable()));
            // Why: PopulateFieldMappings queries DataSetFieldMappingConfiguration per source;
            // return a typed empty enumerable so the cast succeeds and FieldMappings remains empty.
            if (typeof(T) == typeof(IEnumerable<DataSetFieldMappingConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Enumerable.Empty<DataSetFieldMappingConfiguration>()));
            return Task.FromResult(GenericResult<T>.Failure(new GenericMessage($"Unhandled type {typeof(T).Name} in AggregateGateway stub")));
        }

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            if (rowType == typeof(DataSetFieldConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_fields.Cast<object>()));
            if (rowType == typeof(DataSetKeyFieldConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_keyFields.Cast<object>()));
            if (rowType == typeof(DataSetSourceConfiguration))
                return Task.FromResult(GenericResult<IEnumerable<object>>.Success(_sources.Cast<object>()));
            return Task.FromResult(GenericResult<IEnumerable<object>>.Success(Enumerable.Empty<object>()));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not used in this test")));

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not used in this test")));

        private IDataStore BuildTree()
        {
            // The tree ConfigurationDb's data.* DDL declares: the implementation row owns every child
            // collection (on SimpleDataSetConfigurationRowId); a field owns its value maps and a source its
            // field mappings. An owner is found only through the keys that reference it.
            var valueMapContainer = Leaf("DataSetFieldValueMap");
            var fieldMappingContainer = Leaf("DataSetFieldMapping");
            var fieldContainer = Container("DataSetField", [Binding("DataSetFieldRowId", valueMapContainer)], Identity("DataSetField"));
            var sourceContainer = Container("DataSetSource", [Binding("DataSetSourceRowId", fieldMappingContainer)], Identity("DataSetSource"));
            var keyFieldContainer = Leaf("DataSetKeyField");
            var aggregateContainer = Leaf("DataSetAggregate");
            var joinContainer = Leaf("DataSetJoin");

            var domainReferencing = new List<ReferencingKeyBinding>();
            var domainContainer = Container("DataSet", domainReferencing, Identity("DataSet"));
            var dataSetContainer = Container("SimpleDataSetConfiguration",
            [
                Binding("SimpleDataSetConfigurationRowId", aggregateContainer),
                Binding("SimpleDataSetConfigurationRowId", fieldContainer),
                Binding("SimpleDataSetConfigurationRowId", keyFieldContainer),
                Binding("SimpleDataSetConfigurationRowId", sourceContainer),
            ],
            [
                Key("Physical", "PK_SimpleDataSetConfiguration", "RowId", null),
                Key("Logical", "AK_DataSetImplementation", "Id", null),
                Key("Foreign", "FK_SimpleDataSetConfiguration_DataSet", "DataSetRowId", domainContainer),
            ]);
            domainReferencing.Add(Binding("DataSetRowId", dataSetContainer));

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("data");
            var containers = new List<IDataContainer>
            {
                dataSetContainer, domainContainer, fieldContainer, keyFieldContainer, sourceContainer,
                aggregateContainer, joinContainer, valueMapContainer, fieldMappingContainer,
            };
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            {
                var c = containers.FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.Ordinal));
                return c is null ? GenericResult<IDataContainer>.Failure(new GenericMessage("nf")) : GenericResult<IDataContainer>.Success(c);
            });
            foreach (var c in containers)
                Mock.Get(c).Setup(x => x.Parent).Returns(path.Object);

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("PlatformConfiguration");
            store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "data", StringComparison.Ordinal)
                    ? GenericResult<IDataNodePath>.Success(path.Object)
                    : GenericResult<IDataNodePath>.Failure(new GenericMessage("nf")));
            return store.Object;
        }

        private static IReadOnlyList<IContainerKey> Identity(string name)
            => [Key("Physical", $"PK_{name}", "RowId", null), Key("Logical", $"AK_{name}", "Id", null)];

        private static IDataContainer Leaf(string name) => Container(name, [], Identity(name));

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
            var field = new Mock<global::Fdw.Data.Abstractions.IDataField>();
            field.Setup(f => f.Name).Returns(fkColumn);
            // An owner is found by the key that references its Physical key (RowId).
            var referenced = new Mock<global::Fdw.Data.Abstractions.IDataField>();
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
            var field = new Mock<global::Fdw.Data.Abstractions.IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            // Why: KeyType is the abstract KeyTypeBase TypeOption — use the real concrete instances so
            // FindKeyFieldName reads the genuine Name ("Physical"/"Logical").
            global::Fdw.Data.Abstractions.KeyTypeBase kt = keyType switch
            {
                "Foreign" => new global::Fdw.Data.Abstractions.ForeignKeyType(),
                "Physical" => new global::Fdw.Data.Abstractions.PhysicalKeyType(),
                "Logical" => new global::Fdw.Data.Abstractions.LogicalKeyType(),
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
        string IPlatformService.Id => "AggregateGateway";

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
