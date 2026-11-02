using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to get a notification configuration by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetNotificationEndpoint : GetNotificationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationEndpoint"/> class.
    /// </summary>
    public GetNotificationEndpoint(INotificationConfigurationProvider provider, ILogger<GetNotificationEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
