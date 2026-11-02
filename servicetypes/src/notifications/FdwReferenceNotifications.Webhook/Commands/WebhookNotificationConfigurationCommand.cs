using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace FdwReferenceNotifications.Webhook.Commands;

/// <summary>ConfigurationCommands TypeOption for the WebhookNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "WebhookNotificationConfiguration")]
public sealed class WebhookNotificationConfigurationCommand : ConfigurationCommandBase<WebhookNotificationConfiguration>
{
    /// <inheritdoc/>
    public WebhookNotificationConfigurationCommand() : base("WebhookNotificationConfiguration") { }
}
