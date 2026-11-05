using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceMultitenancy.Sql;

/// <summary>States that <see cref="SqlTenantConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqlTenantConfiguration")]
public sealed class SqlTenantConfigurationNoValidation : UnvalidatedConfiguration<SqlTenantConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlTenantConfigurationNoValidation"/> class.</summary>
    public SqlTenantConfigurationNoValidation()
        : base("SqlTenantConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
