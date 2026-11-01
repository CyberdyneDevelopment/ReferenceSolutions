using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.HashiCorpVault.Configuration;

namespace FdwReferenceSecretManagers.HashiCorpVault.Commands;

/// <summary>ConfigurationCommands TypeOption for the Vault typed body (sec.HashiCorpVaultSecretManager).</summary>
[TypeOption(typeof(ConfigurationCommands), "HashiCorpVaultSecretManagerConfiguration")]
public sealed class HashiCorpVaultConfigurationCommand : ConfigurationCommandBase<HashiCorpVaultConfiguration>
{
    /// <inheritdoc/>
    public HashiCorpVaultConfigurationCommand() : base("HashiCorpVaultSecretManagerConfiguration") { }
}
