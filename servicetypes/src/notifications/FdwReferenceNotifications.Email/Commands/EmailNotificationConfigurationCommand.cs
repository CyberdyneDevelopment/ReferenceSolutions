using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceNotifications.Email.Commands;

/// <summary>ConfigurationCommands TypeOption for the EmailNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EmailNotificationConfiguration")]
public sealed class EmailNotificationConfigurationCommand : ConfigurationCommandBase<EmailNotificationConfiguration>
{
    /// <inheritdoc/>
    public EmailNotificationConfigurationCommand() : base("EmailNotificationConfiguration") { }
}
