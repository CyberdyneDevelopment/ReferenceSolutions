using System;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Sqlite.Commands;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Sqlite;

using Fdw.Services.Connections.Sqlite.Authentication;

using Fdw.Services.Connections.Sqlite.Validation;

namespace ReferenceConnections.Sqlite;

/// <summary>
/// Typed configuration provider for SqliteConnection rows in conn.SqliteConnection.
/// </summary>
/// <remarks>
/// Get(Guid id) accepts the parent Connection's logical Id and routes to
/// <c>WHERE [ConnectionId]=@p0 AND IsCurrent=1</c> via the IDataStore tree.
/// </remarks>
public class SqliteConnectionConfigurationProvider
    : DefaultConfigurationProvider<SqliteConnectionConfiguration, SqliteConnectionConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionConfigurationProvider"/> class.
    /// </summary>
    public SqliteConnectionConfigurationProvider(
        ILogger<SqliteConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(
            logger ?? NullLogger<SqliteConnectionConfigurationProvider>.Instance,
            lazyGateway,
            dataStoreName,
            pathName,
            invalidator)
    {
    }
}
