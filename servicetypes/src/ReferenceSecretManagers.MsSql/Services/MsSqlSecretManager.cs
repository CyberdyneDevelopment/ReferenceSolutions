using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Security.Hashing;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using ReferenceSecretManagers.MsSql.Handlers;
using ReferenceSecretManagers.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Services;

/// <summary>
/// MsSql implementation of the secret manager service.
/// Stores and retrieves secrets from a SQL Server database table.
/// </summary>
/// <remarks>
/// Supports GetSecret, SetSecret, DeleteSecret, and ListSecrets operations.
/// Uses the version-on-write pattern with IsCurrent/IsDeleted flags for soft delete and versioning.
/// </remarks>
public sealed class MsSqlSecretManager : ISecretManager, IDisposable
{
    private readonly ILogger<MsSqlSecretManager> _logger;
    private readonly MsSqlSecretManagerConfiguration _configuration;
    private readonly MsSqlExecutionContext _executionContext;
    private readonly string _id;
    private readonly string _name;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <param name="configuration">The MsSql secret manager configuration.</param>
    /// <param name="gateway">The configuration gateway for executing secret commands.</param>
    /// <param name="dataStoreName">
    /// The DataStore name the secret/user/token tables are addressed under (e.g. "ConfigurationDb").
    /// </param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>MsSqlSecretManagerConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the factory threads the real name through this parameter.
    /// </param>
    /// <param name="passwordHasher">Optional password hasher for credential verification.</param>
    /// <param name="tokenHasher">Optional PAT hasher for API key verification.</param>
    /// <param name="tokenGenerator">Optional PAT generator for API key creation.</param>
    /// <param name="hmacKey">Optional HMAC key for API key hashing operations.</param>
    public MsSqlSecretManager(
        ILogger<MsSqlSecretManager> logger,
        MsSqlSecretManagerConfiguration configuration,
        IConfigurationGateway gateway,
        string dataStoreName,
        string secretManagerName = "",
        IPasswordHasher? passwordHasher = null,
        IPersonalAccessTokenHasher? tokenHasher = null,
        IPersonalAccessTokenGenerator? tokenGenerator = null,
        string? hmacKey = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(dataStoreName);
        _id = configuration.Id.ToString();
        _name = secretManagerName;

        _executionContext = new MsSqlExecutionContext(
            logger, configuration, gateway, _id, dataStoreName, secretManagerName,
            passwordHasher, tokenHasher, tokenGenerator, hmacKey);

        MsSqlSecretManagerLogger.ManagerInitialized(_logger, configuration.Schema, configuration.TableName);
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc/>
    public string ServiceType => "MsSql";

    /// <inheritdoc/>
    public bool IsAvailable => !_disposed;

    /// <inheritdoc/>
    public Task<IGenericResult<object?>> Execute(
        ISecretManagerCommand managementCommand,
        CancellationToken cancellationToken = default)
    {
        MsSqlSecretManagerLogger.TraceExecuteEntry(_logger, managementCommand?.CommandType ?? "null");

        if (managementCommand == null)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(MsSqlSecretManagerLogger.CommandNull(_logger)));
        }

        MsSqlSecretManagerLogger.ExecutingCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        return ExecuteCommandInternal(managementCommand, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TResult>> Execute<TResult>(
        ISecretManagerCommand<TResult> managementCommand,
        CancellationToken cancellationToken = default)
    {
        if (managementCommand == null)
        {
            return GenericResult<TResult>.Failure(MsSqlSecretManagerLogger.CommandNull(_logger));
        }

        MsSqlSecretManagerLogger.ExecutingTypedCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

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
            MsSqlSecretManagerLogger.ServiceTypeMismatch(_logger, typeof(TResult).Name));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> ExecuteBatch(
        IReadOnlyList<ISecretManagerCommand> commands,
        CancellationToken cancellationToken = default)
    {
        MsSqlSecretManagerLogger.TraceExecuteBatchEntry(_logger, commands?.Count ?? 0);

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        if (commands.Count == 0)
        {
            throw new ArgumentException("Commands list cannot be empty", nameof(commands));
        }

        MsSqlSecretManagerLogger.ExecutingBatch(_logger, commands.Count);

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
                MsSqlSecretManagerLogger.BatchExecutionFailed(_logger,
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
                MsSqlSecretManagerLogger.BatchOperationException(_logger, ex, command.CommandType,
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

        MsSqlSecretManagerLogger.TraceValidateCommandEntry(_logger, managementCommand.CommandType ?? "null");

        if (string.IsNullOrWhiteSpace(managementCommand.CommandType))
        {
            return GenericResult.Failure(MsSqlSecretManagerLogger.CommandTypeRequired(_logger));
        }

        var handler = MsSqlCommandHandlers.ByName(managementCommand.CommandType);

        if (ReferenceEquals(handler, MsSqlCommandHandlers.NotFound))
        {
            return GenericResult.Failure(
                MsSqlSecretManagerLogger.UnknownCommandType(_logger, managementCommand.CommandType));
        }

        return handler.Validate(managementCommand);
    }

    /// <inheritdoc/>
    Task<IGenericResult<TOut>> IGenericService.Execute<TOut>(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is not ISecretManagerCommand secretCommand)
        {
            return Task.FromResult(
                GenericResult<TOut>.Failure(MsSqlSecretManagerLogger.InvalidCommandType(_logger, nameof(ISecretManagerCommand))));
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
            return GenericResult.Failure(MsSqlSecretManagerLogger.InvalidCommandType(_logger, nameof(ISecretManagerCommand)));
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
            var handler = MsSqlCommandHandlers.ByName(command.CommandType);

            if (ReferenceEquals(handler, MsSqlCommandHandlers.NotFound))
            {
                return GenericResult<object?>.Failure(
                    MsSqlSecretManagerLogger.UnknownCommandType(_logger, command.CommandType));
            }

            return await ExecuteViaHandler(handler, command, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            return GenericResult<object?>.Failure(
                MsSqlSecretManagerLogger.ConnectionFailed(_logger, ex, ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<object?>.Failure(
                MsSqlSecretManagerLogger.OperationFailed(_logger, command.CommandType,
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
            MsSqlSecretManagerLogger.ServiceTypeMismatch(_logger, typeof(TOut).Name));
    }
}
