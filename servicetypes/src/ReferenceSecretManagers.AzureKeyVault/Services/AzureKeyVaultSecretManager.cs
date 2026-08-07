using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using ReferenceSecretManagers.AzureKeyVault.Handlers;
using ReferenceSecretManagers.AzureKeyVault.Logging;
using Microsoft.Extensions.Logging;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Services;

/// <summary>
/// Azure Key Vault implementation of the secret manager service.
/// Provides secure secret storage and retrieval using Azure Key Vault.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AzureKeyVaultSecretManager : ISecretManager, IDisposable
{
    private readonly ILogger<AzureKeyVaultSecretManager> _logger;
    private readonly AzureKeyVaultConfiguration _configuration;
    private readonly string _id;
    private readonly string _name;
    private SecretClient? _client;
    private CertificateClient? _certificateClient;
    private AzureKeyVaultExecutionContext? _executionContext;
    private bool _disposed;
    private bool _isAccessible;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretManager"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <param name="configuration">The Azure Key Vault configuration.</param>
    /// <param name="secretManagerName">
    /// The logical name of the secret manager (from the <c>SecretManagerConfiguration</c> header).
    /// After config-split, <c>AzureKeyVaultConfiguration.Name</c> returns <c>string.Empty</c>;
    /// the factory threads the real name through this parameter.
    /// </param>
    public AzureKeyVaultSecretManager(
        ILogger<AzureKeyVaultSecretManager> logger,
        AzureKeyVaultConfiguration configuration,
        string secretManagerName = "")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _id = configuration.Id.ToString();
        _name = secretManagerName;

        InitializeClient();
    }

    /// <inheritdoc/>
    public string Id => _id;

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc/>
    public string ServiceType => "AzureKeyVault";

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
                GenericResult<object?>.Failure(AzureKeyVaultLogger.CommandNull(_logger)));
        }

        if (_client == null)
        {
            return Task.FromResult<IGenericResult<object?>>(
                GenericResult<object?>.Failure(AzureKeyVaultLogger.ClientNotInitialized(_logger)));
        }

        AzureKeyVaultLogger.ExecutingCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        return ExecuteCommandInternal(managementCommand, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TResult>> Execute<TResult>(
        ISecretManagerCommand<TResult> managementCommand,
        CancellationToken cancellationToken = default)
    {
        if (managementCommand == null)
        {
            return GenericResult<TResult>.Failure(AzureKeyVaultLogger.CommandNull(_logger));
        }

        if (_executionContext is null)
        {
            return GenericResult<TResult>.Failure(AzureKeyVaultLogger.ClientNotInitialized(_logger));
        }

        AzureKeyVaultLogger.ExecutingTypedCommand(_logger, managementCommand.CommandType, managementCommand.SecretKey ?? "N/A");

        try
        {
            // Lookup handler by command name - TypeCollection generates ByName
            var handler = AzureKeyVaultCommandHandlers.ByName(managementCommand.CommandType);

            if (ReferenceEquals(handler, AzureKeyVaultCommandHandlers.NotFound))
            {
                return GenericResult<TResult>.Failure(
                    AzureKeyVaultLogger.NoHandlerFound(_logger, managementCommand.CommandType));
            }

            // Validate
            var validationResult = handler.Validate(managementCommand);
            if (!validationResult.IsSuccess)
            {
                return validationResult.ToNewResult<TResult>();
            }

            // Route through InvokeBoxed and project the boxed result to TResult.
            // Why: InvokeBoxed is the single dispatch path; avoids reflection and duplicate
            // resolution paths on the typed Execute<TResult> overload.
            var boxedResult = await handler.InvokeBoxed(managementCommand, _executionContext!, cancellationToken).ConfigureAwait(false);

            if (!boxedResult.IsSuccess)
            {
                return boxedResult.ToNewResult<TResult>();
            }

            if (boxedResult.Value is TResult typedValue)
            {
                return boxedResult.ToNewResult(typedValue);
            }

            return GenericResult<TResult>.Failure(
                AzureKeyVaultLogger.OperationFailed(_logger, managementCommand.CommandType,
                    managementCommand.SecretKey ?? "N/A", $"Result is not of type {typeof(TResult).Name}"));
        }
        catch (RequestFailedException ex)
        {
            return GenericResult<TResult>.Failure(
                AzureKeyVaultLogger.OperationFailed(_logger, managementCommand.CommandType,
                    managementCommand.SecretKey ?? "N/A", ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<TResult>.Failure(
                AzureKeyVaultLogger.OperationFailed(_logger, managementCommand.CommandType,
                    managementCommand.SecretKey ?? "N/A", ex.Message));
        }
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

        if (_client == null)
        {
            return GenericResult.Failure(AzureKeyVaultLogger.ClientNotInitialized(_logger));
        }

        AzureKeyVaultLogger.ExecutingBatch(_logger, commands.Count);

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
                    AzureKeyVaultLogger.OperationFailed(_logger, command.CommandType, command.SecretKey ?? "N/A", ex.Message));
            }
        }

        if (errors.Count > 0)
        {
            return GenericResult.Failure(
                AzureKeyVaultLogger.BatchExecutionFailed(_logger,
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

        // Basic validation - command type must be specified
        if (string.IsNullOrWhiteSpace(managementCommand.CommandType))
        {
            return GenericResult.Failure(new ErrorMessage("Command type is required"));
        }

        // Lookup handler via TypeCollection
        var handler = AzureKeyVaultCommandHandlers.ByName(managementCommand.CommandType);

        if (ReferenceEquals(handler, AzureKeyVaultCommandHandlers.NotFound))
        {
            return GenericResult.Failure(
                AzureKeyVaultLogger.NoHandlerFound(_logger, managementCommand.CommandType));
        }

        // Delegate validation to handler
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

        // Fallback to non-generic execute
        return Task.FromResult(GenericResult<TOut>.Failure(new ErrorMessage($"Command must be of type ISecretManagerCommand<{typeof(TOut).Name}>")));
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

        // Azure SDK clients don't need explicit disposal
        // But we clear the reference
        _client = null;
        _disposed = true;
    }

    private void InitializeClient()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_configuration.VaultUri))
            {
                throw new InvalidOperationException("Vault URI is required");
            }

            var vaultUri = new Uri(_configuration.VaultUri);
            var credential = CreateCredential();

            _client = new SecretClient(vaultUri, credential);
            _certificateClient = new CertificateClient(vaultUri, credential);

            _executionContext = new AzureKeyVaultExecutionContext(
                logger: _logger,
                configuration: _configuration,
                secretClient: _client,
                certificateClient: _certificateClient,
                serviceId: _id);

            AzureKeyVaultLogger.ClientInitialized(_logger, _configuration.AuthenticationMethod ?? "Unknown");
            AzureKeyVaultLogger.CertificateClientInitialized(_logger);

            // Check accessibility if configured
            if (_configuration.ValidateOnStartup)
            {
                CheckAccessibility();
            }
            else
            {
                _isAccessible = true; // Assume accessible until proven otherwise
            }
        }
        catch (Exception ex)
        {
            AzureKeyVaultLogger.ClientInitializationFailed(_logger, ex.Message);
            _isAccessible = false;
            throw;
        }
    }

    private TokenCredential CreateCredential()
    {
        var authMethod = _configuration.AuthenticationMethod ?? "ManagedIdentity";
        var credentialType = AzureCredentialTypes.ByName(authMethod);

        if (ReferenceEquals(credentialType, AzureCredentialTypes.NotFound))
        {
            throw new NotSupportedException(
                $"Authentication method '{authMethod}' is not supported. Supported methods: ManagedIdentity, ServicePrincipal, Certificate, DeviceCode");
        }

        return credentialType.Create(_configuration);
    }

    private void CheckAccessibility()
    {
        try
        {
            if (_client == null)
            {
                _isAccessible = false;
                return;
            }

            // Try to list secrets to verify accessibility
            // This is a lightweight operation that verifies both authentication and authorization
            var response = _client.GetPropertiesOfSecretsAsync().AsPages(pageSizeHint: 1).GetAsyncEnumerator();
#pragma warning disable VSTHRD002 // Intentional blocking call for synchronous accessibility check
            response.MoveNextAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
            response.DisposeAsync().AsTask().Wait();
#pragma warning restore VSTHRD002

            _isAccessible = true;
        }
        catch (Exception ex)
        {
            AzureKeyVaultLogger.AccessibilityCheckFailed(_logger, ex.Message);
            _isAccessible = false;
        }
    }

    private async Task<IGenericResult<object?>> ExecuteCommandInternal(
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
    {
        if (_executionContext is null)
        {
            return GenericResult<object?>.Failure(
                AzureKeyVaultLogger.ClientNotInitialized(_logger));
        }

        try
        {
            // Lookup handler by command name - TypeCollection generates ByName
            var handler = AzureKeyVaultCommandHandlers.ByName(command.CommandType);

            if (ReferenceEquals(handler, AzureKeyVaultCommandHandlers.NotFound))
            {
                return GenericResult<object?>.Failure(
                    AzureKeyVaultLogger.NoHandlerFound(_logger, command.CommandType));
            }

            AzureKeyVaultLogger.ExecutingViaHandler(_logger, handler.GetType().Name, command.SecretKey ?? "N/A");

            // Validate command using handler
            var validationResult = handler.Validate(command);
            if (!validationResult.IsSuccess)
            {
                return validationResult.ToNewResult<object?>();
            }

            // Execute via delegate stored in handler - ExecuteViaGenericHandler handles the cast and invocation
            return await ExecuteViaGenericHandler(handler, command, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            return GenericResult<object?>.Failure(
                AzureKeyVaultLogger.OperationFailed(_logger, command.CommandType,
                    command.SecretKey ?? "N/A", ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<object?>.Failure(
                AzureKeyVaultLogger.OperationFailed(_logger, command.CommandType,
                    command.SecretKey ?? "N/A", ex.Message));
        }
    }

    private Task<IGenericResult<object?>> ExecuteViaGenericHandler(
        ISecretManagerCommandHandler handler,
        ISecretManagerCommand command,
        CancellationToken cancellationToken)
        => handler.InvokeBoxed(command, _executionContext!, cancellationToken);
}
