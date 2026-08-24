using System;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceNotifications.Teams.Commands;

namespace ReferenceNotifications.Teams;

/// <summary>Reads the Teams notification configurations out of ConfigurationDb.</summary>
/// <remarks>
/// A named type rather than a bare <c>DefaultConfigurationProvider</c> closed over its generics: the
/// name is what lets DI register it and expose it as
/// <c>IServiceConfigurationProvider&lt;TeamsNotificationConfiguration&gt;</c>, which is the seam
/// consumers inject. The gateway stays behind that seam — a caller asks this provider for
/// configuration, never the gateway for rows.
/// </remarks>
public class TeamsNotificationConfigurationProvider
    : DefaultConfigurationProvider<TeamsNotificationConfiguration, TeamsNotificationConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationConfigurationProvider"/> class.</summary>
    public TeamsNotificationConfigurationProvider(
        ILogger<TeamsNotificationConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "notify",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(
            logger ?? NullLogger<TeamsNotificationConfigurationProvider>.Instance,
            lazyGateway,
            dataStoreName,
            pathName,
            invalidator)
    {
    }
}
