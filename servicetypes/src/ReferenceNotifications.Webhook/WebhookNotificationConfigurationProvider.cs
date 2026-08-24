using System;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceNotifications.Webhook.Commands;

namespace ReferenceNotifications.Webhook;

/// <summary>Reads the Webhook notification configurations out of ConfigurationDb.</summary>
/// <remarks>
/// A named type rather than a bare <c>DefaultConfigurationProvider</c> closed over its generics: the
/// name is what lets DI register it and expose it as
/// <c>IServiceConfigurationProvider&lt;WebhookNotificationConfiguration&gt;</c>, which is the seam
/// consumers inject. The gateway stays behind that seam — a caller asks this provider for
/// configuration, never the gateway for rows.
/// </remarks>
public class WebhookNotificationConfigurationProvider
    : DefaultConfigurationProvider<WebhookNotificationConfiguration, WebhookNotificationConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="WebhookNotificationConfigurationProvider"/> class.</summary>
    public WebhookNotificationConfigurationProvider(
        ILogger<WebhookNotificationConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "notify",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(
            logger ?? NullLogger<WebhookNotificationConfigurationProvider>.Instance,
            lazyGateway,
            dataStoreName,
            pathName,
            invalidator)
    {
    }
}
