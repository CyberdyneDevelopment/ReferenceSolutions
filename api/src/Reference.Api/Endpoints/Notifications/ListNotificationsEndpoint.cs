using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Endpoints;

namespace Reference.Api.Endpoints.Notifications;

/// <summary>
/// Endpoint to list all notification configurations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListNotificationsEndpoint : ListNotificationsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListNotificationsEndpoint"/> class.
    /// </summary>
    public ListNotificationsEndpoint(IServiceConfigurationProvider<NotificationConfiguration> provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
