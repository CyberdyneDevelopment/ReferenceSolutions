using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Console.Commands;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReferenceNotifications.Console;
using Fdw.Services.Notifications.Console;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceNotifications.Console;

/// <summary>
/// ServiceType definition for console notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ServiceTypeOptionAttribute"/> and registered
/// with the <see cref="NotificationTypes"/> collection by the source generator.
/// </para>
/// <para>
/// Intended for development and test environments. Logs notification content via
/// structured logging rather than dispatching to real channels.
/// </para>
/// <para>
/// Configuration is loaded from "Notifications:Console:{index}" sections in appsettings.json:
/// <code>
/// {
///   "Notifications": {
///     "Console": [
///       {
///         "Name": "DevNotifications",
///         "NotificationType": "Console",
///         "IsEnabled": true
///       }
///     ]
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(NotificationTypes), "Console")]
public sealed class ConsoleNotificationType
    : NotificationTypeBase<INotificationService, IConsoleNotificationFactory, ConsoleNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleNotificationType"/> class.
    /// Instance is created by source generator in NotificationTypes collection.
    /// </summary>
    public ConsoleNotificationType()
        : base(
            name: "Console",
            channelName: "Console",
            displayName: "Console Notifications",
            description: "Development/test channel that logs notification content via structured logging",
            defaultContainerName: "ConsoleNotification")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>();

            var factory = services.GetRequiredService<IConsoleNotificationFactory>();
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: the typed body attaches to the domain's HEADER provider by discriminator, which is how
            // every other composed-header domain (connections, secret managers, data stores) does it. The
            // old call registered it on the domain provider's own child dictionary instead — a second,
            // parallel mechanism whose only remaining user was this line, and whose widening needed a
            // forwarding adapter to bridge the invariant IServiceConfigurationProvider{T}.
            services.GetRequiredService<NotificationConfigurationProvider>()
                .Register(
                    Name,
                    services.GetRequiredService<DefaultConfigurationProvider<ConsoleNotificationConfiguration, ConsoleNotificationConfigurationCommand>>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            builder.Services.Configure<List<NotificationConfiguration>>(builder.Configuration.GetSection("Notifications:Default"));
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.AddSingleton<IConsoleNotificationFactory, ConsoleNotificationFactory>();

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<ConsoleNotificationConfiguration, ConsoleNotificationConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<ConsoleNotificationConfiguration, ConsoleNotificationConfigurationCommand>>(),
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
