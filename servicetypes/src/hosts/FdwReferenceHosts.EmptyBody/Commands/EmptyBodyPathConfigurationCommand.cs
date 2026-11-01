using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.EmptyBody.Configuration;

namespace FdwReferenceHosts.EmptyBody.Commands;

/// <summary>Reads and writes the EmptyBodyPath child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "EmptyBodyPath")]
public sealed class EmptyBodyPathConfigurationCommand : ConfigurationCommandBase<EmptyBodyPath>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyPathConfigurationCommand"/> class.</summary>
    public EmptyBodyPathConfigurationCommand()
        : base("EmptyBodyPath")
    {
    }
}
