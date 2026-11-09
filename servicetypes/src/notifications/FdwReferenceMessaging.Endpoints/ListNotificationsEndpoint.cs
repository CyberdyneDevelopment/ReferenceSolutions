using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list notification configurations, one page at a time.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationsEndpoint : ListNotificationsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListNotificationsEndpoint"/> class.
    /// </summary>
    public ListNotificationsEndpoint(IDomainRecordProvider<NotificationDomainConfiguration> provider, ILogger<ListNotificationsEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
