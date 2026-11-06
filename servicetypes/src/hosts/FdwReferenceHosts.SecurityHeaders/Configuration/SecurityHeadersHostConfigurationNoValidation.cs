using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.SecurityHeaders.Configuration;

/// <summary>States that <see cref="SecurityHeadersHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SecurityHeadersHostConfiguration")]
public sealed class SecurityHeadersHostConfigurationNoValidation : UnvalidatedConfiguration<SecurityHeadersHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersHostConfigurationNoValidation"/> class.</summary>
    public SecurityHeadersHostConfigurationNoValidation()
        : base("SecurityHeadersHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
