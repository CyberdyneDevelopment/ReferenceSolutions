using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace ReferenceMultitenancy.Sql;

/// <summary>ConfigurationCommands TypeOption for the SqlTenantProvider configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqlTenantProvider")]
public sealed class SqlTenantProviderConfigurationCommand : ConfigurationCommandBase<SqlTenantConfiguration>
{
    /// <inheritdoc/>
    public SqlTenantProviderConfigurationCommand() : base("SqlTenantProvider") { }
}
