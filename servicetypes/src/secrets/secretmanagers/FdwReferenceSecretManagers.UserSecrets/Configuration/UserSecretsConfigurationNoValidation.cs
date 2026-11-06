using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceSecretManagers.UserSecrets.Configuration;

/// <summary>States that <see cref="UserSecretsConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "UserSecretsConfiguration")]
public sealed class UserSecretsConfigurationNoValidation : UnvalidatedConfiguration<UserSecretsConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserSecretsConfigurationNoValidation"/> class.</summary>
    public UserSecretsConfigurationNoValidation()
        : base("UserSecretsConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
