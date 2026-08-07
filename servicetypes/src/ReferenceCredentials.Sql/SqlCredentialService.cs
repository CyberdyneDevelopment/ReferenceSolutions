using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CoreConfig = Fdw.Services.Credentials;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Sql.Options;
using Fdw.Services.Credentials.Sql.Outcomes;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Logging;
using Fdw.Services;
using Fdw;

namespace ReferenceCredentials.Sql;

/// <summary>
/// SQL-backed implementation of <see cref="ICredentialService"/>. A thin, named indirection in front of
/// a credential <see cref="ICredentialVault"/>: it resolves its configured vault once (by
/// <see cref="SqlCredentialServiceConfiguration.CredentialVaultName"/>), casts it to the narrow
/// <see cref="ICredentialVault"/>, and forwards each semantic verb to it.
/// </summary>
/// <remarks>
/// The service owns NO hashing logic — peppering/compare happen inside the vault, the only hash-bearing
/// plane. Consumers (the Users edge) resolve this service by name and call its verbs, mirroring how a
/// connection resolves a secret manager by name. Inputs are already-derived hashes; no plaintext or
/// hash material crosses this boundary.
/// </remarks>
public sealed class SqlCredentialService : ICredentialService, IDisposable
{
    private readonly CoreConfig.CredentialServiceConfiguration _configuration;
    private readonly IDataVaultProvider _vaultProvider;
    private readonly ILogger<SqlCredentialService> _logger;

    // Why: the vault is resolved once lazily so a missing/misconfigured vault name surfaces as a
    // structured failure on first use rather than crashing the DI container at startup.
    private readonly SemaphoreSlim _vaultLock = new(1, 1);
    private ICredentialVault? _vault;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCredentialService"/> class.
    /// </summary>
    /// <param name="configuration">The credential service header configuration (including typed body).</param>
    /// <param name="vaultProvider">Provider for resolving the credential vault by name.</param>
    /// <param name="logger">Optional logger; falls back to NullLogger if not supplied.</param>
    public SqlCredentialService(
        CoreConfig.CredentialServiceConfiguration configuration,
        IDataVaultProvider vaultProvider,
        ILogger<SqlCredentialService>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _vaultProvider = vaultProvider ?? throw new ArgumentNullException(nameof(vaultProvider));
        _logger = logger ?? NullLogger<SqlCredentialService>.Instance;
    }

    // ========================================
    // IGenericService — required contract
    // ========================================

    /// <inheritdoc />
    string IGenericService.Id => _configuration.Id.ToString();

    /// <inheritdoc />
    string IServiceOption.Name => _configuration.Name;

    /// <inheritdoc />
    string IGenericService.ServiceType => "CredentialService";

    /// <inheritdoc />
    bool IGenericService.IsAvailable => _vault is not null || _configuration.Configuration is not null;

    /// <inheritdoc />
    // Why: a credential service exposes ONLY its semantic verbs — a generic command surface is rejected
    // fail-loud, keeping the access policy closed (mirrors the vault).
    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(CredentialServiceLog.CommandNull(_logger)));

    /// <inheritdoc />
    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(CredentialServiceLog.CommandNull(_logger)));

    // ========================================
    // ICredentialService — semantic verbs (forwarded to the configured vault)
    // ========================================

    /// <inheritdoc />
    public async Task<IGenericResult<ICredentialOutcome>> Validate(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default)
    {
        var vaultResult = await ResolveVault(cancellationToken).ConfigureAwait(false);
        if (!vaultResult.IsSuccess || vaultResult.Value is null)
            return vaultResult.ToNewResult<ICredentialOutcome>();

        return await vaultResult.Value.Validate(userId, derivedHash, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Create(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default)
    {
        var vaultResult = await ResolveVault(cancellationToken).ConfigureAwait(false);
        if (!vaultResult.IsSuccess || vaultResult.Value is null)
            return vaultResult;

        return await vaultResult.Value.Create(userId, derivedHash, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Change(Guid userId, byte[] oldDerivedHash, byte[] newDerivedHash, CancellationToken cancellationToken = default)
    {
        var vaultResult = await ResolveVault(cancellationToken).ConfigureAwait(false);
        if (!vaultResult.IsSuccess || vaultResult.Value is null)
            return vaultResult;

        return await vaultResult.Value.Change(userId, oldDerivedHash, newDerivedHash, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Disable(Guid userId, CancellationToken cancellationToken = default)
    {
        var vaultResult = await ResolveVault(cancellationToken).ConfigureAwait(false);
        if (!vaultResult.IsSuccess || vaultResult.Value is null)
            return vaultResult;

        return await vaultResult.Value.Disable(userId, cancellationToken).ConfigureAwait(false);
    }

    // Why: Resolves and caches the service's ONE credential vault. The SemaphoreSlim prevents duplicate
    // resolution under concurrent first-access. The vault name comes from the typed body — a missing
    // typed body, blank vault name, or a resolved vault that is not an ICredentialVault fails loud.
    private async Task<IGenericResult<ICredentialVault>> ResolveVault(CancellationToken cancellationToken)
    {
        if (_vault is not null)
            return GenericResult<ICredentialVault>.Success(_vault);

        await _vaultLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_vault is not null)
                return GenericResult<ICredentialVault>.Success(_vault);

            if (_configuration.Configuration is not SqlCredentialServiceConfiguration typedBody)
                return GenericResult<ICredentialVault>.Failure(
                    CredentialServiceLog.TypedBodyMissing(_logger, _configuration.Name));

            var vaultName = typedBody.CredentialVaultName;
            if (string.IsNullOrWhiteSpace(vaultName))
                return GenericResult<ICredentialVault>.Failure(
                    CredentialServiceLog.CredentialVaultNameMissing(_logger, _configuration.Name));

            var vaultResult = await _vaultProvider
                .Get(new DataVaultRequest(null, vaultName), cancellationToken)
                .ConfigureAwait(false);

            if (!vaultResult.IsSuccess || vaultResult.Value is null)
                return GenericResult<ICredentialVault>.Failure(
                    CredentialServiceLog.CredentialVaultResolveFailed(_logger, vaultName!, _configuration.Name));

            if (vaultResult.Value is not ICredentialVault credentialVault)
                return GenericResult<ICredentialVault>.Failure(
                    CredentialServiceLog.VaultNotCredentialVault(_logger, vaultName!, _configuration.Name));

            CredentialServiceLog.CredentialVaultResolved(_logger, vaultName!, _configuration.Name);
            _vault = credentialVault;
            return GenericResult<ICredentialVault>.Success(_vault);
        }
        finally
        {
            _vaultLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _vaultLock.Dispose();
}
