using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.System.Commands;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReferenceNotifications.System;
using Fdw.Services.Notifications.System;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceNotifications.System;

/// <summary>
/// ServiceType definition for system notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ServiceTypeOptionAttribute"/> and registered
/// with the <see cref="NotificationTypes"/> collection by the source generator.
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
[ServiceTypeOption(typeof(NotificationTypes), "System")]
public sealed class SystemNotificationType
    : NotificationTypeBase<INotificationService, ISystemNotificationFactory, SystemNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemNotificationType"/> class.
    /// Instance is created by source generator in NotificationTypes collection.
    /// </summary>
    public SystemNotificationType()
        : base(
            name: "System",
            channel: NotificationChannels.ByName("System"),
            displayName: "System Notifications",
            description: "In-system message channel that delivers notifications as tracked messages with lifecycle support",
            defaultContainerName: "SystemNotification")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>();

            var factory = services.GetRequiredService<ISystemNotificationFactory>();
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: Resolve from DI — provider was registered with Lazy<IConfigurationGateway> in the Registration phase body.
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<SystemNotificationConfiguration, SystemNotificationConfigurationCommand>>();
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

            builder.Services.AddSingleton<ISystemNotificationFactory, SystemNotificationFactory>();

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<SystemNotificationConfiguration, SystemNotificationConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<SystemNotificationConfiguration, SystemNotificationConfigurationCommand>>(),
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
