using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

/// <summary>States that <see cref="EnvironmentVariableConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "EnvironmentVariableConfiguration")]
public sealed class EnvironmentVariableConfigurationNoValidation : UnvalidatedConfiguration<EnvironmentVariableConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentVariableConfigurationNoValidation"/> class.</summary>
    public EnvironmentVariableConfigurationNoValidation()
        : base("EnvironmentVariableConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
