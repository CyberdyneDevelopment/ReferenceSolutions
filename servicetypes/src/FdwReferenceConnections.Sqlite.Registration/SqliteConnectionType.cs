using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Connections.Sqlite.Authentication;
using Fdw.Services.Connections.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Connections.Sqlite;
using Fdw.Services.Connections.Abstractions.Authentication;
using System.Linq;
using System;

namespace FdwReferenceConnections.Sqlite;

/// <summary>
/// Service type definition for SQLite connections.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "Sqlite")]
public sealed class SqliteConnectionType
    : ConnectionTypeBase<IGenericConnection, ISqliteConnectionFactory, SqliteConnectionConfiguration>,
      ISupportsContainerTypes,
      ISupportsFieldTypes,
      ISupportsWriteModes,
      ISupportsAuthenticationMethods
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionType"/> class.
    /// </summary>
    public SqliteConnectionType() : base(
        name: "Sqlite",
        sectionName: "Sqlite",
        displayName: "SQLite",
        description: "SQLite embedded database connection",
        category: "Database",
        defaultContainerName: "SqliteConnectionConfiguration")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IConnectionConfigurationProvider>().Register(Name, services.GetRequiredService<ISqliteConnectionConfigurationProvider>());

            // The configuration provider above tells the domain how to READ this option's rows; this
            // hands the runtime connection provider what it BUILDS a connection with. Without it
            // IConnectionProvider's registry stays empty and every connection of this kind fails with
            // "no factory for service option" -- which is how the credential vault lost AuthDb.
            var implementationRegistered = services.GetRequiredService<IConnectionProvider>()
                .Register(Name, () => services.GetRequiredService<ISqliteConnectionProvider>());
            if (!implementationRegistered.IsSuccess) return implementationRegistered.ToNewResult<IHost>();

            // without re-resolving from DI each call.
    
            return GenericResult<IHost>.Success(host);
        });

        // Why Append and not Registration: Registration REPLACES the phase body, and
        // ConnectionTypeBase's constructor has already prepended this option's factory registration
        // onto it. Replacing therefore silently discards the base's contribution — which is exactly
        // how every connection kind stopped being creatable while each option's own wiring kept
        // working and logging success. Appending composes onto what the base put there.
        Registration((builder, loggerFactory) =>
        {

            // Why: this option registers its factory WITH WHAT THAT FACTORY NEEDS, exactly as
            // HttpConnectionType registers AddHttpClient() for its own factory. SQLite supports
            // EncryptionKey authentication, which resolves its key from a named secret manager.
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISqliteConnectionFactory),
                sp => new SqliteConnectionFactory(
                sp.GetRequiredService<ILogger<SqliteConnectionFactory>>(),
                sp.GetRequiredService<ILogger<SqliteConnection>>(),
                sp.GetRequiredService<ISecretManagerProvider>()),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISqliteConnectionProvider),
                sp => new SqliteConnectionProvider(
                    sp.GetRequiredService<ISqliteConnectionFactory>(),
                    sp.GetRequiredService<ISecretManagerProvider>(),
                    sp.GetService<ILogger<SqliteConnectionProvider>>()),
                ServiceLifetime.Singleton));
            builder.Services.AddSingleton<ISqliteConnectionConfigurationProvider, SqliteConnectionConfigurationProvider>(sp => new SqliteConnectionConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SqliteConnectionConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConnectionTypes.ConfigurationConnection, PathName, Container));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

    // Why (no Configure override): connection configuration lives in PlatformConfiguration and is read
    // through the domain provider, so there is nothing to bind from appsettings. The Configure
    // phase still runs — ServiceTypeBase supplies the no-op.



    // ── IConnectionType — metadata ─────────────────────────────────────────────

    /// <summary>
    /// SQLite supports structured reads, single-row INSERT/UPDATE/DELETE, batched INSERT,
    /// filtered search, and multi-table JOINs. No bulk copy (no SqlBulkCopy equivalent).
    /// </summary>
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("Query"),
        CommandCapabilityTypes.ByName("Insert"),
        CommandCapabilityTypes.ByName("Update"),
        CommandCapabilityTypes.ByName("Delete"),
        CommandCapabilityTypes.ByName("Find"),
        CommandCapabilityTypes.ByName("CompoundQuery"),
        CommandCapabilityTypes.ByName("BatchInsert"),
    ];

    // ── ISupportsAuthenticationMethods ─────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>Read from the SqliteAuthenticationTypes options' own declarations each time, never cached at construction.</remarks>
    public IReadOnlyList<ConnectionAuthenticationMethod> AuthenticationMethods
        => SqliteAuthenticationTypes.All().Select(method => ConnectionAuthenticationMethod.Of(method)).ToList();

    // ── ISupportsContainerTypes ────────────────────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedContainerTypes => ["Table", "View"];

    // ── ISupportsFieldTypes ────────────────────────────────────────────────────

    /// <inheritdoc/>
    // Why: SQLite uses dynamic typing; these are the SQLite affinity names plus common aliases.
    public IReadOnlyList<FieldTypeInfo> SupportedFieldTypes =>
    [
        new("TEXT",    "TEXT",            "Text"),
        new("INTEGER", "INTEGER",         "Integer"),
        new("REAL",    "REAL",            "Real (floating point)"),
        new("NUMERIC", "NUMERIC",         "Numeric (decimal)"),
        new("BLOB",    "BLOB",            "Binary"),
    ];

    // ── ISupportsWriteModes ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedWriteModes => ["Append", "TruncateInsert"];

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names, which states its own answer
    /// (<c>ConnectionAuthenticationTypeBase.UsesHostIdentity</c>). A method this kind does not know cannot be
    /// answered for, so it is refused rather than read as "no".
    /// </remarks>
    protected override IGenericResult<bool> DecideHostIdentity(SqliteConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = SqliteAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<bool>.Failure(
                ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a Sqlite authentication method"));
        }

        return GenericResult<bool>.Success(authentication.UsesHostIdentity(
            new System.Collections.Generic.Dictionary<string, string?>(configuration.AdditionalProperties, System.StringComparer.OrdinalIgnoreCase)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Asked of the authentication method the connection names: its credential fields, read against the connection's
    /// authentication values. Names only; a credential's value is never read here (FDW-1086).
    /// </remarks>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(SqliteConnectionConfiguration configuration)
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
        SqliteConnectionConfiguration configuration,
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
    private static IGenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)> CredentialsOf(SqliteConnectionConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.AuthenticationType))
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var authentication = SqliteAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (authentication.IsEmpty)
        {
            return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create(
                    "name", configuration.Name,
                    "reason", $"authentication method '{configuration.AuthenticationType}' is not a Sqlite authentication method"));
        }

        return GenericResult<(string Method, IReadOnlyList<ConnectionCredentialField> Credentials)>.Success((configuration.AuthenticationType, authentication.Credentials));
    }
}
