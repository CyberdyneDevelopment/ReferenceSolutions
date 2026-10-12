using Fdw.Services.Connections.PostgreSql.Authentication;
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
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Connections.PostgreSql.Discovery;
using Fdw.Services.Connections.PostgreSql.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.PostgreSql;
using FdwReferenceConnections.PostgreSql.Discovery;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// Service type definition for PostgreSQL connections.
/// Provides metadata, factory creation, and schema discovery capabilities for PostgreSQL connections.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "PostgreSql")]
public sealed class PostgreSqlConnectionType
    : ConnectionTypeBase<IGenericConnection, IPostgreSqlConnectionFactory, PostgreSqlConnectionConfiguration>,
      ISchemaDiscovery<PostgreSqlConnection>,
      ISupportsContainerTypes,
      ISupportsFieldTypes,
      ISupportsWriteModes,
      ISupportsDataPathFormats,
      ISupportsAuthenticationMethods
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
        defaultContainerName: "PostgreSqlConnectionConfiguration")
    {
        // Endpoint behavior: the flat request's Server is this type's Host and its Authentication is
        // AdditionalProperties; which of those are secret is the authentication type's to say; and what a
        // connection must name is checked by CheckConfiguration.
        EndpointAlias("server", nameof(PostgreSqlConnectionConfiguration.Host));
        EndpointAlias("authentication", nameof(PostgreSqlConnectionConfiguration.AdditionalProperties));
        EndpointMask(nameof(PostgreSqlConnectionConfiguration.AdditionalProperties), (configuration, entries) =>
            PostgreSqlAuthenticationTypes.ByName(configuration.AuthenticationType).MaskSecrets(entries, ConnectionSecretMask.Value));
        EndpointValidation(CheckConfiguration);

        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var loggerFactory = services.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<PostgreSqlConnectionType>() ?? NullLogger<PostgreSqlConnectionType>.Instance;

            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch. PostgreSqlConnectionConfiguration no longer inherits
            // IConnectionImplementationConfiguration — it implements IConnectionImplementationConfiguration directly.
            services.GetRequiredService<IConnectionConfigurationProvider>().Register(Name, services.GetRequiredService<IPostgreSqlConnectionConfigurationProvider>());

            // The configuration provider above tells the domain how to READ this option's rows; this
            // hands the runtime connection provider what it BUILDS a connection with. Without it
            // IConnectionProvider's registry stays empty and every connection of this kind fails with
            // "no factory for service option" -- which is how the credential vault lost AuthDb.
            var implementationRegistered = services.GetRequiredService<IConnectionProvider>()
                .Register(Name, () => services.GetRequiredService<IPostgreSqlConnectionProvider>());
            if (!implementationRegistered.IsSuccess) return implementationRegistered.ToNewResult<IHost>();

            PostgreSqlConnectionLog.ConnectionCreated(_logger, "PostgreSqlConnectionType", "provider", 0, "registered");

            // Store IPostgreSqlSchemaDiscoverer directly — avoids circular dependency.
            // IPostgreSqlSchemaDiscoverer (PostgreSqlSchemaDiscoverer) only depends on ILogger — no cycle.
            _discoverer = services.GetService<IPostgreSqlSchemaDiscoverer>();

    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Append and not Registration: Registration REPLACES the phase body, and
        // ConnectionTypeBase's constructor has already prepended this option's factory registration
        // onto it. Replacing therefore silently discards the base's contribution — which is exactly
        // how every connection kind stopped being creatable while each option's own wiring kept
        // working and logging success. Appending composes onto what the base put there.
        Registration((builder, loggerFactory) =>
        {

            // Why: this option registers its factory WITH WHAT THAT FACTORY NEEDS, exactly as
            // HttpConnectionType registers AddHttpClient() for its own factory. PostgreSQL supports
            // password authentication, whose value is read from a named secret manager.
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IPostgreSqlConnectionFactory),
                sp => new PostgreSqlConnectionFactory(
                sp.GetRequiredService<ILogger<PostgreSqlConnectionFactory>>(),
                sp.GetRequiredService<ILogger<PostgreSqlConnection>>(),
                sp.GetRequiredService<ISecretManagerProvider>()),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IPostgreSqlConnectionProvider),
                sp => new PostgreSqlConnectionProvider(
                    sp.GetRequiredService<IPostgreSqlConnectionFactory>(),
                    sp.GetRequiredService<ISecretManagerProvider>(),
                    sp.GetService<ILogger<PostgreSqlConnectionProvider>>()),
                ServiceLifetime.Singleton));
            builder.Services.AddSingleton<IPostgreSqlConnectionConfigurationProvider, PostgreSqlConnectionConfigurationProvider>(sp => new PostgreSqlConnectionConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<PostgreSqlConnectionConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConnectionTypes.ConfigurationConnection, PathName, Container));

            // Why here: DiscoverSchema() on this type resolves the discoverer, so this type registers it.
            builder.Services.AddSingleton<IPostgreSqlSchemaDiscoverer, PostgreSqlSchemaDiscoverer>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);

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
    /// PostgreSQL discovers its schema from its catalog (information_schema); a connection of this kind may also
    /// decline with <c>None</c>.
    /// </summary>
    public override IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes =>
        [SchemaDiscoveryTypes.ByName("PostgreSql"), SchemaDiscoveryTypes.None];

    // ── ISupportsAuthenticationMethods ─────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>Read from the PostgreSqlAuthenticationTypes options' own declarations each time, never cached at construction.</remarks>
    public IReadOnlyList<ConnectionAuthenticationMethod> AuthenticationMethods
        => PostgreSqlAuthenticationTypes.All().Select(method => ConnectionAuthenticationMethod.Of(method)).ToList();

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
    /// <param name="connection">The open connection to discover through.</param>
    /// <param name="discoveryType">The connection's chosen option; this kind has one strategy, its catalog.</param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        PostgreSqlConnection connection,
        ISchemaDiscoveryType discoveryType,
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
        var logger = _logger ?? NullLogger<PostgreSqlConnectionType>.Instance;
        if (connection is not PostgreSqlConnection pgConnection)
            return Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                PostgreSqlSchemaDiscoveryLog.ConnectionTypeMismatch(logger)));

        return DiscoverSchema(pgConnection, discoveryType, options, cancellationToken);
    }

    // What a PostgreSql connection must name before it is saved: where it connects, and every value its
    // authentication type requires. The authentication type's own RequiredProperties say which those are,
    // so a new authentication type needs no rule here. The values naming its stored credentials are the
    // server's to set, after the save stores them (FDW-1086), so they are not asked of the caller here.
    private static IGenericResult CheckConfiguration(PostgreSqlConnectionConfiguration configuration, ILogger logger)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(configuration.Host))
            problems.Add("host is required");
        if (string.IsNullOrWhiteSpace(configuration.Database))
            problems.Add("database is required");
        if (configuration.Port is < 1 or > 65535)
            problems.Add("port must be between 1 and 65535");

        var authentication = PostgreSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            problems.Add($"authentication type '{configuration.AuthenticationType}' is not one of: {string.Join(", ", PostgreSqlAuthenticationTypes.All().Select(type => type.Name))}");
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
            : GenericResult.Failure(ConnectionEndpointBehaviorLog.ConfigurationRejected(logger, "PostgreSql", string.Join("; ", problems)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names, which states its own answer
    /// (<c>ConnectionAuthenticationTypeBase.UsesHostIdentity</c>). A method this kind does not know cannot be
    /// answered for, so it is refused rather than read as "no".
    /// </remarks>
    protected override IGenericResult<bool> DecideHostIdentity(PostgreSqlConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = PostgreSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a PostgreSql authentication method"));
        }

        return GenericResult<bool>.Success(authentication.UsesHostIdentity(
            new System.Collections.Generic.Dictionary<string, string?>(configuration.AdditionalProperties, System.StringComparer.OrdinalIgnoreCase)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names: its credential fields, read against the connection's
    /// authentication values. Names only; a credential's value is never read here (FDW-1086).
    /// </remarks>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(PostgreSqlConnectionConfiguration configuration)
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
        PostgreSqlConnectionConfiguration configuration,
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
    private static IGenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)> CredentialsOf(PostgreSqlConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = PostgreSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a PostgreSql authentication method"));
        }

        return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Success((configuration.AuthenticationType, authentication.Credentials));
    }
}
