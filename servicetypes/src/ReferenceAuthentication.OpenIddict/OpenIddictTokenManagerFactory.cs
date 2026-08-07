using System;
using Fdw.Configuration;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Logging;
using ReferenceAuthentication.OpenIddict.Storage;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict;

/// <summary>
/// Factory that builds <see cref="OpenIdTokenManager"/> instances from a resolved
/// <see cref="Fdw.Services.TokenManagers.TokenManagerConfiguration"/> header (whose <c>Configuration</c> property
/// carries the composed <see cref="OpenIddictTokenManagerConfiguration"/> typed body). Replaces the
/// three deleted <c>OpenIddict*ServerFactory</c> classes.
/// </summary>
internal sealed class OpenIddictTokenManagerFactory : ITokenManagerFactory<ITokenManager, Fdw.Services.TokenManagers.TokenManagerConfiguration>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ExternalIdentityService _externalIdentityService;
    private readonly IEffectivePermissionResolver _permissionResolver;
    private readonly Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>> _secretManagerProvider;
    private readonly RevokedAccessTokenStore _revokedTokenStore;
    private readonly OpenIddictAuthorizationStore _authorizationStore;
    private readonly ExternalIdentityProvisionerBindingConfigurationProvider _bindingProvider;
    private readonly Lazy<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>> _provisionerProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<OpenIddictTokenManagerFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="OpenIddictTokenManagerFactory"/> class.</summary>
    // Why: the two provider dependencies are Lazy so this factory stays pure (FDW045). It is resolved
    // from inside the TokenManagers scoped resolver lambda; a direct IFdwServiceProvider<IExternalIdentity
    // Provisioner,...> param is the exact shape that re-enters a provider realization (the FDW-615 second
    // door — this factory reaches the provisioner provider). Both are dereferenced only in Create()
    // (at token-manager build time), long after realization, so Lazy is safe.
    public OpenIddictTokenManagerFactory(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ExternalIdentityService externalIdentityService,
        IEffectivePermissionResolver permissionResolver,
        Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>> secretManagerProvider,
        RevokedAccessTokenStore revokedTokenStore,
        OpenIddictAuthorizationStore authorizationStore,
        ILoggerFactory? loggerFactory,
        ExternalIdentityProvisionerBindingConfigurationProvider bindingProvider,
        Lazy<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>> provisionerProvider)
    {
        ArgumentNullException.ThrowIfNull(userProvider);
        ArgumentNullException.ThrowIfNull(credentialService);
        ArgumentNullException.ThrowIfNull(externalIdentityService);
        ArgumentNullException.ThrowIfNull(permissionResolver);
        ArgumentNullException.ThrowIfNull(secretManagerProvider);
        ArgumentNullException.ThrowIfNull(revokedTokenStore);
        ArgumentNullException.ThrowIfNull(authorizationStore);
        ArgumentNullException.ThrowIfNull(bindingProvider);
        ArgumentNullException.ThrowIfNull(provisionerProvider);
        _userProvider = userProvider;
        _credentialService = credentialService;
        _externalIdentityService = externalIdentityService;
        _permissionResolver = permissionResolver;
        _secretManagerProvider = secretManagerProvider;
        _revokedTokenStore = revokedTokenStore;
        _authorizationStore = authorizationStore;
        _bindingProvider = bindingProvider;
        _provisionerProvider = provisionerProvider;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<OpenIddictTokenManagerFactory>();
    }

    /// <inheritdoc />
    public IGenericResult<ITokenManager> Create(Fdw.Services.TokenManagers.TokenManagerConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<ITokenManager>.Failure(
                OpenIddictProviderLog.FactoryCreateFailed(_logger, "(null)", "configuration was null."));

        if (configuration.Configuration is not OpenIddictTokenManagerConfiguration typed)
            return GenericResult<ITokenManager>.Failure(
                OpenIddictProviderLog.FactoryCreateFailed(_logger, configuration.Name,
                    "no composed OpenIddictTokenManagerConfiguration typed body — the header's ServiceOptionType must be 'OpenIddict' and the typed provider must be registered."));

        var manager = new OpenIdTokenManager(
            configuration,
            typed,
            _userProvider,
            _credentialService,
            _externalIdentityService,
            _permissionResolver,
            _secretManagerProvider.Value,
            _revokedTokenStore,
            _authorizationStore,
            _loggerFactory.CreateLogger<OpenIdTokenManager>(),
            _bindingProvider,
            _provisionerProvider.Value);

        return GenericResult<ITokenManager>.Success(manager);
    }

    /// <inheritdoc />
    public IGenericResult<ITokenManager> Create(IGenericConfiguration configuration)
    {
        if (configuration is Fdw.Services.TokenManagers.TokenManagerConfiguration typed)
            return Create(typed);

        return GenericResult<ITokenManager>.Failure(
            OpenIddictProviderLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                $"expected TokenManagerConfiguration but received '{configuration?.GetType().FullName ?? "null"}'."));
    }

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : Fdw.Abstractions.IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typed)
            return GenericResult<T>.Success(typed);

        return GenericResult<T>.Failure(
            OpenIddictProviderLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                $"created service does not implement requested type '{typeof(T).FullName}'."));
    }

    /// <inheritdoc />
    IGenericResult<Fdw.Abstractions.IGenericService> Fdw.Abstractions.IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess
            ? GenericResult<Fdw.Abstractions.IGenericService>.Success(result.Value!)
            : result.ToNewResult<Fdw.Abstractions.IGenericService>();
    }
}
