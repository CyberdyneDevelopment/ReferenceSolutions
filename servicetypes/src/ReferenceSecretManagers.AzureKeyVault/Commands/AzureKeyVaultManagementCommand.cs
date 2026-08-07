using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Abstractions;
using Fdw.Results;
using FluentValidation.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions.Commands;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Logging;
using Fdw.Configuration;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;

namespace ReferenceSecretManagers.AzureKeyVault.Commands;

/// <summary>
/// Represents a managementCommand for the Azure Key Vault Secret Management service.
/// </summary>
public sealed class AzureKeyVaultManagementCommand : ISecretManagerCommand
{
    private readonly Guid _commandGuid = Guid.NewGuid();
    private readonly ILogger<AzureKeyVaultManagementCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultManagementCommand"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public AzureKeyVaultManagementCommand(ILogger<AzureKeyVaultManagementCommand>? logger = null)
    {
        _logger = logger ?? NullLogger<AzureKeyVaultManagementCommand>.Instance;
    }

    /// <summary>
    /// Gets the unique identifier for this managementCommand as a Guid.
    /// </summary>
    public Guid CommandId => _commandGuid;

    /// <summary>
    /// Gets the unique identifier for this managementCommand as a string (explicit implementation).
    /// </summary>
    string ISecretManagerCommand.CommandId => _commandGuid.ToString();

    /// <summary>
    /// Gets or sets the type of secret operation to perform.
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key to operate on.
    /// </summary>
    public string? SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional parameters for the managementCommand.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the container for this managementCommand.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets the expected result type.
    /// </summary>
    public Type ExpectedResultType { get; set; } = typeof(object);

    /// <summary>
    /// Gets the timeout for managementCommand execution.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets additional metadata for this managementCommand.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether this managementCommand modifies secrets.
    /// </summary>
    public bool IsSecretModifying =>
        CommandType is "SetSecret" or "DeleteSecret" or "PurgeSecret" or "RestoreSecret" or "BackupSecret";

    /// <summary>
    /// Gets the correlation ID for this managementCommand.
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this managementCommand was created.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the category of the command.
    /// </summary>
    public string Category => "SecretManagement";

    /// <summary>
    /// Gets the configuration for this managementCommand.
    /// </summary>
    public IGenericConfiguration? Configuration { get; set; }

    /// <summary>
    /// Validates the managementCommand parameters.
    /// </summary>
    /// <returns>A validation result indicating whether the managementCommand is valid.</returns>
    public IGenericResult Validate()
    {
        // Basic validation for key and managementCommand type
        var validator = new AzureKeyVaultCommandValidator();
        var result = validator.Validate(this);

        if (result.IsValid)
        {
            return GenericResult.Success();
        }

        var errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
        return GenericResult.Failure(SecretManagerLogger.ValidationFailed(_logger, errorMessage));
    }

    /// <summary>
    /// Creates a copy of this managementCommand with modified parameters.
    /// </summary>
    public ISecretManagerCommand WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        ArgumentNullException.ThrowIfNull(newParameters);

        return new AzureKeyVaultManagementCommand
        {
            CommandType = this.CommandType,
            SecretKey = this.SecretKey,
            Parameters = newParameters,
            Container = this.Container,
            ExpectedResultType = this.ExpectedResultType,
            Timeout = this.Timeout,
            Metadata = this.Metadata,
            CorrelationId = this.CorrelationId,
            Configuration = this.Configuration
        };
    }

    /// <summary>
    /// Creates a copy of this managementCommand with modified metadata.
    /// </summary>
    public ISecretManagerCommand WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        ArgumentNullException.ThrowIfNull(newMetadata);

        return new AzureKeyVaultManagementCommand
        {
            CommandType = this.CommandType,
            SecretKey = this.SecretKey,
            Parameters = this.Parameters,
            Container = this.Container,
            ExpectedResultType = this.ExpectedResultType,
            Timeout = this.Timeout,
            Metadata = newMetadata,
            CorrelationId = this.CorrelationId,
            Configuration = this.Configuration
        };
    }
}