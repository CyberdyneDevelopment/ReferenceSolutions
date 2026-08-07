using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict;

/// <summary>
/// Typed-body configuration provider for <c>auth.OpenIddictTokenManager</c> rows.
/// Extends <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> with
/// <c></c> — all reads go to the gateway against ConfigurationDb.
///
/// <c>Get(Guid id)</c> accepts the parent <c>auth.TokenManager.Id</c> (the durable
/// logical key) and routes to <c>WHERE [TokenManagerId]=@p0 AND IsCurrent=1</c>
/// via the container FK key discovered from the IDataStore tree.
/// </summary>
/// <remarks>
/// Mirrors <c>MsSqlConnectionConfigurationProvider</c> from the Connection domain.
/// </remarks>
public class OpenIddictTokenManagerConfigurationProvider
    : DefaultConfigurationProvider<OpenIddictTokenManagerConfiguration, OpenIddictTokenManagerConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="OpenIddictTokenManagerConfigurationProvider"/> class.
    /// </summary>
    public OpenIddictTokenManagerConfigurationProvider(
        ILogger<OpenIddictTokenManagerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "auth",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<OpenIddictTokenManagerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
