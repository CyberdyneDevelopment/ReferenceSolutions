using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSecretManagers.Sqlite.Configuration;

/// <summary>States that <see cref="SqliteSecretManagerConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqliteSecretManagerConfiguration")]
public sealed class SqliteSecretManagerConfigurationNoValidation : UnvalidatedConfiguration<SqliteSecretManagerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqliteSecretManagerConfigurationNoValidation"/> class.</summary>
    public SqliteSecretManagerConfigurationNoValidation()
        : base("SqliteSecretManagerConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
