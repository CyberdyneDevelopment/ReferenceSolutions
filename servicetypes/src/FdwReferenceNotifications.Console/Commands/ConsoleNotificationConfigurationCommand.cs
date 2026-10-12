using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceNotifications.Console.Commands;

/// <summary>ConfigurationCommands TypeOption for the ConsoleNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "ConsoleNotificationConfiguration")]
public sealed class ConsoleNotificationConfigurationCommand : ConfigurationCommandBase<ConsoleNotificationConfiguration>
{
    /// <inheritdoc/>
    public ConsoleNotificationConfigurationCommand() : base("ConsoleNotificationConfiguration") { }
}
