using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using ReferenceSecretManagers.UserSecrets.Handlers;
using ReferenceSecretManagers.UserSecrets.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Services;

/// <summary>
/// .NET User Secrets implementation of the secret manager service.
/// Provides read-only secret storage and retrieval using .NET User Secrets.
/// </summary>
/// <remarks>
/// This is a read-only implementation that reads secrets from the user secrets
/// JSON file. It supports GetSecret and ListSecrets operations only.
/// SetSecret and DeleteSecret operations are not supported.
/// </remarks>
public sealed class UserSecretsSecretManager : ISecretManager, IDisposable
{
    private readonly ILogger<UserSecretsSecretManager> _logger;
    private readonly UserSecretsConfiguration _configuration;
    private readonly string _id;
    private readonly string _name;
    private readonly string? _secretsFilePath;
    private Dictionary<string, string>? _secrets;
    private FileSystemWatcher? _fileWatcher;
    private bool _disposed;
    private bool _isAccessible;
    private DateTimeOffset _lastModified;
    private UserSecretsExecutionContext? _executionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <param name="configuration">The User Secrets configuration.</param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>UserSecretsConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the factory threads the real name through this parameter.
    /// </param>
    public UserSecretsSecretManager(
        ILogger<UserSecretsSecretManager> logger,
        UserSecretsConfiguration configuration,
        string secretManagerName = "")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = configuration.Id.ToString();
        _name = secretManagerName;

        _secretsFilePath = ResolveSecretsFilePath();
        LoadSecrets();

        _executionContext = new UserSecretsExecutionContext(logger, configuration, _id, _secrets, _lastModified, _secretsFilePath);

        if (_configuration.ReloadOnChange && !string.IsNullOrEmpty(_secretsFilePath))
        {
            SetupFileWatcher();
        }
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc/>
    public string ServiceType => "UserSecrets";

    /// <inheritdoc/>
    public bool IsAvailable => !_disposed && _isAccessible;

    /// <inheritdoc/>
    public Task<IGenericResult<object?>> Execute(
        ISecretManagerCommand managementCommand,
        CancellationToken cancellationToken = default)
    {
        if (managementCommand == null)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(UserSecretsLogger.CommandNull(_logger)));
        }

        if (_secrets == null && !_configuration.Optional)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(UserSecretsLogger.SecretsNotLoaded(_logger)));
        }

        UserSecretsLogger.ExecutingCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        return ExecuteCommandInternal(managementCommand, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TResult>> Execute<TResult>(
        ISecretManagerCommand<TResult> managementCommand,
        CancellationToken cancellationToken = default)
    {
        if (managementCommand == null)
        {
            return GenericResult<TResult>.Failure(UserSecretsLogger.CommandNull(_logger));
        }

        if (_secrets == null && !_configuration.Optional)
        {
            return GenericResult<TResult>.Failure(UserSecretsLogger.SecretsNotLoaded(_logger));
        }

        UserSecretsLogger.ExecutingTypedCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

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
        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        if (commands.Count == 0)
        {
            throw new ArgumentException("Commands list cannot be empty", nameof(commands));
        }

        if (_secrets == null && !_configuration.Optional)
        {
            return GenericResult.Failure(UserSecretsLogger.SecretsNotLoaded(_logger));
        }

        UserSecretsLogger.ExecutingBatch(_logger, commands.Count);

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
                    UserSecretsLogger.OperationFailed(_logger, command.CommandType, command.SecretKey ?? "N/A", ex.Message));
            }
        }

        if (errors.Count > 0)
        {
            return GenericResult.Failure(
                UserSecretsLogger.BatchExecutionFailed(_logger,
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

        var handler = UserSecretsCommandHandlers.ByName(managementCommand.CommandType);

        if (ReferenceEquals(handler, UserSecretsCommandHandlers.NotFound))
        {
            if (string.Equals(managementCommand.CommandType, "SetSecret", StringComparison.Ordinal) ||
                string.Equals(managementCommand.CommandType, "DeleteSecret", StringComparison.Ordinal))
            {
                return GenericResult.Failure(
                    UserSecretsLogger.OperationNotSupported(_logger, managementCommand.CommandType));
            }

            return GenericResult.Failure(
                UserSecretsLogger.UnknownCommandType(_logger, managementCommand.CommandType));
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

        _fileWatcher?.Dispose();
        _fileWatcher = null;
        _secrets = null;
        _disposed = true;
    }

    private string? ResolveSecretsFilePath()
    {
        // If a direct path is specified, use it
        if (!string.IsNullOrWhiteSpace(_configuration.SecretsFilePath))
        {
            return _configuration.SecretsFilePath;
        }

        // Otherwise, construct the path from UserSecretsId
        if (string.IsNullOrWhiteSpace(_configuration.UserSecretsId))
        {
            if (!_configuration.Optional)
            {
                UserSecretsLogger.UserSecretsIdRequired(_logger);
            }
            return null;
        }

        // Determine the user secrets directory based on the platform
        string userSecretsDir;
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            userSecretsDir = Path.Combine(appData, "Microsoft", "UserSecrets", _configuration.UserSecretsId);
        }
        else
        {
            // Linux/macOS
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            userSecretsDir = Path.Combine(home, ".microsoft", "usersecrets", _configuration.UserSecretsId);
        }

        return Path.Combine(userSecretsDir, "secrets.json");
    }

    private void LoadSecrets()
    {
        if (string.IsNullOrEmpty(_secretsFilePath))
        {
            _isAccessible = _configuration.Optional;
            _secrets = _configuration.Optional ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : null;
            return;
        }

        try
        {
            if (!File.Exists(_secretsFilePath))
            {
                if (_configuration.Optional)
                {
                    UserSecretsLogger.SecretsFileNotFoundOptional(_logger, _secretsFilePath);
                    _secrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _isAccessible = true;
                }
                else
                {
                    UserSecretsLogger.SecretsFileNotFound(_logger, _secretsFilePath);
                    _secrets = null;
                    _isAccessible = false;
                }
                return;
            }

            var json = File.ReadAllText(_secretsFilePath);
            var fileInfo = new FileInfo(_secretsFilePath);
            _lastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero);

            // Parse JSON and flatten to key-value pairs (supporting nested objects)
            _secrets = ParseJsonToFlatDictionary(json);
            _isAccessible = true;

            UserSecretsLogger.SecretsLoaded(_logger, _secrets.Count, _secretsFilePath);
        }
        catch (Exception ex)
        {
            UserSecretsLogger.SecretsLoadFailed(_logger, _secretsFilePath ?? "unknown", ex.Message);
            _secrets = _configuration.Optional ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : null;
            _isAccessible = _configuration.Optional;
        }
    }

    private static Dictionary<string, string> ParseJsonToFlatDictionary(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        using var document = JsonDocument.Parse(json);
        FlattenJsonElement(document.RootElement, string.Empty, result);

        return result;
    }

    private static void FlattenJsonElement(JsonElement element, string prefix, Dictionary<string, string> result)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    FlattenJsonElement(property.Value, key, result);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}:{index}";
                    FlattenJsonElement(item, key, result);
                    index++;
                }
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;

            case JsonValueKind.Null:
                // Skip null values
                break;
        }
#pragma warning restore FDW018
    }

    private void SetupFileWatcher()
    {
        if (string.IsNullOrEmpty(_secretsFilePath))
            return;

        var directory = Path.GetDirectoryName(_secretsFilePath);
        var fileName = Path.GetFileName(_secretsFilePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return;

        try
        {
            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _fileWatcher.Changed += OnSecretsFileChanged;
            _fileWatcher.EnableRaisingEvents = true;

            UserSecretsLogger.FileWatcherEnabled(_logger, _secretsFilePath);
        }
        catch (Exception ex)
        {
            UserSecretsLogger.FileWatcherFailed(_logger, ex.Message);
        }
    }

    private void OnSecretsFileChanged(object sender, FileSystemEventArgs e)
    {
        UserSecretsLogger.SecretsFileChanged(_logger);

        // Add a small delay to avoid file lock issues
        Thread.Sleep(100);

        LoadSecrets();

        if (_executionContext != null)
        {
            _executionContext.Secrets = _secrets;
            _executionContext.LastModified = _lastModified;
        }
    }

    private Task<IGenericResult<object?>> ExecuteCommandInternal(
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = UserSecretsCommandHandlers.ByName(command.CommandType);

            if (ReferenceEquals(handler, UserSecretsCommandHandlers.NotFound))
            {
                // Check if it's a known-but-unsupported operation
                if (string.Equals(command.CommandType, "SetSecret", StringComparison.Ordinal) ||
                    string.Equals(command.CommandType, "DeleteSecret", StringComparison.Ordinal))
                {
                    return Task.FromResult<IGenericResult<object?>>(
                        GenericResult<object?>.Failure(
                            UserSecretsLogger.OperationNotSupported(_logger, command.CommandType)));
                }

                return Task.FromResult<IGenericResult<object?>>(
                    GenericResult<object?>.Failure(
                        UserSecretsLogger.UnknownCommandType(_logger, command.CommandType)));
            }

            return ExecuteViaHandler(handler, command, cancellationToken);
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(
                    UserSecretsLogger.OperationFailed(_logger, command.CommandType,
                        command.SecretKey ?? "N/A", ex.Message)));
        }
    }

    private Task<IGenericResult<object?>> ExecuteViaHandler(
        ISecretManagerCommandHandler handler,
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        if (_executionContext is null)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(
                    UserSecretsLogger.SecretsNotLoaded(_logger)));
        }

        return handler.InvokeBoxed(command, _executionContext, cancellationToken);
    }

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
