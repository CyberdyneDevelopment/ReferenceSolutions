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

namespace FdwReferenceNotifications.Email.Registration;

/// <summary>
/// ServiceType definition for email notification services.
/// Handles three-phase registration lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This type is discovered via the <see cref="ImplementationAttribute"/> and registered
/// with the <see cref="NotificationServiceTypes"/> collection by the source generator.
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
[Implementation(typeof(NotificationServiceTypes), "Email")]
public sealed class EmailNotificationType
    : NotificationTypeBase<INotificationService, IEmailNotificationFactory, EmailNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationType"/> class.
    /// Instance is created by source generator in NotificationServiceTypes collection.
    /// </summary>
    public EmailNotificationType()
        : base(
            name: "Email",
            channelName: "Email",
            displayName: "Email Notifications",
            description: "Send notifications via SMTP email",
            defaultContainerName: "EmailNotificationConfiguration")
    {
        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Factory - DI handles all constructor dependencies
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IEmailNotificationFactory), typeof(EmailNotificationFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IEmailNotificationProvider), typeof(EmailNotificationProvider), ServiceLifetime.Singleton));

            // Why: the gateway is resolved per call, so cfg resolution is deferred to first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so the connection is never hardcoded here.
            // Why a named provider and not ImplementationConfigurationProviderBase closed over its generics:
            // the name is what lets the seam below point at something. Without it every consumer
            // that wants this domain's configuration has to name the generic closure itself, and
            // the gateway that provider was constructed with shows through at each of those sites.
            builder.Services.AddSingleton<IEmailNotificationConfigurationProvider, EmailNotificationConfigurationProvider>(sp => new EmailNotificationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<EmailNotificationConfigurationProvider>(),
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

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IEmailNotificationProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<EmailNotificationType>() ?? NullLogger<EmailNotificationType>.Instance,
                        nameof(EmailNotificationType), Name, nameof(IEmailNotificationProvider), factoryResult.CurrentMessage);
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
                .Register(Name, services.GetRequiredService<IEmailNotificationConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
