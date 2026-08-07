using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Oidc;

/// <summary>
/// Factory that builds <see cref="OidcExternalIdentityProvider"/> instances from a resolved
/// <see cref="ExternalIdentityProviderConfiguration"/> header (whose <c>Configuration</c> property
/// carries the composed <see cref="OidcExternalIdentityProviderConfiguration"/> typed body).
/// </summary>
public sealed class OidcExternalIdentityProviderFactory
    : IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<OidcExternalIdentityProviderFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="OidcExternalIdentityProviderFactory"/> class.</summary>
    public OidcExternalIdentityProviderFactory(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<OidcExternalIdentityProviderFactory>();
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvider> Create(ExternalIdentityProviderConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IExternalIdentityProvider>.Failure(
                ExternalIdentityProviderLog.FactoryCreateFailed(_logger, "(null)", "configuration was null."));

        if (configuration.Configuration is not OidcExternalIdentityProviderConfiguration typed)
            return GenericResult<IExternalIdentityProvider>.Failure(
                ExternalIdentityProviderLog.FactoryCreateFailed(_logger, configuration.Name,
                    "no composed OidcExternalIdentityProviderConfiguration typed body — the header's ServiceOptionType must be 'Oidc' and the typed provider must be registered."));

        var provider = new OidcExternalIdentityProvider(
            configuration, typed, _loggerFactory.CreateLogger<OidcExternalIdentityProvider>());

        return GenericResult<IExternalIdentityProvider>.Success(provider);
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvider> Create(IGenericConfiguration configuration)
    {
        if (configuration is ExternalIdentityProviderConfiguration typed)
            return Create(typed);

        return GenericResult<IExternalIdentityProvider>.Failure(
            ExternalIdentityProviderLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                $"expected ExternalIdentityProviderConfiguration but received '{configuration?.GetType().FullName ?? "null"}'."));
    }

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typed)
            return GenericResult<T>.Success(typed);

        return GenericResult<T>.Failure(
            ExternalIdentityProviderLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                $"created service does not implement requested type '{typeof(T).FullName}'."));
    }

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess
            ? GenericResult<IGenericService>.Success(result.Value!)
            : result.ToNewResult<IGenericService>();
    }
}
