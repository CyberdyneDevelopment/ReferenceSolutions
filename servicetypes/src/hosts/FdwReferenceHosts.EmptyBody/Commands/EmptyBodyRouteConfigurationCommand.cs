using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.EmptyBody.Configuration;

namespace FdwReferenceHosts.EmptyBody.Commands;

/// <summary>Reads and writes the EmptyBodyRoute child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "EmptyBodyRoute")]
public sealed class EmptyBodyRouteConfigurationCommand : ConfigurationCommandBase<EmptyBodyRoute>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyRouteConfigurationCommand"/> class.</summary>
    public EmptyBodyRouteConfigurationCommand()
        : base("EmptyBodyRoute")
    {
    }
}
