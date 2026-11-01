using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceMultitenancy.Sql;

/// <summary>ConfigurationCommands TypeOption for the SqlTenantProvider configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqlTenantProviderConfiguration")]
public sealed class SqlTenantProviderConfigurationCommand : ConfigurationCommandBase<SqlTenantConfiguration>
{
    /// <inheritdoc/>
    public SqlTenantProviderConfigurationCommand() : base("SqlTenantProviderConfiguration") { }
}
