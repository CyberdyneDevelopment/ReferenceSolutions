using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceNotifications.Teams.Registration;

/// <summary>
/// The Teams notification service type. This is what makes Teams reachable through the
/// notification provider — without it the service exists but nothing can resolve it.
/// </summary>
[Implementation(typeof(NotificationServiceTypes), "Teams")]
public sealed class TeamsNotificationType
    : NotificationTypeBase<INotificationService, ITeamsNotificationFactory, TeamsNotificationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationType"/> class.</summary>
    public TeamsNotificationType()
        : base(
            name: "Teams",
            channelName: "Teams",
            displayName: "Microsoft Teams Notifications",
            description: "Send notifications via Microsoft Teams webhook",
            defaultContainerName: "TeamsNotificationConfiguration")
    {
        Configuration(builder =>
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ITeamsNotificationFactory), typeof(TeamsNotificationFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ITeamsNotificationProvider), typeof(TeamsNotificationProvider), ServiceLifetime.Singleton));

            // Why a named provider and not ImplementationConfigurationProviderBase closed over its generics:
            // the name is what lets the seam below point at something. Without it every consumer
            // that wants this domain's configuration has to name the generic closure itself, and
            // the gateway that provider was constructed with shows through at each of those sites.
            builder.Services.AddSingleton<ITeamsNotificationConfigurationProvider, TeamsNotificationConfigurationProvider>(sp => new TeamsNotificationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<TeamsNotificationConfigurationProvider>(),
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

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<ITeamsNotificationProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<TeamsNotificationType>() ?? NullLogger<TeamsNotificationType>.Instance,
                        nameof(TeamsNotificationType), Name, nameof(ITeamsNotificationProvider), factoryResult.CurrentMessage);
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
                .Register(Name, services.GetRequiredService<ITeamsNotificationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
