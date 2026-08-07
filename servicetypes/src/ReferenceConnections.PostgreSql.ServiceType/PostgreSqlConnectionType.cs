using System;
using Fdw.Services.Results;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.PostgreSql.Discovery;
using Fdw.Services.Connections.PostgreSql.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Services.Connections.PostgreSql;

using Fdw.Services.Connections.PostgreSql.Authentication;

using Fdw.Services.Connections.PostgreSql.Results;

using Fdw.Services.Connections.PostgreSql.Commands;

using Fdw.Services.Connections.PostgreSql.Validation;

using ReferenceConnections.PostgreSql.Discovery;

namespace ReferenceConnections.PostgreSql;

/// <summary>
/// Service type definition for PostgreSQL connections.
/// Provides metadata, factory creation, and schema discovery capabilities for PostgreSQL connections.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConnectionTypes), "PostgreSql")]
public sealed class PostgreSqlConnectionType
    : ConnectionTypeBase<IGenericConnection, IPostgreSqlConnectionFactory, PostgreSqlConnectionConfiguration>,
      ISchemaDiscovery<PostgreSqlConnection>,
      ISupportsContainerTypes,
      ISupportsFieldTypes,
      ISupportsWriteModes,
      ISupportsDataPathFormats
{
    private ILogger<PostgreSqlConnectionType>? _logger;
    private IPostgreSqlSchemaDiscoverer? _discoverer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnectionType"/> class.
    /// </summary>
    public PostgreSqlConnectionType() : base(
        name: "PostgreSql",
        sectionName: "PostgreSql",
        displayName: "PostgreSQL",
        description: "PostgreSQL database connection",
        category: "Database",
        defaultContainerName: "PostgreSqlConnection")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var provider = (DefaultConnectionProvider)services.GetRequiredService<IConnectionProvider>();

            var loggerFactory = services.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<PostgreSqlConnectionType>() ?? NullLogger<PostgreSqlConnectionType>.Instance;

            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch. PostgreSqlConnectionConfiguration no longer inherits
            // ConnectionConfiguration — it implements IConnectionConfiguration directly.
            var headerProvider = services.GetRequiredService<ConnectionConfigurationProvider>();
            var configProvider = services.GetRequiredService<PostgreSqlConnectionConfigurationProvider>();
            headerProvider.Register(Name, configProvider);

            PostgreSqlConnectionLog.ConnectionCreated(_logger, "PostgreSqlConnectionType", "provider", 0, "registered");

            // Store IPostgreSqlSchemaDiscoverer directly — avoids circular dependency.
            // IPostgreSqlSchemaDiscoverer (PostgreSqlSchemaDiscoverer) only depends on ILogger — no cycle.
            _discoverer = services.GetService<IPostgreSqlSchemaDiscoverer>();

    
            return host;
        });

        Configuration(builder =>
        {

    
            return builder;
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why: this option registers its factory WITH WHAT THAT FACTORY NEEDS, exactly as
            // HttpConnectionType registers AddHttpClient() for its own factory. PostgreSQL supports
            // password authentication, whose value is read from a named secret manager.
            builder.Services.AddSingleton<IPostgreSqlConnectionFactory>(sp => new PostgreSqlConnectionFactory(
                sp.GetRequiredService<ILogger<PostgreSqlConnectionFactory>>(),
                sp.GetRequiredService<ILogger<PostgreSqlConnection>>(),
                sp.GetRequiredService<ISecretManagerProvider>()));
            builder.Services.TryAddSingleton<PostgreSqlConnectionConfigurationProvider>(sp =>
                new PostgreSqlConnectionConfigurationProvider(
                    sp.GetService<ILogger<PostgreSqlConnectionConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    dataStoreName,
                    pathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<Fdw.Services.Abstractions.IServiceConfigurationProvider<PostgreSqlConnectionConfiguration>>(
                sp => sp.GetRequiredService<PostgreSqlConnectionConfigurationProvider>());
            // Why: RegisterFactory (below) requires ConnectionConfigurationProvider (the shared header
            // provider for the whole Connections domain) to already be registered. TryAddSingleton makes
            // this idempotent — every connection-kind option calls it, harmlessly redundant after the first.
            ConnectionConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Why here: DiscoverSchema() on this type resolves the discoverer, so this type registers it.
            builder.Services.AddSingleton<IPostgreSqlSchemaDiscoverer, PostgreSqlSchemaDiscoverer>();

            return builder;

        });

    }




    // ── SupportedCommands ─────────────────────────────────────────────────────

    /// <summary>
    /// PostgreSQL supports structured reads, compound (pushed-down JOIN) reads, single-row INSERT/UPDATE/DELETE.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("Query"),
        CommandCapabilityTypes.ByName("CompoundQuery"),
        CommandCapabilityTypes.ByName("Insert"),
        CommandCapabilityTypes.ByName("Update"),
    ];

    // ── SupportedDiscoveryTypes ────────────────────────────────────────────────

    /// <summary>
    /// PostgreSQL supports automatic schema discovery via information_schema.
    /// </summary>
    public override IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes =>
        [SchemaDiscoveryTypes.ByName("PostgreSql")];

    // ── ISupportsContainerTypes ────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedContainerTypes =>
        ["Table", "View", "Function"];

    // ── ISupportsFieldTypes ────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<FieldTypeInfo> SupportedFieldTypes =>
    [
        new("text",        "text",            "Text"),
        new("varchar",     "character varying", "Variable-Length Text (varchar)"),
        new("integer",     "integer",         "Integer (32-bit)"),
        new("bigint",      "bigint",          "Integer (64-bit)"),
        new("numeric",     "numeric(18,4)",   "Decimal (numeric)"),
        new("timestamp",   "timestamp",       "Date/Time (timestamp)"),
        new("boolean",     "boolean",         "Boolean"),
        new("uuid",        "uuid",            "GUID (uuid)"),
        new("bytea",       "bytea",           "Binary (bytea)"),
    ];

    // ── ISupportsWriteModes ────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedWriteModes =>
        ["Append", "Overwrite", "Upsert"];

    // ── ISupportsDataPathFormats ───────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedPathFormats =>
        ["{schema}.{table}", "{schema}.{function}"];

    // ── ISchemaDiscovery<PostgreSqlConnection> ──────────────────────────────────

    /// <summary>
    /// Discovers the schema of a PostgreSQL database using the provided typed connection.
    /// </summary>
    public async Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        PostgreSqlConnection connection,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var logger = _logger ?? NullLogger<PostgreSqlConnectionType>.Instance;
        if (_discoverer == null)
            return GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                PostgreSqlSchemaDiscoveryLog.SchemaDiscovererNotRegistered(logger));

        if (connection == null)
            return GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                PostgreSqlSchemaDiscoveryLog.DataStoreNull(logger));

        var pgOptions = new Fdw.Services.Connections.PostgreSql.Discovery.SchemaDiscoveryOptions
        {
            ExcludedSchemas = options.ExcludedSchemas,
            IncludeOnlySchemas = options.IncludeOnlySchemas,
            DiscoverViews = options.DiscoverViews,
            DiscoverIndexes = options.DiscoverIndexes
        };

        var discoverResult = await _discoverer.DiscoverSchema(connection, pgOptions, cancellationToken).ConfigureAwait(false);
        if (!discoverResult.IsSuccess || discoverResult.Value == null)
            return discoverResult.ToNewResult<IReadOnlyList<IStorageContainer>>();

        var containers = new List<IStorageContainer>();
        foreach (var path in discoverResult.Value.Paths)
        {
            foreach (var discovered in path.Containers)
            {
                var fields = new List<IField>();
                foreach (var field in discovered.Fields)
                    fields.Add(PostgreSqlSchemaConversion.MapToField(field));

                var schema = new ContainerSchema { Fields = fields };
                var containerPath = new SimplePath(path.SchemaName, "PostgreSql", path.SchemaName, discovered.Name);
                var containerType = PostgreSqlSchemaConversion.GetContainerType(discovered.ContainerType);

                containers.Add(new DiscoveredStorageContainer(
                    name: discovered.Name,
                    containerType: containerType,
                    format: FormatTypes.Tabular,
                    schema: schema,
                    path: containerPath,
                    schemaName: path.SchemaName,
                    primaryKeyColumns: discovered.PrimaryKeyColumns,
                    indexes: discovered.Indexes));
            }
        }

        PostgreSqlSchemaDiscoveryLog.ContainersRegisteredFromDiscovery(logger, containers.Count, connection.Name);
        return GenericResult<IReadOnlyList<IStorageContainer>>.Success(containers);
    }

    /// <summary>
    /// Non-generic ISchemaDiscovery implementation — casts and delegates to the typed overload.
    /// </summary>
    public override Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        IGenericConnection connection,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var logger = _logger ?? NullLogger<PostgreSqlConnectionType>.Instance;
        if (connection is not PostgreSqlConnection pgConnection)
            return Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                PostgreSqlSchemaDiscoveryLog.ConnectionTypeMismatch(logger)));

        return DiscoverSchema(pgConnection, options, cancellationToken);
    }

}
