using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Teams.Commands;

namespace ReferenceNotifications.Teams.Registration;

/// <summary>
/// The Teams notification service type. This is what makes Teams reachable through the
/// notification provider — without it the service exists but nothing can resolve it.
/// </summary>
[ServiceTypeOption(typeof(NotificationTypes), "Teams")]
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
            defaultContainerName: "TeamsNotification")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>();

            var factoryResult = provider.Register(Name, services.GetRequiredService<ITeamsNotificationFactory>());
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            services.GetRequiredService<NotificationConfigurationProvider>()
                .Register(Name, services.GetRequiredService<TeamsNotificationConfigurationProvider>());

            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {
            builder.Services.Configure<List<NotificationConfiguration>>(builder.Configuration.GetSection("Notifications:Default"));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<ITeamsNotificationFactory, TeamsNotificationFactory>();

            // Why a named provider and not DefaultConfigurationProvider closed over its generics:
            // the name is what lets the seam below point at something. Without it every consumer
            // that wants this domain's configuration has to name the generic closure itself, and
            // the gateway that provider was constructed with shows through at each of those sites.
            builder.Services.TryAddSingleton(sp => new TeamsNotificationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<TeamsNotificationConfigurationProvider>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                PathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // The seam consumers inject. They ask this for configuration; the gateway stays behind it.
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<TeamsNotificationConfiguration>>(
                sp => sp.GetRequiredService<TeamsNotificationConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
