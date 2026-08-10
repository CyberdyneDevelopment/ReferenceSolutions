using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Email.Commands;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReferenceNotifications.Email;
using Fdw.Services.Notifications.Email;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceNotifications.Email;

/// <summary>
/// ServiceType definition for email notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ServiceTypeOptionAttribute"/> and registered
/// with the <see cref="NotificationTypes"/> collection by the source generator.
/// </para>
/// <para>
/// Configuration is loaded from "Notifications:{ConfigurationName}" sections in appsettings.json:
/// <code>
/// {
///   "Notifications": {
///     "OrderAlerts": {
///       "NotificationType": "Email",
///       "IsEnabled": true,
///       "SmtpHost": "smtp.example.com",
///       "SmtpPort": 587,
///       "FromAddress": "alerts@example.com"
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(NotificationTypes), "Email")]
public sealed class EmailNotificationType
    : NotificationTypeBase<INotificationService, IEmailNotificationFactory, EmailNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationType"/> class.
    /// Instance is created by source generator in NotificationTypes collection.
    /// </summary>
    public EmailNotificationType()
        : base(
            name: "Email",
            channel: NotificationChannels.ByName("Email"),
            displayName: "Email Notifications",
            description: "Send notifications via SMTP email",
            defaultContainerName: "EmailNotification")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IEmailNotificationFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: Resolve from DI — provider was registered with Lazy<IConfigurationGateway> in the Registration phase body.
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<EmailNotificationConfiguration, EmailNotificationConfigurationCommand>>();
            services.GetRequiredService<NotificationConfigurationProvider>()
                .Register(Name, configProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            builder.Services.Configure<List<NotificationConfiguration>>(builder.Configuration.GetSection("Notifications:Default"));
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Factory - DI handles all constructor dependencies
            builder.Services.AddSingleton<IEmailNotificationFactory, EmailNotificationFactory>();

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // dataStoreName flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<EmailNotificationConfiguration, EmailNotificationConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<EmailNotificationConfiguration, EmailNotificationConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName,
                pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // Why: register the domain header provider (idempotent) it depends on, instead of the
            // entry-point app.
            NotificationConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }


}
