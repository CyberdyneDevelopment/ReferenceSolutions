using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.UserSecrets.Configuration;

namespace FdwReferenceSecretManagers.UserSecrets.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserSecretsSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserSecretsSecretManagerConfiguration")]
public sealed class UserSecretsConfigurationCommand : ConfigurationCommandBase<UserSecretsConfiguration>
{
    /// <inheritdoc/>
    public UserSecretsConfigurationCommand() : base("UserSecretsSecretManagerConfiguration") { }
}
