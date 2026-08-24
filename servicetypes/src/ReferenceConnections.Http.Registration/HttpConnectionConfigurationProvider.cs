using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Http.Commands;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Http;

using Fdw.Services.Connections.Http.Limits;

using Fdw.Services.Connections.Http.Results;

using Fdw.Services.Connections.Http.Logging;

using Fdw.Services.Connections.Http.Validation;

using Fdw.Services.Connections.Http.Protocols;

using Fdw.Services.Connections.Http.Security;


namespace ReferenceConnections.Http;

/// <summary>Typed configuration provider for Http connections.</summary>
/// <remarks>
/// Queries conn.HttpConnection via the configurationGateway. Get(Guid id) accepts the parent
/// Connection's logical Id and routes to <c>WHERE [ConnectionId]=@p0 AND IsCurrent=1</c>
/// via the container FK key discovered from the IDataStore tree.
/// </remarks>
public class HttpConnectionConfigurationProvider : DefaultConfigurationProvider<HttpConnectionConfiguration, HttpConnectionConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="HttpConnectionConfigurationProvider"/> class.</summary>
    public HttpConnectionConfigurationProvider(
        ILogger<HttpConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<HttpConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
