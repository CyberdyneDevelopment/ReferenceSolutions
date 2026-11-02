using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.DataVault.Abstractions;
using FdwReferenceCredentials.Sql.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceCredentials.Sql.Services;

/// <summary>
/// Vault-backed set-password links over <c>auth.PasswordLink</c> (FDW-941). The raw token is minted here,
/// returned once, and only its peppered hash is stored -- the peppering happens inside the resolved
/// <see cref="IPasswordLinkVault"/>, the same vault personal access tokens live in.
/// </summary>
/// <remarks>
/// The vault is resolved lazily on first use from the typed <see cref="SqlCredentialServiceConfiguration"/>
/// row the credential store names, exactly as <see cref="SqlPersonalAccessTokenService"/> resolves it.
/// Token values are never logged.
/// </remarks>
public sealed class SqlPasswordLinkService : IPasswordLinkService, IDisposable
{
    // The credential-store domain row this host reads its selector through.
    private const string CredentialStoreName = "CredentialStore";

    // 32 random bytes: 256 bits, so a link cannot be guessed in any number of tries the redeem endpoint allows.
    private const int TokenBytes = 32;

    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IDataVaultProvider _vaultProvider;
    private readonly ICredentialStoreConfigurationProvider _credentialStores;
    private readonly CredentialServiceConfigurationProvider _configProvider;
    private readonly ILogger<SqlPasswordLinkService> _logger;

    // Why: the vault is resolved once on first use and cached -- it is a system-lifetime singleton and its
    // name does not change at runtime (a restart picks up edits).
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private IPasswordLinkVault? _vault;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlPasswordLinkService"/> class.
    /// </summary>
    public SqlPasswordLinkService(
        IDataVaultProvider vaultProvider,
        ICredentialStoreConfigurationProvider credentialStores,
        CredentialServiceConfigurationProvider configProvider,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<SqlPasswordLinkService>? logger = null)
    {
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _vaultProvider = vaultProvider ?? throw new ArgumentNullException(nameof(vaultProvider));
        _credentialStores = credentialStores ?? throw new ArgumentNullException(nameof(credentialStores));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _logger = logger ?? NullLogger<SqlPasswordLinkService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PasswordLinkCreatedResult>> CreateLink(
        Guid userId, Guid createdBy, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<PasswordLinkCreatedResult>();

        // Why URL-safe Base64: the token travels in a link, so it must survive one without escaping.
        var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        var created = await resolve.Value
            .Create(userId, rawToken, expiresAt, createdBy, cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess)
            return created.ToNewResult<PasswordLinkCreatedResult>();

        // Why: the raw token is returned exactly once; after this only its peppered hash is stored.
        return GenericResult<PasswordLinkCreatedResult>.Success(new PasswordLinkCreatedResult
        {
            LinkId = created.Value,
            RawToken = rawToken,
            ExpiresAt = expiresAt,
        });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PasswordLinkRedemption>> Redeem(string rawToken, CancellationToken cancellationToken = default)
    {
        // Redemption runs before the request has an authenticated principal: the link is the authority.
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<PasswordLinkRedemption>();

        return await resolve.Value.Redeem(rawToken, cancellationToken).ConfigureAwait(false);
    }

    // Why: resolve the vault once and cache. A blank credential-service name, a missing typed body, a
    // blank vault name, or a resolved vault that is not an IPasswordLinkVault all fail loud -- never a
    // fallback.
    private async Task<IGenericResult<IPasswordLinkVault>> Resolve(CancellationToken cancellationToken)
    {
        if (_vault is not null)
            return GenericResult<IPasswordLinkVault>.Success(_vault);

        await _resolveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_vault is not null)
                return GenericResult<IPasswordLinkVault>.Success(_vault);

            var configResult = await ResolveConfig(cancellationToken).ConfigureAwait(false);
            if (!configResult.IsSuccess || configResult.Value is null)
                return configResult.ToNewResult<IPasswordLinkVault>();

            var vaultName = configResult.Value.CredentialVaultName;
            if (string.IsNullOrWhiteSpace(vaultName))
                return GenericResult<IPasswordLinkVault>.Failure(PasswordLinkLog.VaultNameMissing(_logger));

            var vaultResult = await _vaultProvider
                .Get(new DataVaultRequest(null, vaultName), cancellationToken)
                .ConfigureAwait(false);
            if (!vaultResult.IsSuccess || vaultResult.Value is not IPasswordLinkVault linkVault)
                return GenericResult<IPasswordLinkVault>.Failure(PasswordLinkLog.VaultResolveFailed(_logger, vaultName));

            _vault = linkVault;
            return GenericResult<IPasswordLinkVault>.Success(_vault);
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    // Why: pointer-based resolution (NOT a scan) -- the configured credential service NAME selects
    // exactly one typed configuration row via the provider; a blank name or a Get miss fails loud.
    private async Task<IGenericResult<SqlCredentialServiceConfiguration>> ResolveConfig(CancellationToken cancellationToken)
    {
        var name = CredentialServiceName();
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.CredentialServiceNameMissing(_logger));

        var headerResult = await _configProvider.Get(name, cancellationToken).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null)
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.CredentialServiceResolveFailed(_logger, name));

        if (headerResult.Value is not SqlCredentialServiceConfiguration typed)
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.TypedBodyMissing(_logger, name));

        return GenericResult<SqlCredentialServiceConfiguration>.Success(typed);
    }

    /// <inheritdoc />
    public void Dispose() => _resolveLock.Dispose();

    // Why read here: the selector is a row, and the service it names is a row too.
    private string? CredentialServiceName()
    {
        // Read through the credential-store domain by its name, as SqlPersonalAccessTokenService does:
        // CredentialsSql has no Name of its own and is composed onto the CredentialStore domain row.
#pragma warning disable VSTHRD002
        var result = _credentialStores.Get(CredentialStoreName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        return result is { IsSuccess: true, Value: ICredentialsSqlImplementationConfiguration store }
            ? store.CredentialServiceName
            : null;
    }
}
