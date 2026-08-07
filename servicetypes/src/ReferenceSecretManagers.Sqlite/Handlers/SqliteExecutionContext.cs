using System;
using Fdw.Configuration;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// SQLite-specific execution context providing access to the data source and table configuration.
/// </summary>
/// <remarks>
/// Handlers open a <see cref="SqliteConnection"/> per-operation via <see cref="CreateConnection"/>.
/// SQLite connection pooling (built into Microsoft.Data.Sqlite) makes per-operation connections efficient.
/// </remarks>
internal sealed class SqliteExecutionContext : ISecretManagerExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteExecutionContext"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="configuration">The SQLite secret manager configuration.</param>
    /// <param name="serviceId">The service identifier.</param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>SqliteSecretManagerConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the real name is threaded from the factory through this parameter.
    /// </param>
    public SqliteExecutionContext(
        ILogger logger,
        SqliteSecretManagerConfiguration configuration,
        string serviceId,
        string secretManagerName)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SqliteConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        // Why: Name is a header field after config-split; use the threaded name or fall back to the GUID.
        SecretManagerName = !string.IsNullOrEmpty(secretManagerName) ? secretManagerName : serviceId;
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = configuration.DataSource
        }.ToString();
    }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IGenericConfiguration Configuration => SqliteConfiguration;

    /// <summary>Gets the SQLite-specific configuration.</summary>
    public SqliteSecretManagerConfiguration SqliteConfiguration { get; }

    /// <summary>Gets the logical name of this secret manager instance.</summary>
    public string SecretManagerName { get; }

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <summary>Gets the SQLite connection string built from <see cref="SqliteSecretManagerConfiguration.DataSource"/>.</summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Creates a new <see cref="SqliteConnection"/> using the configured data source.
    /// Callers must open and dispose the returned connection.
    /// </summary>
    public SqliteConnection CreateConnection() => new SqliteConnection(ConnectionString);
}
