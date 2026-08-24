using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.PostgreSql.Commands;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Services.Connections;
using Fdw.Services.Connections.PostgreSql;

using Fdw.Services.Connections.PostgreSql.Discovery;

using Fdw.Services.Connections.PostgreSql.Authentication;

using Fdw.Services.Connections.PostgreSql.Results;

using Fdw.Services.Connections.PostgreSql.Logging;

using Fdw.Services.Connections.PostgreSql.Validation;


namespace ReferenceConnections.PostgreSql;

/// <summary>Typed configuration provider for PostgreSql connections.</summary>
/// <remarks>
/// Queries conn.PostgreSqlConnection via the configurationGateway. Get(Guid id) accepts the parent
/// Connection's logical Id and routes to <c>WHERE [ConnectionId]=@p0 AND IsCurrent=1</c>
/// via the container FK key discovered from the IDataStore tree.
/// </remarks>
public class PostgreSqlConnectionConfigurationProvider : DefaultConfigurationProvider<PostgreSqlConnectionConfiguration, PostgreSqlConnectionConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlConnectionConfigurationProvider"/> class.</summary>
    public PostgreSqlConnectionConfigurationProvider(
        ILogger<PostgreSqlConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<PostgreSqlConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
