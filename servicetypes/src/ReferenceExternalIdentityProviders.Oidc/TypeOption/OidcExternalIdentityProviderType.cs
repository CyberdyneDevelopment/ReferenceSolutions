using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Oidc;

/// <summary>
/// Reference Oidc <see cref="ExternalIdentityProviderTypes"/> ServiceTypeOption. Registers the
/// header + typed-body gateway-backed configuration providers and the
/// <see cref="OidcExternalIdentityProviderFactory"/> that builds <see cref="OidcExternalIdentityProvider"/>
/// instances — no OpenIddict/host-specific dependency, self-contained and testable in-repo.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ExternalIdentityProviderTypes), "Oidc")]
public sealed class OidcExternalIdentityProviderType
    : ExternalIdentityProviderTypeBase<
        IExternalIdentityProvider,
        ExternalIdentityProviderConfiguration,
        IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>
{
    /// <summary>Initializes a new instance of <see cref="OidcExternalIdentityProviderType"/>.</summary>
    public OidcExternalIdentityProviderType() : base(name: "Oidc", defaultContainerName: "ExternalIdentityProvider")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>();

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<OidcExternalIdentityProviderType>();

            var factory = services.GetRequiredService<IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>();
            var headerProvider = services.GetRequiredService<ExternalIdentityProviderConfigurationProvider>();
            var typedProvider = services.GetRequiredService<OidcExternalIdentityProviderConfigurationProvider>();

            // Why: register the Oidc typed-body provider with the header provider so ComposeTypedBody
            // dispatches to auth.OidcExternalIdentityProvider rows when the discriminator is "Oidc".
            headerProvider.Register("Oidc", typedProvider);

            // Why: multiple ExternalIdentityProviderTypes options may register against the SAME header
            // provider (unlike TokenManagers' single-active domain) — RegisterParentProvider is safe to
            // call from every option since they all point at the one auth.ExternalIdentityProvider table.
            var parentResult = provider.Register(headerProvider);
            if (!parentResult.IsSuccess) return host;

            var factoryResult = provider.Register("Oidc", factory);
            if (!factoryResult.IsSuccess) return host;

            var headerResult = provider.Register("Oidc", headerProvider);
            if (!headerResult.IsSuccess) return host;

            ExternalIdentityProviderLog.ProviderRegistered(logger, "Oidc");
    
            return host;
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why: registers the option-agnostic domain builder.Services this option depends on (header config
            // provider + resolver) — idempotent (TryAdd*), safe for every sibling option to call. The
            // resolver registration used to live HERE alone, which welded external-IdP login into core
            // auth; it now belongs to the domain so this option stays genuinely optional (FDW-624).
            ExternalIdentityProviderDomainServices.RegisterDomainServices(builder.Services);

            builder.Services.TryAddSingleton<OidcExternalIdentityProviderConfigurationProvider>(sp =>
                new OidcExternalIdentityProviderConfigurationProvider(
                    sp.GetService<ILogger<OidcExternalIdentityProviderConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    dataStoreName,
                    pathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            builder.Services.TryAddSingleton<OidcExternalIdentityProviderFactory>();
            builder.Services.TryAddSingleton<IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>(
                sp => sp.GetRequiredService<OidcExternalIdentityProviderFactory>());

            return builder;
    
        });

    }

}
