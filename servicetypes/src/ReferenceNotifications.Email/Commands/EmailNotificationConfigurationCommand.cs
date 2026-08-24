using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace ReferenceNotifications.Email.Commands;

/// <summary>ConfigurationCommands TypeOption for the EmailNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EmailNotification")]
public sealed class EmailNotificationConfigurationCommand : ConfigurationCommandBase<EmailNotificationConfiguration>
{
    /// <inheritdoc/>
    public EmailNotificationConfigurationCommand() : base("EmailNotification") { }
}
