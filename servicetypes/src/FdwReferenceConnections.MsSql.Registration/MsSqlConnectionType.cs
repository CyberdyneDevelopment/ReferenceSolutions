using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.Logging;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.MsSql;
using FdwReferenceConnections.MsSql.Discovery;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// Service type definition for Microsoft SQL Server connections.
/// Provides metadata, factory creation, and schema discovery capabilities for SQL Server connections.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "MsSql")]
public sealed class MsSqlConnectionType
    : ConnectionTypeBase<IGenericConnection, IMsSqlConnectionFactory, MsSqlConnectionConfiguration>,
      ISchemaDiscovery<MsSqlConnection>,
      ISupportsCalculationPushdown,
      ISupportsContainerTypes,
      ISupportsExecutableContainerTypes,
      ISupportsFieldTypes,
      ISupportsWriteModes,
      ISupportsDataPathFormats,
      ISupportsAuthenticationMethods
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
        defaultContainerName: "MsSqlConnectionConfiguration")
    {
        // Endpoint behavior: the flat request's Authentication is this type's AdditionalProperties; which
        // of those are secret is the authentication type's to say; and what a connection must name is checked
        // by CheckConfiguration.
        EndpointAlias("authentication", nameof(MsSqlConnectionConfiguration.AdditionalProperties));
        EndpointMask(nameof(MsSqlConnectionConfiguration.AdditionalProperties), (configuration, entries) =>
            MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType).MaskSecrets(entries, ConnectionSecretMask.Value));
        EndpointValidation(CheckConfiguration);

        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            // Why the elevation lives HERE and not in the registration generator: host bootstrap runs
            // with no ClaimsPrincipal and no request scope, so the AsyncLocal-backed accessor has
            // nothing to return — every boot-time read of PlatformConfiguration would resolve to the
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

            // Why the option registers itself here: the domain provider holds one implementation
            // provider per Implementation, and this option is the only thing that knows which one it
            // is. It registers into the domain provider, keyed by its own Name.
            services.GetRequiredService<IConnectionConfigurationProvider>().Register(Name, services.GetRequiredService<IMsSqlConnectionConfigurationProvider>());

            // The configuration provider above tells the domain how to READ this option's rows; this
            // hands the runtime connection provider what it BUILDS a connection with. Without it
            // IConnectionProvider's registry stays empty and every connection of this kind fails with
            // "no factory for service option" -- which is how the credential vault lost AuthDb.
            var implementationRegistered = services.GetRequiredService<IConnectionProvider>()
                .Register(Name, () => services.GetRequiredService<IMsSqlConnectionProvider>());
            if (!implementationRegistered.IsSuccess) return implementationRegistered.ToNewResult<IHost>();

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
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IMsSqlConnectionFactory),
                sp => new MsSqlConnectionFactory(
                sp.GetRequiredService<ILogger<MsSqlConnectionFactory>>(),
                sp.GetRequiredService<ILogger<MsSqlConnection>>(),
                sp.GetRequiredService<ISecretManagerProvider>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>()),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IMsSqlConnectionProvider),
                sp => new MsSqlConnectionProvider(
                    sp.GetRequiredService<IMsSqlConnectionFactory>(),
                    sp.GetRequiredService<ISecretManagerProvider>()),
                ServiceLifetime.Singleton));
            builder.Services.AddSingleton<IMsSqlConnectionConfigurationProvider, MsSqlConnectionConfigurationProvider>(sp => new MsSqlConnectionConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<MsSqlConnectionConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConnectionTypes.ConfigurationConnection, PathName, Container));

            // Why here: DiscoverSchema() on this type resolves the discoverer, so this type registers it.
            builder.Services.AddSingleton<IMsSqlSchemaDiscoverer, MsSqlSchemaDiscoverer>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);

        });

    }



    // Why (no Configure override): connection configuration lives in PlatformConfiguration and is read
    // through the domain provider, so there is nothing to bind from appsettings. The Configure
    // phase still runs — ServiceTypeBase supplies the no-op.

    // ── IConnectionType ────────────────────────────────────────────────────────

    /// <summary>
    /// SQL Server supports structured queries, raw SQL, stored procedure execution,
    /// bulk insert, and streaming reads.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    // Why insert/update/upsert: SQL Server is a full CRUD target; all write patterns are supported.
    // Upsert and BulkUpsert are set-based: the rows travel as one JSON parameter, are staged by OPENJSON into a
    // temp table typed like the target, then an UPDATE (or, versioned, a close) and an INSERT run (FDW-1107).
    // InsertFromQuery is INSERT ... SELECT on one server; SoftDelete flags rows; UnitOfWork runs commands in one transaction.
    // Execute runs an executable container (a stored procedure here); ExecutableContainerTypes says which container types.
    // Vectorized: SQL Server 2025 holds vectors, and MsSqlConnection answers the nearest rows by VECTOR_DISTANCE (FDW-1354).
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("Query"),
        CommandCapabilityTypes.ByName("CompoundQuery"),
        CommandCapabilityTypes.ByName("Execute"),
        CommandCapabilityTypes.ByName("Insert"),
        CommandCapabilityTypes.ByName("Update"),
        CommandCapabilityTypes.ByName("Upsert"),
        CommandCapabilityTypes.ByName("BulkInsert"),
        CommandCapabilityTypes.ByName("BulkUpsert"),
        CommandCapabilityTypes.ByName("InsertFromQuery"),
        CommandCapabilityTypes.ByName("SoftDelete"),
        CommandCapabilityTypes.ByName("UnitOfWork"),
        CommandCapabilityTypes.ByName("Stream"),
        CommandCapabilityTypes.ByName("Vectorized"),
    ];

    // ── SupportedDiscoveryTypes ────────────────────────────────────────────────

    /// <summary>
    /// SQL Server discovers its schema from its catalog (INFORMATION_SCHEMA); a connection of this kind may also
    /// decline with <c>None</c>.
    /// </summary>
    public override IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes =>
        [SchemaDiscoveryTypes.ByName("MsSql"), SchemaDiscoveryTypes.None];

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
    /// is per <i>kind</i>, and the kind is MsSql for PlatformConfiguration, OpsDb and NflDb alike, so all
    /// three keep <c>sp_set_session_context</c> on every pooled open exactly as before.
    /// </para>
    /// </remarks>
    public override IReadOnlyCollection<ISessionContext> SessionContextTypes =>
        MsSqlSessionContextTypes.All();

    // ── ISupportsAuthenticationMethods ─────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>Read from the MsSqlAuthenticationTypes options' own declarations each time, never cached at construction.</remarks>
    public IReadOnlyList<ConnectionAuthenticationMethod> AuthenticationMethods
        => MsSqlAuthenticationTypes.All().Select(method => ConnectionAuthenticationMethod.Of(method)).ToList();

    // ── ISupportsCalculationPushdown ───────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedCalculations =>
        ["Sum", "Average", "Min", "Max", "Count", "Percentile", "RunningTotal", "RowNumber", "Rank"];

    // ── ISupportsContainerTypes ────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedContainerTypes =>
        ["Table", "View", "StoredProcedure", "Function"];

    // ── ISupportsExecutableContainerTypes ──────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<string> ExecutableContainerTypes => ["StoredProcedure"];

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
        new("geography",        "geography",          "Geography (spatial)"),
        new("geometry",         "geometry",           "Geometry (spatial)"),
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
    /// <param name="connection">The open connection to discover through.</param>
    /// <param name="discoveryType">The connection's chosen option; this kind has one strategy, its catalog.</param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        MsSqlConnection connection,
        ISchemaDiscoveryType discoveryType,
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
    /// <param name="connection">The open connection to discover through.</param>
    /// <param name="discoveryType">The connection's chosen option.</param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        IGenericConnection connection,
        ISchemaDiscoveryType discoveryType,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var logger = _logger ?? NullLogger<MsSqlConnectionType>.Instance;
        if (connection is not MsSqlConnection msSql)
            return Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                MsSqlSchemaDiscoveryLog.ConnectionTypeMismatch(logger)));

        return DiscoverSchema(msSql, discoveryType, options, cancellationToken);
    }

    // What an MsSql connection must name before it is saved: where it connects, and every value its
    // authentication type requires. The authentication type's own RequiredProperties say which those are,
    // so a new authentication type needs no rule here. The values naming its stored credentials are the
    // server's to set, after the save stores them (FDW-1086), so they are not asked of the caller here.
    private static IGenericResult CheckConfiguration(MsSqlConnectionConfiguration configuration, ILogger logger)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(configuration.Server))
            problems.Add("server is required");
        if (string.IsNullOrWhiteSpace(configuration.Database))
            problems.Add("database is required");
        if (configuration.Port is < 1 or > 65535)
            problems.Add("port must be between 1 and 65535");

        var authentication = MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            problems.Add($"authentication type '{configuration.AuthenticationType}' is not one of: {string.Join(", ", MsSqlAuthenticationTypes.All().Select(type => type.Name))}");
        }
        else
        {
            problems.AddRange(authentication.RequiredProperties
                .Where(property => !authentication.IsServerSet(property))
                .Where(property => !configuration.AdditionalProperties.TryGetValue(property, out var value) || string.IsNullOrEmpty(value))
                .Select(property => $"{property} is required for {authentication.Name} authentication"));
        }

        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(ConnectionEndpointBehaviorLog.ConfigurationRejected(logger, "MsSql", string.Join("; ", problems)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names, which states its own answer
    /// (<c>ConnectionAuthenticationTypeBase.UsesHostIdentity</c>). A method this kind does not know cannot be
    /// answered for, so it is refused rather than read as "no".
    /// </remarks>
    protected override IGenericResult<bool> DecideHostIdentity(MsSqlConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a MsSql authentication method"));
        }

        return GenericResult<bool>.Success(authentication.UsesHostIdentity(
            new System.Collections.Generic.Dictionary<string, string?>(configuration.AdditionalProperties, System.StringComparer.OrdinalIgnoreCase)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names: its credential fields, read against the connection's
    /// authentication values. Names only; a credential's value is never read here (FDW-1086).
    /// </remarks>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(MsSqlConnectionConfiguration configuration)
    {
        var credentials = CredentialsOf(configuration);
        return credentials.IsFailure
            ? credentials.ToNewResult<ConnectionCredentialSlots>()
            : GenericResult<ConnectionCredentialSlots>.Success(
                ConnectionCredentialSlots.From(credentials.Value.Method, credentials.Value.Credentials, configuration.AdditionalProperties));
    }

    /// <inheritdoc/>
    /// <remarks>Refuses a field the connection's authentication method does not use rather than storing a reference nothing reads.</remarks>
    protected override IGenericResult BindCredentialReferences(
        MsSqlConnectionConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField)
    {
        var credentials = CredentialsOf(configuration);
        if (credentials.IsFailure)
            return credentials;

        var unused = secretKeyByField.Keys
            .Where(field => !credentials.Value.Credentials.Any(credential => string.Equals(credential.Field, field, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (unused.Count > 0)
        {
            return GenericResult.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' uses no credential named {string.Join(", ", unused)}"));
        }

        ConnectionCredentialSlots.Bind(configuration.AdditionalProperties, secretManagerName, secretKeyByField);
        return GenericResult.Success();
    }

    // The authentication method the connection names and its credentials; a connection that names none, or
    // one this kind does not know, cannot say which credentials it uses. The method travels with the credentials so a
    // caller never reads the configuration's AuthenticationType again after this has checked it.
    private static IGenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)> CredentialsOf(MsSqlConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a MsSql authentication method"));
        }

        return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Success((configuration.AuthenticationType, authentication.Credentials));
    }
}
