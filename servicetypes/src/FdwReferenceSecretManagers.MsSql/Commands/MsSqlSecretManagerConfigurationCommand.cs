using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.MsSql.Configuration;

namespace FdwReferenceSecretManagers.MsSql.Commands;

/// <summary>ConfigurationCommands TypeOption for the MsSqlSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "MsSqlSecretManagerConfiguration")]
public sealed class MsSqlSecretManagerConfigurationCommand : ConfigurationCommandBase<MsSqlSecretManagerConfiguration>
{
    /// <inheritdoc/>
    public MsSqlSecretManagerConfigurationCommand() : base("MsSqlSecretManagerConfiguration") { }
}
