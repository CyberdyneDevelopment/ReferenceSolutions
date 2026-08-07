using System;
using Fdw.Configuration;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable.Handlers;

/// <summary>
/// Environment Variable specific execution context providing access to configuration.
/// </summary>
public sealed class EnvironmentVariableExecutionContext : ISecretManagerExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableExecutionContext"/> class.
    /// </summary>
    public EnvironmentVariableExecutionContext(
        ILogger logger,
        EnvironmentVariableConfiguration configuration,
        string serviceId)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EnvironmentVariableConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        StringComparison = configuration.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
    }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IGenericConfiguration Configuration => EnvironmentVariableConfiguration;

    /// <summary>
    /// Gets the environment variable specific configuration.
    /// </summary>
    public EnvironmentVariableConfiguration EnvironmentVariableConfiguration { get; }

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <summary>
    /// Gets the string comparison to use for key matching.
    /// </summary>
    public StringComparison StringComparison { get; }
}
