using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.Commands;

/// <summary>ConfigurationCommands TypeOption for the AzureKeyVaultSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "AzureKeyVaultSecretManagerConfiguration")]
public sealed class AzureKeyVaultConfigurationCommand : ConfigurationCommandBase<AzureKeyVaultConfiguration>
{
    /// <inheritdoc/>
    public AzureKeyVaultConfigurationCommand() : base("AzureKeyVaultSecretManagerConfiguration") { }
}
