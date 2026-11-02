using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Security.Hashing;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql.Configuration;
using FdwReferenceCredentials.Sql.Logging;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Credentials.Sql;
namespace FdwReferenceCredentials.Sql.Services;

/// <summary>
/// Vault-backed Personal Access Token service over <c>auth.PersonalAccessToken</c>. The raw token is
/// minted here (the edge owns the generator + policy), returned once, and only its peppered hash is
/// stored — the peppering happens inside the resolved <see cref="IPatVault"/>.
/// </summary>
/// <remarks>
/// The credential policy (vault name, environment segment, per-user token limit) is resolved lazily on
/// first use from the typed <see cref="SqlCredentialServiceConfiguration"/> row selected by
/// <see cref="ICredentialsSqlImplementationConfiguration.CredentialServiceName"/>. Token values are never logged.
/// </remarks>
public sealed class SqlPersonalAccessTokenService : IPersonalAccessTokenService, IDisposable
{
    // The credential-store domain row this host reads its selector through.
    private const string CredentialStoreName = "CredentialStore";

    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IDataVaultProvider _vaultProvider;
    private readonly IPersonalAccessTokenGenerator _generator;
    private readonly ICredentialStoreConfigurationProvider _credentialStores;
    private readonly CredentialServiceConfigurationProvider _configProvider;
    private readonly ILogger<SqlPersonalAccessTokenService> _logger;

    // Why: vault + policy are resolved once on first use and cached — the vault is a system-lifetime
    // singleton and the policy does not change at runtime (a restart picks up edits).
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private IPatVault? _vault;
    private string? _credentialPrefix;
    private string? _environment;
    private int _maxTokensPerUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlPersonalAccessTokenService"/> class.
    /// </summary>
    public SqlPersonalAccessTokenService(
        IDataVaultProvider vaultProvider,
        IPersonalAccessTokenGenerator generator,
        ICredentialStoreConfigurationProvider credentialStores,
        CredentialServiceConfigurationProvider configProvider,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<SqlPersonalAccessTokenService>? logger = null)
    {
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _vaultProvider = vaultProvider ?? throw new ArgumentNullException(nameof(vaultProvider));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _credentialStores = credentialStores ?? throw new ArgumentNullException(nameof(credentialStores));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _logger = logger ?? NullLogger<SqlPersonalAccessTokenService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PersonalAccessTokenCreatedResult>> CreateToken(
        Guid userId, string label, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<PersonalAccessTokenCreatedResult>();

        // Why: the raw token is minted here (the edge owns the generator + environment); the vault only
        // peppers and stores it, returning the new token id.
        var rawToken = _generator.Generate(_credentialPrefix!, _environment!);
        var createResult = await resolve.Value
            .Create(userId, rawToken, _generator.ExtractPrefix(rawToken), label, expiresAt, _maxTokensPerUser, cancellationToken)
            .ConfigureAwait(false);
        if (!createResult.IsSuccess)
            return createResult.ToNewResult<PersonalAccessTokenCreatedResult>();

        // Why: the raw token is returned exactly once; after this only the peppered hash is stored.
        return GenericResult<PersonalAccessTokenCreatedResult>.Success(new PersonalAccessTokenCreatedResult
        {
            TokenId = createResult.Value,
            RawToken = rawToken,
            Prefix = _generator.ExtractPrefix(rawToken),
            Label = label,
            ExpiresAt = expiresAt,
        });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PersonalAccessTokenValidationResult>> ValidateToken(
        string rawToken, CancellationToken cancellationToken = default)
    {
        // Credential validation runs before the request has an authenticated principal.
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<PersonalAccessTokenValidationResult>();

        return await resolve.Value.Validate(rawToken, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<PersonalAccessTokenSummary>>> ListTokens(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<IReadOnlyList<PersonalAccessTokenSummary>>();

        return await resolve.Value.List(userId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RevokeToken(
        Guid userId, Guid tokenId, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve;

        return await resolve.Value.Revoke(userId, tokenId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RevokeAllTokens(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve;

        return await resolve.Value.RevokeAll(userId, cancellationToken).ConfigureAwait(false);
    }

    // Why: resolve the vault + policy once and cache. A blank credential-service name, missing typed
    // body, blank vault name, blank credential prefix, blank environment, non-positive limit, or a resolved vault that is not an
    // IPatVault all fail loud — never a fallback.
    private async Task<IGenericResult<IPatVault>> Resolve(CancellationToken cancellationToken)
    {
        if (_vault is not null)
            return GenericResult<IPatVault>.Success(_vault);

        await _resolveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_vault is not null)
                return GenericResult<IPatVault>.Success(_vault);

            var configResult = await ResolveConfig(cancellationToken).ConfigureAwait(false);
            if (!configResult.IsSuccess || configResult.Value is null)
                return configResult.ToNewResult<IPatVault>();
            var config = configResult.Value;

            var vaultName = config.CredentialVaultName;
            if (string.IsNullOrWhiteSpace(vaultName))
                return GenericResult<IPatVault>.Failure(PersonalAccessTokenLog.VaultNameMissing(_logger));

            if (string.IsNullOrWhiteSpace(config.CredentialPrefix))
                return GenericResult<IPatVault>.Failure(PersonalAccessTokenLog.CredentialPrefixMissing(_logger));

            if (string.IsNullOrWhiteSpace(config.Environment))
                return GenericResult<IPatVault>.Failure(PersonalAccessTokenLog.EnvironmentMissing(_logger));

            if (config.MaxTokensPerUser <= 0)
                return GenericResult<IPatVault>.Failure(PersonalAccessTokenLog.MaxTokensInvalid(_logger, config.MaxTokensPerUser));

            var vaultResult = await _vaultProvider
                .Get(new DataVaultRequest(null, vaultName), cancellationToken)
                .ConfigureAwait(false);
            if (!vaultResult.IsSuccess || vaultResult.Value is null)
                return GenericResult<IPatVault>.Failure(PersonalAccessTokenLog.VaultResolveFailed(_logger, vaultName!));

            if (vaultResult.Value is not IPatVault patVault)
                return GenericResult<IPatVault>.Failure(PersonalAccessTokenLog.VaultResolveFailed(_logger, vaultName!));

            _credentialPrefix = config.CredentialPrefix;
            _environment = config.Environment;
            _maxTokensPerUser = config.MaxTokensPerUser;
            _vault = patVault;
            return GenericResult<IPatVault>.Success(_vault);
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    // Why: pointer-based resolution (NOT a scan) — the configured credential service NAME selects
    // exactly one typed configuration row via the provider; a blank name or a Get miss fails loud.
    private async Task<IGenericResult<SqlCredentialServiceConfiguration>> ResolveConfig(CancellationToken cancellationToken)
    {
        var name = CredentialServiceName();
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.CredentialServiceNameMissing(_logger));

        var headerResult = await _configProvider.Get(name!, cancellationToken).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null)
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.CredentialServiceResolveFailed(_logger, name!));

        if (headerResult.Value is not SqlCredentialServiceConfiguration typed)
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.TypedBodyMissing(_logger, name!));

        return GenericResult<SqlCredentialServiceConfiguration>.Success(typed);
    }

    /// <inheritdoc />
    public void Dispose() => _resolveLock.Dispose();

    // Why read here: the selector is a row, and the service it names is a row too.
    private string? CredentialServiceName()
    {
        // Read through the credential-store domain by its name. CredentialsSql has no Name of its own: it
        // is the Sql implementation of the CredentialStore domain row and is composed onto it, so the
        // domain read is what carries it. (FDW-731)
#pragma warning disable VSTHRD002
        var result = _credentialStores.Get(CredentialStoreName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        return result is { IsSuccess: true, Value: ICredentialsSqlImplementationConfiguration store }
            ? store.CredentialServiceName
            : null;
    }
}
