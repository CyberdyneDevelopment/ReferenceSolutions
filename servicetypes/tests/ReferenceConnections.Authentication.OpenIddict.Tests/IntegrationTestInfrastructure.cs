using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Moq;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// Shared infrastructure for DataGateway integration tests. Provides <see cref="TestContainer"/>
/// and <see cref="DirectDataGateway"/> used by all three store integration test classes.
/// </summary>

// Why: A concrete IDataContainer implementation (not Moq) is needed because:
// - The INSERT translator requires container is IStorageContainer (uses Schema.Fields for column list)
// - The SELECT translator requires container is IDataContainer (uses Fields for column list)
// - Both checks are done via 'is' type tests on the same object; a Moq<IStorageContainer> fails the
//   'is IDataContainer' check, causing "container does not implement IDataContainer" error.
// The class provides both interfaces from one object and returns the correct field lists for each.
internal sealed class TestContainer : IDataContainer
{
    private readonly DatabasePath _path;
    private readonly IContainerSchema _schema;

    internal TestContainer(string schemaName, string tableName, IEnumerable<(string Name, bool IsSystemProvided)> fieldDefs)
    {
        Name = tableName;
        _path = new DatabasePath(null, schemaName, tableName);

        var fieldList = fieldDefs.ToList();

        // Why: a single IField list backs Schema.Fields — read by both the INSERT translator (which
        // filters IsSystemProvided) and the SELECT translator (column projection). The container's
        // own Schema is preserved because GetFields yields nothing (see below).
        var insertFields = fieldList.Select(f =>
        {
            var m = new Mock<IField>();
            m.Setup(ff => ff.Name).Returns(f.Name);
            m.Setup(ff => ff.IsSystemProvided).Returns(f.IsSystemProvided);
            m.Setup(ff => ff.IsIdentity).Returns(false);
            m.Setup(ff => ff.IsComputed).Returns(false);
            m.Setup(ff => ff.IsNullable).Returns(true);
            return m.Object;
        }).ToList<IField>();

        var schemaMock = new Mock<IContainerSchema>();
        schemaMock.Setup(s => s.Fields).Returns(insertFields);
        _schema = schemaMock.Object;
    }

    // ── IDataContainer / IDataNode ───────────────────────────────────────────
    public string Name { get; }
    public string? Description => null;
    // Why: the unified container has no async GetFields; translators read the column list from the
    // synchronous Schema.Fields (built above). The uniform IDataNode child surface (Nodes/Node) is
    // empty here because this test container's schema is supplied directly, not via field child nodes.
    public IReadOnlyList<IDataNode> Nodes => [];
    public IGenericResult<IDataNode> Node(string name)
        => GenericResult<IDataNode>.Failure(new GenericMessage("not a navigable test container"));
    public IReadOnlyList<IContainerKey> Keys => [];
    // Why: Parent (the tree-navigation back-reference) is never read in these direct-gateway tests —
    // DirectDataGateway calls the connection's Execute without tree navigation. Translators use the
    // physical IStorageContainer.Path (DatabasePath) via pattern match. Return null! here safely.
    public IDataPath Parent => null!;
    public IGenericResult<IReadOnlyList<ReferencingKeyBinding>> ReferencingKeys
        => GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Failure(new GenericMessage("not loaded"));

    // ── IStorageContainer ────────────────────────────────────────────────────
    // Why: null! for ContainerType and Format — the MsSql translators and POCO mapper
    // never access these; only Path, Schema.Fields, and IDataNode.Fields are used.
    public IContainerType ContainerType => null!;
    public IFormatType Format => null!;
    public IContainerSchema Schema => _schema;
    // Why: IStorageContainer.Path must be DatabasePath for the MsSql translators'
    // 'container.Path is DatabasePath dbPath' pattern match to succeed.
    IPath IStorageContainer.Path => _path;
    public IReadOnlyDictionary<string, object> Metadata
        => new Dictionary<string, object>(StringComparer.Ordinal);
    public string[] SupportedOperations => ["Query", "Insert", "Update", "Delete"];
}

// Why: Routes IDataGateway.Execute<T> directly to MsSqlConnection.Execute<T>(command, IDataContainer, ct).
// Uses real FDW MsSql command translation, parameter binding, and POCO mapper against the live AuthDb.
internal sealed class DirectDataGateway : IDataGateway
{
    private readonly IDataConnection _connection;
    private readonly Dictionary<string, IDataContainer> _containers;

    internal DirectDataGateway(IDataConnection connection, Dictionary<string, IDataContainer> containers)
    {
        _connection = connection;
        _containers = containers;
    }

    // Why: test double — useCache not exercised in OpenIddict integration tests; delegates to existing implementation.
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
        => Execute<T>(command, target, cancellationToken);

    // Why: test double routes DataStoreTarget through the container dictionary keyed by target.Container.
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (!_containers.TryGetValue(target.Container, out var container))
            throw new InvalidOperationException($"No container registered for '{target.Container}'");
        return _connection.Execute<T>(command, container, cancellationToken);
    }

    // Why: DataSet federation is not exercised in store integration tests.
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("DataSet routing not used in store integration tests.");

    // Why: streaming record-source cursor is not exercised by this test double.
    public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        => throw new System.NotImplementedException();

    public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        string connectionName,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Transactions not used in store integration tests.");
}
