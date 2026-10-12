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
using Fdw.Services.Notifications.System;
using Fdw.Results;

namespace FdwReferenceNotifications.System.Registration;

/// <summary>
/// ServiceType definition for system notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ImplementationAttribute"/> and registered
/// with the <see cref="NotificationServiceTypes"/> collection by the source generator.
/// </para>
/// <para>
/// System notifications deliver content as in-app messages via the messaging framework,
/// providing lifecycle tracking (delivered, read, dismissed, archived).
/// </para>
/// <para>
/// Configuration is loaded from "Notifications:System:{index}" sections in appsettings.json:
/// <code>
/// {
///   "Notifications": {
///     "System": [
///       {
///         "Name": "InAppNotifications",
///         "NotificationType": "System",
///         "IsEnabled": true,
///         "DefaultSeverity": "Info",
///         "DefaultMessageType": "Notification"
///       }
///     ]
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[Implementation(typeof(NotificationServiceTypes), "System")]
public sealed class SystemNotificationType
    : NotificationTypeBase<INotificationService, ISystemNotificationFactory, SystemNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemNotificationType"/> class.
    /// Instance is created by source generator in NotificationServiceTypes collection.
    /// </summary>
    public SystemNotificationType()
        : base(
            name: "System",
            channelName: "System",
            displayName: "System Notifications",
            description: "In-system message channel that delivers notifications as tracked messages with lifecycle support",
            defaultContainerName: "SystemNotificationConfiguration")
    {
        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISystemNotificationFactory), typeof(SystemNotificationFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISystemNotificationProvider), typeof(SystemNotificationProvider), ServiceLifetime.Singleton));

            // Why: the gateway is resolved per call, so cfg resolution is deferred to first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so the connection is never hardcoded here.
            builder.Services.AddSingleton<ISystemNotificationConfigurationProvider, SystemNotificationConfigurationProvider>(sp => new SystemNotificationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SystemNotificationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                NotificationServiceTypes.ConfigurationConnection, "notify", "SystemNotificationConfiguration"));

            // Decorate the domain's own AddScoped<INotificationServiceProvider> registration --
            // already in builder.Services by the time this runs, since NotificationServiceTypes'
            // own Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(INotificationServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<INotificationServiceProvider>(sp =>
            {
                var provider = (INotificationServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<ISystemNotificationProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<SystemNotificationType>() ?? NullLogger<SystemNotificationType>.Instance,
                        nameof(SystemNotificationType), Name, nameof(ISystemNotificationProvider), factoryResult.CurrentMessage);
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
                .Register(Name, services.GetRequiredService<ISystemNotificationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
