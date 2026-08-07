using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Security.Hashing;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql.Configuration;
using ReferenceCredentials.Sql.Logging;
using Fdw.Services.Credentials.Sql.Options;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Services.Credentials.Sql.Outcomes;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Logging;
using Fdw.Services;
using Fdw;

namespace ReferenceCredentials.Sql.Services;

/// <summary>
/// Vault-backed agent key service over <c>auth.AgentKey</c>. The raw key is minted here, returned once,
/// and only its peppered hash is stored — the peppering happens inside the resolved
/// <see cref="IAgentKeyVault"/>.
/// </summary>
/// <remarks>
/// Agent-key grant validation (the agent_key grant type) is handled by the Users credential edge — not
/// by this service, which covers the create/list/delete lifecycle only. The credential vault name is
/// resolved on first use from the typed <see cref="SqlCredentialServiceConfiguration"/> row selected by
/// <see cref="CredentialsSqlOptions.CredentialServiceName"/>. Key values are never logged.
/// </remarks>
public sealed class SqlAgentKeyService : IAgentKeyService, IDisposable
{
    // Why: embedded in generated raw keys so an agent key is visually distinct from a user PAT
    // (matches the retired CreateKeyCommand's segment).
    private const string KeyEnvironmentSegment = "agent";

    private readonly IDataVaultProvider _vaultProvider;
    private readonly IPersonalAccessTokenGenerator _generator;
    private readonly IOptions<CredentialsSqlOptions> _credentialsSqlOptions;
    private readonly CredentialServiceConfigurationProvider _configProvider;
    private readonly ILogger<SqlAgentKeyService> _logger;

    // Why: vault is resolved once lazily so a missing/misconfigured vault name surfaces as a structured
    // failure on first use rather than crashing the DI container at startup.
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private IAgentKeyVault? _vault;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlAgentKeyService"/> class.
    /// </summary>
    public SqlAgentKeyService(
        IDataVaultProvider vaultProvider,
        IPersonalAccessTokenGenerator generator,
        IOptions<CredentialsSqlOptions> credentialsSqlOptions,
        CredentialServiceConfigurationProvider configProvider,
        ILogger<SqlAgentKeyService>? logger = null)
    {
        _vaultProvider = vaultProvider ?? throw new ArgumentNullException(nameof(vaultProvider));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _credentialsSqlOptions = credentialsSqlOptions ?? throw new ArgumentNullException(nameof(credentialsSqlOptions));
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
        var rawKey = _generator.Generate(KeyEnvironmentSegment);
        var createResult = await resolve.Value
            .Create(userId, userName, rawKey, label, expiresAt, cancellationToken)
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
    // vault name, or a resolved vault that is not an IAgentKeyVault all fail loud — never a fallback.
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

            var vaultResult = await _vaultProvider
                .Get(new DataVaultRequest(null, vaultName), cancellationToken)
                .ConfigureAwait(false);
            if (!vaultResult.IsSuccess || vaultResult.Value is null)
                return GenericResult<IAgentKeyVault>.Failure(AgentKeyLog.VaultResolveFailed(_logger, vaultName!));

            if (vaultResult.Value is not IAgentKeyVault agentKeyVault)
                return GenericResult<IAgentKeyVault>.Failure(AgentKeyLog.VaultResolveFailed(_logger, vaultName!));

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
        var name = _credentialsSqlOptions.Value.CredentialServiceName;
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.CredentialServiceNameMissing(_logger));

        var headerResult = await _configProvider.Get(name!, cancellationToken).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null)
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.CredentialServiceResolveFailed(_logger, name!));

        if (headerResult.Value.Configuration is not SqlCredentialServiceConfiguration typed)
            return GenericResult<SqlCredentialServiceConfiguration>.Failure(
                CredentialServiceLog.TypedBodyMissing(_logger, name!));

        return GenericResult<SqlCredentialServiceConfiguration>.Success(typed);
    }

    /// <inheritdoc />
    public void Dispose() => _resolveLock.Dispose();
}
