using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint: the severities a notification rule may carry (FDW-2931).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationRuleSeveritiesEndpoint : ListNotificationRuleSeveritiesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListNotificationRuleSeveritiesEndpoint"/> class.</summary>
    public ListNotificationRuleSeveritiesEndpoint(ILogger<ListNotificationRuleSeveritiesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
