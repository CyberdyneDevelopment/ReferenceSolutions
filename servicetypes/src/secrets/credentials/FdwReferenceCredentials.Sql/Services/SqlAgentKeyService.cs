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
/// Vault-backed agent key service over <c>auth.AgentKey</c>. The raw key is minted here, returned once,
/// and only its peppered hash is stored — the peppering happens inside the resolved
/// <see cref="IAgentKeyVault"/>.
/// </summary>
/// <remarks>
/// Agent-key grant validation (the agent_key grant type) is handled by the Users credential edge — not
/// by this service, which covers the create/list/delete lifecycle only. The credential vault name is
/// resolved on first use from the typed <see cref="SqlCredentialServiceConfiguration"/> row selected by
/// <see cref="ICredentialsSqlImplementationConfiguration.CredentialServiceName"/>. Key values are never logged.
/// </remarks>
public sealed class SqlAgentKeyService : IAgentKeyService, IDisposable
{
    // Why: embedded in generated raw keys so an agent key is visually distinct from a user PAT
    // (matches the retired CreateKeyCommand's segment).
    private const string KeyEnvironmentSegment = "agent";

    // The credential-store domain row this host reads its selector through.
    private const string CredentialStoreName = "CredentialStore";

    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IDataVaultProvider _vaultProvider;
    private readonly IPersonalAccessTokenGenerator _generator;
    private readonly ICredentialStoreConfigurationProvider _credentialStores;
    private readonly CredentialServiceConfigurationProvider _configProvider;
    private readonly ILogger<SqlAgentKeyService> _logger;

    // Why: vault is resolved once lazily so a missing/misconfigured vault name surfaces as a structured
    // failure on first use rather than crashing the DI container at startup.
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private IAgentKeyVault? _vault;
    private string? _credentialPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlAgentKeyService"/> class.
    /// </summary>
    public SqlAgentKeyService(
        IDataVaultProvider vaultProvider,
        IPersonalAccessTokenGenerator generator,
        ICredentialStoreConfigurationProvider credentialStores,
        CredentialServiceConfigurationProvider configProvider,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<SqlAgentKeyService>? logger = null)
    {
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _vaultProvider = vaultProvider ?? throw new ArgumentNullException(nameof(vaultProvider));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _credentialStores = credentialStores ?? throw new ArgumentNullException(nameof(credentialStores));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _logger = logger ?? NullLogger<SqlAgentKeyService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AgentKeyCreatedResult>> CreateKey(
        Guid userId, string userName, string label, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<AgentKeyCreatedResult>();

        // Why: the raw key is minted here (the edge owns the generator); the vault only peppers and
        // stores it, returning the new key id.
        var rawKey = _generator.Generate(_credentialPrefix!, KeyEnvironmentSegment);
        var createResult = await resolve.Value
            .Create(userId, userName, rawKey, _generator.ExtractPrefix(rawKey), label, expiresAt, cancellationToken)
            .ConfigureAwait(false);
        if (!createResult.IsSuccess)
            return createResult.ToNewResult<AgentKeyCreatedResult>();

        // Why: the raw key is returned exactly once; after this only the peppered hash is stored.
        return GenericResult<AgentKeyCreatedResult>.Success(new AgentKeyCreatedResult
        {
            KeyId = createResult.Value,
            RawKey = rawKey,
            Prefix = _generator.ExtractPrefix(rawKey),
            Label = label,
            ExpiresAt = expiresAt,
        });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AgentKeyValidationResult>> ValidateKey(
        string rawKey, CancellationToken cancellationToken = default)
    {
        // Credential validation runs before the request has an authenticated principal.
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<AgentKeyValidationResult>();

        return await resolve.Value.Validate(rawKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>The format is the configured credential prefix followed by the agent segment (FDW-939).</remarks>
    public async Task<IGenericResult<bool>> IsAgentKey(string rawKey, CancellationToken cancellationToken = default)
    {
        // Runs before the request has an authenticated principal, like ValidateKey.
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<bool>();

        return GenericResult<bool>.Success(
            rawKey.StartsWith(_credentialPrefix + KeyEnvironmentSegment + "_", StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AgentKeySummary>>> ListKeys(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve.ToNewResult<IReadOnlyList<AgentKeySummary>>();

        return await resolve.Value.List(userId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteKey(
        Guid userId, Guid keyId, CancellationToken cancellationToken = default)
    {
        var resolve = await Resolve(cancellationToken).ConfigureAwait(false);
        if (!resolve.IsSuccess || resolve.Value is null)
            return resolve;

        return await resolve.Value.Delete(userId, keyId, cancellationToken).ConfigureAwait(false);
    }

    // Why: resolve the vault once and cache. A blank credential-service name, missing typed body, blank
    // vault name, blank credential prefix, or a resolved vault that is not an IAgentKeyVault all fail loud — never a fallback.
    private async Task<IGenericResult<IAgentKeyVault>> Resolve(CancellationToken cancellationToken)
    {
        if (_vault is not null)
            return GenericResult<IAgentKeyVault>.Success(_vault);

        await _resolveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_vault is not null)
                return GenericResult<IAgentKeyVault>.Success(_vault);

            var configResult = await ResolveConfig(cancellationToken).ConfigureAwait(false);
            if (!configResult.IsSuccess || configResult.Value is null)
                return configResult.ToNewResult<IAgentKeyVault>();

            var vaultName = configResult.Value.CredentialVaultName;
            if (string.IsNullOrWhiteSpace(vaultName))
                return GenericResult<IAgentKeyVault>.Failure(AgentKeyLog.VaultNameMissing(_logger));

            if (string.IsNullOrWhiteSpace(configResult.Value.CredentialPrefix))
                return GenericResult<IAgentKeyVault>.Failure(AgentKeyLog.CredentialPrefixMissing(_logger));

            var vaultResult = await _vaultProvider
                .Get(new DataVaultRequest(null, vaultName), cancellationToken)
                .ConfigureAwait(false);
            if (!vaultResult.IsSuccess || vaultResult.Value is null)
                return GenericResult<IAgentKeyVault>.Failure(AgentKeyLog.VaultResolveFailed(_logger, vaultName!));

            if (vaultResult.Value is not IAgentKeyVault agentKeyVault)
                return GenericResult<IAgentKeyVault>.Failure(AgentKeyLog.VaultResolveFailed(_logger, vaultName!));

            _credentialPrefix = configResult.Value.CredentialPrefix;
            _vault = agentKeyVault;
            return GenericResult<IAgentKeyVault>.Success(_vault);
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
