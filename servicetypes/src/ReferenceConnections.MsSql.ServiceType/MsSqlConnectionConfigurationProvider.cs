using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.MsSql.Commands;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Services.Connections;
using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Discovery;

using Fdw.Services.Connections.MsSql.Authentication;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.ErrorHandlers;

using ReferenceConnections.MsSql.Mapping;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Results;

using Fdw.Services.Connections.MsSql.Logging;

using Fdw.Services.Connections.MsSql.Validation;



namespace ReferenceConnections.MsSql;

/// <summary>
/// Typed configuration provider for cfg-tier MsSqlConnection rows.
/// </summary>
/// <remarks>
/// Queries conn.MsSqlConnection via the configurationGateway. Get(Guid id) accepts the parent
/// Connection's logical Id and routes to <c>WHERE [ConnectionId]=@p0 AND IsCurrent=1</c>
/// via the container FK key discovered from the IDataStore tree.
/// Startup connections (ConfigurationDb) are not served by this provider — consumers needing
/// the startup database use <see cref="IConfigurationGateway"/> directly.
/// </remarks>
public class MsSqlConnectionConfigurationProvider : DefaultConfigurationProvider<MsSqlConnectionConfiguration, MsSqlConnectionConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlConnectionConfigurationProvider"/> class.</summary>
    public MsSqlConnectionConfigurationProvider(
        ILogger<MsSqlConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<MsSqlConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
