using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceHosts.EmptyBody.Configuration;

/// <summary>States that <see cref="EmptyBodyPath"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "EmptyBodyPath")]
public sealed class EmptyBodyPathConfigurationNoValidation : UnvalidatedConfiguration<EmptyBodyPath>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyPathConfigurationNoValidation"/> class.</summary>
    public EmptyBodyPathConfigurationNoValidation()
        : base("EmptyBodyPath", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
