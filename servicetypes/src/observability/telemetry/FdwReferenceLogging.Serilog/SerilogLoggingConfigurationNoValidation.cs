using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace FdwReferenceLogging.Serilog;

/// <summary>States that <see cref="SerilogLoggingConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SerilogLoggingConfiguration")]
public sealed class SerilogLoggingConfigurationNoValidation : UnvalidatedConfiguration<SerilogLoggingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SerilogLoggingConfigurationNoValidation"/> class.</summary>
    public SerilogLoggingConfigurationNoValidation()
        : base("SerilogLoggingConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
