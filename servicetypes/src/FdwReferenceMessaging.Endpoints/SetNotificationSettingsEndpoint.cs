using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint: set the settings of an existing notification of any type (FDW-2931).
/// </summary>
[ExcludeFromCodeCoverage]
public class SetNotificationSettingsEndpoint : SetNotificationSettingsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="SetNotificationSettingsEndpoint"/> class.</summary>
    public SetNotificationSettingsEndpoint(INotificationConfigurationProvider provider, ILogger<SetNotificationSettingsEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
