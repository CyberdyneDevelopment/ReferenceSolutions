using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SecretManager;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using FdwReferenceSecretManagers.HashiCorpVault.Configuration;
using FdwReferenceSecretManagers.HashiCorpVault.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers;

namespace FdwReferenceSecretManagers.HashiCorpVault;

/// <summary>
/// Reads secrets from HashiCorp Vault — either a stored value from the KV engine, or a freshly
/// issued, lease-bound database credential from the database engine.
/// </summary>
/// <remarks>
/// <para>
/// This is the first concrete <see cref="ISecretManager"/> in FDW; the domain previously shipped
/// abstractions, base classes and configuration POCOs only.
/// </para>
/// <para>
/// Only reads are implemented. Writing, deleting and rotating are refused explicitly rather than
/// silently no-op'd: Vault's write path is governed by policy that lives in Vault, and a secret
/// manager that quietly accepted a write it never performed is worse than one that says it cannot.
/// Dynamic database credentials are not writable at all — Vault mints them.
/// </para>
/// </remarks>
public sealed class HashiCorpVaultSecretManager
    : SecretManagerServiceBase<SecretManagerCommandBase, HashiCorpVaultConfiguration, HashiCorpVaultSecretManager>
{
    private readonly VaultApiClient _vault;
    private readonly VaultReadContext _context;

    /// <summary>Initializes a new instance of the <see cref="HashiCorpVaultSecretManager"/> class.</summary>
    /// <param name="logger">The logger for this secret manager.</param>
    /// <param name="configuration">The typed Vault configuration body.</param>
    /// <param name="vault">The Vault API client.</param>
    /// <param name="context">The resolved read context — which Vault, which engine, how to log in.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="vault"/> or <paramref name="context"/> is null.</exception>
    public HashiCorpVaultSecretManager(
        ILogger<HashiCorpVaultSecretManager>? logger,
        HashiCorpVaultConfiguration configuration,
        VaultApiClient vault,
        VaultReadContext context)
        : base(logger!, configuration)
    {
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<object?>> Execute(ISecretManagerCommand managementCommand, CancellationToken cancellationToken = default)
    {
        var secret = await ReadSecret(managementCommand, cancellationToken).ConfigureAwait(false);
        return secret.IsSuccess
            ? GenericResult<object?>.Success(secret.Value)
            : secret.ToNewResult<object?>();
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<TResult>> Execute<TResult>(ISecretManagerCommand<TResult> managementCommand, CancellationToken cancellationToken = default)
        => ReadAs<TResult>(managementCommand, cancellationToken);

    /// <inheritdoc/>
    public override async Task<IGenericResult> Execute(SecretManagerCommandBase command, CancellationToken cancellationToken = default)
        => await ReadSecret(command, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public override Task<IGenericResult<T>> Execute<T>(SecretManagerCommandBase command, CancellationToken cancellationToken = default)
        => ReadAs<T>(command, cancellationToken);

    /// <inheritdoc/>
    public override Task<IGenericResult> ExecuteBatch(IReadOnlyList<ISecretManagerCommand> commands, CancellationToken cancellationToken = default)
        // Why refused rather than looped: Vault has no batch read, so a "batch" here would be N
        // sequential calls wearing the word. A caller needing several secrets should ask for several
        // and see the cost rather than have it hidden behind a name that implies one round trip.
        => Task.FromResult(GenericResult.Failure(
            VaultLog.OperationNotSupported(Logger, Name, nameof(ExecuteBatch))));

    /// <inheritdoc/>
    public override IGenericResult ValidateCommand(ISecretManagerCommand managementCommand)
    {
        if (managementCommand is null)
            return GenericResult.Failure(VaultLog.ConfigurationValueMissing(Logger, Name, nameof(managementCommand)));

        // Why IsSecretModifying and not a command-name comparison: it is the discriminator the
        // ISecretManagerCommand contract actually exposes, so this stays correct for commands added
        // later without this class having to learn their names.
        if (managementCommand.IsSecretModifying)
            return GenericResult.Failure(VaultLog.OperationNotSupported(Logger, Name, managementCommand.GetType().Name));

        return string.IsNullOrWhiteSpace(managementCommand.SecretKey)
            ? GenericResult.Failure(VaultLog.ConfigurationValueMissing(Logger, Name, nameof(managementCommand.SecretKey)))
            : GenericResult.Success();
    }

    private async Task<IGenericResult<SecretValue>> ReadSecret(ISecretManagerCommand managementCommand, CancellationToken cancellationToken)
    {
        var validated = ValidateCommand(managementCommand);
        return validated.IsFailure
            ? validated.ToNewResult<SecretValue>()
            : await _vault.Read(_context, managementCommand.SecretKey!, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<TResult>> ReadAs<TResult>(ISecretManagerCommand managementCommand, CancellationToken cancellationToken)
    {
        var secret = await ReadSecret(managementCommand, cancellationToken).ConfigureAwait(false);
        if (secret.IsFailure || secret.Value is null)
            return secret.ToNewResult<TResult>();

        return secret.Value is TResult typed
            ? GenericResult<TResult>.Success(typed)
            : GenericResult<TResult>.Failure(
                VaultLog.ResponseIncomplete(Logger, Name, typeof(TResult).Name, nameof(SecretValue)));
    }
}
