using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Handlers;
using ReferenceSecretManagers.EnvironmentVariable.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable.Services;

/// <summary>
/// Environment Variable implementation of the secret manager service.
/// Provides read-only secret retrieval from environment variables.
/// </summary>
/// <remarks>
/// This is a read-only secret manager that retrieves secrets from environment variables.
/// It does not support setting or deleting secrets as environment variables are typically
/// managed externally (e.g., by the operating system, container orchestration, or CI/CD pipelines).
/// </remarks>
public sealed class EnvironmentVariableSecretManager : ISecretManager, IDisposable
{
    private readonly ILogger<EnvironmentVariableSecretManager> _logger;
    private readonly EnvironmentVariableConfiguration _configuration;
    private readonly string _id;
    private readonly string _name;
    private readonly EnvironmentVariableExecutionContext _executionContext;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <param name="configuration">The Environment Variable configuration.</param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>EnvironmentVariableConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the factory threads the real name through this parameter.
    /// </param>
    public EnvironmentVariableSecretManager(
        ILogger<EnvironmentVariableSecretManager> logger,
        EnvironmentVariableConfiguration configuration,
        string secretManagerName = "")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = configuration.Id.ToString();
        _name = secretManagerName;
        _executionContext = new EnvironmentVariableExecutionContext(logger, configuration, _id);

        EnvironmentVariableLogger.ManagerInitialized(_logger, configuration.Prefix);
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc/>
    public string ServiceType => "EnvironmentVariable";

    /// <inheritdoc/>
    public bool IsAvailable => !_disposed;

    /// <inheritdoc/>
    public Task<IGenericResult<object?>> Execute(
        ISecretManagerCommand managementCommand,
        CancellationToken cancellationToken = default)
    {
        EnvironmentVariableLogger.TraceExecuteEntry(_logger, managementCommand?.CommandType ?? "null");

        if (managementCommand == null)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(EnvironmentVariableLogger.CommandNull(_logger)));
        }

        EnvironmentVariableLogger.ExecutingCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        return ExecuteCommandInternal(managementCommand, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TResult>> Execute<TResult>(
        ISecretManagerCommand<TResult> managementCommand,
        CancellationToken cancellationToken = default)
    {
        if (managementCommand == null)
        {
            return GenericResult<TResult>.Failure(EnvironmentVariableLogger.CommandNull(_logger));
        }

        EnvironmentVariableLogger.ExecutingTypedCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        var result = await ExecuteCommandInternal(managementCommand, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.ToNewResult<TResult>();
        }

        if (result.Value is TResult typedValue)
        {
            return GenericResult<TResult>.Success(typedValue);
        }

        return GenericResult<TResult>.Failure(new ErrorMessage($"Result is not of type {typeof(TResult).Name}"));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> ExecuteBatch(
        IReadOnlyList<ISecretManagerCommand> commands,
        CancellationToken cancellationToken = default)
    {
        EnvironmentVariableLogger.TraceExecuteBatchEntry(_logger, commands?.Count ?? 0);

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        if (commands.Count == 0)
        {
            throw new ArgumentException("Commands list cannot be empty", nameof(commands));
        }

        EnvironmentVariableLogger.ExecutingBatch(_logger, commands.Count);

        var errors = new List<IGenericMessage>();
        var successCount = 0;

        foreach (var command in commands)
        {
            try
            {
                var result = await ExecuteCommandInternal(command, cancellationToken).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    errors.AddRange(result.Messages);
                }
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(
                    EnvironmentVariableLogger.OperationFailed(_logger, command.CommandType, command.SecretKey ?? "N/A", ex.Message));
            }
        }

        if (errors.Count > 0)
        {
            return GenericResult.Failure(
                EnvironmentVariableLogger.BatchExecutionFailed(_logger,
                    $"{errors.Count} of {commands.Count} commands failed"));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public IGenericResult ValidateCommand(ISecretManagerCommand managementCommand)
    {
        if (managementCommand == null)
        {
            throw new ArgumentNullException(nameof(managementCommand));
        }

        if (string.IsNullOrWhiteSpace(managementCommand.CommandType))
        {
            return GenericResult.Failure(new ErrorMessage("Command type is required"));
        }

        var handler = EnvironmentVariableCommandHandlers.ByName(managementCommand.CommandType);

        if (ReferenceEquals(handler, EnvironmentVariableCommandHandlers.NotFound))
        {
            // Check if it's a known-but-unsupported operation
            if (string.Equals(managementCommand.CommandType, "SetSecret", StringComparison.Ordinal))
            {
                return GenericResult.Failure(EnvironmentVariableLogger.SetSecretNotSupported(_logger));
            }
            if (string.Equals(managementCommand.CommandType, "DeleteSecret", StringComparison.Ordinal))
            {
                return GenericResult.Failure(EnvironmentVariableLogger.DeleteSecretNotSupported(_logger));
            }

            return GenericResult.Failure(
                EnvironmentVariableLogger.UnknownCommandType(_logger, managementCommand.CommandType));
        }

        return handler.Validate(managementCommand);
    }

    /// <inheritdoc/>
    Task<IGenericResult<TOut>> IGenericService.Execute<TOut>(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is not ISecretManagerCommand secretCommand)
        {
            return Task.FromResult(
                GenericResult<TOut>.Failure(new ErrorMessage($"Command must be of type {nameof(ISecretManagerCommand)}")));
        }

        if (secretCommand is ISecretManagerCommand<TOut> typedCommand)
        {
            return Execute(typedCommand, cancellationToken);
        }

        // Execute as object and try to cast
        return ExecuteAndCast<TOut>(secretCommand, cancellationToken);
    }

    /// <inheritdoc/>
    async Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
    {
        if (command is not ISecretManagerCommand secretCommand)
        {
            return GenericResult.Failure(new ErrorMessage($"Command must be of type {nameof(ISecretManagerCommand)}"));
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

    private Task<IGenericResult<object?>> ExecuteCommandInternal(
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = EnvironmentVariableCommandHandlers.ByName(command.CommandType);

            if (ReferenceEquals(handler, EnvironmentVariableCommandHandlers.NotFound))
            {
                return Task.FromResult<IGenericResult<object?>>(
                    GenericResult<object?>.Failure(
                        EnvironmentVariableLogger.UnknownCommandType(_logger, command.CommandType)));
            }

            return ExecuteViaHandler(handler, command, cancellationToken);
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(
                    EnvironmentVariableLogger.OperationFailed(_logger, command.CommandType,
                        command.SecretKey ?? "N/A", ex.Message)));
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
            return GenericResult<TOut>.Success(typedValue);
        }

        return GenericResult<TOut>.Failure(new ErrorMessage($"Result is not of type {typeof(TOut).Name}"));
    }
}
