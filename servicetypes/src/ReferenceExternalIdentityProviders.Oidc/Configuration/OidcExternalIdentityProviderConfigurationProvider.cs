using System;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Oidc;

/// <summary>
/// Typed-body configuration provider for <c>auth.OidcExternalIdentityProvider</c> rows.
/// Extends <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> — all reads go to the gateway
/// against ConfigurationDb.
///
/// <c>Get(Guid id)</c> accepts the parent <c>auth.ExternalIdentityProvider.Id</c> (the durable
/// logical key) and routes to <c>WHERE [ExternalIdentityProviderId]=@p0 AND IsCurrent=1</c> via the
/// container FK key discovered from the IDataStore tree.
/// </summary>
/// <remarks>
/// Mirrors <c>OpenIddictTokenManagerConfigurationProvider</c> from the TokenManagers domain.
/// </remarks>
public class OidcExternalIdentityProviderConfigurationProvider
    : DefaultConfigurationProvider<OidcExternalIdentityProviderConfiguration, OidcExternalIdentityProviderConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="OidcExternalIdentityProviderConfigurationProvider"/> class.
    /// </summary>
    public OidcExternalIdentityProviderConfigurationProvider(
        ILogger<OidcExternalIdentityProviderConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "auth",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<OidcExternalIdentityProviderConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
