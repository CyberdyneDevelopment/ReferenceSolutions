using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Webhook.Commands;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReferenceNotifications.Webhook;
using Fdw.Services.Notifications.Webhook;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceNotifications.Webhook;

/// <summary>
/// ServiceType definition for webhook notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ServiceTypeOptionAttribute"/> and registered
/// with the <see cref="NotificationTypes"/> collection by the source generator.
/// </para>
/// <para>
/// Configuration is loaded from "Notifications:Webhook:{index}" sections in appsettings.json:
/// <code>
/// {
///   "Notifications": {
///     "Webhook": [
///       {
///         "Name": "OrderAlerts",
///         "NotificationType": "Webhook",
///         "IsEnabled": true,
///         "Url": "https://hooks.example.com/notify",
///         "Method": "POST",
///         "ContentType": "application/json",
///         "RetryCount": 3,
///         "TimeoutSeconds": 30
///       }
///     ]
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(NotificationTypes), "Webhook")]
public sealed class WebhookNotificationType
    : NotificationTypeBase<INotificationService, IWebhookNotificationFactory, WebhookNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotificationType"/> class.
    /// Instance is created by source generator in NotificationTypes collection.
    /// </summary>
    public WebhookNotificationType()
        : base(
            name: "Webhook",
            channelName: "Webhook",
            displayName: "Webhook Notifications",
            description: "Send notifications via generic HTTP webhook",
            defaultContainerName: "WebhookNotification")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IWebhookNotificationFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: Resolve from DI — provider was registered with Lazy<IConfigurationGateway> in the Registration phase body.
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<WebhookNotificationConfiguration, WebhookNotificationConfigurationCommand>>();
            services.GetRequiredService<NotificationConfigurationProvider>()
                .Register(Name, configProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            builder.Services.Configure<List<NotificationConfiguration>>(builder.Configuration.GetSection("Notifications:Default"));
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Factory — DI handles all constructor dependencies
            builder.Services.AddSingleton<IWebhookNotificationFactory, WebhookNotificationFactory>();

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<WebhookNotificationConfiguration, WebhookNotificationConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<WebhookNotificationConfiguration, WebhookNotificationConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                PathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // Why: register the domain header provider (idempotent) it depends on, instead of the
            // entry-point app.

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

}
