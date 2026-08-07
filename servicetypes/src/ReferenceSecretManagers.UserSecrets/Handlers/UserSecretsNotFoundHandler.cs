using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Handlers;

/// <summary>
/// NotFound sentinel handler for User Secrets command lookup.
/// </summary>
[TypeOption(typeof(UserSecretsCommandHandlers), "NotFound")]
[ExcludeFromCodeCoverage]
public sealed class UserSecretsNotFoundHandler : ISecretManagerCommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsNotFoundHandler"/> class.
    /// </summary>
    public UserSecretsNotFoundHandler()
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
