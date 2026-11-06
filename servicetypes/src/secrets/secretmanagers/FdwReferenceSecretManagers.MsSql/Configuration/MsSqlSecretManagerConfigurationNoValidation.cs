using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSecretManagers.MsSql.Configuration;

/// <summary>States that <see cref="MsSqlSecretManagerConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "MsSqlSecretManagerConfiguration")]
public sealed class MsSqlSecretManagerConfigurationNoValidation : UnvalidatedConfiguration<MsSqlSecretManagerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlSecretManagerConfigurationNoValidation"/> class.</summary>
    public MsSqlSecretManagerConfigurationNoValidation()
        : base("MsSqlSecretManagerConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
