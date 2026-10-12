using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.Sqlite.Configuration;

namespace FdwReferenceSecretManagers.Sqlite.Commands;

/// <summary>ConfigurationCommands TypeOption for the SqliteSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqliteSecretManagerConfiguration")]
public sealed class SqliteSecretManagerConfigurationCommand : ConfigurationCommandBase<SqliteSecretManagerConfiguration>
{
    /// <inheritdoc/>
    public SqliteSecretManagerConfigurationCommand() : base("SqliteSecretManagerConfiguration") { }
}
