using System.Linq;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace FdwReferenceNotifications.Webhook.Registration;

/// <summary>
/// ServiceType definition for webhook notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ImplementationAttribute"/> and registered
/// with the <see cref="NotificationServiceTypes"/> collection by the source generator.
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
[Implementation(typeof(NotificationServiceTypes), "Webhook")]
public sealed class WebhookNotificationType
    : NotificationTypeBase<INotificationService, IWebhookNotificationFactory, WebhookNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotificationType"/> class.
    /// Instance is created by source generator in NotificationServiceTypes collection.
    /// </summary>
    public WebhookNotificationType()
        : base(
            name: "Webhook",
            channelName: "Webhook",
            displayName: "Webhook Notifications",
            description: "Send notifications via generic HTTP webhook",
            defaultContainerName: "WebhookNotificationConfiguration")
    {
        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Factory — DI handles all constructor dependencies
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IWebhookNotificationFactory), typeof(WebhookNotificationFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IWebhookNotificationProvider), typeof(WebhookNotificationProvider), ServiceLifetime.Singleton));

            // Why: the gateway is resolved per call, so cfg resolution is deferred to first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so the connection is never hardcoded here.
            // Why a named provider and not ImplementationConfigurationProviderBase closed over its generics:
            // the name is what lets the seam below point at something. Without it every consumer
            // that wants this domain's configuration has to name the generic closure itself, and
            // the gateway that provider was constructed with shows through at each of those sites.
            builder.Services.AddSingleton<IWebhookNotificationConfigurationProvider, WebhookNotificationConfigurationProvider>(sp => new WebhookNotificationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<WebhookNotificationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                NotificationServiceTypes.ConfigurationConnection, "notify", "WebhookNotificationConfiguration"));

            // Decorate the domain's own AddScoped<INotificationServiceProvider> registration --
            // already in builder.Services by the time this runs, since NotificationServiceTypes'
            // own Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(INotificationServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<INotificationServiceProvider>(sp =>
            {
                var provider = (INotificationServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IWebhookNotificationProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<WebhookNotificationType>() ?? NullLogger<WebhookNotificationType>.Instance,
                        nameof(WebhookNotificationType), Name, nameof(IWebhookNotificationProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // INotificationConfigurationProvider is Singleton, so root's Initialize call reaches the
        // same instance every later resolution sees -- unlike the domain SERVICE provider above,
        // this one is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<INotificationConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IWebhookNotificationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
