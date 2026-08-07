using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.DataSet;

/// <summary>
/// Proves the keystone base read composes the full DataSet aggregate — Fields + KeyFields + Sources —
/// via <see cref="DataSetConfigurationProvider"/>.Get(id). Children load via a JOIN keyed off the owner's
/// metadata Physical (RowId) + Logical (Id) key columns (RowId is DB-managed and invisible to the POCOs),
/// filtered by the owner's durable Id, with the robust descriptor match (container ↔
/// ConfigurationCommand.ContainerName) bridging the DataFieldConfiguration ↔ data.DataSetField name
/// divergence. Also asserts SourceIds derives from Sources.
/// </summary>
public sealed class DataSetAggregateCompositionTests
{
    private static readonly Guid DataSetId = Guid.NewGuid();
    private static readonly Guid Source1Id = Guid.NewGuid();
    private static readonly Guid Source2Id = Guid.NewGuid();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetComposesFieldsKeyFieldsSourcesViaBaseMechanism()
    {
        var gateway = new AggregateGateway();
        var provider = new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
            "ConfigurationDb",
            "data");

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
        private readonly IReadOnlyList<IDataStore> _stores;
        private readonly List<DataSetConfiguration> _datasets;
        private readonly List<DataFieldConfiguration> _fields;
        private readonly List<DataSetKeyFieldConfiguration> _keyFields;
        private readonly List<DataSetSourceConfiguration> _sources;

        public AggregateGateway()
        {
            _datasets = [new DataSetConfiguration { Id = DataSetId, Name = "DS1" }];
            _fields =
            [
                new DataFieldConfiguration { Id = Guid.NewGuid(), Name = "A", Ordinal = 0 },
                new DataFieldConfiguration { Id = Guid.NewGuid(), Name = "B", Ordinal = 1 },
                new DataFieldConfiguration { Id = Guid.NewGuid(), Name = "C", Ordinal = 2 }
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
            if (typeof(T) == typeof(IEnumerable<DataSetConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_datasets.AsEnumerable()));
            // Why: PopulateFieldMappings queries DataSetFieldMappingConfiguration per source;
            // return a typed empty enumerable so the cast succeeds and FieldMappings remains empty.
            if (typeof(T) == typeof(IEnumerable<DataSetFieldMappingConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Enumerable.Empty<DataSetFieldMappingConfiguration>()));
            return Task.FromResult(GenericResult<T>.Failure(new GenericMessage($"Unhandled type {typeof(T).Name} in AggregateGateway stub")));
        }

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            if (rowType == typeof(DataFieldConfiguration))
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
            // Leaf containers (no children) need no keys.
            var fieldContainer = Container("DataSetField", [], null);
            var keyFieldContainer = Container("DataSetKeyField", [], null);
            var sourceContainer = Container("DataSetSource", [], null);
            // DataSet is the owner of every child collection — the new child read resolves its
            // Physical (RowId) + Logical (Id) key columns from metadata to build the JOIN, so it
            // needs BOTH keys present or ResolveOwnerKeyColumns returns null and children don't load.
            var dataSetContainer = Container("DataSet",
            [
                Binding("DataSetRowId", fieldContainer),
                Binding("DataSetRowId", keyFieldContainer),
                Binding("DataSetRowId", sourceContainer)
            ],
            [Key("Physical", "PK_DataSet", "RowId", null), Key("Logical", "AK_DataSet", "Id", null)]);

            var path = new Mock<IDataPath>();
            path.Setup(p => p.Name).Returns("data");
            var containers = new List<IDataContainer> { dataSetContainer, fieldContainer, keyFieldContainer, sourceContainer };
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
            store.Setup(s => s.Paths).Returns(new List<IDataPath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "data", StringComparison.Ordinal)
                    ? GenericResult<IDataPath>.Success(path.Object)
                    : GenericResult<IDataPath>.Failure(new GenericMessage("nf")));
            return store.Object;
        }

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
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
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
    }
}
