using Fdw.Services.Authentication.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Messages;
using ReferenceAuthentication.OpenIddict.Storage;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// Real-DB round-trip gate proof for OpenIddictScopeStore.
/// Uses the REAL FDW MsSql DataGateway against AuthDb on the configured MSSQL_TEST_SERVER (no mocks on the DB path).
/// Skipped automatically when MSSQL_SA_PASSWORD is not set.
/// </summary>
public sealed class OpenIddictScopeStoreIntegrationTests
{
    // Why: the target server is deployment-specific and this file is published publicly, so it
    // comes from the environment beside the credential rather than being committed. It gates the
    // same skip as the password: an unset target means these integration tests cannot mean
    // anything, so they skip rather than build a half-formed connection string and fail obscurely.
    private static readonly string? ServerAddress = Environment.GetEnvironmentVariable("MSSQL_TEST_SERVER");
    private const string DatabaseName = "AuthDb";
    private const string LoginName = "sa";
    private const string EnvVarName = "MSSQL_SA_PASSWORD";

    // Why: A concrete IDataContainer implementation (not Moq) is needed because:
    // - The INSERT translator requires container is IStorageContainer (uses Schema.Fields for column list)
    // - The SELECT translator requires container is IDataContainer (uses Fields for column list)
    // - Both checks are done via 'is' type tests on the same object; a Moq<IStorageContainer> fails the
    //   'is IDataContainer' check, causing "container does not implement IDataContainer" error.
    // The class provides both interfaces from one object and returns the correct field lists for each.
    private sealed class TestContainer : IDataContainer
    {
        private readonly DatabasePath _path;
        private readonly IContainerSchema _schema;

        internal TestContainer(string schemaName, string tableName, IEnumerable<(string Name, bool IsSystemProvided)> fieldDefs)
        {
            Name = tableName;
            _path = new DatabasePath(null, schemaName, tableName);

            var fieldList = fieldDefs.ToList();

            // Why: a single IField list backs Schema.Fields — read by both the INSERT translator
            // (which filters IsSystemProvided) and the SELECT translator (column projection).
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
        // empty here because this test container's schema is supplied directly, not via field children.
        public IReadOnlyList<IDataNode> Nodes => [];
        public IGenericResult<IDataNode> Node(string name)
            => GenericResult<IDataNode>.Failure(new GenericMessage("not a navigable test container"));
        public IReadOnlyList<IContainerKey> Keys => [];
        // Why: Parent (the tree-navigation back-reference) is never read in these direct-gateway tests.
        // Translators use the physical IStorageContainer.Path (DatabasePath) via pattern match.
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
    private sealed class DirectDataGateway : IDataGateway
    {
        private readonly IDataConnection _connection;
        private readonly Dictionary<string, IDataContainer> _containers;

        internal DirectDataGateway(IDataConnection connection, Dictionary<string, IDataContainer> containers)
        {
            _connection = connection;
            _containers = containers;
        }

        // Why: test double — useCache not exercised in scope store integration tests; delegates to existing implementation.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        // Why: test double routes DataStoreTarget through the container dictionary keyed by target.Container.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (!_containers.TryGetValue(target.Container, out var container))
                throw new InvalidOperationException($"No container registered for '{target.Container}'");
            return _connection.Execute<T>(command, container, cancellationToken);
        }

        // Why: DataSet federation is not exercised in scope store integration tests.
        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("DataSet routing not used in scope store integration tests.");

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
            string connectionName,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions not used in scope store integration tests.");
    }

    private static string? GetPassword()
    {
        if (string.IsNullOrEmpty(ServerAddress))
            return null;

        var value = Environment.GetEnvironmentVariable(EnvVarName);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static (OpenIddictScopeStore store, MsSqlConnection connection, DirectDataGateway gateway) BuildStore(string password)
    {
        var connectionString = BuildConnectionString(password);
        var msSqlConnection = new MsSqlConnection(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString);

        var scopeContainer = new TestContainer("auth", "OpenIddictScope", new[]
        {
            ("RowId",       true),   // NEWSEQUENTIALID() — skip on INSERT
            ("Id",          false),
            ("Name",        false),
            ("DisplayName", false),
            ("Description", false),
            ("IsCurrent",   false),
            ("IsDeleted",   false),
            ("CreatedAt",   false),
            ("ModifiedAt",  false)
        });

        var scopeResourceContainer = new TestContainer("auth", "OpenIddictScopeResource", new[]
        {
            ("RowId",    true),     // NEWSEQUENTIALID() — skip on INSERT
            ("ScopeId",  false),
            ("Resource", false),
            ("IsCurrent", false),
            ("IsDeleted", false),
            ("CreatedAt", false)
        });

        var containers = new Dictionary<string, IDataContainer>(StringComparer.Ordinal)
        {
            ["OpenIddictScope"] = scopeContainer,
            ["OpenIddictScopeResource"] = scopeResourceContainer
        };

        var gateway = new DirectDataGateway(msSqlConnection, containers);
        var store = new OpenIddictScopeStore(
            new Lazy<IDataGateway>(() => gateway),
            new AuthenticationContextAccessor(),
            NullLogger<OpenIddictScopeStore>.Instance);

        return (store, msSqlConnection, gateway);
    }

    private static string BuildConnectionString(string password)
        => $"Server={ServerAddress},1433;Database={DatabaseName};User Id={LoginName};Password={password};TrustServerCertificate=True;Encrypt=True";

    /// <summary>
    /// Gate proof: Create scope + 2 resources → GetResourcesAsync reads them back →
    /// SetResourcesAsync + UpdateAsync supersedes old set, inserts new set →
    /// verify via DataGateway query that old rows IsCurrent=0 and new rows IsCurrent=1.
    /// SQL: SELECT IsCurrent, COUNT(*) FROM auth.OpenIddictScopeResource WHERE ScopeId=… GROUP BY IsCurrent
    /// Isolation: try/finally hard-DELETEs ALL scope + resource rows (all versions) via
    /// DataGateway DeleteCommand so AuthDb is left exactly as it was found.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Integration")]
    public async Task CreateScope_GetResources_SetResources_VerifiesSupersession()
    {
        var password = GetPassword();
        if (password is null)
        {
            // Skip: MSSQL_SA_PASSWORD not set.
            return;
        }

        var ct = TestContext.Current.CancellationToken;
        var (store, connection, gateway) = BuildStore(password);
        using var _ = connection;

        // Unique name to avoid collisions with parallel test runs.
        var scopeName = $"test.scope.{Guid.NewGuid():N}";
        var scopeId = Guid.Empty;

        try
        {
            // ── Step 1: Create scope with 2 resources ─────────────────────────────────
            var scope = await store.InstantiateAsync(ct);
            await store.SetNameAsync(scope, scopeName, ct);
            await store.SetDescriptionAsync(scope, "Integration test scope", ct);
            await store.SetResourcesAsync(scope, ImmutableArray.Create("https://resource1.test", "https://resource2.test"), ct);

            await store.CreateAsync(scope, ct);

            scope.Id.ShouldNotBe(Guid.Empty);
            scopeId = scope.Id;

            // ── Step 2: GetResourcesAsync reads back the 2 resources ──────────────────
            var resources = await store.GetResourcesAsync(scope, ct);
            resources.Length.ShouldBe(2);
            resources.ShouldContain("https://resource1.test");
            resources.ShouldContain("https://resource2.test");

            // ── Step 3: SetResourcesAsync + UpdateAsync supersedes old set ─────────────
            await store.SetResourcesAsync(scope, ImmutableArray.Create("https://resource3.test"), ct);
            await store.UpdateAsync(scope, ct);

            // ── Step 4: GetResourcesAsync reflects the new single resource ─────────────
            var resourcesAfterUpdate = await store.GetResourcesAsync(scope, ct);
            resourcesAfterUpdate.Length.ShouldBe(1);
            resourcesAfterUpdate.ShouldContain("https://resource3.test");

            // ── Step 5: SQL gate proof — verify IsCurrent supersession ────────────────
            // Query ALL rows (no IsCurrent filter) for this ScopeId.
            // Expected: 2 superseded rows (IsCurrent=false) + 1 current row (IsCurrent=true).
            var allRows = await QueryAllResourceRows(scope.Id, gateway, ct);

            var currentRows = allRows.Where(r => r.IsCurrent).ToList();
            var supersededRows = allRows.Where(r => !r.IsCurrent).ToList();

            // THE GATE CHECK: SELECT IsCurrent, COUNT(*) … GROUP BY IsCurrent
            currentRows.Count.ShouldBe(1,
                $"IsCurrent=1 should be 1; got {currentRows.Count}. All: [{string.Join(", ", allRows.Select(r => $"IsCurrent={r.IsCurrent} Resource={r.Resource}"))}]");
            supersededRows.Count.ShouldBe(2,
                $"IsCurrent=0 should be 2 (original 2 resources superseded); got {supersededRows.Count}");

            supersededRows.Select(r => r.Resource).ShouldContain("https://resource1.test");
            supersededRows.Select(r => r.Resource).ShouldContain("https://resource2.test");
            currentRows[0].Resource.ShouldBe("https://resource3.test");
        }
        finally
        {
            // Why: Hard-DELETE (not soft-delete) all version rows for this scope so the live
            // AuthDb is returned to exactly the state it was in before the test. DeleteAsync
            // only soft-deletes (IsCurrent=0, IsDeleted=1); version rows persist and accumulate
            // across runs. The finally block runs whether the test passes, fails, or throws.
            if (scopeId != Guid.Empty)
            {
                await HardDeleteScopeRows(scopeId, gateway, ct);
            }
        }
    }

    // Queries ALL resource rows (IsCurrent=0 and IsCurrent=1) for a ScopeId to verify supersession.
    private static async Task<IReadOnlyList<OpenIddictScopeResourceRecord>> QueryAllResourceRows(
        Guid scopeId,
        DirectDataGateway gateway,
        CancellationToken ct)
    {
        // Query without IsCurrent filter to get all version rows.
        var command = DataQuery.From<OpenIddictScopeResourceRecord>("AuthDb", "auth", "OpenIddictScopeResource")
            .Where(r => r.ScopeId).Equal(scopeId)
            .Build();

        var result = await gateway.Execute<IEnumerable<OpenIddictScopeResourceRecord>>(command.Command, command.Target, ct)
            .ConfigureAwait(false);

        result.IsSuccess.ShouldBeTrue("verification query should succeed");
        return (result.Value ?? Enumerable.Empty<OpenIddictScopeResourceRecord>()).ToList();
    }

    // Why: Hard-DELETEs ALL version rows for the test scope (not a soft-delete) so the live
    // AuthDb is left exactly as it was found. Resources must be deleted before scope rows
    // to satisfy the logical parent-child ordering (no hard FK, but keeps data coherent).
    private static async Task HardDeleteScopeRows(
        Guid scopeId,
        DirectDataGateway gateway,
        CancellationToken ct)
    {
        // DELETE child resource rows first (all versions for this scope's logical Id).
        var deleteResourcesCmd = CmdBuilders.Delete.From("OpenIddictScopeResource")
            .DataStore("AuthDb")
            .Path("auth")
            .Where("ScopeId", scopeId)
            .Build();

        await gateway.Execute<int>(deleteResourcesCmd.Command, deleteResourcesCmd.Target, ct).ConfigureAwait(false);

        // DELETE all parent scope version rows (both IsCurrent and superseded).
        var deleteScopeCmd = CmdBuilders.Delete.From("OpenIddictScope")
            .DataStore("AuthDb")
            .Path("auth")
            .Where("Id", scopeId)
            .Build();

        await gateway.Execute<int>(deleteScopeCmd.Command, deleteScopeCmd.Target, ct).ConfigureAwait(false);
    }
}
