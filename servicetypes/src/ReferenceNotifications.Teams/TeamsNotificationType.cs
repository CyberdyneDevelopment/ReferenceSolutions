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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReferenceNotifications.Teams.Commands;

namespace ReferenceNotifications.Teams;

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
                .Register(Name, services.GetRequiredService<DefaultConfigurationProvider<TeamsNotificationConfiguration, TeamsNotificationConfigurationCommand>>());

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
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<TeamsNotificationConfiguration, TeamsNotificationConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<TeamsNotificationConfiguration, TeamsNotificationConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                PathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
