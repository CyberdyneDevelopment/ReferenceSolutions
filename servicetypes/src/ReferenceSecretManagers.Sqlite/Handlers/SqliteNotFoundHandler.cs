using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Handlers;

/// <summary>
/// NotFound sentinel handler for SQLite command lookup.
/// </summary>
[TypeOption(typeof(SqliteCommandHandlers), "NotFound")]
[ExcludeFromCodeCoverage]
public sealed class SqliteNotFoundHandler : ISecretManagerCommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteNotFoundHandler"/> class.
    /// </summary>
    public SqliteNotFoundHandler()
    {
        ExecuteFunc = new Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<IGenericResult<object?>>>(
            (cmd, ctx, ct) => Task.FromResult(
                GenericResult<object?>.Failure(
                    SecretManagerResultCodes.ByName("NoHandlerFound"),
                    ResultDetails.Create().With("CommandType", cmd?.CommandType ?? "null"))));
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
        return GenericResult.Failure(
            SecretManagerResultCodes.ByName("NoHandlerFound"),
            ResultDetails.Create().With("CommandType", command?.CommandType ?? "null"));
    }
}
