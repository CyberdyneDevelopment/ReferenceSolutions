using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using IDataField = Fdw.Data.Abstractions.IDataField;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests the three DataSet strategy KINDS — <see cref="SimpleDataSetType"/>,
/// <see cref="CompoundDataSetType"/>, and <see cref="FederatedDataSetType"/> — directly through their
/// <c>Execute&lt;T&gt;(IDataSetExecutionContext, IDataCommand, CancellationToken)</c> entry point.
/// </summary>
/// <remarks>
/// Why test the strategies directly (not via <c>DataGatewayService</c>): routing goes through the static
/// <c>DataSetTypes.ByName()</c> TypeCollection, and registering a strategy into that static collection
/// would leak state across tests. The strategies are stateless module-init singletons; per-execution
/// state flows through a <see cref="DataSetExecutionContext"/> built here from mocked providers. The
/// DataGatewayService routing itself is covered by DataGatewayServiceTests.
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayServiceDataSetTests
{
    private readonly Mock<IDataConnectionProvider> _connectionProviderMock;
    private readonly Mock<IDataStoreProvider> _dataStoreProviderMock;

    public DataGatewayServiceDataSetTests()
    {
        _connectionProviderMock = new Mock<IDataConnectionProvider>();
        _dataStoreProviderMock = new Mock<IDataStoreProvider>();

        // Why: default success container resolution so individual tests only override when testing the
        // failure path. Resolution goes through the dot-walk Get(store, path, container) overload.
        var containerMock = new Mock<IDataContainer>();
        _dataStoreProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(containerMock.Object));
    }

    private DataSetExecutionContext BuildContext(DataSetConfiguration config)
        => new(
            config,
            _connectionProviderMock.Object,
            _dataStoreProviderMock.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance),
            NullLogger.Instance);

    // ===================================================================================
    // Simple strategy
    // ===================================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleReturnsFailureWhenNoSources()
    {
        var dataset = CreateDataSetConfig("EmptyDataSet", []);
        var command = CreateCommand();

        var result = await new SimpleDataSetType().Execute<string>(BuildContext(dataset), command.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleReturnsFailureWhenMoreThanOneSource()
    {
        var dataset = CreateDataSetConfig("TwoSources",
        [
            CreateSourceConfig("S1", "conn1", httpEndpoint: "/a"),
            CreateSourceConfig("S2", "conn2", httpEndpoint: "/b"),
        ]);
        var command = CreateCommand();

        var result = await new SimpleDataSetType().Execute<string>(BuildContext(dataset), command.Object, TestContext.Current.CancellationToken);

        // Why: a Simple dataset is exactly one source — any other count fails loud (no guessing).
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleWithSingleSourceSucceeds()
    {
        var dataset = CreateDataSetConfig("ApiProducts",
            [CreateSourceConfig("RestSource", "conn1", httpEndpoint: "/api/products")]);
        SetupExecuteConnection("conn1", "rest-result");

        var result = await new SimpleDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("rest-result");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleReturnsFailureWhenConnectionNotFound()
    {
        var dataset = CreateDataSetConfig("ConnFail",
            [CreateSourceConfig("RestSource", "missing-conn", httpEndpoint: "/api/data")]);
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>("missing-conn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Failure(new GenericMessage("Connection not found")));

        var result = await new SimpleDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleReturnsFailureWhenDataStoreProviderFails()
    {
        var dataset = CreateDataSetConfig("UnresolvableContainer",
            [CreateSourceConfig("Source1", "conn1", httpEndpoint: "/api/data", dataStoreName: "UnknownStore")]);

        // Why: container resolution fails loud — no fallback to a default container.
        _dataStoreProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("DataStore not found")));
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>("conn1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(new Mock<IDataConnection>().Object));

        var result = await new SimpleDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleRenamesPhysicalKeysToLogicalKeysViaFieldMappings()
    {
        var mappingId1 = Guid.NewGuid();
        var mappingId2 = Guid.NewGuid();
        // Why: FieldMappings is now composed by DataSetConfigurationProvider.Get; set it directly
        // on the source config so the strategy reads it without a resolver.
        var source = CreateSourceConfig("EarthquakeSource", "quake-conn",
            httpEndpoint: "/earthquakes/all_day.geojson", fieldMappingIds: [mappingId1, mappingId2]);
        source.FieldMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Magnitude", "properties.mag" },
            { "EventId", "id" },
        };
        var dataset = CreateDataSetConfig("EarthquakeDataSet", [source]);

        var physicalRows = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase) { { "properties.mag", 3.5 }, { "id", "us7000abc1" }, { "time", 1700000000000L } },
        };
        SetupExecuteRows("quake-conn", physicalRows);

        var result = await new SimpleDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var row = result.Value!.Single();
        row.ShouldContainKey("Magnitude");
        row.ShouldContainKey("EventId");
        row["Magnitude"].ShouldBe(3.5);
        row["EventId"].ShouldBe("us7000abc1");
        // Unmapped physical key passes through unchanged; mapped physical keys must no longer appear.
        row.ShouldContainKey("time");
        row.ShouldNotContainKey("properties.mag");
        row.ShouldNotContainKey("id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SimpleLeavesRowsUnchangedWhenSourceHasNoFieldMappings()
    {
        var dataset = CreateDataSetConfig("NoMappingDataSet",
            [CreateSourceConfig("RawSource", "raw-conn", httpEndpoint: "/api/raw")]);

        var rawRows = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase) { { "mag", 2.1 }, { "place", "California" } },
        };
        SetupExecuteRows("raw-conn", rawRows);

        var result = await new SimpleDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var row = result.Value!.Single();
        row.ShouldContainKey("mag");
        row.ShouldContainKey("place");
        // Why: no FieldMappings on the source → rows pass through unchanged (no rename applied).
    }

    // ===================================================================================
    // Compound strategy (single-store pushed-down join — currently fails loud, never in-memory)
    // ===================================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompoundReturnsFailureWhenNoSources()
    {
        var dataset = CreateDataSetConfig("EmptyCompound", []);

        var result = await new CompoundDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompoundReturnsFailureWhenSourcesSpanStores()
    {
        var dataset = CreateDataSetConfig("CrossStoreCompound",
        [
            CreateSourceConfig("S1", "conn1", containerName: "T1", dataStoreName: "StoreA"),
            CreateSourceConfig("S2", "conn2", containerName: "T2", dataStoreName: "StoreB"),
        ]);

        var result = await new CompoundDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Why: a Compound join is pushed down to a SINGLE store; cross-store sources are a Federated case.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompoundSingleStorePushdownSucceeds()
    {
        var dataset = CreateDataSetConfig("SingleStoreCompound",
        [
            CreateSourceConfig("S1", "conn1", containerName: "T1", dataStoreName: "StoreA"),
            CreateSourceConfig("S2", "conn1", containerName: "T2", dataStoreName: "StoreA"),
        ]);
        SetupExecuteConnection("conn1", "compound-result");

        var result = await new CompoundDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Why: single-store compound pushdown is now implemented — all sources in StoreA so the
        // joined query executes through conn1 and returns the mocked result.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("compound-result");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompoundPushdownExecutesJoinedQueryAgainstSingleConnection()
    {
        // Arrange: two sources in the same store with a join configuration.
        var joins = new List<JoinConfiguration>
        {
            new() { LeftSource = "Customers", RightSource = "Orders", LeftField = "Id", RightField = "CustomerId", JoinType = "Inner" },
        };
        var dataset = CreateDataSetConfig("JoinedCompound",
        [
            CreateSourceConfig("Customers", "conn1", containerName: "Customers", dataStoreName: "StoreA"),
            CreateSourceConfig("Orders", "conn1", containerName: "Orders", dataStoreName: "StoreA"),
        ], joins: joins);

        IDataCommand? capturedCommand = null;
        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, IDataContainer, CancellationToken>((cmd, _, _) => capturedCommand = cmd)
            .ReturnsAsync(GenericResult<string>.Success("joined-result"));
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>("conn1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await new CompoundDataSetType().Execute<string>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Assert: the compound query executed against the single connection, carrying the JOIN.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("joined-result");
        capturedCommand.ShouldNotBeNull();
        // Why: the compound type wraps the command as QueryCommand<T> internally; cast to verify JOIN shape.
        var capturedQuery = capturedCommand.ShouldBeOfType<QueryCommand<string>>();
        capturedQuery.Joins.ShouldNotBeNull();
        capturedQuery.Joins.Count.ShouldBe(1);
        capturedQuery.Joins[0].TargetContainerName.ShouldBe("Orders");
        capturedQuery.Joins[0].JoinType.ShouldBe("Inner");
    }

    // ===================================================================================
    // Federated strategy (cross-store in-memory join over DataRecord)
    // ===================================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedReturnsFailureWhenNoSources()
    {
        var dataset = CreateDataSetConfig("EmptyFed", [], federationStrategy: "Sequential");

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedReturnsFailureWhenNoFederationStrategy()
    {
        var dataset = CreateDataSetConfig("NoStrategyFed",
            [CreateSourceConfig("S1", "conn1", httpEndpoint: "/a", dataStoreName: "StoreA")],
            federationStrategy: null);

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Why: the federation strategy is authored, never defaulted — fail loud when missing.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedReturnsFailureWhenFederationStrategyUnknown()
    {
        var dataset = CreateDataSetConfig("BadStrategyFed",
            [CreateSourceConfig("S1", "conn1", httpEndpoint: "/a", dataStoreName: "StoreA")],
            federationStrategy: "NotARealStrategy");

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedFailsLoudWhenSourceConnectionIsNotRecordCapable()
    {
        var dataset = CreateDataSetConfig("NonRecordFed",
            [CreateSourceConfig("S1", "plain-conn", httpEndpoint: "/a", dataStoreName: "StoreA")],
            federationStrategy: "Sequential");

        // A plain IDataConnection that does NOT implement IRecordSourceConnection.
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>("plain-conn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(new Mock<IDataConnection>().Object));

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Why: a federated in-memory join pulls each source as DataRecord through IRecordSourceConnection;
        // a connection that does not advertise it fails loud (NO FALLBACKS), never a materializing path.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedConcatenatesRecordsWithNoJoins()
    {
        var dataset = CreateDataSetConfig("NoJoinFed",
        [
            CreateSourceConfig("Source1", "conn1", httpEndpoint: "/api/t1", dataStoreName: "RS1"),
            CreateSourceConfig("Source2", "conn2", httpEndpoint: "/api/t2", dataStoreName: "RS2"),
        ], federationStrategy: "Sequential");

        var (schema1, rows1) = BuildRecords(["Name"], ["Alice"]);
        var (schema2, rows2) = BuildRecords(["Name"], ["Bob"]);
        SetupRecordConnection("conn1", schema1, rows1);
        SetupRecordConnection("conn2", schema2, rows2);

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Why: with no join graph the federated result is the concatenation of every source's records.
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedPerformsInnerJoinOverDataRecord()
    {
        var joins = new List<JoinConfiguration>
        {
            new() { LeftSource = "Customers", RightSource = "Orders", LeftField = "Id", RightField = "CustomerId", JoinType = "Inner" },
        };
        var dataset = CreateDataSetConfig("InnerJoinDS",
        [
            CreateSourceConfig("Customers", "conn1", httpEndpoint: "/api/customers", dataStoreName: "CustStore"),
            CreateSourceConfig("Orders", "conn2", httpEndpoint: "/api/orders", dataStoreName: "OrdStore"),
        ], federationStrategy: "Sequential", joins: joins);

        var (custSchema, customers) = BuildRecords(["Id", "Name"], [1, "Alice"], [2, "Bob"], [3, "Charlie"]);
        var (ordSchema, orders) = BuildRecords(["CustomerId", "Amount"], [1, 100m], [2, 200m]);
        SetupRecordConnection("conn1", custSchema, customers);
        SetupRecordConnection("conn2", ordSchema, orders);

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Inner join: only customers 1 and 2 have matching orders → 2 merged rows.
        result.IsSuccess.ShouldBeTrue();
        var merged = result.Value!.ToList();
        merged.Count.ShouldBe(2);
        merged[0]["Name"].ShouldBe("Alice");
        merged[0]["Amount"].ShouldBe(100m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedPerformsLeftJoinIncludingUnmatchedLeftRows()
    {
        var joins = new List<JoinConfiguration>
        {
            new() { LeftSource = "Customers", RightSource = "Orders", LeftField = "Id", RightField = "CustomerId", JoinType = "Left" },
        };
        var dataset = CreateDataSetConfig("LeftJoinDS",
        [
            CreateSourceConfig("Customers", "conn1", httpEndpoint: "/api/customers", dataStoreName: "CustStore2"),
            CreateSourceConfig("Orders", "conn2", httpEndpoint: "/api/orders", dataStoreName: "OrdStore2"),
        ], federationStrategy: "Sequential", joins: joins);

        var (custSchema, customers) = BuildRecords(["Id", "Name"], [1, "Alice"], [2, "Bob"]);
        var (ordSchema, orders) = BuildRecords(["CustomerId", "Amount"], [1, 100m]);
        SetupRecordConnection("conn1", custSchema, customers);
        SetupRecordConnection("conn2", ordSchema, orders);

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Left join: Bob (no order) is still included → 2 rows, Bob's Amount is null.
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedReturnsFailureWhenJoinSourceNotFound()
    {
        var joins = new List<JoinConfiguration>
        {
            new() { LeftSource = "NonExistent", RightSource = "Orders", LeftField = "Id", RightField = "CustomerId", JoinType = "Inner" },
        };
        var dataset = CreateDataSetConfig("BadJoin",
        [
            CreateSourceConfig("Customers", "conn1", httpEndpoint: "/api/customers", dataStoreName: "BS1"),
            CreateSourceConfig("Orders", "conn2", httpEndpoint: "/api/orders", dataStoreName: "BS2"),
        ], federationStrategy: "Sequential", joins: joins);

        var (custSchema, customers) = BuildRecords(["Id"], [1]);
        var (ordSchema, orders) = BuildRecords(["CustomerId"], [1]);
        SetupRecordConnection("conn1", custSchema, customers);
        SetupRecordConnection("conn2", ordSchema, orders);

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        // Join references "NonExistent" which is not a materialized source → fail loud.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FederatedReturnsFailureWhenSourceConnectionNotFound()
    {
        var dataset = CreateDataSetConfig("FedConnFail",
        [
            CreateSourceConfig("S1", "conn1", httpEndpoint: "/a", dataStoreName: "F1"),
            CreateSourceConfig("S2", "missing", httpEndpoint: "/b", dataStoreName: "F2"),
        ], federationStrategy: "Sequential");

        var (schema1, rows1) = BuildRecords(["Name"], ["Alice"]);
        SetupRecordConnection("conn1", schema1, rows1);
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Failure(new GenericMessage("Connection timeout")));

        var result = await new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(BuildContext(dataset), CreateCommand().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ===================================================================================
    // Helpers
    // ===================================================================================

    private static Mock<IDataCommand> CreateCommand()
    {
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        return commandMock;
    }

    private static DataSetConfiguration CreateDataSetConfig(
        string name,
        IList<DataSetSourceConfiguration> sources,
        string? federationStrategy = null,
        IList<JoinConfiguration>? joins = null,
        IList<DataFieldConfiguration>? fields = null)
    {
        return new DataSetConfiguration
        {
            Name = name,
            Sources = sources,
            Fields = fields ?? new List<DataFieldConfiguration>(),
            Joins = joins ?? new List<JoinConfiguration>(),
            FederationStrategy = federationStrategy ?? string.Empty,
        };
    }

    private static DataSetSourceConfiguration CreateSourceConfig(
        string sourceName,
        string connectionName,
        string? containerName = null,
        string? httpEndpoint = null,
        string? dataStoreName = null,
        IList<Guid>? fieldMappingIds = null)
    {
        return new DataSetSourceConfiguration
        {
            SourceName = sourceName,
            ConnectionName = connectionName,
            DataStoreName = dataStoreName ?? sourceName + "Store",
            ContainerName = containerName ?? string.Empty,
            HttpEndpoint = httpEndpoint,
            SupportsPredicatePushdown = true,
            FieldMappingIds = fieldMappingIds ?? new List<Guid>(),
        };
    }

    private void SetupExecuteConnection(string connectionName, string value)
    {
        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success(value));
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>(connectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));
    }

    private void SetupExecuteRows(string connectionName, IEnumerable<Dictionary<string, object?>> rows)
    {
        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>(connectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));
    }

    private void SetupRecordConnection(string connectionName, RecordSchema schema, IReadOnlyList<DataRecord> records)
    {
        var connectionMock = new Mock<IDataConnection>();
        connectionMock.As<IRecordSourceConnection>()
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(new FakeRecordSource(schema, records)));
        _connectionProviderMock
            .Setup(p => p.Get<IDataConnection>(connectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));
    }

    private static (RecordSchema Schema, List<DataRecord> Records) BuildRecords(string[] fieldNames, params object?[][] rows)
    {
        var fields = fieldNames.Select(name =>
        {
            var fieldMock = new Mock<IDataField>();
            fieldMock.Setup(f => f.Name).Returns(name);
            return fieldMock.Object;
        }).ToList();
        var schema = new RecordSchema(fields);
        var records = rows.Select(values => new DataRecord(schema, values)).ToList();
        return (schema, records);
    }

    // Why: an in-memory IRecordSource<DataRecord> for the federated join tests — yields the supplied
    // records over the shared schema flyweight, exactly as a real cursor would.
    private sealed class FakeRecordSource : IRecordSource<DataRecord>
    {
        private readonly IReadOnlyList<DataRecord> _records;

        public FakeRecordSource(RecordSchema schema, IReadOnlyList<DataRecord> records)
        {
            Schema = schema;
            _records = records;
        }

        public RecordSchema Schema { get; }

        public IEnumerable<IGenericResult<DataRecord>> Read()
            => _records.Select(GenericResult<DataRecord>.Success);

        public async IAsyncEnumerable<IGenericResult<DataRecord>> Read(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var record in _records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return GenericResult<DataRecord>.Success(record);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
