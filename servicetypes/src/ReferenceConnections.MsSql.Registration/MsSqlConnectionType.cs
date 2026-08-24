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
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql.Discovery;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Authentication;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.ErrorHandlers;

using ReferenceConnections.MsSql.Mapping;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Results;

using Fdw.Services.Connections.MsSql.Commands;

using Fdw.Services.Connections.MsSql.Validation;

using ReferenceConnections.MsSql.Discovery;

namespace ReferenceConnections.MsSql;

/// <summary>
/// Service type definition for Microsoft SQL Server connections.
/// Provides metadata, factory creation, and schema discovery capabilities for SQL Server connections.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConnectionTypes), "MsSql")]
public sealed class MsSqlConnectionType
    : ConnectionTypeBase<IGenericConnection, IMsSqlConnectionFactory, MsSqlConnectionConfiguration>,
      ISchemaDiscovery<MsSqlConnection>,
      ISupportsCalculationPushdown,
      ISupportsContainerTypes,
      ISupportsFieldTypes,
      ISupportsWriteModes,
      ISupportsDataPathFormats
{
    private ILogger<MsSqlConnectionType>? _logger;
    private IMsSqlSchemaDiscoverer? _discoverer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionType"/> class.
    /// </summary>
    public MsSqlConnectionType() : base(
        name: "MsSql",
        sectionName: "MsSql",
        displayName: "SQL Server",
        description: "Microsoft SQL Server database connection",
        category: "Database",
        defaultContainerName: "MsSqlConnection")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            // Why the elevation lives HERE and not in the registration generator: host bootstrap runs
            // with no ClaimsPrincipal and no request scope, so the AsyncLocal-backed accessor has
            // nothing to return — every boot-time read of ConfigurationDb would resolve to the
            // deny-everywhere principal and the app could not read its own connection catalog to
            // start. A prior generator version bracketed EVERY domain's emitted Initialize in this
            // scope; that broke every host with no auth-context-consuming connection layer (a
            // FileSystem-only UI host) with "No service for type 'IAuthenticationContextAccessor'",
            // because only this option registers the accessor. It is guarded against by
            // PlatformServicesRegistrationGeneratorTests.NeverWrapsOrElevatesInitializeRegardlessOfAuthAbstractionsVisibility.
            // The sanctioned mechanism is exactly this: the one domain that needs something around
            // its own Initialize does it in its own Initialization override, so hosts that never
            // reference this option are unaffected.
            //
            // Why GetRequiredService: this same option's Registration phase (which runs before Build,
            // and therefore before this) registered the accessor. Its absence is a broken container.
            //
            // Why the scope is disposed at the end of the block: SystemAuthenticationContextScope
            // restores the accessor's PRIOR value, so the elevation cannot outlive initialization and
            // the request pipeline starts with none.
            using var systemScope = new SystemAuthenticationContextScope(
                services.GetRequiredService<IAuthenticationContextAccessor>());

            var loggerFactory = services.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<MsSqlConnectionType>() ?? NullLogger<MsSqlConnectionType>.Instance;

            MsSqlSchemaDiscoveryLog.RegisterFactoryStarting(_logger);

            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch, not with the domain provider (DefaultConnectionProvider) via
            // inheritance-based Register<TDerived>. MsSqlConnectionConfiguration no longer inherits
            // ConnectionConfiguration — it implements IConnectionConfiguration directly.
            // The generic overload wraps via ConfigurationProviderAdapter so the marker-interface dict accepts it.
            var headerProvider = services.GetRequiredService<ConnectionConfigurationProvider>();
            var configProvider = services.GetRequiredService<MsSqlConnectionConfigurationProvider>();
            headerProvider.Register(Name, configProvider);

            MsSqlSchemaDiscoveryLog.ConfigurationProviderRegistered(_logger);

            // Store IMsSqlSchemaDiscoverer directly — avoids circular dependency.
            // IMsSqlSchemaDiscoverer (MsSqlSchemaDiscoverer) only depends on ILogger — no cycle.
            // Why: resolving IDataConnectionProvider here (inside the IConnectionProvider singleton
            // factory) would re-enter the factory → infinite loop.
            _discoverer = services.GetService<IMsSqlSchemaDiscoverer>();

    
            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why Singleton: MsSqlConnectionFactory is itself Singleton (required for the three-phase
            // eager-resolve/cache pattern), so it cannot safely ctor-inject a Scoped IAuthenticationContext
            // (captive dependency). IAuthenticationContextAccessor is AsyncLocal-backed, so it is safe to
            // register Singleton while still reflecting the CURRENT logical call flow's tenant context —
            // see IAuthenticationContextAccessor's remarks. TryAddSingleton keeps this idempotent across
            // every connection-kind option, mirroring ConnectionConfigurationProvider.RegisterDomainConfiguration below.
            builder.Services.TryAddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
            // Why: this option registers its factory WITH WHAT THAT FACTORY NEEDS, exactly as
            // HttpConnectionType registers AddHttpClient() for its own factory. MsSql supports SqlAuth,
            // which declares a secret-bearing property, so the secret-manager provider is a hard requirement — a
            // missing registration must throw at composition, never silently produce a factory that
            // cannot open a password-authenticated connection.
            // Why the ACCESSOR (registered just above) and not IAuthenticationContext: nothing
            // anywhere registers IAuthenticationContext, so GetService<IAuthenticationContext>()
            // returned null on every host, every time — and this factory is Singleton, so even a
            // registered Scoped context would be captured once at composition, before any request
            // exists. Either way every connection computed the deny-everywhere plan. The accessor is
            // AsyncLocal-backed: the connection reads .Current at PLAN TIME, on the flow actually
            // opening the connection. GetRequiredService, not GetService — this option registered
            // the accessor itself two lines up, so its absence is a broken container, not an
            // optional feature.
            builder.Services.AddSingleton<IMsSqlConnectionFactory>(sp => new MsSqlConnectionFactory(
                sp.GetRequiredService<ILogger<MsSqlConnectionFactory>>(),
                sp.GetRequiredService<ILogger<MsSqlConnection>>(),
                sp.GetRequiredService<ISecretManagerProvider>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>()));
            builder.Services.TryAddSingleton<MsSqlConnectionConfigurationProvider>(sp =>
                new MsSqlConnectionConfigurationProvider(
                    sp.GetService<ILogger<MsSqlConnectionConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore,
                    PathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<MsSqlConnectionConfiguration>>(
                sp => sp.GetRequiredService<MsSqlConnectionConfigurationProvider>());

            // Why here: DiscoverSchema() on this type resolves the discoverer, so this type registers it.
            builder.Services.AddSingleton<IMsSqlSchemaDiscoverer, MsSqlSchemaDiscoverer>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);

        });

    }



    // Why (no Configure override): connection configuration lives in ConfigurationDb and is read
    // through the domain provider, so there is nothing to bind from appsettings. The Configure
    // phase still runs — ServiceTypeBase supplies the no-op.

    // ── IConnectionType ────────────────────────────────────────────────────────

    /// <summary>
    /// SQL Server supports structured queries, raw SQL, stored procedure execution,
    /// bulk insert, and streaming reads.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    // Why insert/update/upsert: SQL Server is a full CRUD target; all write patterns are supported.
    // BulkUpsert uses SqlBulkCopy + MERGE — the same path as BulkInsert but with key matching.
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("Query"),
        CommandCapabilityTypes.ByName("CompoundQuery"),
        CommandCapabilityTypes.ByName("RawQuery"),
        CommandCapabilityTypes.ByName("Execute"),
        CommandCapabilityTypes.ByName("Insert"),
        CommandCapabilityTypes.ByName("Update"),
        CommandCapabilityTypes.ByName("Upsert"),
        CommandCapabilityTypes.ByName("BulkInsert"),
        CommandCapabilityTypes.ByName("BulkUpsert"),
        CommandCapabilityTypes.ByName("Stream"),
    ];

    // ── SupportedDiscoveryTypes ────────────────────────────────────────────────

    /// <summary>
    /// SQL Server supports automatic schema discovery via INFORMATION_SCHEMA.
    /// </summary>
    public override IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes =>
        [SchemaDiscoveryTypes.ByName("MsSql")];

    // ── SessionContextTypes ────────────────────────────────────────────────────

    /// <summary>
    /// This connection type participates in the <b>reference</b> row-level-security scheme, whose
    /// session contexts are <see cref="MsSqlSessionContextTypes"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Overriding the base default (<c>NoSessionContextTypes</c>) is what declares participation.
    /// The collection named here is the whole contract: its members are clause-level contracts with
    /// <c>security.fn_TenantFilter</c> as deployed, not neutral primitives. A consumer running a
    /// different scheme — different predicate, different key names, a tenancy model that is not
    /// tenant+org, or one that does not use <c>SESSION_CONTEXT</c> at all — points this at their own
    /// collection rather than adding a member to this one.
    /// </para>
    /// <para>
    /// A declared capability surface, at the same status as <see cref="SupportedDiscoveryTypes"/>:
    /// it feeds validation, logging and the option picker. It is not the runtime selection —
    /// <c>MsSqlConnection.SelectSessionContext</c> owns that, and buys no round-trips. Participation
    /// is per <i>kind</i>, and the kind is MsSql for ConfigurationDb, OpsDb and NflDb alike, so all
    /// three keep <c>sp_set_session_context</c> on every pooled open exactly as before.
    /// </para>
    /// </remarks>
    public override IReadOnlyCollection<ISessionContext> SessionContextTypes =>
        MsSqlSessionContextTypes.All();

    // ── ISupportsCalculationPushdown ───────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedCalculations =>
        ["Sum", "Average", "Min", "Max", "Count", "Percentile", "RunningTotal", "RowNumber", "Rank"];

    // ── ISupportsContainerTypes ────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedContainerTypes =>
        ["Table", "View", "StoredProcedure", "Function"];

    // ── ISupportsFieldTypes ────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<FieldTypeInfo> SupportedFieldTypes =>
    [
        new("varchar",          "varchar(max)",       "Text (varchar)"),
        new("nvarchar",         "nvarchar(max)",      "Unicode Text (nvarchar)"),
        new("int",              "int",                "Integer (32-bit)"),
        new("bigint",           "bigint",             "Integer (64-bit)"),
        new("decimal",          "decimal(18,4)",      "Decimal"),
        new("datetime2",        "datetime2",          "Date/Time"),
        new("bit",              "bit",                "Boolean (bit)"),
        new("uniqueidentifier", "uniqueidentifier",   "GUID"),
        new("varbinary",        "varbinary(max)",     "Binary"),
    ];

    // ── ISupportsWriteModes ────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedWriteModes =>
        ["Append", "Overwrite", "Upsert", "TruncateInsert"];

    // ── ISupportsDataPathFormats ───────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedPathFormats =>
        ["{schema}.{table}", "{schema}.{storedprocedure}"];

    // ── ISchemaDiscovery<MsSqlConnection> ──────────────────────────────────────

    /// <summary>
    /// Discovers the schema of a SQL Server database using the provided typed connection.
    /// </summary>
    public async Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        MsSqlConnection connection,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var logger = _logger ?? NullLogger<MsSqlConnectionType>.Instance;
        if (_discoverer == null)
            return GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                MsSqlSchemaDiscoveryLog.SchemaDiscovererNotRegistered(logger));

        if (connection == null)
            return GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                MsSqlSchemaDiscoveryLog.DataStoreNull(logger));

        var msSqlOptions = new Fdw.Services.Connections.MsSql.Discovery.SchemaDiscoveryOptions
        {
            ExcludedSchemas = options.ExcludedSchemas,
            IncludeOnlySchemas = options.IncludeOnlySchemas,
            DiscoverViews = options.DiscoverViews,
            DiscoverIndexes = options.DiscoverIndexes
        };

        var discoverResult = await _discoverer.DiscoverSchema(connection, msSqlOptions, cancellationToken).ConfigureAwait(false);
        if (!discoverResult.IsSuccess || discoverResult.Value == null)
            return discoverResult.ToNewResult<IReadOnlyList<IStorageContainer>>();

        var containers = new List<IStorageContainer>();
        foreach (var path in discoverResult.Value.Paths)
        {
            foreach (var discovered in path.Containers)
            {
                var fields = new List<IField>();
                foreach (var field in discovered.Fields)
                    fields.Add(MsSqlSchemaConversion.MapToField(field));

                var schema = new ContainerSchema { Fields = fields };
                var containerPath = new SimplePath(path.SchemaName, "Sql", path.SchemaName, discovered.Name);
                var containerType = MsSqlSchemaConversion.GetContainerType(discovered.ContainerType);

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

        MsSqlSchemaDiscoveryLog.ContainersRegisteredFromDiscovery(logger, containers.Count, connection.Name);
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
        var logger = _logger ?? NullLogger<MsSqlConnectionType>.Instance;
        if (connection is not MsSqlConnection msSql)
            return Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                MsSqlSchemaDiscoveryLog.ConnectionTypeMismatch(logger)));

        return DiscoverSchema(msSql, options, cancellationToken);
    }

}
