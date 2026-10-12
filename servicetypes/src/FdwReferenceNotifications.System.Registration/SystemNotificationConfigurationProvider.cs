using Fdw.Services.Configuration;
using Fdw.Services.Notifications.System;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.Notifications.Abstractions;
namespace FdwReferenceNotifications.System.Registration;

/// <summary>Supplies the SystemNotification implementation's own configuration.</summary>
public sealed class SystemNotificationConfigurationProvider
    : ImplementationProviderBase<SystemNotificationConfiguration, INotificationImplementationConfiguration>,
      ISystemNotificationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SystemNotificationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public SystemNotificationConfigurationProvider(
        ILogger<SystemNotificationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
