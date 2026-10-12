using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace SampleDomain.SampleImplementation.Commands;

/// <summary>ConfigurationCommands TypeOption for the SampleImplementation table.</summary>
[TypeOption(typeof(ConfigurationCommands), "SampleImplementation")]
public sealed class SampleImplementationConfigurationCommand : ConfigurationCommandBase<SampleImplementationConfiguration>
{
    /// <inheritdoc/>
    public SampleImplementationConfigurationCommand() : base("SampleImplementation") { }
}
