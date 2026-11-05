using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceEndpoints.Configuration;

/// <summary>States that <see cref="AuthChallengeHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "AuthChallengeHostConfiguration")]
public sealed class AuthChallengeHostConfigurationNoValidation : UnvalidatedConfiguration<AuthChallengeHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthChallengeHostConfigurationNoValidation"/> class.</summary>
    public AuthChallengeHostConfigurationNoValidation()
        : base("AuthChallengeHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
