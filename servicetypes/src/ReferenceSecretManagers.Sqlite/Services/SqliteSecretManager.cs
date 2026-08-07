using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using ReferenceSecretManagers.Sqlite.Handlers;
using ReferenceSecretManagers.Sqlite.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite.Services;

/// <summary>
/// SQLite implementation of the secret manager service.
/// Stores and retrieves secrets from a SQLite database file.
/// </summary>
/// <remarks>
/// Supports GetSecret, SetSecret, DeleteSecret, and ListSecrets operations.
/// Uses the version-on-write pattern with IsCurrent/IsDeleted flags for soft delete and versioning.
/// Secrets are stored as plain text, matching the MsSqlSecretManager / UserSecrets precedent.
/// File-system permissions are the security boundary for local-dev use.
/// </remarks>
public sealed class SqliteSecretManager : ISecretManager, IDisposable
{
    private readonly ILogger<SqliteSecretManager> _logger;
    private readonly SqliteSecretManagerConfiguration _configuration;
    private readonly SqliteExecutionContext _executionContext;
    private readonly string _id;
    private readonly string _name;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <param name="configuration">The SQLite secret manager configuration.</param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>SqliteSecretManagerConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the factory threads the real name through this parameter.
    /// </param>
    public SqliteSecretManager(
        ILogger<SqliteSecretManager> logger,
        SqliteSecretManagerConfiguration configuration,
        string secretManagerName = "")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = configuration.Id.ToString();
        _name = secretManagerName;

        _executionContext = new SqliteExecutionContext(logger, configuration, _id, secretManagerName);

        SqliteSecretManagerLogger.ManagerInitialized(_logger, configuration.TableName, configuration.DataSource);
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc/>
    public string ServiceType => "Sqlite";

    /// <inheritdoc/>
    public bool IsAvailable => !_disposed;

    /// <inheritdoc/>
    public Task<IGenericResult<object?>> Execute(
        ISecretManagerCommand managementCommand,
        CancellationToken cancellationToken = default)
    {
        SqliteSecretManagerLogger.TraceExecuteEntry(_logger, managementCommand?.CommandType ?? "null");

        if (managementCommand == null)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(SqliteSecretManagerLogger.CommandNull(_logger)));
        }

        SqliteSecretManagerLogger.ExecutingCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        return ExecuteCommandInternal(managementCommand, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TResult>> Execute<TResult>(
        ISecretManagerCommand<TResult> managementCommand,
        CancellationToken cancellationToken = default)
    {
        if (managementCommand == null)
        {
            return GenericResult<TResult>.Failure(SqliteSecretManagerLogger.CommandNull(_logger));
        }

        SqliteSecretManagerLogger.ExecutingTypedCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        var result = await ExecuteCommandInternal(managementCommand, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.ToNewResult<TResult>();
        }

        if (result.Value is TResult typedValue)
        {
            return result.ToNewResult(typedValue);
        }

        return GenericResult<TResult>.Failure(
            SqliteSecretManagerLogger.ServiceTypeMismatch(_logger, typeof(TResult).Name));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> ExecuteBatch(
        IReadOnlyList<ISecretManagerCommand> commands,
        CancellationToken cancellationToken = default)
    {
        SqliteSecretManagerLogger.TraceExecuteBatchEntry(_logger, commands?.Count ?? 0);

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        if (commands.Count == 0)
        {
            throw new ArgumentException("Commands list cannot be empty", nameof(commands));
        }

        SqliteSecretManagerLogger.ExecutingBatch(_logger, commands.Count);

        var errors = new List<IGenericMessage>();
        var successCount = 0;

        foreach (var command in commands)
        {
            var result = await ExecuteSingleCommand(command, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                successCount++;
            }
            else
            {
                errors.AddRange(result.Messages);
            }
        }

        if (errors.Count > 0)
        {
            return GenericResult.Failure(
                SqliteSecretManagerLogger.BatchExecutionFailed(_logger,
                    $"{errors.Count} of {commands.Count} commands failed"));
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> ExecuteSingleCommand(
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteCommandInternal(command, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SqliteSecretManagerLogger.BatchOperationException(_logger, ex, command.CommandType,
                    command.SecretKey ?? "N/A", ex.Message));
        }
    }

    /// <inheritdoc/>
    public IGenericResult ValidateCommand(ISecretManagerCommand managementCommand)
    {
        if (managementCommand == null)
        {
            throw new ArgumentNullException(nameof(managementCommand));
        }

        SqliteSecretManagerLogger.TraceValidateCommandEntry(_logger, managementCommand.CommandType ?? "null");

        if (string.IsNullOrWhiteSpace(managementCommand.CommandType))
        {
            return GenericResult.Failure(SqliteSecretManagerLogger.CommandTypeRequired(_logger));
        }

        var handler = SqliteCommandHandlers.ByName(managementCommand.CommandType);

        if (ReferenceEquals(handler, SqliteCommandHandlers.NotFound))
        {
            return GenericResult.Failure(
                SqliteSecretManagerLogger.UnknownCommandType(_logger, managementCommand.CommandType));
        }

        return handler.Validate(managementCommand);
    }

    /// <inheritdoc/>
    Task<IGenericResult<TOut>> IGenericService.Execute<TOut>(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is not ISecretManagerCommand secretCommand)
        {
            return Task.FromResult(
                GenericResult<TOut>.Failure(SqliteSecretManagerLogger.InvalidCommandType(_logger, nameof(ISecretManagerCommand))));
        }

        if (secretCommand is ISecretManagerCommand<TOut> typedCommand)
        {
            return Execute(typedCommand, cancellationToken);
        }

        return ExecuteAndCast<TOut>(secretCommand, cancellationToken);
    }

    /// <inheritdoc/>
    async Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is not ISecretManagerCommand secretCommand)
        {
            return GenericResult.Failure(SqliteSecretManagerLogger.InvalidCommandType(_logger, nameof(ISecretManagerCommand)));
        }

        var result = await Execute(secretCommand, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result;
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }

    private async Task<IGenericResult<object?>> ExecuteCommandInternal(
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = SqliteCommandHandlers.ByName(command.CommandType);

            if (ReferenceEquals(handler, SqliteCommandHandlers.NotFound))
            {
                return GenericResult<object?>.Failure(
                    SqliteSecretManagerLogger.UnknownCommandType(_logger, command.CommandType));
            }

            return await ExecuteViaHandler(handler, command, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            return GenericResult<object?>.Failure(
                SqliteSecretManagerLogger.ConnectionFailed(_logger, ex, _configuration.DataSource, ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<object?>.Failure(
                SqliteSecretManagerLogger.OperationFailed(_logger, ex, command.CommandType,
                    command.SecretKey ?? "N/A", ex.Message));
        }
    }

    private Task<IGenericResult<object?>> ExecuteViaHandler(
        ISecretManagerCommandHandler handler,
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
        => handler.InvokeBoxed(command, _executionContext, cancellationToken);

    private async Task<IGenericResult<TOut>> ExecuteAndCast<TOut>(
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommandInternal(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.ToNewResult<TOut>();
        }

        if (result.Value is TOut typedValue)
        {
            return result.ToNewResult(typedValue);
        }

        return GenericResult<TOut>.Failure(
            SqliteSecretManagerLogger.ServiceTypeMismatch(_logger, typeof(TOut).Name));
    }
}
