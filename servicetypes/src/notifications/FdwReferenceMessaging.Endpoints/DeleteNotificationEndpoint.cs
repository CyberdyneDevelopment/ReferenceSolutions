using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>Deletes a notification by name: <c>DELETE /notifications/{Name}</c>.</summary>
[ExcludeFromCodeCoverage]
public class DeleteNotificationEndpoint : DeleteNotificationEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteNotificationEndpoint"/> class.</summary>
    /// <param name="provider">The notification domain's configuration provider.</param>
    /// <param name="logger">The logger for the endpoint.</param>
    public DeleteNotificationEndpoint(INotificationConfigurationProvider provider, ILogger<DeleteNotificationEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
