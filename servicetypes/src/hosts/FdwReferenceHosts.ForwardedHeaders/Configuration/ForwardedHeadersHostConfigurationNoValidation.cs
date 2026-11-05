using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.ForwardedHeaders.Configuration;

/// <summary>States that <see cref="ForwardedHeadersHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ForwardedHeadersHostConfiguration")]
public sealed class ForwardedHeadersHostConfigurationNoValidation : UnvalidatedConfiguration<ForwardedHeadersHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersHostConfigurationNoValidation"/> class.</summary>
    public ForwardedHeadersHostConfigurationNoValidation()
        : base("ForwardedHeadersHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
