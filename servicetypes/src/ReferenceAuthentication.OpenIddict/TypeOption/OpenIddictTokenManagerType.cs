using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using Fdw.Collections;
using ReferenceAuthentication.OpenIddict.Claims;
using ReferenceAuthentication.OpenIddict.Hosting;
using ReferenceAuthentication.OpenIddict.Logging;
using ReferenceAuthentication.OpenIddict.Services;
using ReferenceAuthentication.OpenIddict.Storage;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.TokenManagers;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceAuthentication.OpenIddict;

/// <summary>
/// OpenIddict <see cref="TokenManagerTypes"/> ServiceTypeOption. One uniform registration — a host
/// issues tokens iff it hosts <c>/connect/token</c>; there is no separate issuance-only/validation-only
/// split (that FDW-570 capability split is retired — every OpenIddict host registers the full
/// AddCore + AddServer + AddValidation pipeline). Registers everything the OpenIddict token-manager
/// engine needs EXCEPT the client-secret provisioner (deleted — <see cref="OpenIdTokenManager"/> now
/// validates client_credentials secrets directly against the secret manager at issuance time).
/// </summary>
/// <remarks>
/// The RS256 signing key and issuer are resolved on demand through the gateway-backed configuration
/// providers and the secret manager by <see cref="OpenIddictSigningKeyConfigurator"/> (an
/// <c>IConfigureOptions&lt;OpenIddictServerOptions&gt;</c>) when OpenIddict first builds its options —
/// the same injected-provider path every other FDW service uses, no hosted service, no mutable singleton.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(TokenManagerTypes), "OpenIddict")]
public sealed class OpenIddictTokenManagerType
    : TokenManagerTypeBase<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>
{
    /// <summary>The confidential service clients this auth server issues to.</summary>
    private static readonly string[] ServiceClientIds = ["fdw.api", "fdw.etl", "fdw.scheduler"];

    /// <summary>Initializes a new instance of <see cref="OpenIddictTokenManagerType"/>.</summary>
    public OpenIddictTokenManagerType() : base(name: "OpenIddict", defaultContainerName: "TokenManager")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>>();

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<OpenIddictTokenManagerType>();

            var factory = services.GetRequiredService<ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>();
            var headerProvider = services.GetRequiredService<TokenManagerConfigurationProvider>();
            var typedProvider = services.GetRequiredService<OpenIddictTokenManagerConfigurationProvider>();

            // Why: Register the OpenIddict typed-body provider with the header provider so that
            // ComposeTypedBody dispatches to auth.OpenIddictTokenManager rows when the discriminator is
            // "OpenIddict". The generic overload wraps via ConfigurationProviderAdapter.
            headerProvider.Register("OpenIddict", typedProvider);

            // Why: FDW-402 — the config base is polymorphic so the source-generated Initialize cannot
            // determine the parent provider. Single active OpenIddict config: register the header provider
            // explicitly as the parent.
            var parentResult = provider.Register(headerProvider);
            if (!parentResult.IsSuccess) return parentResult.ToNewResult<IHost>();

            var factoryResult = provider.Register("OpenIddict", factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            var headerResult = provider.Register("OpenIddict", headerProvider);
            if (!headerResult.IsSuccess) return headerResult.ToNewResult<IHost>();

            // Why here: a confidential client seeded by SQL has no usable secret -- OpenIddict compares
            // against a hash only its own manager can produce -- so every client_credentials exchange
            // fails with "The credentials provided are invalid" until this runs. Initialize is the first
            // point the manager can be resolved.
            var provisionResult = new OpenIddictClientSecretProvisioner(
                    loggerFactory.CreateLogger<OpenIddictClientSecretProvisioner>())
                .Provision(services, "MsSqlSecrets", ServiceClientIds)
                .GetAwaiter().GetResult();
            if (provisionResult.IsFailure) return provisionResult.ToNewResult<IHost>();

            OpenIddictProviderLog.ProviderRegistered(logger, "OpenIddict", "TokenManager");
    
            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            RegisterConfigurationProviders(builder.Services, DataStore, PathName);
            RegisterRuntimeServices(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

    /// <summary>
    /// Registers the configuration providers this option reads through.
    /// </summary>
    /// <remarks>
    /// Called from the <c>Registration</c> phase body, which runs before <c>Build()</c> and so still has
    /// an <c>IServiceCollection</c> — the phase that runs after Build gets only an
    /// <c>IServiceProvider</c> and cannot add registrations.
    /// </remarks>
    // Why: registers the header (TokenManagerConfigurationProvider) + typed-body
    // (OpenIddictTokenManagerConfigurationProvider) gateway-backed config providers this option needs,
    // plus the domain config providers RegisterRuntimeServices' dependents require
    // (UserRole/Role for DefaultPrincipalResolver, the generic authN service itself).
    private static void RegisterConfigurationProviders(IServiceCollection services, string dataStoreName, string pathName)
    {
        TokenManagerConfigurationProvider.RegisterDomainServices(services);

        // Why: ConnectTokenEndpointBase (this package) takes ExternalIdentityProviderResolver as a
        // required ctor dependency, so this option must register it rather than relying on some
        // ExternalIdentityProviderTypes option having done so. Previously only the Oidc option
        // registered it, which meant dropping the OIDC package broke /connect/token entirely —
        // password grant included (FDW-624).
        ExternalIdentityProviderDomainServices.RegisterDomainServices(services);

        services.TryAddSingleton<OpenIddictTokenManagerConfigurationProvider>(sp =>
            new OpenIddictTokenManagerConfigurationProvider(
                sp.GetService<ILogger<OpenIddictTokenManagerConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName,
                pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        // Why: DefaultPrincipalResolver (resolved via IPrincipalResolver below) needs the Authorization
        // domain's own config providers — this option calls the owning domain's ONE registration shape,
        // idempotent (TryAdd*) so every consumer gets the same singleton at the same default location.
        UserRoleConfigurationProvider.RegisterDomainConfiguration(services);
        RoleConfigurationProvider.RegisterDomainConfiguration(services);

        // Why: OpenIdTokenManager resolves its per-(tenant, external provider) provisioner selection via
        // this provider. The provisioner ServiceTypeCollection itself
        // (Fdw.Services.ExternalIdentityProviders.ExternalIdentityProvisionerTypes) auto-registers its
        // own IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>
        // via the [ServiceTypeCollection] PlatformServices sweep — no manual registration needed here for
        // that half.
        ExternalIdentityProvisionerBindingConfigurationProvider.RegisterDomainServices(services);

        Fdw.Services.TokenManagers.AuthenticationService.RegisterDomainServices(services);
    }

    // Why: OpenIddict's own DI (core stores + server issuance pipeline + validation handler) plus every
    // runtime service the OpenIddict ITokenManager engine needs is wired HERE, in the ServiceTypeOption's
    // registration — the one registration surface, not a separate helper. No client-secret provisioner:
    // OpenIdTokenManager.Issue validates client_credentials secrets directly against the secret manager.
    private static void RegisterRuntimeServices(IServiceCollection services)
    {
        RegisterOpenIddictComponents(services);

        // Why: Scoped stores match OpenIddict's per-request resolution lifetime.
        services.AddScoped<OpenIddictApplicationStore>();
        services.AddScoped<OpenIddictAuthorizationStore>();
        services.AddScoped<OpenIddictScopeStore>();
        services.AddScoped<ExternalIdentityService>();
        services.AddScoped<RevokedAccessTokenStore>();

        // Why: register Lazy wrappers for the two FDW provider dependencies so the (pure, FDW045)
        // OpenIddictTokenManagerFactory receives them lazily — deferring resolution past the factory's
        // construction inside the TokenManagers resolver lambda (the FDW-615 second door via the
        // provisioner provider). MEDI does not synthesize Lazy<T>; these must be registered explicitly.
        services.TryAddScoped(sp => new Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>(
            () => sp.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>()));
        services.TryAddScoped(sp => new Lazy<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(
            () => sp.GetRequiredService<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>()));

        // Why: Scoped — the factory builds OpenIdTokenManager holding these scoped runtime deps.
        services.TryAddScoped<OpenIddictTokenManagerFactory>();
        services.TryAddScoped<ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>(sp =>
            sp.GetRequiredService<OpenIddictTokenManagerFactory>());

        // Why: DefaultPrincipalResolver injects UserRoleConfigurationProvider/RoleConfigurationProvider
        // (registered in RegisterConfigurationProviders) plus the Authorization domain's
        // IEffectivePermissionResolver/IOrganizationProvider — registered by the Authorization domain
        // itself when referenced; this option only wires the resolver seam.
        services.AddScoped<IPrincipalResolver, DefaultPrincipalResolver>();
        services.AddScoped<ProcessSignInClaimsHandler>();

        // Why: Named client so it doesn't collide with other IHttpClientFactory registrations.
        services.AddHttpClient("OpenIddictOutbound");

        // Why: IOutboundCredentialService is the machine-to-machine seam (client-credentials flow)
        // used by inter-service HTTP clients. It resolves the token endpoint on demand from the
        // gateway-backed config provider (via IServiceScopeFactory) at call time.
        services.TryAddSingleton<IOutboundCredentialService>(sp =>
            new OpenIddictOutboundCredentialService(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                (sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance)
                    .CreateLogger<OpenIddictOutboundCredentialService>()));
    }

    // Why: PostConfigureLifetimes resolves the OpenIddict config on demand through the gateway-backed
    // provider (via IServiceScopeFactory) and applies AccessTokenLifetime / RefreshTokenLifetime to
    // OpenIddictServerOptions. Runs once when options are first built — no mutable singleton.
    private sealed class PostConfigureLifetimes : IPostConfigureOptions<OpenIddictServerOptions>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;

        internal PostConfigureLifetimes(IServiceScopeFactory scopeFactory, ILoggerFactory? loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<PostConfigureLifetimes>();
        }

        // Why: sync-over-async at the synchronous IPostConfigureOptions seam, resolved once at
        // options-build via a short-lived scope — mirrors MsSqlConnectionFactory.ResolvePasswordSync.
        // Config resolution is inlined here (no shared helper) — this option injects only the
        // providers IT needs.
#pragma warning disable VSTHRD002
        public void PostConfigure(string? name, OpenIddictServerOptions options)
        {
            using var scope = _scopeFactory.CreateScope();

            var allHeaders = scope.ServiceProvider.GetRequiredService<TokenManagerConfigurationProvider>()
                .Get(CancellationToken.None).GetAwaiter().GetResult();
            if (!allHeaders.IsSuccess)
                throw new InvalidOperationException(
                    $"OpenIddict is registered but the header configuration could not be loaded; token lifetimes "
                    + $"cannot be applied. Reason: {allHeaders.CurrentMessage}");

            var header = System.Linq.Enumerable.FirstOrDefault(allHeaders.Value ?? [],
                c => string.Equals(c.ServiceOptionType, "OpenIddict", StringComparison.OrdinalIgnoreCase));
            if (header is null)
                throw new InvalidOperationException(
                    "OpenIddict is registered but no enabled OpenIddict token manager configuration exists; token lifetimes cannot be applied.");

            var typedResult = scope.ServiceProvider.GetRequiredService<OpenIddictTokenManagerConfigurationProvider>()
                .Get(header.Id, CancellationToken.None).GetAwaiter().GetResult();
            if (!typedResult.IsSuccess || typedResult.Value is not OpenIddictTokenManagerConfiguration typed)
                throw new InvalidOperationException(
                    $"OpenIddict is registered but the typed-body configuration could not be loaded; token "
                    + $"lifetimes cannot be applied. Reason: {typedResult.CurrentMessage}");

            if (!string.IsNullOrEmpty(typed.AccessTokenLifetime))
                options.AccessTokenLifetime = System.Xml.XmlConvert.ToTimeSpan(typed.AccessTokenLifetime);

            if (!string.IsNullOrEmpty(typed.RefreshTokenLifetime))
                options.RefreshTokenLifetime = System.Xml.XmlConvert.ToTimeSpan(typed.RefreshTokenLifetime);
        }
#pragma warning restore VSTHRD002
    }

    /// <inheritdoc />
    // Why: mirrors DefaultSchedulerType — register factory + header provider + typed provider, then
    // register the header provider as the parent so name-based Get() resolves auth.TokenManager rows.

    // Why: marker guards against double-registration when multiple OpenIddict configs are enabled in
    // the same DI container (only the first RegisterOpenIddictComponents call wires OpenIddict).
    private sealed class OpenIddictRegisteredMarker { }

    // Why: OpenIddict's own DI (core stores + server issuance pipeline + validation handler) is wired
    // HERE, in the ServiceTypeOption's registration — the one registration surface, not a separate
    // helper. No configuration is required: entity/store mappings, endpoint routes, flows, and the
    // sign-in event handler are all static. The only config-driven inputs — the RS256 signing/
    // encryption key, the issuer, and token lifetimes — are applied to OpenIddictServerOptions
    // separately by OpenIddictSigningKeyConfigurator / PostConfigureLifetimes, which read the
    // gateway-backed config providers on demand.
    private static void RegisterOpenIddictComponents(IServiceCollection services)
    {
        // Why: descriptor scan (we can't BuildServiceProvider here — that would create a second
        // container) guards against double-registration when multiple OpenIddict configs are enabled.
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(OpenIddictRegisteredMarker))
                return;
        }

        services.AddSingleton<OpenIddictRegisteredMarker>();

        // Why: OpenIddictSigningKeyConfigurator resolves the RS256 key + issuer on demand through the
        // config/secret providers and applies them to OpenIddictServerOptions when options are first built.
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>, OpenIddictSigningKeyConfigurator>();

        services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp =>
            new PostConfigureLifetimes(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetService<ILoggerFactory>()));

        // Why: OpenIddict's AddValidation().UseAspNetCore() registers the validation auth HANDLER, but
        // does not make it the DEFAULT scheme. Protected ([Authorize]) endpoints invoke the default
        // authenticate/challenge scheme — without one set, ASP.NET throws "No authenticationScheme was
        // specified" (500 on every protected route). Set OpenIddict validation as the default scheme.
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                // Why: SetDefault*Entity maps OpenIddict's generic IOpenIddict*Store<TEntity> to our
                // concrete records so OpenIddict resolves the correct store from DI. NO token entity/
                // store here — DisableTokenStorage() below means OpenIddict never persists a token row
                // (access tokens are stateless RS256 JWTs; refresh-token/authorization-code lifecycle
                // is tracked entirely through the persisted authorization, not a per-token row).
                options.SetDefaultApplicationEntity<OpenIddictApplicationRecord>()
                       .SetDefaultAuthorizationEntity<OpenIddictAuthorizationRecord>()
                       .SetDefaultScopeEntity<OpenIddictScopeRecord>();

                // Why: Replace*Store<TEntity, TStore> registers the DataGateway-backed store so
                // OpenIddict resolves it instead of the built-in (EF-backed) default store.
                options.ReplaceApplicationStore<OpenIddictApplicationRecord, OpenIddictApplicationStore>()
                       .ReplaceAuthorizationStore<OpenIddictAuthorizationRecord, OpenIddictAuthorizationStore>()
                       .ReplaceScopeStore<OpenIddictScopeRecord, OpenIddictScopeStore>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetRevocationEndpointUris("/connect/revoke")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetConfigurationEndpointUris("/.well-known/openid-configuration")
                       .SetJsonWebKeySetEndpointUris("/.well-known/jwks");

                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange()
                       .AllowClientCredentialsFlow()
                       .AllowRefreshTokenFlow()
                       .AllowCustomFlow("password")
                       .AllowCustomFlow("agent_key")
                       .AllowCustomFlow("external_identity");

                // Why: DisableAccessTokenEncryption() produces plain RS256 JWTs verifiable by resource
                // servers using the JWKS endpoint — no shared data-protection key needed.
                options.DisableAccessTokenEncryption();

                // Why: access tokens are stateless RS256 JWTs — OpenIddict never persists a token row
                // for them (no per-token database round-trip on every request). Revocation is handled
                // by OpenIdTokenManager.Invalidate writing to the auth.RevokedAccessToken deny-list, and
                // by revoking the AUTHORIZATION (OpenIdTokenManager.Logout), which invalidates refresh
                // tokens and authorization codes tied to it — so no separate token-row persistence is
                // needed for those either. This is what lets auth.OpenIddictToken be deleted entirely.
                options.DisableTokenStorage();

                // Why: EnableTokenEndpointPassthrough delegates /connect/token to user code
                // (ConnectTokenEndpoint) so the custom FDW grants (password / agent_key / external_identity)
                // can resolve a principal and SignIn. Without passthrough OpenIddict owns the token endpoint
                // and rejects custom grants. Authorization endpoint passthrough is enabled for the same reason.
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough();

                // Why: client authentication is owned by the token service, NOT OpenIddict. OpenIddict's
                // built-in ValidateClientSecret handler verifies the presented secret against the app's
                // ClientSecretHash column — which is deliberately NULL here: the client secret is OUR
                // secret, stored in the secret manager (OAUTH_{clientId}), never copied onto the app row
                // (no provisioner ⇒ the app row is never version-on-written ⇒ its seeded permissions stay
                // linked, which is the ID2063 fix). Remove that handler so OpenIddict passes the request
                // through to ConnectTokenEndpoint, where OpenIdTokenManager.IssueForClientCredentials
                // resolves OAUTH_{clientId} from the secret manager and constant-time-compares it.
                // OpenIddict still identifies the client (ValidateClientId) and requires a secret to be
                // present (ValidateClientType); it just no longer verifies it against a stored hash.
                options.RemoveEventHandler(OpenIddictServerHandlers.ValidateClientSecret.Descriptor);

                // Why: ProcessSignInClaimsHandler bakes FDW sub/tenant_id/org_id/role/perm into every access
                // token, for BOTH user-interactive and machine-to-machine grant paths. Scoped handler
                // resolves UserTenantConfigurationProvider per request.
                options.AddEventHandler<OpenIddictServerEvents.ProcessSignInContext>(
                    builder => builder.UseScopedHandler<ProcessSignInClaimsHandler>());
            })
            .AddValidation(options =>
            {
                // Why: UseLocalServer() trusts the co-resident AddServer() registered above — no
                // separate key/issuer configuration needed.
                options.UseLocalServer();
                options.UseAspNetCore();
                // Why: FDW bakes role names under the "roles" claim key (plural); configure validation to
                // recognize "roles" as the role claim type so User.IsInRole() works on validated tokens.
                options.Configure(o => o.TokenValidationParameters.RoleClaimType = ClaimDefinitions.roles.Name);
            });
    }

    // Why: build the absolute outbound token URL by joining Authority + the configured TokenEndpoint. The
    // caller (OpenIddictOutboundCredentialService) fails loud when TokenEndpoint is unset, so this never
    // invents a default path. Joining with exactly one '/' avoids a malformed host (e.g.
    // "https://host.devconnect/token") when the configured path lacks a leading slash.
    // Why internal (not private): consumers outside this file (OpenIddictOutboundCredentialService) call
    // this pure string-join helper directly rather than duplicating it.
    internal static string ResolveTokenEndpoint(OpenIddictTokenManagerConfiguration config)
    {
        var path = config.TokenEndpoint;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return path;

        var basePart = config.Authority.TrimEnd('/');
        var relPart = path.StartsWith('/') ? path : "/" + path;
        return basePart + relPart;
    }
}
