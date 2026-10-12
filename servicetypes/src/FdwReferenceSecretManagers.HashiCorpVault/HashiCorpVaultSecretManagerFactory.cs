using Fdw.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using FdwReferenceSecretManagers.HashiCorpVault.Auth;
using FdwReferenceSecretManagers.HashiCorpVault.Configuration;
using FdwReferenceSecretManagers.HashiCorpVault.Engines;
using FdwReferenceSecretManagers.HashiCorpVault.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSecretManagers.HashiCorpVault;

/// <summary>
/// Builds <see cref="HashiCorpVaultSecretManager"/> instances from a resolved
/// <see cref="HashiCorpVaultConfiguration"/> implementation record.
/// </summary>
/// <remarks>
/// <para>
/// Resolving this Vault manager's own login secret through ANOTHER secret manager is deliberate. The
/// alternative — a Vault token or AppRole secret id sitting in a PlatformConfiguration column — would put
/// the key to every secret in the same database the secrets were moved out of.
/// </para>
/// <para>
/// The secret-manager provider is taken as a <see cref="Lazy{T}"/> because this factory is resolved
/// from inside that same domain's scoped resolver lambda; touching the provider eagerly would
/// re-enter a lambda whose cache entry is not published yet, which hangs the host silently rather
/// than throwing (FDW-615).
/// </para>
/// </remarks>
public sealed class HashiCorpVaultSecretManagerFactory
    : ServiceFactoryBase<ISecretManager, HashiCorpVaultConfiguration>, ISecretManagerServiceFactory<ISecretManager, HashiCorpVaultConfiguration>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HashiCorpVaultSecretManagerFactory> _logger;
    private readonly HttpClient _http;
    private readonly Lazy<IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>> _secretManagers;

    /// <summary>Initializes a new instance of the <see cref="HashiCorpVaultSecretManagerFactory"/> class.</summary>
    /// <param name="loggerFactory">The logger factory for created services.</param>
    /// <param name="http">The HTTP client used to reach Vault.</param>
    /// <param name="secretManagers">Provider resolving the secret manager holding this Vault login's own secret.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="http"/> or <paramref name="secretManagers"/> is null.</exception>
    public HashiCorpVaultSecretManagerFactory(
        ILoggerFactory? loggerFactory,
        HttpClient http,
        Lazy<IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>> secretManagers)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<HashiCorpVaultSecretManagerFactory>();
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _secretManagers = secretManagers ?? throw new ArgumentNullException(nameof(secretManagers));
    }

    /// <inheritdoc />
    public override IGenericResult<ISecretManager> Create(HashiCorpVaultConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<ISecretManager>.Failure(VaultLog.ConfigurationValueMissing(_logger, "(null)", nameof(configuration)));
        // NO FALLBACKS: each of these is required to reach Vault at all, and is reported by name.
        if (configuration.Address is not { Length: > 0 } address)
            return GenericResult<ISecretManager>.Failure(VaultLog.ConfigurationValueMissing(_logger, configuration.Name, nameof(configuration.Address)));
        if (configuration.Mount is not { Length: > 0 } mount)
            return GenericResult<ISecretManager>.Failure(VaultLog.ConfigurationValueMissing(_logger, configuration.Name, nameof(configuration.Mount)));
        if (configuration.Engine is not { Length: > 0 } engineName)
            return GenericResult<ISecretManager>.Failure(VaultLog.ConfigurationValueMissing(_logger, configuration.Name, nameof(configuration.Engine)));
        if (configuration.AuthMethod is not { Length: > 0 } authMethodName)
            return GenericResult<ISecretManager>.Failure(VaultLog.ConfigurationValueMissing(_logger, configuration.Name, nameof(configuration.AuthMethod)));

        // Why ByName and not a switch: both sets are TypeCollections precisely so a deployment can add
        // an engine or auth method in its own assembly. A switch would close sets meant to stay open.
        var engine = VaultSecretEngines.ByName(engineName);
        if (engine == VaultSecretEngines.NotFound)
            return GenericResult<ISecretManager>.Failure(VaultLog.OptionNotRegistered(_logger, configuration.Name, "secret engine", engineName));

        var authMethod = VaultAuthMethods.ByName(authMethodName);
        if (authMethod == VaultAuthMethods.NotFound)
            return GenericResult<ISecretManager>.Failure(VaultLog.OptionNotRegistered(_logger, configuration.Name, "auth method", authMethodName));

        return GenericResult<ISecretManager>.Success(
            new HashiCorpVaultSecretManager(
                _loggerFactory.CreateLogger<HashiCorpVaultSecretManager>(),
                configuration,
                new VaultApiClient(_http, _loggerFactory.CreateLogger<VaultApiClient>()),
                new VaultReadContext(
                    configuration.Name,
                    address,
                    mount,
                    engine,
                    authMethod,
                    ct => ResolveAuthSecret(configuration, authMethod, ct),
                    configuration.AuthRoleId,
                    configuration.AuthMount,
                    configuration.VaultNamespace)));
    }

    // Why async and deferred rather than resolved in Create: IServiceFactory.Create is synchronous
    // across every FDW domain, so resolving here would mean blocking on a task inside factory
    // construction — the deadlock the VSTHRD002 analyzer refuses. Deferring also means a workload
    // assertion is read when it is used rather than when the manager was built, and those rotate.
    private async Task<IGenericResult<string>> ResolveAuthSecret(
        HashiCorpVaultConfiguration typed,
        IVaultAuthMethod authMethod,
        CancellationToken cancellationToken)
    {
        if (!authMethod.RequiresStoredSecret)
        {
            // A workload-identity method reads its assertion from the environment at login time.
            if (typed.AuthAssertionLocation is not { Length: > 0 } location)
                return GenericResult<string>.Failure(
                    VaultLog.ConfigurationValueMissing(_logger, typed.Name, nameof(typed.AuthAssertionLocation)));

            return Environment.GetEnvironmentVariable(location) is { Length: > 0 } assertion
                ? GenericResult<string>.Success(assertion)
                : GenericResult<string>.Failure(
                    VaultLog.ConfigurationValueMissing(_logger, typed.Name, location));
        }

        if (typed.AuthSecretManagerName is not { Length: > 0 } secretManagerName)
            return GenericResult<string>.Failure(VaultLog.ConfigurationValueMissing(_logger, typed.Name, nameof(typed.AuthSecretManagerName)));
        if (typed.AuthSecretKeyName is not { Length: > 0 } secretKeyName)
            return GenericResult<string>.Failure(VaultLog.ConfigurationValueMissing(_logger, typed.Name, nameof(typed.AuthSecretKeyName)));

        var provider = await _secretManagers.Value.Get(secretManagerName, cancellationToken).ConfigureAwait(false);
        if (!provider.IsSuccess || provider.Value is null)
            return provider.ToNewResult<string>();

        var resolved = await provider.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: secretKeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess || resolved.Value is null)
            return resolved.ToNewResult<string>();

        using var secretValue = resolved.Value;
        return secretValue.GetStringValue() is { Length: > 0 } login
            ? GenericResult<string>.Success(login)
            : GenericResult<string>.Failure(VaultLog.ConfigurationValueMissing(_logger, typed.Name, secretKeyName));
    }



}
