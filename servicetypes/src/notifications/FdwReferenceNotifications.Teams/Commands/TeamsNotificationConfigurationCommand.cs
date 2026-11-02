using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceNotifications.Teams.Commands;

/// <summary>ConfigurationCommands TypeOption for the Teams notification typed body.</summary>
[TypeOption(typeof(ConfigurationCommands), "TeamsNotificationConfiguration")]
public sealed class TeamsNotificationConfigurationCommand : ConfigurationCommandBase<TeamsNotificationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationConfigurationCommand"/> class.</summary>
    public TeamsNotificationConfigurationCommand() : base("TeamsNotificationConfiguration") { }
}
