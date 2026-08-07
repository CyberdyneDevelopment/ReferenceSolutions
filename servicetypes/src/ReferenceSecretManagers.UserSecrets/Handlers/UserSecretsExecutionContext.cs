using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Handlers;

/// <summary>
/// User Secrets specific execution context providing access to the loaded secrets dictionary.
/// </summary>
public sealed class UserSecretsExecutionContext : ISecretManagerExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsExecutionContext"/> class.
    /// </summary>
    public UserSecretsExecutionContext(
        ILogger logger,
        UserSecretsConfiguration configuration,
        string serviceId,
        IDictionary<string, string>? secrets,
        DateTimeOffset lastModified,
        string? secretsFilePath)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UserSecretsConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        Secrets = secrets;
        LastModified = lastModified;
        SecretsFilePath = secretsFilePath;
    }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IGenericConfiguration Configuration => UserSecretsConfiguration;

    /// <summary>
    /// Gets the User Secrets specific configuration.
    /// </summary>
    public UserSecretsConfiguration UserSecretsConfiguration { get; }

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <summary>
    /// Gets the loaded secrets dictionary.
    /// </summary>
    public IDictionary<string, string>? Secrets { get; internal set; }

    /// <summary>
    /// Gets the last modified timestamp of the secrets file.
    /// </summary>
    public DateTimeOffset LastModified { get; internal set; }

    /// <summary>
    /// Gets the path to the secrets file.
    /// </summary>
    public string? SecretsFilePath { get; }
}
