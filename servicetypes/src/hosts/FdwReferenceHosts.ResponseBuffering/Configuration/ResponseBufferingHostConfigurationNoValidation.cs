using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.ResponseBuffering.Configuration;

/// <summary>States that <see cref="ResponseBufferingHostConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ResponseBufferingHostConfiguration")]
public sealed class ResponseBufferingHostConfigurationNoValidation : UnvalidatedConfiguration<ResponseBufferingHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ResponseBufferingHostConfigurationNoValidation"/> class.</summary>
    public ResponseBufferingHostConfigurationNoValidation()
        : base("ResponseBufferingHostConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
