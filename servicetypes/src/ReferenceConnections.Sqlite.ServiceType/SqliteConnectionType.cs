using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.Sqlite;

using Fdw.Services.Connections.Sqlite.Authentication;

using Fdw.Services.Connections.Sqlite.Commands;

using Fdw.Services.Connections.Sqlite.Validation;

namespace ReferenceConnections.Sqlite;

/// <summary>
/// Service type definition for SQLite connections.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConnectionTypes), "Sqlite")]
public sealed class SqliteConnectionType
    : ConnectionTypeBase<IGenericConnection, ISqliteConnectionFactory, SqliteConnectionConfiguration>,
      ISupportsContainerTypes,
      ISupportsFieldTypes,
      ISupportsWriteModes
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
        defaultContainerName: "SqliteConnection")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = (DefaultConnectionProvider)services.GetRequiredService<IConnectionProvider>();

            var headerProvider = services.GetRequiredService<ConnectionConfigurationProvider>();
            var configProvider = services.GetRequiredService<SqliteConnectionConfigurationProvider>();
            headerProvider.Register(Name, configProvider);

            // without re-resolving from DI each call.
    
            return host;
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why: this option registers its factory WITH WHAT THAT FACTORY NEEDS, exactly as
            // HttpConnectionType registers AddHttpClient() for its own factory. SQLite supports
            // EncryptionKey authentication, which resolves its key from a named secret manager.
            builder.Services.AddSingleton<ISqliteConnectionFactory>(sp => new SqliteConnectionFactory(
                sp.GetRequiredService<ILogger<SqliteConnectionFactory>>(),
                sp.GetRequiredService<ILogger<SqliteConnection>>(),
                sp.GetRequiredService<ISecretManagerProvider>()));
            builder.Services.TryAddSingleton<SqliteConnectionConfigurationProvider>(sp =>
                new SqliteConnectionConfigurationProvider(
                    sp.GetService<ILogger<SqliteConnectionConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    dataStoreName,
                    pathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<SqliteConnectionConfiguration>>(
                sp => sp.GetRequiredService<SqliteConnectionConfigurationProvider>());
            // Why: RegisterFactory (below) requires ConnectionConfigurationProvider (the shared header
            // provider for the whole Connections domain) to already be registered. TryAddSingleton makes
            // this idempotent — every connection-kind option calls it, harmlessly redundant after the first.
            ConnectionConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return builder;
    
        });

    }

    // Why (no Configure override): connection configuration lives in ConfigurationDb and is read
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
}
