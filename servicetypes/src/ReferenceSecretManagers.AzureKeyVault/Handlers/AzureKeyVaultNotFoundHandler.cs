using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Handlers;

/// <summary>
/// NotFound sentinel handler for Azure Key Vault command lookup.
/// </summary>
[TypeOption(typeof(AzureKeyVaultCommandHandlers), "NotFound")]
[ExcludeFromCodeCoverage]
public sealed class AzureKeyVaultNotFoundHandler : ISecretManagerCommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultNotFoundHandler"/> class.
    /// </summary>
    public AzureKeyVaultNotFoundHandler()
    {
        ExecuteFunc = new Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<IGenericResult<object?>>>(
            (cmd, ctx, ct) => Task.FromResult(
                GenericResult<object?>.Failure(new ErrorMessage($"No handler found for command type '{cmd?.CommandType ?? "null"}'"))));
    }

    /// <inheritdoc />
    public int Id => 0;

    /// <inheritdoc />
    public string Name => "NotFound";

    /// <inheritdoc />
    public Type CommandTypeClass => typeof(void);

    /// <inheritdoc />
    public Type ResultType => typeof(void);

    /// <inheritdoc />
    public Delegate ExecuteFunc { get; }

    /// <inheritdoc />
    public Task<IGenericResult<object?>> InvokeBoxed(
        ISecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
        => ((Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<IGenericResult<object?>>>)ExecuteFunc)(command, context, cancellationToken);

    /// <inheritdoc />
    public IGenericResult Validate(ISecretManagerCommand command)
    {
        return GenericResult.Failure(new ErrorMessage("Cannot execute NotFound handler"));
    }
}
