using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Commands;

/// <summary>ConfigurationCommands TypeOption for the EnvironmentVariableSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EnvironmentVariableSecretManagerConfiguration")]
public sealed class EnvironmentVariableConfigurationCommand : ConfigurationCommandBase<EnvironmentVariableConfiguration>
{
    /// <inheritdoc/>
    public EnvironmentVariableConfigurationCommand() : base("EnvironmentVariableSecretManagerConfiguration") { }
}
