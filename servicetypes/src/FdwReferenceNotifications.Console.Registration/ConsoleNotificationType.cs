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

namespace FdwReferenceNotifications.Console.Registration;

/// <summary>
/// ServiceType definition for console notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ImplementationAttribute"/> and registered
/// with the <see cref="NotificationServiceTypes"/> collection by the source generator.
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
[Implementation(typeof(NotificationServiceTypes), "Console")]
public sealed class ConsoleNotificationType
    : NotificationTypeBase<INotificationService, IConsoleNotificationFactory, ConsoleNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleNotificationType"/> class.
    /// Instance is created by source generator in NotificationServiceTypes collection.
    /// </summary>
    public ConsoleNotificationType()
        : base(
            name: "Console",
            channelName: "Console",
            displayName: "Console Notifications",
            description: "Development/test channel that logs notification content via structured logging",
            defaultContainerName: "ConsoleNotificationConfiguration")
    {
        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IConsoleNotificationFactory), typeof(ConsoleNotificationFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IConsoleNotificationProvider), typeof(ConsoleNotificationProvider), ServiceLifetime.Singleton));

            // Why: the gateway is resolved per call, so cfg resolution is deferred to first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so the connection is never hardcoded here.
            // Why a named provider and not ImplementationConfigurationProviderBase closed over its generics:
            // the name is what lets the seam below point at something. Without it every consumer
            // that wants this domain's configuration has to name the generic closure itself, and
            // the gateway that provider was constructed with shows through at each of those sites.
            builder.Services.AddSingleton<IConsoleNotificationConfigurationProvider, ConsoleNotificationConfigurationProvider>(sp => new ConsoleNotificationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConsoleNotificationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                NotificationServiceTypes.ConfigurationConnection, PathName, Container));

            // Decorate the domain's own AddScoped<INotificationServiceProvider> registration --
            // already in builder.Services by the time this runs, since NotificationServiceTypes'
            // own Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(INotificationServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<INotificationServiceProvider>(sp =>
            {
                var provider = (INotificationServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IConsoleNotificationProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<ConsoleNotificationType>() ?? NullLogger<ConsoleNotificationType>.Instance,
                        nameof(ConsoleNotificationType), Name, nameof(IConsoleNotificationProvider), factoryResult.CurrentMessage);
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
                .Register(Name, services.GetRequiredService<IConsoleNotificationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
